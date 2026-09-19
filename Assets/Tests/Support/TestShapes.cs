using System.Collections.Generic;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    public static class TestShapes
    {
        public static readonly Vector3 AircraftSizeMetres = new Vector3(21.21f, 5.38f, 27.44f);

        public static readonly Vector3 BeltLoaderSizeMetres = new Vector3(1.62f, 0.79f, 4.68f);

        public static VehicleFootprint BeltLoaderFootprint
            => VehicleFootprint.Of(BeltLoaderSizeMetres, BeltLoaderCentreLocal, null, null);

        public static readonly Vector3 BeltLoaderCentreLocal = new Vector3(0f, 0.3966f, 0.2741f);

        public static VehicleShape.Measurements BeltLoader()
        {
            const float wheelRadius = 0.265f;

            var chassis = new Vector3(1.40f, 0.39f, 3.15f);
            var chassisMiddle = new Vector3(0f, 0.435f, 0.24f);

            var belt = new Vector3(0.87f, 0.16f, 4.68f);
            var beltMiddle = new Vector3(0f, 0.71f, 0.27f);

            return new VehicleShape.Measurements
            {
                Wheels = new List<VehicleShape.WheelPlacement>
                {
                    new VehicleShape.WheelPlacement(new Vector3(-0.67f, wheelRadius, 1.33f), wheelRadius),
                    new VehicleShape.WheelPlacement(new Vector3(0.67f, wheelRadius, 1.33f), wheelRadius),
                    new VehicleShape.WheelPlacement(new Vector3(-0.67f, wheelRadius, -1.00f), wheelRadius),
                    new VehicleShape.WheelPlacement(new Vector3(0.67f, wheelRadius, -1.00f), wheelRadius)
                },
                FrontCouplingLocal = null,
                RearCouplingLocal = null,
                SeatLocal = new Vector3(-0.70f, 0.63f, -0.835f),
                EnvelopeSizeMetres = BeltLoaderSizeMetres,
                EnvelopeCentreLocal = new Vector3(0f, 0.395f, 0.27f),
                InteriorLocal = new Bounds(Vector3.zero, Vector3.zero),
                SolidParts = new List<VehicleShape.SolidPart>
                {
                    new VehicleShape.SolidPart("Body", chassis, chassisMiddle),
                    new VehicleShape.SolidPart("Belt", belt, beltMiddle)
                }
            };
        }

        public static readonly Vector3 AircraftCentreLocal = new Vector3(0f, 3.5306f, -0.3998f);

        public static VehicleFootprint AircraftFootprint
            => VehicleFootprint.Of(AircraftSizeMetres, AircraftCentreLocal, null, null);

        public static readonly VehicleFootprint NoAircraft = new VehicleFootprint();

        public static VehicleShape On(GameObject vehicle, VehicleShape.Measurements measurements)
        {
            var shape = vehicle.GetComponent<VehicleShape>();
            if (shape == null)
            {
                shape = vehicle.AddComponent<VehicleShape>();
            }

            shape.Describe(measurements);

            return shape;
        }

        public static VehicleShape.Measurements BoxVehicle() => BoxVehicle(StandInSizeMetres);

        public static readonly Vector3 StandInSizeMetres = new Vector3(1.5f, 1.7f, 3f);

        public static VehicleShape.Measurements BoxVehicle(Vector3 sizeMetres)
        {
            const float wheelRadius = 0.3f;

            var halfWheelbase = sizeMetres.z * 0.35f;
            var halfTrack = sizeMetres.x * 0.42f;
            var reach = (sizeMetres.z * 0.5f) + 0.3f;
            const float couplingHeight = 0.5f;

            var middle = new Vector3(0f, wheelRadius + (sizeMetres.y * 0.5f), 0f);

            return new VehicleShape.Measurements
            {
                Wheels = new List<VehicleShape.WheelPlacement>
                {
                    new VehicleShape.WheelPlacement(new Vector3(-halfTrack, wheelRadius, halfWheelbase), wheelRadius),
                    new VehicleShape.WheelPlacement(new Vector3(halfTrack, wheelRadius, halfWheelbase), wheelRadius),
                    new VehicleShape.WheelPlacement(new Vector3(-halfTrack, wheelRadius, -halfWheelbase), wheelRadius),
                    new VehicleShape.WheelPlacement(new Vector3(halfTrack, wheelRadius, -halfWheelbase), wheelRadius)
                },
                FrontCouplingLocal = new Vector3(0f, couplingHeight, reach),
                RearCouplingLocal = new Vector3(0f, couplingHeight, -reach),
                EnvelopeSizeMetres = sizeMetres,
                EnvelopeCentreLocal = middle,
                InteriorLocal = new Bounds(Vector3.zero, Vector3.zero),
                SolidParts = new List<VehicleShape.SolidPart>
                {
                    new VehicleShape.SolidPart("Body", sizeMetres, middle)
                }
            };
        }

        public static VehicleShape.Measurements Cart()
        {
            const float deckTop = 0.4727f;
            const float clearInside = 1.626f;
            const float wheelRadius = 0.157f;

            return new VehicleShape.Measurements
            {
                Wheels = new List<VehicleShape.WheelPlacement>
                {
                    new VehicleShape.WheelPlacement(new Vector3(-0.7930f, 0.1557f, 1.5805f), wheelRadius),
                    new VehicleShape.WheelPlacement(new Vector3(0.7930f, 0.1557f, 1.5805f), wheelRadius),
                    new VehicleShape.WheelPlacement(new Vector3(-0.7930f, 0.1557f, -1.5805f), wheelRadius),
                    new VehicleShape.WheelPlacement(new Vector3(0.7930f, 0.1557f, -1.5805f), wheelRadius)
                },
                FrontCouplingLocal = new Vector3(0f, 0.2075f, 3.1617f),
                RearCouplingLocal = new Vector3(0f, 0.1310f, -1.8159f),
                EnvelopeSizeMetres = new Vector3(1.8855f, 2.0155f, 3.8152f),
                EnvelopeCentreLocal = new Vector3(0f, 1.0909f, 0.1296f),
                InteriorLocal = new Bounds(
                    new Vector3(0f, deckTop + (clearInside * 0.5f), 0f),
                    new Vector3(1.7211f, clearInside, 3.1538f)),
                SolidParts = CartSolidParts(deckTop)
            };
        }

        static List<VehicleShape.SolidPart> CartSolidParts(float deckTop)
        {
            const float deckLength = 3.1538f;
            const float deckWidth = 1.7211f;
            const float slabThickness = 0.15f;
            const float lipHeight = 0.18f;
            const float lipThickness = 0.05f;
            const float roofUnderside = 2.0987f;

            return new List<VehicleShape.SolidPart>
            {
                new VehicleShape.SolidPart(
                    "Deck",
                    new Vector3(deckWidth, slabThickness, deckLength),
                    new Vector3(0f, deckTop - (slabThickness * 0.5f), 0f)),

                new VehicleShape.SolidPart(
                    "Lip left",
                    new Vector3(lipThickness, lipHeight, deckLength),
                    new Vector3(-((deckWidth * 0.5f) - (lipThickness * 0.5f)), deckTop + (lipHeight * 0.5f), 0f)),

                new VehicleShape.SolidPart(
                    "Lip right",
                    new Vector3(lipThickness, lipHeight, deckLength),
                    new Vector3((deckWidth * 0.5f) - (lipThickness * 0.5f), deckTop + (lipHeight * 0.5f), 0f)),

                new VehicleShape.SolidPart(
                    "End front",
                    new Vector3(deckWidth, roofUnderside - deckTop, slabThickness),
                    new Vector3(0f, (deckTop + roofUnderside) * 0.5f, (deckLength + slabThickness) * 0.5f)),

                new VehicleShape.SolidPart(
                    "End rear",
                    new Vector3(deckWidth, roofUnderside - deckTop, slabThickness),
                    new Vector3(0f, (deckTop + roofUnderside) * 0.5f, -(deckLength + slabThickness) * 0.5f)),

                new VehicleShape.SolidPart(
                    "Roof",
                    new Vector3(deckWidth, slabThickness, deckLength),
                    new Vector3(0f, roofUnderside + (slabThickness * 0.5f), 0f))
            };
        }

        public static VehicleShape.Measurements Tractor()
        {
            const float frontRadius = 0.2203f;
            const float rearRadius = 0.2647f;

            var bodywork = new Vector3(1.618f, 1.7023f, 2.8002f);
            var middle = new Vector3(0f, 0.9989f, -0.0858f);

            return new VehicleShape.Measurements
            {
                Wheels = new List<VehicleShape.WheelPlacement>
                {
                    new VehicleShape.WheelPlacement(new Vector3(-0.5998f, frontRadius, 0.7576f), frontRadius),
                    new VehicleShape.WheelPlacement(new Vector3(0.5977f, frontRadius, 0.7576f), frontRadius),
                    new VehicleShape.WheelPlacement(new Vector3(-0.5998f, rearRadius, -0.7576f), rearRadius),
                    new VehicleShape.WheelPlacement(new Vector3(0.5977f, rearRadius, -0.7576f), rearRadius)
                },
                FrontCouplingLocal = null,
                RearCouplingLocal = new Vector3(0f, 0.2075f, -1.3692f),
                EnvelopeSizeMetres = bodywork,
                EnvelopeCentreLocal = middle,

                InteriorLocal = new Bounds(Vector3.zero, Vector3.zero),
                SolidParts = new List<VehicleShape.SolidPart>
                {
                    new VehicleShape.SolidPart("Body", bodywork, middle)
                }
            };
        }
    }
}
