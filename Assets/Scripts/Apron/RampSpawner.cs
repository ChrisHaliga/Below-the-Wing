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
    /// Builds the apron once a session exists, and adds a crew member for each player who arrives.
    ///
    /// The aircraft, tractors and carts are put there by exactly one machine -- the session owner --
    /// so that everybody is looking at the same apron rather than each building their own. Crew are
    /// the exception: a player spawns their own, because the character a player controls should be
    /// simulated on the machine of the person controlling it and nowhere else.
    ///
    /// The scene itself contains no aircraft, tractors or carts, for the same reason: there is one
    /// description of what stands where, and it is the plan this spawner follows.
    ///
    /// This is also where the pieces are wired to each other. It is the one place that knows about
    /// vehicles, crew, ownership and the readout all at once, which is what keeps any of those from
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

        readonly List<VehicleController> m_Vehicles = new List<VehicleController>();

        CrewCharacter m_LocalCrew;
        readonly List<CartChain> m_Trains = new List<CartChain>();

        /// <summary>Every vehicle standing on the apron.</summary>
        public IReadOnlyList<VehicleController> Vehicles => m_Vehicles;

        /// <summary>Every train on the apron, one per tractor.</summary>
        public IReadOnlyList<CartChain> Trains => m_Trains;

        /// <summary>Whether the apron has been built yet.</summary>
        public bool ApronBuilt { get; private set; }

        /// <summary>The profile a given kind of equipment runs on.</summary>
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

        void BuildApron()
        {
            var plan = ApronLayout.Build(m_Layout, m_TractorProfile, m_CartProfile, m_AircraftProfile);

            Place(m_AircraftPrefab, plan.Aircraft, RampObject.Kind.Aircraft);

            foreach (var train in plan.Trains)
            {
                var members = new List<VehicleController>
                {
                    PlaceVehicle(plan: train.Tractor, kind: RampObject.Kind.Tractor)
                };

                foreach (var cart in train.Carts)
                {
                    members.Add(PlaceVehicle(cart, RampObject.Kind.Cart));
                }

                m_Trains.Add(CartChain.Couple(members, m_Coupling));
            }

            ApronBuilt = true;

            if (m_Readout != null)
            {
                m_Readout.Observe(m_Trains, m_Broker);
            }
        }

        VehicleController PlaceVehicle(Placement plan, RampObject.Kind kind)
        {
            var spawned = Place(m_VehiclePrefab, plan, kind);
            var vehicle = spawned.GetComponent<VehicleController>();

            vehicle.Configure(ProfileFor(kind), plan.Name);
            m_Vehicles.Add(vehicle);

            return vehicle;
        }

        NetworkObject Place(NetworkObject prefab, Placement plan, RampObject.Kind kind)
        {
            var spawned = Instantiate(prefab, plan.Position, plan.Rotation);
            spawned.Spawn();

            // Anyone may take a tractor or a cart, so their ownership has to be able to move. The
            // request is what gives the machine that currently holds one the chance to say no.
            spawned.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable | NetworkObject.OwnershipStatus.RequestRequired);
            spawned.GetComponent<RampObject>().Describe(kind, plan.Name);

            return spawned;
        }

        void SpawnOwnCrew()
        {
            var standingAt = new Vector3(m_Layout.firstTractorPosition.x + 4f, 1.2f, m_Layout.firstTractorPosition.z);

            var crew = Instantiate(m_CrewPrefab, standingAt, Quaternion.identity);
            crew.Spawn();
            crew.GetComponent<RampObject>().Describe(RampObject.Kind.Crew, $"Player {NetworkManager.LocalClientId}");

            m_LocalCrew = crew.GetComponent<CrewCharacter>();
            m_LocalCrew.Configure(m_CrewProfile, m_Broker, NearbyVehicles);
            m_LocalCrew.Camera = m_Camera;

            // Only this player's own character reads this machine's keyboard. Everybody else's
            // arrives as replicated movement.
            crew.gameObject.AddComponent<LocalCrewInput>();

            if (m_Camera != null)
            {
                m_Camera.Subject = m_LocalCrew.transform;
            }
        }

        IReadOnlyList<IDriveable> NearbyVehicles()
        {
            // Rebuilt on demand rather than cached, because a client that joined after the apron
            // was built receives its vehicles over the following moments rather than all at once.
            var everything = new List<IDriveable>();

            foreach (var vehicle in FindObjectsByType<VehicleController>(FindObjectsSortMode.None))
            {
                everything.Add(vehicle);
            }

            return everything;
        }

        void LateUpdate()
        {
            // The camera follows whatever the local player is in charge of, which changes the
            // moment they climb into a tractor or step back out of one. Only this machine's own
            // character was given this camera, so only that one moves it.
            if (m_LocalCrew != null && m_LocalCrew.Seat != null)
            {
                m_Camera.Subject = m_LocalCrew.Seat.Subject;
            }
        }
    }
}
