using BelowTheWing.Apron;
using BelowTheWing.Vehicles;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// What a thing on the apron is called, told to every machine.
    ///
    /// Names are the only thing distinguishing one grey box from another, and appearance is built
    /// from the name arriving: no name, no shape and no label. So this has to reach every machine,
    /// not just the one that placed the object -- otherwise a player who joins looks at an empty
    /// grey plane and walks into colliders they cannot see.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(ApronAppearance))]
    public sealed class ApronIdentity : NetworkBehaviour
    {
        readonly NetworkVariable<FixedString64Bytes> m_Name = new NetworkVariable<FixedString64Bytes>(
            writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>What this is called, such as "Tug 1" or "Cart 2-3".</summary>
        public string DisplayName => m_Name.Value.ToString();

        /// <summary>
        /// Names this object. Called before it is spawned, so the name travels with the spawn
        /// rather than arriving afterwards.
        /// </summary>
        public void Called(string displayName) => m_Name.Value = displayName;

        public override void OnNetworkSpawn()
        {
            m_Name.OnValueChanged += OnRenamed;
            ShowIfNamed();
        }

        public override void OnNetworkDespawn() => m_Name.OnValueChanged -= OnRenamed;

        void OnRenamed(FixedString64Bytes previous, FixedString64Bytes current) => ShowIfNamed();

        void ShowIfNamed()
        {
            var called = DisplayName;
            if (string.IsNullOrEmpty(called))
            {
                return;
            }

            GetComponent<ApronAppearance>().Show(called);

            // The prompt to drive a vehicle reads the vehicle's own name, which would otherwise be
            // whatever Unity called the clone -- "BaggageTractor(Clone)".
            var vehicle = GetComponent<VehicleController>();
            if (vehicle != null)
            {
                vehicle.Rename(called);
            }
        }
    }
}
