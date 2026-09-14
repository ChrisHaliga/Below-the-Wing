using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class CartOnTheGroundPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_CartProfile;
        VehicleProfile m_TractorProfile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_CartProfile = TestProfiles.Cart();
            m_TractorProfile = TestProfiles.Tractor();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_CartProfile);
            Object.DestroyImmediate(m_TractorProfile);
        }

        VehicleController ACart(Vector3 at)
            => m_Apron.AddVehicle(m_CartProfile, "Cart 1", at, Quaternion.identity, TestShapes.Cart());

        [UnityTest]
        public IEnumerator ACartPlacedOnTheGroundStaysOnTheGround()
        {
            var cart = ACart(Vector3.zero);

            yield return Steps.Seconds(2f);

            Assert.That(cart.transform.position.y, Is.EqualTo(0f).Within(0.03f),
                $"settled at {cart.transform.position.y:F3} m. The origin is ground level now, so a " +
                "cart put down at zero has to stay at about zero -- anything else means it was " +
                "dropped from a height or buried in the apron");
        }

        [UnityTest]
        public IEnumerator ACartsWheelsMeetTheTarmac()
        {
            var cart = ACart(Vector3.zero);
            var shape = cart.GetComponent<VehicleShape>();

            yield return Steps.Seconds(2f);

            foreach (var wheel in shape.Wheels)
            {
                var centre = cart.transform.TransformPoint(wheel.CentreLocal);
                var bottom = centre.y - wheel.RadiusMetres;

                Assert.That(bottom, Is.EqualTo(0f).Within(0.06f),
                    $"a wheel's underside is at {bottom:F3} m. Floating wheels and buried wheels " +
                    "look equally wrong, and on a cart with 0.157 m wheels there is very little " +
                    "room to be wrong in");
            }
        }

        [UnityTest]
        public IEnumerator ACartAtRestSitsPartlyCompressedRatherThanAtFullExtension()
        {
            var cart = ACart(Vector3.zero);

            yield return Steps.Seconds(2f);

            var expected = VehicleController.SuspensionCompressionAtRest(m_CartProfile);

            Assert.That(expected, Is.InRange(0.05f, 0.4f),
                "a cart resting at full extension has nothing left to absorb a bump, and one " +
                "resting bottomed out has nothing left to give");
        }

        [UnityTest]
        public IEnumerator TwoDifferentVehiclesStandingCoupledNeitherLean()
        {
            var train = m_Apron.AddTrain(
                m_TractorProfile, m_CartProfile, cartCount: 2, Vector3.zero,
                tractorShape: TestShapes.Tractor(), cartShape: TestShapes.Cart());

            yield return Steps.Seconds(4f);

            foreach (var member in train.Members)
            {
                var lean = Vector3.Angle(member.transform.up, Vector3.up);

                Assert.That(lean, Is.LessThan(4f),
                    $"'{member.DisplayName}' is leaning {lean:F1} degrees. A coupling that holds two " +
                    "hitches apart vertically leaves both vehicles leaning, and a leaning vehicle " +
                    "has its suspension pushing sideways -- so a parked train wanders off across " +
                    "the apron under a force nobody applied");
            }
        }

        [UnityTest]
        public IEnumerator ATrainStandsAtTheDistanceItsCouplingsActuallyReach()
        {
            var train = m_Apron.AddTrain(
                m_TractorProfile, m_CartProfile, cartCount: 2, Vector3.zero,
                tractorShape: TestShapes.Tractor(), cartShape: TestShapes.Cart());

            yield return Steps.Seconds(4f);

            for (var i = 0; i < train.Members.Count - 1; i++)
            {
                var inFront = train.Members[i];
                var behind = train.Members[i + 1];

                var theirEnd = inFront.transform.TransformPoint(inFront.RearHitchLocal.Value);
                var ourEnd = behind.transform.TransformPoint(behind.FrontHitchLocal.Value);

                var apart = Vector3.ProjectOnPlane(theirEnd - ourEnd, Vector3.up).magnitude;

                Assert.That(apart, Is.LessThan(0.1f),
                    $"'{behind.DisplayName}' is hitched {apart:F2} m from where its coupling reaches. " +
                    "A joint holding a gap open pulls its train about for the rest of the session");
            }
        }
    }

    public sealed class WheelLookPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_CartProfile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_CartProfile = TestProfiles.Cart();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_CartProfile);
        }

        (VehicleController cart, WheelLook look, Transform[] wheels) ACartWithWheels()
        {
            var cart = m_Apron.AddVehicle(
                m_CartProfile, "Cart 1", Vector3.zero, Quaternion.identity, TestShapes.Cart());

            var model = new GameObject("Look").transform;
            model.SetParent(cart.transform, worldPositionStays: false);
            model.localScale = Vector3.one * 100f;
            model.localRotation = Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(270f, 0f, 0f);

            var shape = cart.GetComponent<VehicleShape>();
            var wheels = new Transform[shape.Wheels.Count];

            for (var i = 0; i < wheels.Length; i++)
            {
                var wheel = new GameObject($"Wheel_{i + 1}").transform;
                wheel.SetParent(model, worldPositionStays: false);
                wheel.localRotation = Quaternion.Euler(0f, 270f, 270f);
                wheel.position = cart.transform.TransformPoint(shape.Wheels[i].CentreLocal);
                wheels[i] = wheel;
            }

            var look = cart.gameObject.AddComponent<WheelLook>();
            look.Watch(wheels);

            return (cart, look, wheels);
        }

        [UnityTest]
        public IEnumerator AStandingCartDoesNotTurnItsWheels()
        {
            var (_, look, _) = ACartWithWheels();

            yield return Steps.Seconds(2f);

            Assert.That(look.TurnedDegrees(0), Is.EqualTo(0f).Within(1f),
                "wheels creeping round on a parked cart is what an angle accumulated from noise " +
                "looks like");
        }

        [UnityTest]
        public IEnumerator ADrivingCartTurnsItsWheelsAtRoadSpeed()
        {
            var (cart, look, _) = ACartWithWheels();
            yield return Steps.Seconds(1f);

            cart.Body.linearVelocity = new Vector3(0f, 0f, 4f);
            var before = look.TurnedDegrees(0);

            yield return Steps.Seconds(1f);

            var expected = 4f / cart.GetComponent<VehicleShape>().Wheels[0].RadiusMetres * Mathf.Rad2Deg;

            Assert.That(look.TurnedDegrees(0) - before, Is.EqualTo(expected).Within(expected * 0.25f),
                "a wheel that turns at anything but road speed reads as the cart skidding " +
                "everywhere it goes");
        }

        [UnityTest]
        public IEnumerator AWheelStaysOnTheTarmacWhileTheBodySinksOntoIt()
        {
            var (cart, _, wheels) = ACartWithWheels();
            var shape = cart.GetComponent<VehicleShape>();

            yield return Steps.Seconds(2f);

            for (var i = 0; i < wheels.Length; i++)
            {
                var bottom = wheels[i].position.y - shape.Wheels[i].RadiusMetres;

                Assert.That(bottom, Is.EqualTo(0f).Within(0.04f),
                    $"the visible wheel's underside is at {bottom:F3} m. Parented rigidly to the " +
                    "body it sinks by however far the suspension compressed, which on this cart is " +
                    "most of the wheel");

                var where = cart.transform.InverseTransformPoint(wheels[i].position);
                var axle = shape.Wheels[i].CentreLocal;
                Assert.That(new Vector2(where.x - axle.x, where.z - axle.z).magnitude, Is.LessThan(0.02f),
                    $"wheel {i + 1} is drawn {where} but its axle is at {axle}. A height written as " +
                    "a coordinate in the model's own frame is a distance along whichever axis the " +
                    "model happens to point that way, a hundred times over");
            }
        }

        [UnityTest]
        public IEnumerator AWheelKeepsTheWayItWasModelledAndTurnsAboutItsAxle()
        {
            var (cart, _, wheels) = ACartWithWheels();
            var authored = Quaternion.Inverse(cart.transform.rotation) * wheels[0].rotation;

            yield return Steps.Seconds(1f);

            var standingStill = Quaternion.Inverse(cart.transform.rotation) * wheels[0].rotation;
            Assert.That(Quaternion.Angle(standingStill, authored), Is.LessThan(1f),
                "a wheel on a parked cart is exactly as it was modelled: the way it faces was " +
                "authored in the model's frame, and overwriting it with a spin about the cart's " +
                "axis lays the wheel flat");

            cart.Body.linearVelocity = new Vector3(0f, 0f, 2f);
            yield return Steps.Seconds(0.5f);

            var rolling = Quaternion.Inverse(cart.transform.rotation) * wheels[0].rotation;
            var turnedBy = rolling * Quaternion.Inverse(authored);
            turnedBy.ToAngleAxis(out var degrees, out var axis);

            Assert.That(degrees, Is.GreaterThan(5f), "it has to have turned at all");
            Assert.That(Mathf.Abs(axis.x), Is.GreaterThan(0.9f),
                $"a rolling wheel turns about the cart's sideways axis; this one turned about {axis}");
        }
    }
}
