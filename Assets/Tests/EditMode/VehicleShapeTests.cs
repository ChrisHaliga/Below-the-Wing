using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Where a vehicle's parts are, and what depends on knowing.
    ///
    /// The measurements here are the real ones off the baggage cart model. They matter as
    /// measurements rather than as arbitrary numbers because the cart is not symmetric: its drawbar
    /// sticks 3.16 m out in front and its socket is recessed 1.82 m behind, and the two halves of a
    /// coupling meet at different heights on purpose so that they do not try to occupy the same
    /// space. Every one of those facts breaks something that assumed a vehicle was a box.
    /// </summary>
    public sealed class VehicleShapeTests
    {
        GameObject m_CartObject;
        GameObject m_TractorObject;
        VehicleShape m_Cart;
        VehicleShape m_Tractor;

        [SetUp]
        public void SetUp()
        {
            m_CartObject = new GameObject("Cart");
            m_Cart = TestShapes.On(m_CartObject, TestShapes.Cart());

            m_TractorObject = new GameObject("Tractor");
            m_Tractor = TestShapes.On(m_TractorObject, TestShapes.Tractor());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_CartObject);
            Object.DestroyImmediate(m_TractorObject);
        }

        [Test]
        public void ACartReachesFurtherForwardThanBack()
        {
            Assert.That(m_Cart.FrontReachMetres, Is.EqualTo(3.1617f).Within(1e-3f));
            Assert.That(m_Cart.RearReachMetres, Is.EqualTo(1.8159f).Within(1e-3f));
        }

        [Test]
        public void TwoCoupledCartsStandTheSumOfTheirTwoReachesApart()
        {
            var apart = m_Cart.RearReachMetres + m_Cart.FrontReachMetres;

            Assert.That(apart, Is.EqualTo(4.9776f).Within(1e-3f),
                "doubling one reach puts them either 1.35 m too close or 2.69 m too far, and a " +
                "coupling holding a gap open drags its train about for the rest of the session");
        }

        [Test]
        public void TheTwoHalvesOfACouplingSitAtDifferentHeights()
        {
            Assert.That(m_Cart.FrontCouplingLocal.y, Is.EqualTo(0.2075f).Within(1e-3f));
            Assert.That(m_Cart.RearCouplingLocal.y, Is.EqualTo(0.1310f).Within(1e-3f));
            Assert.That(m_Cart.FrontCouplingLocal.y - m_Cart.RearCouplingLocal.y,
                Is.EqualTo(0.0766f).Within(1e-3f),
                "a real drawbar is offset vertically so the male and female halves do not intersect");
        }

        [Test]
        public void WheelbaseAndTrackComeFromWhereTheWheelsActuallyAre()
        {
            Assert.That(m_Cart.WheelbaseMetres, Is.EqualTo(3.1609f).Within(1e-3f));
            Assert.That(m_Cart.TrackMetres, Is.EqualTo(1.5851f).Within(1e-3f),
                "written down separately from the model, this is the number that put the invisible " +
                "suspension probes fourteen centimetres from the visible wheels");
        }

        [Test]
        public void ACartsOriginSitsOnTheGroundBetweenItsWheels()
        {
            foreach (var wheel in m_Cart.WheelCentresLocal)
            {
                Assert.That(wheel.y, Is.LessThan(0.3f),
                    "the origin is at ground level, so a wheel centre is one wheel radius above it");
            }

            Assert.That(m_Cart.EnvelopeCentreLocal.y, Is.GreaterThan(0.5f),
                "and the bodywork is entirely above the origin rather than centred on it");
        }

        [Test]
        public void AVehicleWithNoModelIsDescribedInExactlyTheSameTerms()
        {
            Assert.That(m_Tractor.FrontReachMetres, Is.GreaterThan(0f));
            Assert.That(m_Tractor.RearReachMetres, Is.GreaterThan(0f));
            Assert.That(m_Tractor.WheelCentresLocal.Count, Is.EqualTo(4));
            Assert.That(m_Tractor.SolidParts, Is.Not.Empty);
        }

        [Test]
        public void ACartIsHollowAndATractorIsNot()
        {
            Assert.That(m_Cart.InteriorLocal.size.magnitude, Is.GreaterThan(0f));
            Assert.That(m_Tractor.InteriorLocal.size, Is.EqualTo(Vector3.zero),
                "nothing rides inside a tractor, and an interior nobody can reach is a hole in its " +
                "side waiting to be found");
        }

        [Test]
        public void NoSolidPartOfACartStandsInsideIt()
        {
            var interior = m_Cart.InteriorLocal;

            foreach (var part in m_Cart.SolidParts)
            {
                var solid = new Bounds(part.CentreLocal, part.SizeMetres);

                Assert.That(solid.Contains(interior.center), Is.False,
                    $"'{part.Name}' fills the space bags are supposed to go in. A cart is a " +
                    "container, and one box the size of the vehicle means nothing can ever be " +
                    "inside it");
            }
        }

        [Test]
        public void ACartHasRoomInsideForSomebodyCrouching()
        {
            var crew = TestProfiles.CrewMember();

            Assert.That(m_Cart.InteriorLocal.size.y, Is.LessThan(crew.heightMetres),
                "this test is only worth anything while a standing person does not fit");
            Assert.That(m_Cart.InteriorLocal.size.y, Is.GreaterThan(crew.crouchedHeightMetres),
                "and a crouching one has to, or the cart is a box nobody can load by hand");

            Object.DestroyImmediate(crew);
        }

        [Test]
        public void ACartRoofIsOutOfJumpingReach()
        {
            var crew = TestProfiles.CrewMember();
            var roof = m_Cart.EnvelopeCentreLocal.y + (m_Cart.EnvelopeSizeMetres.y * 0.5f);

            Assert.That(roof, Is.GreaterThan(crew.jumpHeightMetres),
                "riding on top is something a player climbs to, from the drawbar or from another " +
                "cart. Reachable from a standing jump it would stop being worth doing");

            Object.DestroyImmediate(crew);
        }
    }

    /// <summary>
    /// Laying out an apron from what the equipment actually measures.
    ///
    /// The layout is where hitch spacing is decided, before any vehicle exists, so it is the first
    /// place an asymmetric cart breaks something. Its old arithmetic doubled a single reach.
    /// </summary>
    public sealed class ApronLayoutFromShapesTests
    {
        GameObject m_CartObject;
        GameObject m_TractorObject;
        VehicleShape m_Cart;
        VehicleShape m_Tractor;
        AircraftProfile m_Aircraft;

        [SetUp]
        public void SetUp()
        {
            m_CartObject = new GameObject("Cart");
            m_Cart = TestShapes.On(m_CartObject, TestShapes.Cart());

            m_TractorObject = new GameObject("Tractor");
            m_Tractor = TestShapes.On(m_TractorObject, TestShapes.Tractor());

            m_Aircraft = TestProfiles.Aircraft();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_CartObject);
            Object.DestroyImmediate(m_TractorObject);
            Object.DestroyImmediate(m_Aircraft);
        }

        static ApronLayoutSettings OneTrain(int carts)
        {
            var settings = ApronLayoutSettings.Default;
            settings.trainCount = 1;
            settings.cartsPerTrain = carts;
            return settings;
        }

        [Test]
        public void ACartIsPlacedItsOwnFrontReachBehindTheTractorsRearReach()
        {
            var plan = ApronLayout.Build(OneTrain(1), m_Tractor, m_Cart, m_Aircraft, Vector3.one);
            var train = plan.Trains[0];

            var gap = train.Tractor.Position.z - train.Carts[0].Position.z;

            Assert.That(gap, Is.EqualTo(m_Tractor.RearReachMetres + m_Cart.FrontReachMetres).Within(1e-3f));
        }

        [Test]
        public void TwoCartsBehindEachOtherStandTheirTwoReachesApart()
        {
            var plan = ApronLayout.Build(OneTrain(2), m_Tractor, m_Cart, m_Aircraft, Vector3.one);
            var carts = plan.Trains[0].Carts;

            var gap = carts[0].Position.z - carts[1].Position.z;

            Assert.That(gap, Is.EqualTo(4.9776f).Within(1e-3f),
                "spaced at twice one reach they stand either buried in each other or with every " +
                "coupling in the train stretched");
        }

        [Test]
        public void EverythingIsPlacedStandingOnTheGround()
        {
            var plan = ApronLayout.Build(OneTrain(2), m_Tractor, m_Cart, m_Aircraft, Vector3.one);
            var train = plan.Trains[0];

            Assert.That(train.Tractor.Position.y, Is.EqualTo(0f).Within(1e-4f));

            foreach (var cart in train.Carts)
            {
                Assert.That(cart.Position.y, Is.EqualTo(0f).Within(1e-4f),
                    "a vehicle's origin is on the ground now, so placing one means saying where it " +
                    "touches down rather than where the middle of its bodywork floats");
            }
        }
    }
}
