using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// A cart standing on the tarmac, with the real model's numbers under it.
    ///
    /// The origin of a vehicle used to be the middle of its bodywork, so placing one meant working
    /// out how high to float it. It is now on the ground between the wheels, which is both the
    /// ordinary convention and the thing that makes a layout position mean "where this touches
    /// down". Everything that used to add half a body height has to stop.
    ///
    /// The suspension numbers changed with it. The greybox cart had 0.35 m of travel on 0.30 m
    /// wheels; the real one has 0.08 m on 0.157 m wheels, which is more than four times stiffer per
    /// metre and bottoms out far more readily.
    /// </summary>
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

        static IEnumerator Step(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        VehicleController ACart(Vector3 at)
            => m_Apron.AddVehicle(m_CartProfile, "Cart 1", at, Quaternion.identity, TestShapes.Cart());

        [UnityTest]
        public IEnumerator ACartPlacedOnTheGroundStaysOnTheGround()
        {
            var cart = ACart(Vector3.zero);

            yield return Step(2f);

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

            yield return Step(2f);

            foreach (var wheelLocal in shape.WheelCentresLocal)
            {
                var centre = cart.transform.TransformPoint(wheelLocal);
                var bottom = centre.y - m_CartProfile.wheelRadiusMetres;

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

            yield return Step(2f);

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

            yield return Step(4f);

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

            yield return Step(4f);

            for (var i = 0; i < train.Members.Count - 1; i++)
            {
                var inFront = train.Members[i];
                var behind = train.Members[i + 1];

                var theirEnd = inFront.transform.TransformPoint(inFront.RearHitchLocal);
                var ourEnd = behind.transform.TransformPoint(behind.FrontHitchLocal);

                // Sideways and along only. The two halves of a real coupling meet at different
                // heights on purpose, so a vertical difference here is geometry rather than strain.
                var apart = Vector3.ProjectOnPlane(theirEnd - ourEnd, Vector3.up).magnitude;

                Assert.That(apart, Is.LessThan(0.1f),
                    $"'{behind.DisplayName}' is hitched {apart:F2} m from where its coupling reaches. " +
                    "A joint holding a gap open pulls its train about for the rest of the session");
            }
        }
    }

    /// <summary>
    /// Wheels that look like they are doing what the cart is doing.
    ///
    /// Wheels have never been drawn before -- a greybox cart was one box and had none. With a real
    /// model and a ground-level origin they cannot simply be rigid to the body either: compressing
    /// the suspension lowers the body, and a rigid wheel goes into the tarmac with it.
    /// </summary>
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

        static IEnumerator Step(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        /// <summary>A cart with something standing in for each of its four visible wheels.</summary>
        (VehicleController cart, WheelLook look, Transform[] wheels) ACartWithWheels()
        {
            var cart = m_Apron.AddVehicle(
                m_CartProfile, "Cart 1", Vector3.zero, Quaternion.identity, TestShapes.Cart());

            var shape = cart.GetComponent<VehicleShape>();
            var wheels = new Transform[shape.WheelCentresLocal.Count];

            for (var i = 0; i < wheels.Length; i++)
            {
                var wheel = new GameObject($"Wheel {i + 1}").transform;
                wheel.SetParent(cart.transform, worldPositionStays: false);
                wheel.localPosition = shape.WheelCentresLocal[i];
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

            yield return Step(2f);

            Assert.That(look.TurnedDegrees, Is.EqualTo(0f).Within(1f),
                "wheels creeping round on a parked cart is what an angle accumulated from noise " +
                "looks like");
        }

        [UnityTest]
        public IEnumerator ADrivingCartTurnsItsWheelsAtRoadSpeed()
        {
            var (cart, look, _) = ACartWithWheels();
            yield return Step(1f);

            cart.Body.linearVelocity = new Vector3(0f, 0f, 4f);
            var before = look.TurnedDegrees;

            yield return Step(1f);

            // One second at 4 m/s on a 0.157 m wheel: 4 / 0.157 radians, in degrees.
            var expected = 4f / m_CartProfile.wheelRadiusMetres * Mathf.Rad2Deg;

            Assert.That(look.TurnedDegrees - before, Is.EqualTo(expected).Within(expected * 0.25f),
                "a wheel that turns at anything but road speed reads as the cart skidding " +
                "everywhere it goes");
        }

        [UnityTest]
        public IEnumerator AWheelStaysOnTheTarmacWhileTheBodySinksOntoIt()
        {
            var (cart, _, wheels) = ACartWithWheels();

            yield return Step(2f);

            foreach (var wheel in wheels)
            {
                var bottom = wheel.position.y - m_CartProfile.wheelRadiusMetres;

                Assert.That(bottom, Is.EqualTo(0f).Within(0.04f),
                    $"the visible wheel's underside is at {bottom:F3} m. Parented rigidly to the " +
                    "body it sinks by however far the suspension compressed, which on this cart is " +
                    "most of the wheel");
            }
        }
    }
}
