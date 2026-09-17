using System.Collections.Generic;
using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Diagnostics;
using BelowTheWing.Net;
using BelowTheWing.Vehicles;
using Unity.Netcode;
using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Session
{
    [DisallowMultipleComponent]
    public sealed class RampSession : NetworkBehaviour
    {
        [SerializeField, Tooltip("Where everything stands")]
        ApronLayoutSettings m_Layout = ApronLayoutSettings.Default;

        [SerializeField, Tooltip("How couplings are set up")]
        ChainJointSettings m_Coupling = ChainJointSettings.Default;

        [Header("Equipment")]
        [SerializeField] AircraftProfile m_AircraftProfile;
        [SerializeField] CrewProfile m_CrewProfile;

        [Header("Prefabs")]
        [SerializeField, Tooltip("Tractor prefab")]
        NetworkObject m_TractorPrefab;

        [SerializeField, Tooltip("Cart prefab")]
        NetworkObject m_CartPrefab;

        [SerializeField, Tooltip("Aircraft prefab")]
        NetworkObject m_AircraftPrefab;

        [SerializeField, Tooltip("Crew prefab")]
        NetworkObject m_CrewPrefab;

        [SerializeField, Tooltip("Bag prefab")]
        NetworkObject m_BagPrefab;

        [SerializeField, Tooltip("Bags spawned per train")]
        int m_BagsPerTrain = 4;

        [Header("Wiring")]
        [SerializeField] NetworkOwnershipBroker m_Broker;
        [SerializeField] FollowCamera m_Camera;
        [SerializeField] RampReadout m_Readout;

        readonly List<TrainMember> m_Vehicles = new List<TrainMember>();
        readonly List<TrainMembership> m_Described = new List<TrainMembership>();

        readonly List<VehicleController> m_OnTheApron = new List<VehicleController>();

        TrainRegistry m_Trains;
        readonly Reclaiming m_Reclaiming = new Reclaiming();
        LocalPlayerRig m_LocalPlayer;

        public IReadOnlyList<CartChain> Trains => m_Trains.Trains;

        public IReadOnlyList<VehicleController> Vehicles => m_OnTheApron;

        void Awake() => m_Trains = new TrainRegistry(m_Coupling);

        public override void OnNetworkSpawn()
        {
            if (m_Broker == null)
            {
                throw MisbuiltException.Refuse(
                    this,
                    "has no ownership broker, so nothing would decide which machine holds which train");
            }

            if (NetworkManager.LocalClient.IsSessionOwner)
            {
                ApronBuilder.Build(m_Layout, m_AircraftProfile, m_CrewProfile,
                    m_TractorPrefab, m_CartPrefab, m_AircraftPrefab, m_BagPrefab, m_BagsPerTrain);
            }

            NetworkManager.OnConnectionEvent += OnSomebodyCameOrWent;

            SpawnOwnCrew();
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager != null)
            {
                NetworkManager.OnConnectionEvent -= OnSomebodyCameOrWent;
            }
        }

        void OnSomebodyCameOrWent(NetworkManager manager, ConnectionEventData what)
        {
            if (what.EventType == ConnectionEvent.PeerDisconnected || what.EventType == ConnectionEvent.ClientDisconnected)
            {
                ReclaimWhatTheyLeftBehind(what.ClientId);
            }
        }

        void ReclaimWhatTheyLeftBehind(ulong departed)
        {
            if (!NetworkManager.LocalClient.IsSessionOwner)
            {
                return;
            }

            foreach (var train in m_Trains.TrainsHeldBy(departed, m_Broker))
            {
                m_Reclaiming.TakeBack(train);
            }
        }

        public void Reshaped(IReadOnlyList<VehicleController> train, int trainIndex)
        {
            for (var place = 0; place < train.Count; place++)
            {
                var member = train[place].GetComponent<TrainMember>();
                if (member != null)
                {
                    member.Joins(trainIndex, place);
                }
            }

            MembershipChanged();
        }

        public int TrainIndexOf(IReadOnlyList<VehicleController> train)
        {
            var member = train[0].GetComponent<TrainMember>();
            return member != null ? member.Membership.TrainIndex : TrainMembership.NoTrain;
        }

        public int ATrainNumberNobodyIsUsing()
        {
            var highest = TrainMembership.NoTrain;

            foreach (var member in m_Vehicles)
            {
                if (member != null)
                {
                    highest = Mathf.Max(highest, member.Membership.TrainIndex);
                }
            }

            return highest + 1;
        }

        public void Arrived(TrainMember member)
        {
            if (!m_Vehicles.Contains(member))
            {
                m_Vehicles.Add(member);
            }

            MembershipChanged();
        }

        public void Left(TrainMember member)
        {
            if (m_Vehicles.Remove(member))
            {
                MembershipChanged();
            }
        }

        public void MembershipChanged()
        {
            m_Described.Clear();
            m_OnTheApron.Clear();

            foreach (var member in m_Vehicles)
            {
                if (member == null || member.Vehicle == null)
                {
                    continue;
                }

                m_Described.Add(member.Membership);
                m_OnTheApron.Add(member.Vehicle);
            }

            m_Trains.Rebuild(m_Described);

            if (m_Readout != null)
            {
                m_Readout.Observe(m_Trains.Trains, m_Broker);
            }
        }

        void SpawnOwnCrew()
        {
            var plan = ApronLayout.Build(
                m_Layout,
                m_TractorPrefab.GetComponent<VehicleShape>().Footprint,
                m_CartPrefab.GetComponent<VehicleShape>().Footprint,
                m_AircraftProfile,
                CrewSize());

            var mine = FirstFreeArrival(plan.CrewSpawnPoints);

            var crew = Instantiate(m_CrewPrefab, mine.Position, mine.Rotation);
            crew.GetComponent<ApronIdentity>().Called($"Player {NetworkManager.LocalClientId}");
            crew.Spawn();

            m_LocalPlayer = new LocalPlayerRig(
                crew.GetComponent<CrewCharacter>(), m_Camera, m_Broker, () => Vehicles, this);
        }

        static Placement FirstFreeArrival(IReadOnlyList<Placement> arrivals)
        {
            foreach (var arrival in arrivals)
            {
                if (!Physics.CheckBox(
                        arrival.Position, arrival.SizeMetres * 0.5f, arrival.Rotation, ~0,
                        QueryTriggerInteraction.Ignore))
                {
                    return arrival;
                }
            }

            return arrivals[0];
        }

        Vector3 CrewSize()
            => new Vector3(m_CrewProfile.radiusMetres * 2f, m_CrewProfile.heightMetres, m_CrewProfile.radiusMetres * 2f);

        void FixedUpdate()
        {
            if (m_Broker != null)
            {
                m_Reclaiming.Chase(m_Broker, Time.fixedDeltaTime);
            }
        }

        void LateUpdate() => m_LocalPlayer?.FollowWhateverTheyAreControlling();
    }
}
