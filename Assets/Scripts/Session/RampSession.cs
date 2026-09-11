using System.Collections.Generic;
using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Diagnostics;
using BelowTheWing.Net;
using BelowTheWing.Vehicles;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// The one place that knows how the pieces of this game fit together.
    ///
    /// Everything else is deliberately ignorant of everything else: vehicles know nothing about
    /// networking, the apron layout knows nothing about crew, the readout knows nothing about which
    /// networking library is underneath. Something has to hold those facts, and holding them in one
    /// named place is what lets the rest stay separate.
    ///
    /// Its jobs are to build the apron once per session, keep the register of which vehicles form
    /// which trains up to date as objects arrive, and decide which of those trains this machine is
    /// responsible for simulating.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RampSession : NetworkBehaviour
    {
        [SerializeField, Tooltip("How much of what goes where.")]
        ApronLayoutSettings m_Layout = ApronLayoutSettings.Default;

        [SerializeField, Tooltip("How couplings between vehicles are set up.")]
        ChainJointSettings m_Coupling = ChainJointSettings.Default;

        [Header("Equipment")]
        [SerializeField] VehicleProfile m_TractorProfile;
        [SerializeField] VehicleProfile m_CartProfile;
        [SerializeField] AircraftProfile m_AircraftProfile;
        [SerializeField] CrewProfile m_CrewProfile;

        [Header("Prefabs")]
        [SerializeField, Tooltip("A baggage tractor, carrying its own profile.")]
        NetworkObject m_TractorPrefab;

        [SerializeField, Tooltip("A baggage cart, carrying its own profile.")]
        NetworkObject m_CartPrefab;

        [SerializeField, Tooltip("The aircraft.")]
        NetworkObject m_AircraftPrefab;

        [SerializeField, Tooltip("A ramp worker.")]
        NetworkObject m_CrewPrefab;

        [Header("Wiring")]
        [SerializeField] NetworkOwnershipBroker m_Broker;
        [SerializeField] FollowCamera m_Camera;
        [SerializeField] RampReadout m_Readout;

        readonly List<TrainMember> m_Vehicles = new List<TrainMember>();
        readonly List<TrainMembership> m_Described = new List<TrainMembership>();

        /// <summary>
        /// Reused rather than rebuilt, because it is handed to the local player's seat on every
        /// fixed step and allocating a fresh list fifty times a second is a waste.
        /// </summary>
        readonly List<IDriveable> m_Driveable = new List<IDriveable>();

        TrainRegistry m_Trains;
        LocalPlayerRig m_LocalPlayer;

        /// <summary>Every train on the apron, as this machine understands it.</summary>
        public IReadOnlyList<CartChain> Trains => m_Trains.Trains;

        /// <summary>Everything a player might be offered a chance to drive.</summary>
        public IReadOnlyList<IDriveable> Driveable() => m_Driveable;

        void Awake() => m_Trains = new TrainRegistry(m_Coupling);

        public override void OnNetworkSpawn()
        {
            if (m_Broker == null)
            {
                Debug.LogError(
                    $"{nameof(RampSession)} has no ownership broker. Nothing would decide which machine " +
                    "holds which train, so every copy of every vehicle would try to simulate itself and " +
                    "fight what arrives over the network.", this);
                enabled = false;
                return;
            }

            if (NetworkManager.LocalClient.IsSessionOwner)
            {
                ApronBuilder.Build(m_Layout, m_TractorProfile, m_CartProfile, m_AircraftProfile, m_CrewProfile,
                    m_TractorPrefab, m_CartPrefab, m_AircraftPrefab);
            }

            // Nothing redistributes vehicles automatically any more, which is deliberate: doing it
            // one object at a time is what split trains across machines. The cost is that a train
            // belonging to somebody who leaves is nobody's until this picks it up.
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

        /// <summary>
        /// Takes back any train the departing player was holding, whole.
        ///
        /// Whole is the point. Reclaiming the members one at a time would leave the train exactly as
        /// split as letting the netcode layer redistribute it, which is the thing this game goes to
        /// some trouble to prevent.
        /// </summary>
        void OnSomebodyCameOrWent(NetworkManager manager, ConnectionEventData what)
        {
            // Under distributed authority nobody is a server, so the disconnect callback only ever
            // fires for this machine's own disconnection. Another player leaving arrives as a peer
            // event, which is the one that matters here.
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
                train.RequestOwnership(m_Broker, _ => { });
            }
        }

        /// <summary>A vehicle has appeared, from wherever. Work the trains out again.</summary>
        public void Arrived(TrainMember member)
        {
            if (!m_Vehicles.Contains(member))
            {
                m_Vehicles.Add(member);
            }

            MembershipChanged();
        }

        /// <summary>A vehicle has gone away.</summary>
        public void Left(TrainMember member)
        {
            if (m_Vehicles.Remove(member))
            {
                MembershipChanged();
            }
        }

        /// <summary>Something about which vehicle belongs to which train has changed.</summary>
        public void MembershipChanged()
        {
            m_Described.Clear();
            m_Driveable.Clear();

            foreach (var member in m_Vehicles)
            {
                if (member == null || member.Vehicle == null)
                {
                    continue;
                }

                m_Described.Add(member.Membership);
                m_Driveable.Add(member.Vehicle);
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
                m_Layout, m_TractorProfile, m_CartProfile, m_AircraftProfile, CrewSize());

            // The first arrival point with nobody standing on it.
            //
            // Neither a client id nor a position in the roster works: ids climb forever, and roster
            // positions shift when somebody leaves, so both eventually put two people in the same
            // place. Two capsules starting inside one another do not settle, they fire apart.
            var mine = FirstFreeArrival(plan.CrewSpawnPoints);

            var crew = Instantiate(m_CrewPrefab, mine.Position, mine.Rotation);
            crew.GetComponent<ApronIdentity>().Called($"Player {NetworkManager.LocalClientId}");
            crew.Spawn();

            m_LocalPlayer = new LocalPlayerRig(
                crew.GetComponent<CrewCharacter>(), m_Camera, m_Broker, Driveable);
        }

        /// <summary>
        /// An arrival point with nobody already standing on it, or the first one if the apron is
        /// somehow full.
        /// </summary>
        static Placement FirstFreeArrival(IReadOnlyList<Placement> arrivals)
        {
            foreach (var arrival in arrivals)
            {
                if (!Physics.CheckBox(arrival.Position, arrival.SizeMetres * 0.5f, arrival.Rotation))
                {
                    return arrival;
                }
            }

            return arrivals[0];
        }

        Vector3 CrewSize()
            => new Vector3(m_CrewProfile.radiusMetres * 2f, m_CrewProfile.heightMetres, m_CrewProfile.radiusMetres * 2f);

        void LateUpdate() => m_LocalPlayer?.FollowWhateverTheyAreControlling();
    }
}
