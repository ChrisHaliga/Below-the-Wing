using BelowTheWing.Apron;
using BelowTheWing.Cargo;
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
            CrewProfile crewProfile,
            NetworkObject tractorPrefab,
            NetworkObject cartPrefab,
            NetworkObject aircraftPrefab,
            NetworkObject bagPrefab)
        {
            var cartFootprint = cartPrefab.GetComponent<VehicleShape>().Footprint;
            var bagSize = bagPrefab.GetComponent<Bag>().Profile.sizeMetres;

            var plan = ApronLayout.Build(layout, new ApronEquipment(
                tractorPrefab.GetComponent<VehicleShape>().Footprint,
                cartFootprint,
                aircraftPrefab.GetComponent<AircraftShape>().EnvelopeSizeMetres,
                aircraftPrefab.GetComponent<AircraftShape>().EnvelopeCentreLocal,
                crewProfile.SizeMetres));

            Place(aircraftPrefab, plan.Aircraft);

            for (var t = 0; t < plan.Trains.Count; t++)
            {
                var train = plan.Trains[t];

                Place(tractorPrefab, train.Tractor, t, placeInTrain: 0);

                for (var c = 0; c < train.Carts.Count; c++)
                {
                    Place(cartPrefab, train.Carts[c], t, placeInTrain: c + 1);
                }

                foreach (var bag in ApronLayout.BagsBeside(train, cartFootprint, bagSize, layout))
                {
                    Place(bagPrefab, bag);
                }
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
