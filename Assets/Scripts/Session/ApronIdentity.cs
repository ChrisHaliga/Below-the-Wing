using BelowTheWing.Apron;
using BelowTheWing.Vehicles;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(ApronAppearance))]
    public sealed class ApronIdentity : NetworkBehaviour
    {
        readonly NetworkVariable<FixedString64Bytes> m_Name = new NetworkVariable<FixedString64Bytes>(
            writePerm: NetworkVariableWritePermission.Owner);

        public string DisplayName => m_Name.Value.ToString();

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

            var vehicle = GetComponent<VehicleController>();
            if (vehicle != null)
            {
                vehicle.Rename(called);
            }
        }
    }
}
