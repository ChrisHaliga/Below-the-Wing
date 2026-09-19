using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class ArcadeHandlingTests
    {
        VehicleProfile m_Profile;

        [SetUp]
        public void SetUp()
        {
            m_Profile = TestProfiles.Tractor();
            m_Profile.maxSteerAngleDegrees = 60f;
            m_Profile.fastestTurnDegreesPerSecond = 240f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Profile);

        const float Wheelbase = 1.515f;

        static float RadiusAt(float speed, float yawDegreesPerSecond)
            => speed / (yawDegreesPerSecond * Mathf.Deg2Rad);

        [Test]
        public void FullLockTurnsAsTightlyAsItsWheelsAndWheelbaseAllow()
        {
            var geometric = Wheelbase / Mathf.Tan(m_Profile.maxSteerAngleDegrees * Mathf.Deg2Rad);
            var yaw = ArcadeHandling.YawDegreesPerSecond(1f, 2f, Wheelbase, m_Profile);

            Assert.That(RadiusAt(2f, yaw), Is.EqualTo(geometric).Within(0.05f),
                $"at a crawl with {m_Profile.maxSteerAngleDegrees:F0} degrees of lock and a " +
                $"{Wheelbase:F2} m wheelbase the circle should be {geometric:F2} m, and it came round " +
                $"on {RadiusAt(2f, yaw):F2} m. The number a driver asks for is the wheel angle");
        }

        [Test]
        public void ItNeverSpinsFasterThanTheProfileAllows()
        {
            var yaw = ArcadeHandling.YawDegreesPerSecond(1f, 40f, Wheelbase, m_Profile);

            Assert.That(yaw, Is.EqualTo(m_Profile.fastestTurnDegreesPerSecond).Within(0.01f),
                $"at 40 m/s it wanted {yaw:F0} degrees a second. Holding one radius at any speed means " +
                "spinning arbitrarily fast, so something has to cap it");
        }

        [Test]
        public void StandingStillItDoesNotTurn()
        {
            Assert.That(ArcadeHandling.YawDegreesPerSecond(1f, 0f, Wheelbase, m_Profile), Is.EqualTo(0f).Within(0.01f),
                "a parked tractor turned its whole body when the wheel moved, which no vehicle does");
        }

        [Test]
        public void ReversingTurnsTheOtherWay()
        {
            var forward = ArcadeHandling.YawDegreesPerSecond(1f, 6f, Wheelbase, m_Profile);
            var back = ArcadeHandling.YawDegreesPerSecond(1f, -6f, Wheelbase, m_Profile);

            Assert.That(back, Is.EqualTo(-forward).Within(0.01f),
                $"going forward it came round at {forward:F1} and reversing at {back:F1}. Reversing " +
                "into a stand with the wheel over has to swing the back the other way");
        }

        [Test]
        public void GripPullsTheBodyBackOntoItsHeading()
        {
            var sliding = new Vector3(4f, 0f, 10f);
            var held = ArcadeHandling.HeldToItsHeading(sliding, Vector3.forward, holdsPerSecond: 3f, mostSideGripMetresPerSecondSquared: 100f, deltaTime: 0.1f);

            Assert.That(held.magnitude, Is.EqualTo(sliding.magnitude).Within(0.01f),
                $"it went into the corner at {sliding.magnitude:F2} m/s and came out at " +
                $"{held.magnitude:F2}. Grip turns a slide into travel; it does not scrub the speed off");
            Assert.That(Vector3.Angle(held, Vector3.forward), Is.LessThan(Vector3.Angle(sliding, Vector3.forward)),
                "it is travelling no closer to where it is pointed than it was, so no grip was applied");
        }

        [Test]
        public void ReversingStraightBackStaysStraightBack()
        {
            var reversing = new Vector3(0f, 0f, -5f);

            var held = ArcadeHandling.HeldToItsHeading(
                reversing, Vector3.forward, holdsPerSecond: 3f,
                mostSideGripMetresPerSecondSquared: 20f, deltaTime: 0.02f);

            Assert.That(held.x, Is.EqualTo(0f).Within(0.01f),
                $"reversing straight back at 5 m/s, grip pushed {held.x:F2} m/s of sideways speed " +
                "into it in one step. Velocity and heading are opposite when reversing, and there " +
                "is no defined axis to rotate one onto the other about, so the body slides off to " +
                "whichever side the rotation happens to pick");

            Assert.That(held.z, Is.LessThan(0f),
                $"reversing at 5 m/s came out at {held.z:F2} m/s along the heading. Grip holds a " +
                "body to the line it is travelling, and must not turn it round to face the way it " +
                "is pointed");
        }

        [Test]
        public void ReversingAtAnAngleIsPulledBackOntoTheLineItBacksAlong()
        {
            var sliding = new Vector3(2f, 0f, -5f);

            var held = ArcadeHandling.HeldToItsHeading(
                sliding, Vector3.forward, holdsPerSecond: 3f,
                mostSideGripMetresPerSecondSquared: 100f, deltaTime: 0.1f);

            Assert.That(Mathf.Abs(held.x), Is.LessThan(Mathf.Abs(sliding.x)),
                $"backing at an angle, the sideways {sliding.x:F2} m/s came out as {held.x:F2}. " +
                "Grip works the same way in reverse as forwards");

            Assert.That(held.magnitude, Is.EqualTo(sliding.magnitude).Within(0.01f),
                "grip turns a slide into travel rather than scrubbing the speed off");
        }

        [Test]
        public void NoGripAtAllIsAFullSlide()
        {
            var sliding = new Vector3(4f, 0f, 10f);
            var held = ArcadeHandling.HeldToItsHeading(sliding, Vector3.forward, holdsPerSecond: 0f, mostSideGripMetresPerSecondSquared: 100f, deltaTime: 0.1f);

            Assert.That(held.x, Is.EqualTo(4f).Within(0.01f),
                "with no grip the sideways speed has to survive untouched, or there is no such thing " +
                "as a drift in this model");
        }
    }
}
