using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// Puts the aircraft, the tractors and the carts on the apron, once, at the start of a session.
    ///
    /// Exactly one machine does this -- the session owner -- so that everybody is looking at the
    /// same apron rather than each building their own. Crew are not its business: a player brings
    /// their own character, because the person controlling it should be the one simulating it.
    /// </summary>
    public static class ApronBuilder
    {
        /// <summary>Places everything the plan describes.</summary>
        public static void Build(
            ApronLayoutSettings layout,
            VehicleProfile tractorProfile,
            VehicleProfile cartProfile,
            AircraftProfile aircraftProfile,
            CrewProfile crewProfile,
            NetworkObject tractorPrefab,
            NetworkObject cartPrefab,
            NetworkObject aircraftPrefab)
        {
            var crewSize = new Vector3(
                crewProfile.radiusMetres * 2f, crewProfile.heightMetres, crewProfile.radiusMetres * 2f);

            var plan = ApronLayout.Build(layout, tractorProfile, cartProfile, aircraftProfile, crewSize);

            Place(aircraftPrefab, plan.Aircraft);

            for (var t = 0; t < plan.Trains.Count; t++)
            {
                var train = plan.Trains[t];

                Place(tractorPrefab, train.Tractor, t, placeInTrain: 0);

                for (var c = 0; c < train.Carts.Count; c++)
                {
                    Place(cartPrefab, train.Carts[c], t, placeInTrain: c + 1);
                }
            }
        }

        /// <summary>
        /// Makes a spawned object one that changes hands only when somebody asks for it and is
        /// granted it.
        ///
        /// Clearing first matters: the prefabs are saved as distributable, and merely adding a flag
        /// would leave the netcode layer free to keep handing vehicles out one at a time whenever
        /// anybody joins or leaves -- splitting trains across machines behind the back of every rule
        /// written to stop exactly that.
        /// </summary>
        public static void OnlyByAsking(NetworkObject placed)
            => placed.SetOwnershipStatus(NetworkObject.OwnershipStatus.RequestRequired, clearAndSet: true);

        static void Place(
            NetworkObject prefab,
            Placement plan,
            int trainIndex = TrainMembership.NoTrain,
            int placeInTrain = 0)
        {
            var placed = Object.Instantiate(prefab, plan.Position, plan.Rotation);

            // Everything about this object is settled before it is spawned. Netcode raises
            // OnNetworkSpawn synchronously from inside Spawn(), so anything described afterwards is
            // described too late: every machine present, including this one, would have already
            // looked at the object and made up its mind about what it was.
            placed.GetComponent<ApronIdentity>().Called(plan.Name);

            var member = placed.GetComponent<TrainMember>();
            if (member != null)
            {
                member.Joins(trainIndex, placeInTrain);
            }

            placed.Spawn();
            OnlyByAsking(placed);
        }
    }
}
