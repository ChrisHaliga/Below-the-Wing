using BelowTheWing.Vehicles;
using Unity.Netcode;
using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Session
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(VehicleController))]
    public sealed class TrainMember : NetworkBehaviour
    {
        readonly NetworkVariable<int> m_TrainIndex = new NetworkVariable<int>(
            TrainMembership.NoTrain, writePerm: NetworkVariableWritePermission.Owner);

        readonly NetworkVariable<int> m_PlaceInTrain = new NetworkVariable<int>(
            writePerm: NetworkVariableWritePermission.Owner);

        RampSession m_Session;

        public VehicleController Vehicle { get; private set; }

        public TrainMembership Membership => new TrainMembership(Vehicle, m_TrainIndex.Value, m_PlaceInTrain.Value);

        public void Joins(int trainIndex, int placeInTrain)
        {
            m_TrainIndex.Value = trainIndex;
            m_PlaceInTrain.Value = placeInTrain;
        }

        void Awake() => Vehicle = GetComponent<VehicleController>();

        public override void OnNetworkSpawn()
        {
            m_Session = FindAnyObjectByType<RampSession>();

            if (m_Session == null)
            {
                throw MisbuiltException.Refuse(
                    this,
                    $"was spawned with no {nameof(RampSession)} in the scene to work out which train it belongs to");
            }

            m_TrainIndex.OnValueChanged += OnPlaceChanged;
            m_PlaceInTrain.OnValueChanged += OnPlaceChanged;
            m_Session.Arrived(this);
        }

        public override void OnNetworkDespawn()
        {
            m_TrainIndex.OnValueChanged -= OnPlaceChanged;
            m_PlaceInTrain.OnValueChanged -= OnPlaceChanged;
            m_Session?.Left(this);
        }

        void OnPlaceChanged(int previous, int current) => m_Session?.MembershipChanged();
    }
}
