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
            m_Profile.tightestTurnRadiusMetres = 8f;
            m_Profile.fastestTurnDegreesPerSecond = 120f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Profile);

        static float RadiusAt(float speed, float yawDegreesPerSecond)
            => speed / (yawDegreesPerSecond * Mathf.Deg2Rad);

        [Test]
        public void FullLockHoldsTheRadiusItWasAskedForAtEverySpeed()
        {
            foreach (var speed in new[] { 5f, 10f, 15f })
            {
                var yaw = ArcadeHandling.YawDegreesPerSecond(1f, speed, m_Profile);

                Assert.That(RadiusAt(speed, yaw), Is.EqualTo(m_Profile.tightestTurnRadiusMetres).Within(0.1f),
                    $"at {speed:F0} m/s full lock came round at {yaw:F1} degrees a second, which is a " +
                    $"{RadiusAt(speed, yaw):F1} m circle. The figure on the profile says " +
                    $"{m_Profile.tightestTurnRadiusMetres:F0} m, and a driver who cannot predict the " +
                    "circle from the number cannot be given one");
            }
        }

        [Test]
        public void ItNeverSpinsFasterThanTheProfileAllows()
        {
            var yaw = ArcadeHandling.YawDegreesPerSecond(1f, 40f, m_Profile);

            Assert.That(yaw, Is.EqualTo(m_Profile.fastestTurnDegreesPerSecond).Within(0.01f),
                $"at 40 m/s it wanted {yaw:F0} degrees a second. Holding one radius at any speed means " +
                "spinning arbitrarily fast, so something has to cap it");
        }

        [Test]
        public void StandingStillItDoesNotTurn()
        {
            Assert.That(ArcadeHandling.YawDegreesPerSecond(1f, 0f, m_Profile), Is.EqualTo(0f).Within(0.01f),
                "a parked tractor turned its whole body when the wheel moved, which no vehicle does");
        }

        [Test]
        public void ReversingTurnsTheOtherWay()
        {
            var forward = ArcadeHandling.YawDegreesPerSecond(1f, 6f, m_Profile);
            var back = ArcadeHandling.YawDegreesPerSecond(1f, -6f, m_Profile);

            Assert.That(back, Is.EqualTo(-forward).Within(0.01f),
                $"going forward it came round at {forward:F1} and reversing at {back:F1}. Reversing " +
                "into a stand with the wheel over has to swing the back the other way");
        }

        [Test]
        public void GripPullsTheBodyBackOntoItsHeading()
        {
            var sliding = new Vector3(4f, 0f, 10f);
            var held = ArcadeHandling.HeldToItsHeading(sliding, Vector3.forward, holdsPerSecond: 3f, deltaTime: 0.1f);

            Assert.That(held.z, Is.EqualTo(10f).Within(0.01f), "it may not lose the speed it is carrying");
            Assert.That(Mathf.Abs(held.x), Is.LessThan(4f),
                $"sideways speed went from 4.00 to {held.x:F2} m/s. Grip is what turns a slide back " +
                "into travel, and none of it was applied");
        }

        [Test]
        public void NoGripAtAllIsAFullSlide()
        {
            var sliding = new Vector3(4f, 0f, 10f);
            var held = ArcadeHandling.HeldToItsHeading(sliding, Vector3.forward, holdsPerSecond: 0f, deltaTime: 0.1f);

            Assert.That(held.x, Is.EqualTo(4f).Within(0.01f),
                "with no grip the sideways speed has to survive untouched, or there is no such thing " +
                "as a drift in this model");
        }
    }
}
