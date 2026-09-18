using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    public static class ApronBuilder
    {
        public static void Build(
            ApronLayoutSettings layout,
            AircraftProfile aircraftProfile,
            CrewProfile crewProfile,
            NetworkObject tractorPrefab,
            NetworkObject cartPrefab,
            NetworkObject aircraftPrefab,
            NetworkObject bagPrefab,
            int bagsPerTrain)
        {
            var plan = ApronLayout.Build(
                layout,
                tractorPrefab.GetComponent<VehicleShape>().Footprint,
                cartPrefab.GetComponent<VehicleShape>().Footprint,
                aircraftProfile,
                crewProfile.SizeMetres);

            Place(aircraftPrefab, plan.Aircraft);

            for (var t = 0; t < plan.Trains.Count; t++)
            {
                var train = plan.Trains[t];

                Place(tractorPrefab, train.Tractor, t, placeInTrain: 0);

                for (var c = 0; c < train.Carts.Count; c++)
                {
                    Place(cartPrefab, train.Carts[c], t, placeInTrain: c + 1);
                }

                Scatter(bagPrefab, train, bagsPerTrain);
            }
        }

        static void Scatter(NetworkObject bagPrefab, TrainPlan train, int howMany)
        {
            if (bagPrefab == null || howMany <= 0 || train.Carts.Count == 0)
            {
                return;
            }

            var beside = train.Carts[0];

            for (var i = 0; i < howMany; i++)
            {
                var where = beside.Position
                            + (Vector3.right * 2.5f)
                            + (Vector3.back * (i * 0.9f))
                            + (Vector3.up * 0.4f);

                var bag = Object.Instantiate(bagPrefab, where, Quaternion.identity);
                bag.GetComponent<ApronIdentity>()?.Called($"Bag {i + 1}");
                bag.Spawn();

                OnlyByAsking(bag);
            }
        }

        public static void OnlyByAsking(NetworkObject placed)
            => placed.SetOwnershipStatus(NetworkObject.OwnershipStatus.RequestRequired, clearAndSet: true);

        static void Place(
            NetworkObject prefab,
            Placement plan,
            int trainIndex = TrainMembership.NoTrain,
            int placeInTrain = 0)
        {
            var placed = Object.Instantiate(prefab, plan.Position, plan.Rotation);

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
