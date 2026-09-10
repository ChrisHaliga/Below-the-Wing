using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// How the steered wheels come round.
    ///
    /// A three-tonne tractor whose wheels snapped from centre to full lock in a single step would
    /// turn in ways nothing on wheels can, so the rate limit is the behaviour under test here.
    /// </summary>
    public sealed class SteeringTests
    {
        VehicleProfile m_Tractor;

        [SetUp]
        public void SetUp() => m_Tractor = TestProfiles.Tractor();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Tractor);

        [Test]
        public void OneStepOfFullLockMovesOnlyAsFarAsTheSteeringRateAllows()
        {
            const float step = 0.02f;

            var after = Steering.Step(0f, 1f, step, m_Tractor);

            Assert.That(after, Is.EqualTo(m_Tractor.steerRateDegreesPerSecond * step).Within(1e-3f));
            Assert.That(after, Is.LessThan(m_Tractor.maxSteerAngleDegrees),
                "one step at full lock must not arrive at full lock");
        }

        [Test]
        public void SteeringHeldOnEventuallyReachesTheProfilesLockAndStops()
        {
            var angle = 0f;
            for (var i = 0; i < 500; i++)
            {
                angle = Steering.Step(angle, 1f, 0.02f, m_Tractor);
            }

            Assert.That(angle, Is.EqualTo(m_Tractor.maxSteerAngleDegrees).Within(1e-3f),
                "held long enough the wheels reach the lock, and never go past it");
        }

        [Test]
        public void SteeringComesBackToCentreWhenReleased()
        {
            var angle = m_Tractor.maxSteerAngleDegrees;

            var after = Steering.Step(angle, 0f, 0.02f, m_Tractor);

            Assert.That(after, Is.LessThan(angle));
            Assert.That(after, Is.GreaterThanOrEqualTo(0f), "releasing does not throw the wheels past centre");
        }

        [Test]
        public void SteeringTheOtherWayIsRateLimitedTheSameAmount()
        {
            const float step = 0.02f;

            var after = Steering.Step(0f, -1f, step, m_Tractor);

            Assert.That(after, Is.EqualTo(-m_Tractor.steerRateDegreesPerSecond * step).Within(1e-3f));
        }
    }
}
