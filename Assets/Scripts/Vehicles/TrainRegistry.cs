using System.Collections.Generic;

namespace BelowTheWing.Vehicles
{
    /// <summary>One vehicle's place in the world: which train it belongs to, and where in it.</summary>
    public readonly struct TrainMembership
    {
        /// <summary>Given as the train index for a vehicle that is not part of a train.</summary>
        public const int NoTrain = -1;

        public readonly VehicleController Vehicle;

        /// <summary>
        /// Which train this belongs to, or <see cref="NoTrain"/>. A number rather than an object
        /// reference, because this fact has to survive being sent to a machine that has not
        /// received the other members yet.
        /// </summary>
        public readonly int TrainIndex;

        /// <summary>Where in its train this sits: 0 is the tractor, 1 the first cart, and so on.</summary>
        public readonly int PlaceInTrain;

        public TrainMembership(VehicleController vehicle, int trainIndex = NoTrain, int placeInTrain = 0)
        {
            Vehicle = vehicle;
            TrainIndex = trainIndex;
            PlaceInTrain = placeInTrain;
        }
    }

    /// <summary>
    /// Which vehicles form which trains, and which of those this machine is responsible for.
    ///
    /// Both questions have to be answered on every machine in a session, not only on the one that
    /// built the apron. A machine that thinks a tractor is standing on its own will ask for the
    /// tractor when a player gets in, take it, and drive away leaving four carts owned and
    /// simulated by somebody else.
    ///
    /// Deliberately a plain object with no scene and no networking in it, because this is where
    /// that mistake was made and it needs to be somewhere a test can reach.
    /// </summary>
    public sealed class TrainRegistry
    {
        readonly ChainJointSettings m_Settings;
        readonly List<CartChain> m_Trains = new List<CartChain>();

        /// <summary>Every train, as this machine currently understands the apron.</summary>
        public IReadOnlyList<CartChain> Trains => m_Trains;

        public TrainRegistry(ChainJointSettings settings) => m_Settings = settings;

        /// <summary>
        /// Works out the trains afresh from what every machine was told about the vehicles on the
        /// apron.
        ///
        /// A train whose members and order have not changed keeps the chain it had, couplings and
        /// all. Rebuilding one that has not changed would destroy and re-create its hinges, and a
        /// hinge created mid-turn takes its jackknife limits from whatever angle the cart is at,
        /// so a train being driven when somebody joins would silently acquire a different range of
        /// movement than the one it started with.
        /// </summary>
        public void Rebuild(IReadOnlyList<TrainMembership> members)
        {
            var wanted = GroupIntoTrains(members);
            var kept = new List<CartChain>(wanted.Count);

            foreach (var lineup in wanted)
            {
                var existing = MatchExisting(lineup);
                kept.Add(existing ?? CartChain.Couple(lineup, m_Settings));
            }

            // Anything not carried across is a chain nobody holds a reference to any more, and its
            // couplings would be joints nothing will ever destroy.
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

        /// <summary>
        /// Puts couplings on the trains this machine owns outright and takes them off the ones it
        /// does not own at all.
        ///
        /// A train the machine owns only part of is left exactly as it is. Ownership of five
        /// vehicles does not move in one instant, and during that round trip neither the machine
        /// handing the train over nor the one taking it owns all of it. Acting on a half-answer
        /// means both let go, and the train is simulated by nobody until the last response lands.
        /// </summary>
        public void TakeUpWhatWeOwn(IOwnershipBroker broker)
        {
            foreach (var train in m_Trains)
            {
                var ours = 0;
                foreach (var member in train.Members)
                {
                    if (broker.OwnedByUs(member))
                    {
                        ours++;
                    }
                }

                if (ours == train.Members.Count)
                {
                    Hold(train, held: true);
                }
                else if (ours == 0)
                {
                    Hold(train, held: false);
                }
            }
        }

        /// <summary>
        /// Every train holding a vehicle that belongs to the named machine.
        ///
        /// Used when somebody leaves a session. Their equipment is handed on one object at a time by
        /// the netcode layer, which takes no notice of which train anything belongs to, so a train
        /// can end up scattered across whoever is left. Asking which trains were touched is what
        /// lets one machine take each of them back whole.
        /// </summary>
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

        static void Hold(CartChain train, bool held)
        {
            if (held)
            {
                train.EngageCouplings();
            }
            else
            {
                train.ReleaseCouplings();
            }

        }

        /// <summary>
        /// Sorts the described vehicles into the trains they say they belong to, each ordered from
        /// the front backwards. A vehicle in no train comes back as a train of itself.
        /// </summary>
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

                // Place in train decides the order, not the order the descriptions arrived in.
                // Objects reach a machine in whatever order the network delivers them.
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

        /// <summary>
        /// The existing train made of exactly these vehicles in exactly this order, or null if
        /// there isn't one.
        /// </summary>
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
