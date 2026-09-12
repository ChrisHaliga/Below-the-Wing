using System;
using System.Collections.Generic;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Apron
{
    /// <summary>Where one object stands on the apron, and how much room it takes up.</summary>
    public readonly struct Placement
    {
        /// <summary>What this object is called, both on its label and in the prompt to drive it.</summary>
        public readonly string Name;

        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        /// <summary>Width, height and length in metres.</summary>
        public readonly Vector3 SizeMetres;

        public Placement(string name, Vector3 position, Quaternion rotation, Vector3 sizeMetres)
        {
            Name = name;
            Position = position;
            Rotation = rotation;
            SizeMetres = sizeMetres;
        }

        /// <summary>The room this object needs, for checking that nothing is placed inside anything else.</summary>
        public Bounds Bounds => new Bounds(Position, SizeMetres);
    }

    /// <summary>One tractor and the carts lined up behind it, before any of it exists.</summary>
    public sealed class TrainPlan
    {
        public Placement Tractor { get; }

        /// <summary>The carts, nearest the tractor first.</summary>
        public IReadOnlyList<Placement> Carts { get; }

        public TrainPlan(Placement tractor, IReadOnlyList<Placement> carts)
        {
            Tractor = tractor;
            Carts = carts;
        }
    }

    /// <summary>Everything that should be standing on the apron when a session starts.</summary>
    public sealed class ApronPlan
    {
        public Placement Aircraft { get; }
        public IReadOnlyList<TrainPlan> Trains { get; }

        /// <summary>Every placement in the plan, aircraft included, in no particular order.</summary>
        public IReadOnlyList<Placement> Everything { get; }

        /// <summary>
        /// Where each player arrives, one spot per player, clear of the equipment and of each other.
        /// Two capsules starting in the same place do not settle: they fire apart.
        /// </summary>
        public IReadOnlyList<Placement> CrewSpawnPoints { get; }

        public ApronPlan(
            Placement aircraft,
            IReadOnlyList<TrainPlan> trains,
            IReadOnlyList<Placement> everything,
            IReadOnlyList<Placement> crewSpawnPoints)
        {
            Aircraft = aircraft;
            Trains = trains;
            Everything = everything;
            CrewSpawnPoints = crewSpawnPoints;
        }
    }

    /// <summary>How much of what goes where.</summary>
    [Serializable]
    public struct ApronLayoutSettings
    {
        [Tooltip("How many tractors, each with its own row of carts behind it.")]
        public int trainCount;

        [Tooltip("How many carts are hooked up behind each tractor.")]
        public int cartsPerTrain;

        [Tooltip("Clear space left between one train and the next, side to side, in metres.")]
        public float trainSpacingMetres;

        [Tooltip("Where the first train's tractor stands, relative to the aircraft.")]
        public Vector3 firstTractorPosition;

        [Tooltip("How many players the apron makes room for.")]
        public int crewSpawnPoints;

        [Tooltip("Clear space left between one arriving player and the next, in metres.")]
        public float crewSpacingMetres;

        /// <summary>Two tractors with four carts each, parked clear of the aircraft.</summary>
        public static ApronLayoutSettings Default => new ApronLayoutSettings
        {
            trainCount = 2,
            cartsPerTrain = 4,
            trainSpacingMetres = 6f,
            firstTractorPosition = new Vector3(-18f, 0f, -22f),
            crewSpawnPoints = 5,
            crewSpacingMetres = 2f
        };
    }

    /// <summary>
    /// Deciding where everything stands, before any of it is built.
    ///
    /// Working the layout out as plain values rather than by placing objects in a scene means the
    /// arrangement can be checked before anything is spawned -- in particular, that no two things
    /// have been put in the same place. Rigidbodies that begin a session inside one another do not
    /// settle; they fire apart.
    /// </summary>
    public static class ApronLayout
    {
        /// <summary>
        /// Where players arrive: a row alongside the trains, spaced so nobody lands inside anybody
        /// else, and clear of everything already placed.
        /// </summary>
        static IReadOnlyList<Placement> ArrivalPoints(
            ApronLayoutSettings settings,
            Vector3 crewSizeMetres,
            IReadOnlyList<Placement> equipment)
        {
            // Taken as a size rather than a crew profile, so that working out where things stand
            // stays arithmetic about the apron and does not drag in what a ramp worker is.
            var standingHeight = crewSizeMetres.y * 0.5f;

            // Ahead of where the trains are parked, so nobody arrives among the carts.
            var alongZ = settings.firstTractorPosition.z + 6f;
            var points = new List<Placement>(settings.crewSpawnPoints);

            for (var i = 0; i < settings.crewSpawnPoints; i++)
            {
                var at = new Vector3(
                    settings.firstTractorPosition.x - 2f + (i * settings.crewSpacingMetres),
                    standingHeight,
                    alongZ);

                var arrival = new Placement($"Arrival {i + 1}", at, Quaternion.identity, crewSizeMetres);

                foreach (var thing in equipment)
                {
                    if (arrival.Bounds.Intersects(thing.Bounds))
                    {
                        throw new InvalidOperationException(
                            $"Player {i + 1} would arrive inside '{thing.Name}'. Arrival points are part " +
                            "of the layout precisely so this is caught here rather than as bodies firing " +
                            "apart when a session starts.");
                    }
                }

                points.Add(arrival);
            }

            return points;
        }

        /// <summary>
        /// Works out where the aircraft and every train should stand.
        ///
        /// Vehicles are placed nose to tail along each train at exactly coupling distance, worked
        /// out from how far each one's own hitch reaches, so a plan built from bigger equipment
        /// spreads out rather than overlapping.
        ///
        /// The two ends of a vehicle do not reach equally far. A baggage cart's drawbar sticks over
        /// three metres out in front and its socket is recessed under two metres behind, so the gap
        /// between two coupled vehicles is the rear reach of the one in front plus the front reach
        /// of the one behind. Doubling either leaves every coupling in the train holding a gap open,
        /// and a joint under permanent strain drags its train around the apron.
        ///
        /// Everything is placed standing on the ground. A vehicle's origin is where it touches down
        /// rather than the middle of its bodywork, so there is no height to work out here at all.
        /// </summary>
        public static ApronPlan Build(
            ApronLayoutSettings settings,
            VehicleFootprint tractor,
            VehicleFootprint cart,
            AircraftProfile aircraft,
            Vector3 crewSizeMetres)
        {
            var everything = new List<Placement>();

            var aircraftPlacement = new Placement(
                "Aircraft",
                new Vector3(0f, aircraft.centrelineHeightMetres, 0f),
                Quaternion.identity,
                new Vector3(aircraft.fuselageDiameterMetres, aircraft.fuselageDiameterMetres, aircraft.lengthMetres));
            everything.Add(aircraftPlacement);

            // Trains are laid out side by side, far enough apart that the widest thing in one has
            // clear air beside the widest thing in the next.
            var widest = Mathf.Max(tractor.EnvelopeSizeMetres.x, cart.EnvelopeSizeMetres.x);
            var lanePitch = widest + settings.trainSpacingMetres;

            var trains = new List<TrainPlan>(settings.trainCount);

            for (var t = 0; t < settings.trainCount; t++)
            {
                var trainNumber = t + 1;
                var lane = settings.firstTractorPosition.x + (t * lanePitch);

                var tractorPlacement = new Placement(
                    $"Tug {trainNumber}",
                    new Vector3(lane, 0f, settings.firstTractorPosition.z),
                    Quaternion.identity,
                    tractor.EnvelopeSizeMetres);

                // Vehicles are spaced hitch to hitch: the distance between two coupled vehicles is
                // the sum of how far each one's coupling reaches. Anything else leaves the joint
                // holding a gap open, and a coupling under permanent strain drags its train about.
                var carts = new List<Placement>(settings.cartsPerTrain);
                var behind = settings.firstTractorPosition.z
                             - tractor.RearReachMetres
                             - cart.FrontReachMetres;

                for (var c = 0; c < settings.cartsPerTrain; c++)
                {
                    carts.Add(new Placement(
                        $"Cart {trainNumber}-{c + 1}",
                        new Vector3(lane, 0f, behind),
                        Quaternion.identity,
                        cart.EnvelopeSizeMetres));

                    behind -= cart.RearReachMetres + cart.FrontReachMetres;
                }

                trains.Add(new TrainPlan(tractorPlacement, carts));
                everything.Add(tractorPlacement);
                everything.AddRange(carts);
            }

            return new ApronPlan(aircraftPlacement, trains, everything, ArrivalPoints(settings, crewSizeMetres, everything));
        }
    }
}
