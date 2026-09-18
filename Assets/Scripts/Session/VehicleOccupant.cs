using BelowTheWing.Net;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(VehicleController))]
    public sealed class VehicleOccupant : NetworkBehaviour
    {
        readonly NetworkVariable<ulong> m_Driver = new NetworkVariable<ulong>(
            Shift.Nobody, writePerm: NetworkVariableWritePermission.Owner);

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
            NetworkObject.OnOwnershipRequested = _ => !m_Vehicle.Occupied;

            if (!IsOwner)
            {
                return;
            }

            if (m_Driver.Value != NetworkManager.LocalClientId)
            {
                m_Driver.Value = Shift.Nobody;
            }
        }

        void OnSomebodyGotInOrOut(bool occupied)
        {
            if (!IsOwner)
            {
                return;
            }

            m_Driver.Value = occupied ? NetworkManager.LocalClientId : Shift.Nobody;
        }

        void OnDriverChanged(ulong previous, ulong current) => ApplyToVehicle();

        void ApplyToVehicle() => m_Vehicle.Occupied = m_Driver.Value != Shift.Nobody;
    }
}
