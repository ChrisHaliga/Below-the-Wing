using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class LoadSpaceEdgeTests
    {
        const float LipTopMetres = 0.6527f;
        const float EndWallTopMetres = 2.0987f;
        const float DeckTopMetres = 0.4727f;

        GameObject m_Vehicle;

        [SetUp]
        public void MakeAVehicle() => m_Vehicle = new GameObject("vehicle");

        [TearDown]
        public void PutItAway() => Object.DestroyImmediate(m_Vehicle);

        VehicleShape Shaped(VehicleShape.Measurements measurements)
            => TestShapes.On(m_Vehicle, measurements);

        static LoadSpaceEdge Facing(VehicleShape shape, Vector3 outward)
        {
            foreach (var edge in shape.LoadSpaceEdges)
            {
                if (Vector3.Dot(edge.OutwardLocal, outward) > 0.99f)
                {
                    return edge;
                }
            }

            Assert.Fail($"no edge of the load space faces {outward}");
            return default;
        }

        [Test]
        public void ACartHasAnEdgePerSideOfItsLoadSpace()
        {
            var shape = Shaped(TestShapes.Cart());

            Assert.That(shape.LoadSpaceEdges.Count, Is.EqualTo(4),
                "a load space is a box and each of its four sides is somewhere a person could be " +
                "standing. Fewer than four and one approach to the cart silently offers nothing");
        }

        [Test]
        public void SomethingWithNoLoadSpaceHasNoEdges()
        {
            var shape = Shaped(TestShapes.Tractor());

            Assert.That(shape.LoadSpaceEdges, Is.Empty,
                "a tractor is solid through and through. Edges on it would put a climb prompt on " +
                "the side of a machine there is nothing to climb into");
        }

        [Test]
        public void ACartsSidesAreAsHighAsItsLips()
        {
            var shape = Shaped(TestShapes.Cart());

            foreach (var outward in new[] { Vector3.left, Vector3.right })
            {
                var edge = Facing(shape, outward);

                Assert.That(edge.TopMetres, Is.EqualTo(LipTopMetres).Within(0.01f),
                    $"the {outward} side of a cart is a lip a person steps over, and how high it " +
                    "stands is the whole question of whether they can. Measured as anything taller " +
                    "-- the roof, say -- and no cart is ever climbable");
            }
        }

        [Test]
        public void ACartsEndsAreAsHighAsItsWalls()
        {
            var shape = Shaped(TestShapes.Cart());

            foreach (var outward in new[] { Vector3.forward, Vector3.back })
            {
                var edge = Facing(shape, outward);

                Assert.That(edge.TopMetres, Is.EqualTo(EndWallTopMetres).Within(0.01f),
                    $"the {outward} end of a cart is a wall to the roof. Measured as the lip height " +
                    "instead, a player is invited to climb through it and lands in the bodywork");
            }
        }

        [Test]
        public void EveryEdgeKnowsTheFloorBehindIt()
        {
            var shape = Shaped(TestShapes.Cart());

            foreach (var edge in shape.LoadSpaceEdges)
            {
                Assert.That(edge.FloorMetres, Is.EqualTo(DeckTopMetres).Within(0.01f),
                    "an edge without the height of the floor behind it cannot say how far a person " +
                    "still has to fall, and a climb aimed at the wrong height either drops them " +
                    "through the deck or leaves them standing on air");
            }
        }

        [Test]
        public void AnEdgePointsOutOfTheLoadSpaceAndSitsOnItsFace()
        {
            var shape = Shaped(TestShapes.Cart());
            var interior = shape.InteriorLocal;

            var left = Facing(shape, Vector3.left);

            Assert.That(left.MiddleLocal.x, Is.EqualTo(interior.min.x).Within(0.001f),
                "the edge has to sit on the face of the load space. Anywhere else and the launch is " +
                "aimed at a line that is not where the lip is");
            Assert.That(left.LengthMetres, Is.EqualTo(interior.size.z).Within(0.001f),
                "and it runs the length of that face, because a person can be standing anywhere " +
                "along it");
        }
    }
}
