using System.Collections.Generic;
using BelowTheWing.Crew;
using BelowTheWing.Diagnostics;
using BelowTheWing.Net;
using BelowTheWing.Vehicles;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Apron
{
    /// <summary>
    /// Builds the apron once a session exists, and looks after everything standing on it.
    ///
    /// The aircraft, tractors and carts are put there by exactly one machine -- the session owner --
    /// so that everybody is looking at the same apron rather than each building their own. Crew are
    /// the exception: a player spawns their own, because the character a player controls should be
    /// simulated on the machine of the person controlling it and nowhere else.
    ///
    /// After that, every machine does the same work. Each one configures the objects it receives
    /// from their profiles, works out from the replicated description which vehicles form which
    /// train, and decides which of those trains it is responsible for simulating. A machine that
    /// skipped this would believe every tractor was standing on its own, and taking one over would
    /// leave its carts behind in somebody else's hands.
    ///
    /// This is also where the pieces are wired to each other. It is the one place that knows about
    /// vehicles, crew, ownership and the readout at once, which is what keeps any of those from
    /// having to know about the others.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RampSpawner : NetworkBehaviour
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
        [SerializeField, Tooltip("Networked prefab used for every vehicle. Its profile decides what it is.")]
        NetworkObject m_VehiclePrefab;

        [SerializeField, Tooltip("Networked prefab for a crew member.")]
        NetworkObject m_CrewPrefab;

        [SerializeField, Tooltip("Networked prefab for the aircraft.")]
        NetworkObject m_AircraftPrefab;

        [Header("Wiring")]
        [SerializeField] NetworkOwnershipBroker m_Broker;
        [SerializeField] FollowCamera m_Camera;
        [SerializeField] RampReadout m_Readout;

        readonly List<RampObject> m_OnTheApron = new List<RampObject>();
        readonly List<VehicleController> m_Vehicles = new List<VehicleController>();
        readonly List<CartChain> m_Trains = new List<CartChain>();

        /// <summary>
        /// Reused rather than rebuilt, because it is handed to every crew member's seat on every
        /// fixed step and allocating a fresh list fifty times a second per player is a waste.
        /// </summary>
        readonly List<IDriveable> m_Driveable = new List<IDriveable>();

        CrewCharacter m_LocalCrew;

        /// <summary>Every vehicle standing on the apron, as this machine knows it.</summary>
        public IReadOnlyList<VehicleController> Vehicles => m_Vehicles;

        /// <summary>Every train on the apron, one per tractor.</summary>
        public IReadOnlyList<CartChain> Trains => m_Trains;

        /// <summary>The profile a given kind of vehicle runs on.</summary>
        public VehicleProfile ProfileFor(RampObject.Kind kind) => kind switch
        {
            RampObject.Kind.Cart => m_CartProfile,
            _ => m_TractorProfile
        };

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.LocalClient.IsSessionOwner)
            {
                BuildApron();
            }

            SpawnOwnCrew();
        }

        /// <summary>
        /// Takes charge of an object that has just appeared, wherever it came from: configures it
        /// from its profile, gives it its appearance, and works the trains out again now that the
        /// apron has changed.
        /// </summary>
        public void Adopt(RampObject arrival)
        {
            if (!m_OnTheApron.Contains(arrival))
            {
                m_OnTheApron.Add(arrival);
            }

            Configure(arrival);
            arrival.Dress();
            RebuildTrains();
        }

        /// <summary>Forgets an object that has gone away.</summary>
        public void Abandon(RampObject departed)
        {
            if (m_OnTheApron.Remove(departed))
            {
                RebuildTrains();
            }
        }

        /// <summary>A vehicle has changed hands, so which machine holds which couplings has too.</summary>
        public void OwnershipMoved() => TakeUpTheTrainsThisMachineOwns();

        /// <summary>
        /// Everything a crew member might be offered a chance to drive.
        ///
        /// Handed to every seat as a callback rather than a snapshot, because vehicles arrive over
        /// the first few moments of a session and a list captured at the start would never include
        /// them.
        /// </summary>
        public IReadOnlyList<IDriveable> Driveable() => m_Driveable;

        void BuildApron()
        {
            var plan = ApronLayout.Build(m_Layout, m_TractorProfile, m_CartProfile, m_AircraftProfile);

            Place(m_AircraftPrefab, plan.Aircraft, RampObject.Kind.Aircraft);

            for (var t = 0; t < plan.Trains.Count; t++)
            {
                var train = plan.Trains[t];
                Place(m_VehiclePrefab, train.Tractor, RampObject.Kind.Tractor, t, placeInTrain: 0);

                for (var c = 0; c < train.Carts.Count; c++)
                {
                    Place(m_VehiclePrefab, train.Carts[c], RampObject.Kind.Cart, t, placeInTrain: c + 1);
                }
            }
        }

        NetworkObject Place(
            NetworkObject prefab,
            Placement plan,
            RampObject.Kind kind,
            int trainIndex = RampObject.NoTrain,
            int placeInTrain = 0)
        {
            var spawned = Instantiate(prefab, plan.Position, plan.Rotation);
            spawned.Spawn();

            // Anyone may take a tractor or a cart, so their ownership has to be able to move. The
            // request is what gives the machine that currently holds one the chance to say no.
            spawned.SetOwnershipStatus(
                NetworkObject.OwnershipStatus.Distributable | NetworkObject.OwnershipStatus.RequestRequired);

            spawned.GetComponent<RampObject>().Describe(kind, plan.Name, trainIndex, placeInTrain);
            return spawned;
        }

        void SpawnOwnCrew()
        {
            var standingAt = new Vector3(
                m_Layout.firstTractorPosition.x + 4f, 1.2f, m_Layout.firstTractorPosition.z);

            var crew = Instantiate(m_CrewPrefab, standingAt, Quaternion.identity);
            crew.Spawn();
            crew.GetComponent<RampObject>().Describe(RampObject.Kind.Crew, $"Player {NetworkManager.LocalClientId}");
        }

        /// <summary>
        /// Gives an object what it needs to behave as the thing it says it is. Runs on every
        /// machine, for every object, including copies of things somebody else is simulating --
        /// a replicated crew member with no profile has the prefab's mass and collider, and gets
        /// launched across the apron by the first thing that touches it.
        /// </summary>
        void Configure(RampObject arrival)
        {
            switch (arrival.EquipmentKind)
            {
                case RampObject.Kind.Crew:
                    var character = arrival.GetComponent<CrewCharacter>();
                    if (character != null && character.Profile == null)
                    {
                        character.Configure(m_CrewProfile, m_Broker, Driveable);
                        AdoptAsOwnCrewIfMine(arrival, character);
                    }

                    break;

                case RampObject.Kind.Aircraft:
                    break;

                default:
                    var vehicle = arrival.GetComponent<VehicleController>();
                    if (vehicle != null && vehicle.Profile == null)
                    {
                        vehicle.Configure(ProfileFor(arrival.EquipmentKind), arrival.DisplayName);
                    }

                    break;
            }
        }

        void AdoptAsOwnCrewIfMine(RampObject arrival, CrewCharacter character)
        {
            if (!arrival.IsOwner)
            {
                return;
            }

            m_LocalCrew = character;
            character.Camera = m_Camera;

            // Only this player's own character reads this machine's keyboard. Everybody else's
            // arrives as replicated movement.
            character.gameObject.AddComponent<LocalCrewInput>();

            if (m_Camera != null)
            {
                m_Camera.Subject = character.transform;
            }
        }

        /// <summary>
        /// Works out afresh which vehicles form which train, from what every machine was told about
        /// them, and hands the result to the readout.
        /// </summary>
        void RebuildTrains()
        {
            // Any couplings currently in place belong to chains about to be replaced, and a joint
            // whose chain has been forgotten is a joint nothing will ever destroy.
            foreach (var train in m_Trains)
            {
                train.ReleaseCouplings();
            }

            m_Trains.Clear();
            m_Vehicles.Clear();
            m_Driveable.Clear();

            var byTrain = new SortedDictionary<int, SortedList<int, VehicleController>>();
            var loners = new List<VehicleController>();

            foreach (var member in m_OnTheApron)
            {
                var vehicle = member != null ? member.GetComponent<VehicleController>() : null;
                if (vehicle == null || vehicle.Profile == null)
                {
                    continue;
                }

                m_Vehicles.Add(vehicle);
                m_Driveable.Add(vehicle);

                if (member.TrainIndex == RampObject.NoTrain)
                {
                    loners.Add(vehicle);
                    continue;
                }

                if (!byTrain.TryGetValue(member.TrainIndex, out var places))
                {
                    places = new SortedList<int, VehicleController>();
                    byTrain[member.TrainIndex] = places;
                }

                places[member.PlaceInTrain] = vehicle;
            }

            foreach (var places in byTrain.Values)
            {
                m_Trains.Add(CartChain.Couple(new List<VehicleController>(places.Values), m_Coupling));
            }

            foreach (var lone in loners)
            {
                m_Trains.Add(CartChain.Couple(new[] { lone }, m_Coupling));
            }

            if (m_Readout != null)
            {
                m_Readout.Observe(m_Trains, m_Broker);
            }

            TakeUpTheTrainsThisMachineOwns();
        }

        /// <summary>
        /// Puts couplings on the trains this machine is simulating and takes them off the rest.
        ///
        /// A hinge between two bodies being integrated by different machines has half its
        /// constraint solver working against something it cannot move, so only the machine that
        /// owns a whole train holds it physically together. Everywhere else its members are copies
        /// following their owner, and joints between them would only fight that.
        /// </summary>
        void TakeUpTheTrainsThisMachineOwns()
        {
            if (m_Broker == null)
            {
                return;
            }

            foreach (var train in m_Trains)
            {
                var ours = EveryMemberIsOurs(train);

                if (ours)
                {
                    train.EngageCouplings();
                }
                else
                {
                    train.ReleaseCouplings();
                }

                foreach (var member in train.Members)
                {
                    member.Simulated = ours;
                }
            }
        }

        bool EveryMemberIsOurs(CartChain train)
        {
            foreach (var member in train.Members)
            {
                if (m_Broker.OwnerOf(member) != m_Broker.LocalClientId)
                {
                    return false;
                }
            }

            return true;
        }

        void LateUpdate()
        {
            // The camera follows whatever the local player is in charge of, which changes the
            // moment they climb into a tractor or step back out of one.
            if (m_LocalCrew != null && m_LocalCrew.Seat != null && m_Camera != null)
            {
                m_Camera.Subject = m_LocalCrew.Seat.Subject;
            }
        }
    }
}
