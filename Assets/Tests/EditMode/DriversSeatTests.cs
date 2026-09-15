using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class DriversSeatTests
    {
        const string TractorPrefabPath = "Assets/Content/Prefabs/BaggageTractor.prefab";
        const string CartPrefabPath = "Assets/Content/Prefabs/BaggageCart.prefab";

        static VehicleShape Shape(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, $"there is no prefab at {path}");
            return prefab.GetComponent<VehicleShape>();
        }

        [Test]
        public void ATractorKnowsWhereItsDriverSits()
        {
            var seat = Shape(TractorPrefabPath).SeatLocal;

            Assert.That(seat.HasValue, Is.True,
                "a tractor with no seat puts its driver at its own origin, which is a point on the " +
                "tarmac between the front wheels: the camera sits at ground level between the axles " +
                "and the driver is buried in the machine");

            Assert.That(seat.Value.y, Is.GreaterThan(0.3f),
                $"the seat is {seat.Value.y:F2} m up, which is under the axles");
        }

        [Test]
        public void ATractorsSeatIsBehindItsFrontAxle()
        {
            var shape = Shape(TractorPrefabPath);
            var seat = shape.SeatLocal.Value;

            var frontAxle = float.MinValue;
            foreach (var wheel in shape.Wheels)
            {
                frontAxle = Mathf.Max(frontAxle, wheel.CentreLocal.z);
            }

            Assert.That(seat.z, Is.LessThan(frontAxle),
                $"the seat is at z={seat.z:F2} and the front axle at z={frontAxle:F2}. A driver " +
                "seated over or ahead of the front wheels is sitting on the bonnet");
        }

        [Test]
        public void ACartHasNoSeatBecauseNobodyDrivesIt()
        {
            Assert.That(Shape(CartPrefabPath).SeatLocal.HasValue, Is.False,
                "a cart is towed. A seat on one is a driving position for a vehicle with no controls");
        }

        [Test]
        public void AShapeWithNoSeatSaysSoRatherThanOfferingTheOrigin()
        {
            var vehicle = new GameObject("vehicle");

            try
            {
                var shape = TestShapes.On(vehicle, TestShapes.BoxVehicle());

                Assert.That(shape.SeatLocal.HasValue, Is.False,
                    "an unset seat has to be absent rather than (0,0,0). A zero reads as a seat on " +
                    "the ground at the vehicle's origin, and nothing downstream can tell the two apart");
            }
            finally
            {
                Object.DestroyImmediate(vehicle);
            }
        }
    }
}
