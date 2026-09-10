using BelowTheWing.Vehicles;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// Tells every machine whether somebody is sitting in this vehicle, and refuses to hand it over
    /// while they are.
    ///
    /// Both halves are needed and neither works alone.
    ///
    /// Without the first, an occupied tractor looks free to everyone except its driver, because
    /// "somebody is driving this" was only ever recorded on the driver's own machine. Everybody
    /// else walks up and is cheerfully offered it.
    ///
    /// Without the second, being offered it is enough. Netcode approves an ownership request unless
    /// something says otherwise, so a second player pressing E takes a tractor out from under the
    /// person driving it, whose character is left parented inside a vehicle they no longer own.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(VehicleController))]
    public sealed class VehicleOccupant : NetworkBehaviour
    {
        /// <summary>Stands for "nobody", since client ids start at zero and zero is a real client.</summary>
        const ulong Nobody = ulong.MaxValue;

        readonly NetworkVariable<ulong> m_Driver = new NetworkVariable<ulong>(
            Nobody, writePerm: NetworkVariableWritePermission.Owner);

        VehicleController m_Vehicle;

        void Awake() => m_Vehicle = GetComponent<VehicleController>();

        public override void OnNetworkSpawn()
        {
            m_Driver.OnValueChanged += OnDriverChanged;
            m_Vehicle.OccupiedChanged += OnSomebodyGotInOrOut;

            ApplyToVehicle();
            NetworkObject.OnOwnershipRequested = _ => !m_Vehicle.Occupied;
        }

        public override void OnNetworkDespawn()
        {
            m_Driver.OnValueChanged -= OnDriverChanged;
            m_Vehicle.OccupiedChanged -= OnSomebodyGotInOrOut;
            NetworkObject.OnOwnershipRequested = null;
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            // The approval callback only runs on the machine that owns the object, so it has to be
            // in place wherever the vehicle has just arrived.
            NetworkObject.OnOwnershipRequested = _ => !m_Vehicle.Occupied;

            if (!IsOwner)
            {
                return;
            }

            // Only the owner may say who is in a seat, so a driver who lost the vehicle -- or left
            // the session -- can never record that they got out. Whoever picks it up says so
            // instead. Without this the tractor stays occupied on every machine for ever: never
            // offered to anybody, and refusing every request for it.
            if (!m_Vehicle.Occupied)
            {
                m_Driver.Value = Nobody;
            }
        }

        void OnSomebodyGotInOrOut(bool occupied)
        {
            // Only the machine simulating this vehicle may say who is in it, and that is always the
            // machine whose player just got in or out of it.
            if (!IsOwner)
            {
                return;
            }

            m_Driver.Value = occupied ? NetworkManager.LocalClientId : Nobody;
        }

        void OnDriverChanged(ulong previous, ulong current) => ApplyToVehicle();

        void ApplyToVehicle() => m_Vehicle.Occupied = m_Driver.Value != Nobody;
    }
}
