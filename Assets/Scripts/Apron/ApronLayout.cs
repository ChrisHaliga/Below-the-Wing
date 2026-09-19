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

        public readonly Vector3 CentreLocal;

        public Placement(string name, Vector3 position, Quaternion rotation, Vector3 sizeMetres)
            : this(name, position, rotation, sizeMetres, Vector3.zero)
        {
        }

        public Placement(
            string name, Vector3 position, Quaternion rotation, Vector3 sizeMetres, Vector3 centreLocal)
        {
            Name = name;
            Position = position;
            Rotation = rotation;
            SizeMetres = sizeMetres;
            CentreLocal = centreLocal;
        }

        public Bounds Bounds => new Bounds(Position + (Rotation * CentreLocal), RoomItFills);

        Vector3 RoomItFills
        {
            get
            {
                var across = Rotation * new Vector3(SizeMetres.x, 0f, 0f);
                var up = Rotation * new Vector3(0f, SizeMetres.y, 0f);
                var along = Rotation * new Vector3(0f, 0f, SizeMetres.z);

                return new Vector3(
                    Mathf.Abs(across.x) + Mathf.Abs(up.x) + Mathf.Abs(along.x),
                    Mathf.Abs(across.y) + Mathf.Abs(up.y) + Mathf.Abs(along.y),
                    Mathf.Abs(across.z) + Mathf.Abs(up.z) + Mathf.Abs(along.z));
            }
        }
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

        public Placement BeltLoader { get; }

        public IReadOnlyList<TrainPlan> Trains { get; }

        public IReadOnlyList<Placement> Everything { get; }

        public IReadOnlyList<Placement> CrewSpawnPoints { get; }

        public ApronPlan(
            Placement aircraft,
            Placement beltLoader,
            IReadOnlyList<TrainPlan> trains,
            IReadOnlyList<Placement> everything,
            IReadOnlyList<Placement> crewSpawnPoints)
        {
            Aircraft = aircraft;
            BeltLoader = beltLoader;
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

        [Tooltip("How far ahead of the tractors the arrival line stands, m")]
        public float crewArriveAheadOfTheTractorsMetres;

        [Tooltip("How far left of the first tractor the arrival line starts, m")]
        public float crewLineStartsLeftOfTheTractorsMetres;

        [Tooltip("How far left of the aircraft's centreline the belt loader stands, m")]
        public float beltLoaderStandsLeftOfTheCentrelineMetres;

        [Tooltip("How far aft of the aircraft's centre the belt loader stands, m")]
        public float beltLoaderStandsAftOfTheCentreMetres;

        [Tooltip("Bags dropped beside each train's first cart")]
        public int bagsPerTrain;

        [Tooltip("Clear space between a bag and the cart's side, m")]
        public float bagClearanceFromTheCartMetres;

        [Tooltip("Spacing between bags along the cart, m")]
        public float bagPitchMetres;

        public static ApronLayoutSettings Default => new ApronLayoutSettings
        {
            trainCount = 2,
            cartsPerTrain = 4,
            trainSpacingMetres = 6f,
            firstTractorPosition = new Vector3(-18f, 0f, -22f),
            crewSpawnPoints = Shift.MostCrew,
            crewSpacingMetres = 2f,
            crewArriveAheadOfTheTractorsMetres = 6f,
            crewLineStartsLeftOfTheTractorsMetres = 2f,
            beltLoaderStandsLeftOfTheCentrelineMetres = 4.5f,
            beltLoaderStandsAftOfTheCentreMetres = 7f,
            bagsPerTrain = 4,
            bagClearanceFromTheCartMetres = 0.6f,
            bagPitchMetres = 0.9f
        };
    }

    public static class ApronLayout
    {
        const float BagDropMetres = 0.15f;

        public static IReadOnlyList<Placement> BagsBeside(
            TrainPlan train, VehicleFootprint cart, Vector3 bagSizeMetres, ApronLayoutSettings settings)
        {
            var bags = new List<Placement>(Mathf.Max(settings.bagsPerTrain, 0));

            if (train.Carts.Count == 0)
            {
                return bags;
            }

            var beside = train.Carts[0];

            var clearOfTheSide = (cart.EnvelopeSizeMetres.x * 0.5f)
                                 + settings.bagClearanceFromTheCartMetres
                                 + (bagSizeMetres.x * 0.5f);

            var offTheGround = (bagSizeMetres.y * 0.5f) + BagDropMetres;

            for (var i = 0; i < settings.bagsPerTrain; i++)
            {
                var at = beside.Position
                         + (beside.Rotation * new Vector3(clearOfTheSide, offTheGround, -(i * settings.bagPitchMetres)));

                bags.Add(new Placement($"Bag {i + 1}", at, beside.Rotation, bagSizeMetres));
            }

            return bags;
        }

        static IReadOnlyList<Placement> ArrivalPoints(
            ApronLayoutSettings settings,
            Vector3 crewSizeMetres,
            IReadOnlyList<Placement> standing)
        {
            var standingHeight = crewSizeMetres.y * 0.5f;

            var alongZ = settings.firstTractorPosition.z + settings.crewArriveAheadOfTheTractorsMetres;
            var points = new List<Placement>(settings.crewSpawnPoints);

            for (var i = 0; i < settings.crewSpawnPoints; i++)
            {
                var at = new Vector3(
                    settings.firstTractorPosition.x
                    - settings.crewLineStartsLeftOfTheTractorsMetres
                    + (i * settings.crewSpacingMetres),
                    standingHeight,
                    alongZ);

                var arrival = new Placement($"Arrival {i + 1}", at, Quaternion.identity, crewSizeMetres);

                foreach (var thing in standing)
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

        public static ApronPlan Build(ApronLayoutSettings settings, ApronEquipment equipment)
        {
            var tractor = equipment.Tractor;
            var cart = equipment.Cart;

            var everything = new List<Placement>();

            var aircraftPlacement = new Placement(
                "Aircraft",
                Vector3.zero,
                Quaternion.identity,
                equipment.AircraftSizeMetres,
                equipment.AircraftCentreLocal);
            everything.Add(aircraftPlacement);

            var beltLoaderPlacement = new Placement(
                "Belt loader",
                new Vector3(
                    -settings.beltLoaderStandsLeftOfTheCentrelineMetres,
                    0f,
                    -settings.beltLoaderStandsAftOfTheCentreMetres),
                Quaternion.LookRotation(Vector3.right),
                equipment.BeltLoader.EnvelopeSizeMetres);
            everything.Add(beltLoaderPlacement);

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

            return new ApronPlan(
                aircraftPlacement, beltLoaderPlacement, trains, everything,
                ArrivalPoints(settings, equipment.CrewSizeMetres, everything));
        }
    }
}
