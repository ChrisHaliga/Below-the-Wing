using System.Collections.Generic;

namespace BelowTheWing.Vehicles
{
    public readonly struct TrainMembership
    {
        public const int NoTrain = -1;

        public readonly VehicleController Vehicle;

        public readonly int TrainIndex;

        public readonly int PlaceInTrain;

        public TrainMembership(VehicleController vehicle, int trainIndex = NoTrain, int placeInTrain = 0)
        {
            Vehicle = vehicle;
            TrainIndex = trainIndex;
            PlaceInTrain = placeInTrain;
        }
    }

    public sealed class TrainRegistry
    {
        readonly ChainJointSettings m_Settings;
        readonly List<CartChain> m_Trains = new List<CartChain>();

        public IReadOnlyList<CartChain> Trains => m_Trains;

        public TrainRegistry(ChainJointSettings settings) => m_Settings = settings;

        public void Rebuild(IReadOnlyList<TrainMembership> members)
        {
            var wanted = GroupIntoTrains(members);
            var kept = new List<CartChain>(wanted.Count);

            foreach (var lineup in wanted)
            {
                var existing = MatchExisting(lineup);
                if (existing == null)
                {
                    existing = CartChain.Couple(lineup, m_Settings);

                    existing.EngageCouplings();
                }

                kept.Add(existing);
            }

            foreach (var train in m_Trains)
            {
                if (!kept.Contains(train))
                {
                    train.ReleaseCouplings();
                }
            }

            m_Trains.Clear();
            m_Trains.AddRange(kept);
        }

        public IReadOnlyList<CartChain> TrainsHeldBy(ulong client, IOwnershipBroker broker)
        {
            var theirs = new List<CartChain>();

            foreach (var train in m_Trains)
            {
                foreach (var member in train.Members)
                {
                    if (broker.OwnerOf(member) == client)
                    {
                        theirs.Add(train);
                        break;
                    }
                }
            }

            return theirs;
        }

        static List<List<VehicleController>> GroupIntoTrains(IReadOnlyList<TrainMembership> members)
        {
            var byIndex = new SortedDictionary<int, SortedList<int, VehicleController>>();
            var alone = new List<List<VehicleController>>();

            foreach (var member in members)
            {
                if (member.Vehicle == null)
                {
                    continue;
                }

                if (member.TrainIndex == TrainMembership.NoTrain)
                {
                    alone.Add(new List<VehicleController> { member.Vehicle });
                    continue;
                }

                if (!byIndex.TryGetValue(member.TrainIndex, out var places))
                {
                    places = new SortedList<int, VehicleController>();
                    byIndex[member.TrainIndex] = places;
                }

                places[member.PlaceInTrain] = member.Vehicle;
            }

            var lineups = new List<List<VehicleController>>(byIndex.Count + alone.Count);
            foreach (var places in byIndex.Values)
            {
                lineups.Add(new List<VehicleController>(places.Values));
            }

            lineups.AddRange(alone);
            return lineups;
        }

        CartChain MatchExisting(IReadOnlyList<VehicleController> lineup)
        {
            foreach (var train in m_Trains)
            {
                if (train.Members.Count != lineup.Count)
                {
                    continue;
                }

                var same = true;
                for (var i = 0; i < lineup.Count; i++)
                {
                    if (train.Members[i] != lineup[i])
                    {
                        same = false;
                        break;
                    }
                }

                if (same)
                {
                    return train;
                }
            }

            return null;
        }
    }
}
