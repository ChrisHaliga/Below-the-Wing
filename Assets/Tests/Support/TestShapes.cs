using System.Collections.Generic;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    /// <summary>
    /// The physical shape of each vehicle, for tests to run against.
    ///
    /// The companion to <see cref="TestProfiles"/>: a profile says how a vehicle drives, a shape
    /// says where its parts are. Built in memory rather than read off the shipped prefabs so that
    /// remodelling a vehicle cannot turn a test red -- but the numbers are the real measured ones,
    /// because several tests are about those measurements being real.
    /// </summary>
    public static class TestShapes
    {
        /// <summary>Puts a shape on an object and fills it in. Returns the component.</summary>
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

        /// <summary>
        /// A plain box on four wheels, for tests that are about driving rather than about geometry.
        ///
        /// A stand-in, and named as one. Most tests in this project want a vehicle that has weight,
        /// wheels and a coupling at each end and do not care what it looks like; giving them the
        /// real cart would make them fail whenever somebody remodels it. Anything actually testing
        /// where a part of a vehicle is asks for the measured shape instead.
        /// </summary>
        public static VehicleShape.Measurements BoxVehicle(Vector3 sizeMetres)
        {
            const float wheelRadius = 0.3f;

            var halfWheelbase = sizeMetres.z * 0.35f;
            var halfTrack = sizeMetres.x * 0.42f;
            var reach = (sizeMetres.z * 0.5f) + 0.3f;
            const float couplingHeight = 0.5f;

            // Clear of the tarmac. The origin is on the ground, so a body box centred on it has its
            // underside level with the apron, carries the vehicle's weight itself, and leaves the
            // suspension doing nothing at all.
            var middle = new Vector3(0f, wheelRadius + (sizeMetres.y * 0.5f), 0f);

            return new VehicleShape.Measurements
            {
                WheelCentresLocal = new List<Vector3>
                {
                    new Vector3(-halfTrack, wheelRadius, halfWheelbase),
                    new Vector3(halfTrack, wheelRadius, halfWheelbase),
                    new Vector3(-halfTrack, wheelRadius, -halfWheelbase),
                    new Vector3(halfTrack, wheelRadius, -halfWheelbase)
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

        /// <summary>
        /// The baggage cart, as measured off the model.
        ///
        /// The two ends do not match, and that is the point of having real measurements here. The
        /// drawbar sticks 3.16 m out in front; the socket is recessed and reaches 1.82 m back. They
        /// also meet at different heights, because a real drawbar is offset vertically so that the
        /// male and female halves do not try to occupy the same space.
        /// </summary>
        public static VehicleShape.Measurements Cart()
        {
            const float deckTop = 0.4727f;
            const float clearInside = 1.626f;

            return new VehicleShape.Measurements
            {
                WheelCentresLocal = new List<Vector3>
                {
                    new Vector3(-0.7930f, 0.1557f, 1.5805f),
                    new Vector3(0.7930f, 0.1557f, 1.5805f),
                    new Vector3(-0.7930f, 0.1557f, -1.5805f),
                    new Vector3(0.7930f, 0.1557f, -1.5805f)
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

        /// <summary>
        /// What a cart is solid where.
        ///
        /// Not one box. A cart is a container, and a box the size of the vehicle fills the space
        /// bags are supposed to go in -- so this is a floor, the lips that keep a load aboard while
        /// the cart is parked, the fixed ends, and the roof.
        /// </summary>
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

        /// <summary>
        /// The baggage tractor, which has no model yet.
        ///
        /// Described exactly as the cart is, from numbers rather than from geometry. Nothing
        /// downstream can tell the difference, which is the whole reason a shape exists as a thing
        /// in its own right rather than as a way of reading a mesh.
        /// </summary>
        public static VehicleShape.Measurements Tractor()
        {
            var size = new Vector3(1.3f, 1.6f, 3.0f);
            var reach = (size.z * 0.5f) + 0.3f;
            const float couplingHeight = 0.2075f;
            var middle = new Vector3(0f, 0.3f + (size.y * 0.5f), 0f);

            return new VehicleShape.Measurements
            {
                WheelCentresLocal = new List<Vector3>
                {
                    new Vector3(-0.55f, 0.3f, 0.9f),
                    new Vector3(0.55f, 0.3f, 0.9f),
                    new Vector3(-0.55f, 0.3f, -0.9f),
                    new Vector3(0.55f, 0.3f, -0.9f)
                },
                FrontCouplingLocal = new Vector3(0f, couplingHeight, reach),
                RearCouplingLocal = new Vector3(0f, couplingHeight, -reach),
                EnvelopeSizeMetres = size,
                EnvelopeCentreLocal = middle,

                // Solid through and through. Nothing rides inside a tractor.
                InteriorLocal = new Bounds(Vector3.zero, Vector3.zero),
                SolidParts = new List<VehicleShape.SolidPart>
                {
                    new VehicleShape.SolidPart("Body", size, middle)
                }
            };
        }
    }
}
