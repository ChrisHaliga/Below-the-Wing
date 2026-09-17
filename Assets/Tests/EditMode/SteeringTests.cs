using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class SteeringTests
    {
        VehicleProfile m_Profile;

        [SetUp]
        public void MakeAProfile()
        {
            m_Profile = ScriptableObject.CreateInstance<VehicleProfile>();
            m_Profile.maxSteerAngleDegrees = 45f;
            m_Profile.steerRateDegreesPerSecond = 1000f;
            m_Profile.topSpeedMetresPerSecond = 20f;
            m_Profile.lateralGripCurve = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(3f, 12f), new Keyframe(12f, 5f));
        }

        [TearDown]
        public void PutItAway() => Object.DestroyImmediate(m_Profile);

        float SettlesAt(float steer, float speed)
        {
            var angle = 0f;
            for (var i = 0; i < 200; i++)
            {
                angle = Steering.Step(angle, steer, speed, 0.02f, m_Profile);
            }

            return angle;
        }

        [Test]
        public void AtACrawlTheWheelsGoAllTheWayOver()
        {
            Assert.That(SettlesAt(steer: 1f, speed: 0.5f),
                Is.EqualTo(m_Profile.maxSteerAngleDegrees).Within(0.5f),
                "manoeuvring a tractor between carts happens at walking pace and needs every degree " +
                "of lock there is. Held back at a crawl, a machine that could turn in its own length " +
                "needs three attempts to line up on a hitch");
        }

        [Test]
        public void AtSpeedTheWheelsStopShortOfFullLock()
        {
            var atSpeed = SettlesAt(steer: 1f, speed: m_Profile.topSpeedMetresPerSecond);

            Assert.That(atSpeed, Is.LessThan(m_Profile.maxSteerAngleDegrees),
                $"the wheels went to {atSpeed:F1} degrees at full speed. A tyre makes its sideways " +
                "force out of sliding a little: dragged sideways far past that, it pushes with less " +
                "than half of what it has. Full lock at speed is a front axle scrubbing, and the " +
                "tractor carries straight on with its wheels turned");
        }

        [Test]
        public void MoreLockIsNeverLessSteering()
        {
            var speed = 12f;

            var gentle = SettlesAt(steer: 0.3f, speed: speed);
            var firm = SettlesAt(steer: 0.7f, speed: speed);
            var everything = SettlesAt(steer: 1f, speed: speed);

            Assert.That(firm, Is.GreaterThanOrEqualTo(gentle - 0.01f),
                $"asking for more gave less: {gentle:F1} degrees at a third, {firm:F1} at two thirds. " +
                "A control that answers less the harder it is pushed is one a player cannot learn");
            Assert.That(everything, Is.GreaterThanOrEqualTo(firm - 0.01f),
                $"and {everything:F1} degrees at full lock against {firm:F1} at two thirds");
        }

        [Test]
        public void TheWheelsStillTakeTimeToSwingOver()
        {
            m_Profile.steerRateDegreesPerSecond = 120f;

            var afterOneStep = Steering.Step(0f, 1f, 0.5f, 0.02f, m_Profile);

            Assert.That(afterOneStep, Is.EqualTo(120f * 0.02f).Within(0.01f),
                "the wheels swing over at the profile's steer rate. Snapped straight to their " +
                "limit instead, a tractor changes direction in a single step and whatever it is " +
                "towing is left to catch up through its coupling");
        }
    }
}
