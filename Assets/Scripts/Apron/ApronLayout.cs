using System;
using System.Collections.Generic;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Apron
{
    public readonly struct Placement
    {
        public readonly string Name;

        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public readonly Vector3 SizeMetres;

        public Placement(string name, Vector3 position, Quaternion rotation, Vector3 sizeMetres)
        {
            Name = name;
            Position = position;
            Rotation = rotation;
            SizeMetres = sizeMetres;
        }

        public Bounds Bounds => new Bounds(Position, SizeMetres);
    }

    public sealed class TrainPlan
    {
        public Placement Tractor { get; }

        public IReadOnlyList<Placement> Carts { get; }

        public TrainPlan(Placement tractor, IReadOnlyList<Placement> carts)
        {
            Tractor = tractor;
            Carts = carts;
        }
    }

    public sealed class ApronPlan
    {
        public Placement Aircraft { get; }
        public IReadOnlyList<TrainPlan> Trains { get; }

        public IReadOnlyList<Placement> Everything { get; }

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

    [Serializable]
    public struct ApronLayoutSettings
    {
        [Tooltip("Trains on the apron")]
        public int trainCount;

        [Tooltip("Carts behind each tractor")]
        public int cartsPerTrain;

        [Tooltip("Clear space between trains, m")]
        public float trainSpacingMetres;

        [Tooltip("Where the first tractor stands, m")]
        public Vector3 firstTractorPosition;

        [Tooltip("Arrival points for players")]
        public int crewSpawnPoints;

        [Tooltip("Clear space between arrivals, m")]
        public float crewSpacingMetres;

        public static ApronLayoutSettings Default => new ApronLayoutSettings
        {
            trainCount = 2,
            cartsPerTrain = 4,
            trainSpacingMetres = 6f,
            firstTractorPosition = new Vector3(-18f, 0f, -22f),
            crewSpawnPoints = Shift.MostCrew,
            crewSpacingMetres = 2f
        };
    }

    public static class ApronLayout
    {
        static IReadOnlyList<Placement> ArrivalPoints(
            ApronLayoutSettings settings,
            Vector3 crewSizeMetres,
            IReadOnlyList<Placement> equipment)
        {
            var standingHeight = crewSizeMetres.y * 0.5f;

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
