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
        public void AtSpeedTheWheelsStillTurnFarEnoughToBeWorthTurning()
        {
            var atSpeed = SettlesAt(steer: 1f, speed: m_Profile.topSpeedMetresPerSecond);

            Assert.That(atSpeed, Is.GreaterThan(15f),
                $"at top speed the wheels may only reach {atSpeed:F1} degrees. Held to the " +
                "slip a tyre grips hardest at, a machine at speed is steering on a sliver of " +
                "lock and a driver reads that as a wheel that does nothing");
        }

        [Test]
        public void TheLockFallsOffEvenlyWithSpeedRatherThanCollapsing()
        {
            var top = m_Profile.topSpeedMetresPerSecond;

            var quarter = Steering.AsFarAsItMayTurnAt(top * 0.25f, m_Profile);
            var half = Steering.AsFarAsItMayTurnAt(top * 0.5f, m_Profile);
            var threeQuarters = Steering.AsFarAsItMayTurnAt(top * 0.75f, m_Profile);

            var first = m_Profile.maxSteerAngleDegrees - quarter;
            var second = quarter - half;
            var third = half - threeQuarters;

            Assert.That(second, Is.EqualTo(first).Within(0.5f),
                $"the lock gave up {first:F1} degrees over the first quarter of the speed range and " +
                $"{second:F1} over the second. A limit that collapses early leaves a driver with a " +
                "wheel that stops answering the moment they are moving at all");
            Assert.That(third, Is.EqualTo(second).Within(0.5f));
        }

        [Test]
        public void AtTopSpeedTheLockIsTheFigureTheProfileNames()
        {
            var atTop = Steering.AsFarAsItMayTurnAt(m_Profile.topSpeedMetresPerSecond, m_Profile);

            Assert.That(atTop, Is.EqualTo(m_Profile.steerLockAtTopSpeedDegrees).Within(0.01f),
                $"at top speed the wheels may reach {atTop:F1} degrees and the profile names " +
                $"{m_Profile.steerLockAtTopSpeedDegrees}. This figure is how much wheel a driver has " +
                "at speed, and it is chosen rather than derived");
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
        public void TheLockAtSpeedKeepsTheTyresNearTheGripTheyPeakAt()
        {
            var speed = m_Profile.topSpeedMetresPerSecond;
            var angle = SettlesAt(steer: 1f, speed: speed);

            var slidingSideways = speed * Mathf.Sin(angle * Mathf.Deg2Rad);
            var peak = m_Profile.SlipItGripsHardestAt;

            Assert.That(slidingSideways, Is.GreaterThan(peak),
                $"on the lock it settles at, the front tyres slide sideways at {slidingSideways:F2} m/s " +
                $"and they grip hardest at {peak:F2}. The lock at speed is chosen for how much wheel " +
                "a driver wants rather than for what the tyres make the most force at, so this " +
                "records that the two no longer agree");
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
                "the wheels swing over at the rate they always did. Snapped straight to their limit " +
                "instead, a tractor changes direction in a single step and whatever it is towing is " +
                "left to catch up through its coupling");
        }
    }
}
