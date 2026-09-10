using BelowTheWing.Vehicles;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// Which train a vehicle belongs to, and where in it.
    ///
    /// The only thing about a vehicle that has to travel over the network. What it *is* comes from
    /// the prefab it was made from, and what it looks like comes from the same place; neither can
    /// arrive late or arrive wrong. Which train it is part of genuinely cannot be worked out
    /// locally, because it is a decision the machine that laid out the apron made.
    ///
    /// A machine that does not know a tractor is towing four carts will ask for the tractor alone
    /// when a player gets in, take it, and drive off leaving the carts behind in somebody else's
    /// hands. That is why this is replicated and not inferred from where things happen to be parked.
    /// </summary>
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

        /// <summary>The vehicle this speaks for.</summary>
        public VehicleController Vehicle { get; private set; }

        /// <summary>How this vehicle's place on the apron is described to every other machine.</summary>
        public TrainMembership Membership => new TrainMembership(Vehicle, m_TrainIndex.Value, m_PlaceInTrain.Value);

        /// <summary>
        /// Says where this vehicle sits in its train. Called by whichever machine placed it, before
        /// it is spawned, so that the values travel with the spawn rather than arriving afterwards.
        /// </summary>
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
                Debug.LogError(
                    $"'{name}' was spawned with no {nameof(RampSession)} in the scene. Nothing will " +
                    "work out which train it belongs to, so it will behave as a vehicle standing alone.",
                    this);
                return;
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

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            // Which machine holds a train decides which machine holds its couplings, so a change of
            // hands has to be noticed above the netcode layer as well as inside it.
            m_Session?.OwnershipMoved();
        }

        void OnPlaceChanged(int previous, int current) => m_Session?.MembershipChanged();
    }
}
