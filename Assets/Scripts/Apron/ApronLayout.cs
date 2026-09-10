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

        public ApronPlan(Placement aircraft, IReadOnlyList<TrainPlan> trains, IReadOnlyList<Placement> everything)
        {
            Aircraft = aircraft;
            Trains = trains;
            Everything = everything;
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

        /// <summary>Two tractors with four carts each, parked clear of the aircraft.</summary>
        public static ApronLayoutSettings Default => new ApronLayoutSettings
        {
            trainCount = 2,
            cartsPerTrain = 4,
            trainSpacingMetres = 6f,
            firstTractorPosition = new Vector3(-18f, 0f, -22f)
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
        /// Works out where the aircraft and every train should stand.
        ///
        /// Vehicles are placed nose to tail along each train at exactly coupling distance, worked out
        /// from how far each one's own hitch reaches, so a plan built from bigger equipment spreads
        /// out rather than overlapping.
        /// </summary>
        public static ApronPlan Build(
            ApronLayoutSettings settings,
            VehicleProfile tractor,
            VehicleProfile cart,
            AircraftProfile aircraft)
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
            var widest = Mathf.Max(tractor.bodySizeMetres.x, cart.bodySizeMetres.x);
            var lanePitch = widest + settings.trainSpacingMetres;

            var trains = new List<TrainPlan>(settings.trainCount);

            for (var t = 0; t < settings.trainCount; t++)
            {
                var trainNumber = t + 1;
                var lane = settings.firstTractorPosition.x + (t * lanePitch);

                var tractorPlacement = new Placement(
                    $"Tug {trainNumber}",
                    new Vector3(lane, VehicleController.RestingHeightMetres(tractor), settings.firstTractorPosition.z),
                    Quaternion.identity,
                    tractor.bodySizeMetres);

                // Vehicles are spaced hitch to hitch: the distance between two coupled vehicles is
                // the sum of how far each one's coupling reaches. Anything else leaves the joint
                // holding a gap open, and a coupling under permanent strain drags its train about.
                var carts = new List<Placement>(settings.cartsPerTrain);
                var behind = settings.firstTractorPosition.z
                             - VehicleController.HitchReachMetres(tractor)
                             - VehicleController.HitchReachMetres(cart);

                for (var c = 0; c < settings.cartsPerTrain; c++)
                {
                    carts.Add(new Placement(
                        $"Cart {trainNumber}-{c + 1}",
                        new Vector3(lane, VehicleController.RestingHeightMetres(cart), behind),
                        Quaternion.identity,
                        cart.bodySizeMetres));

                    behind -= 2f * VehicleController.HitchReachMetres(cart);
                }

                trains.Add(new TrainPlan(tractorPlacement, carts));
                everything.Add(tractorPlacement);
                everything.AddRange(carts);
            }

            return new ApronPlan(aircraftPlacement, trains, everything);
        }
    }
}
