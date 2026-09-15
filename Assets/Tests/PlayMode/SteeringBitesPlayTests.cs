using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class SteeringBitesPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_Profile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron(400f);
            m_Profile = TestProfiles.Tractor();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_Profile);
        }

        VehicleController ATractor()
            => m_Apron.AddVehicle(
                m_Profile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity, TestShapes.Tractor());

        static IEnumerator UpToSpeed(VehicleController tractor)
        {
            tractor.IntentSource = new FixedIntent(steer: 0f, throttle: 1f);

            var steps = Mathf.CeilToInt(10f / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator AtSpeedTheWheelsStopShortOfFullLock()
        {
            var tractor = ATractor();
            yield return UpToSpeed(tractor);

            tractor.IntentSource = new FixedIntent(steer: 1f, throttle: 1f);
            yield return Steps.Seconds(1.5f);

            Assert.That(Mathf.Abs(tractor.SteerAngleDegrees),
                Is.LessThan(m_Profile.maxSteerAngleDegrees - 1f),
                $"at {tractor.Body.linearVelocity.magnitude:F1} m/s the wheels went to " +
                $"{tractor.SteerAngleDegrees:F1} of {m_Profile.maxSteerAngleDegrees} degrees. Full " +
                "lock at speed drags the front tyres sideways far past the slip they grip hardest " +
                "at, and a scrubbing front axle pushes with less than half of what it has");
        }

        [UnityTest]
        public IEnumerator AtACrawlTheWheelsStillGoAllTheWayOver()
        {
            var tractor = ATractor();
            yield return Steps.Seconds(1f);

            tractor.IntentSource = new FixedIntent(steer: 1f, throttle: 0.05f);
            yield return Steps.Seconds(2f);

            Assert.That(Mathf.Abs(tractor.SteerAngleDegrees),
                Is.EqualTo(m_Profile.maxSteerAngleDegrees).Within(1f),
                $"at {tractor.Body.linearVelocity.magnitude:F2} m/s the wheels only reached " +
                $"{tractor.SteerAngleDegrees:F1} degrees. Lining a tractor up on a cart happens at " +
                "walking pace and wants every degree there is");
        }

        [UnityTest]
        public IEnumerator AtSpeedAFullLockCornerComesRoundInsideThirtyMetres()
        {
            var tractor = ATractor();
            yield return UpToSpeed(tractor);

            tractor.IntentSource = new FixedIntent(steer: 1f, throttle: 1f);
            yield return Steps.Seconds(4f);

            var speed = tractor.Body.linearVelocity.magnitude;
            var comingRound = Mathf.Abs(tractor.Body.angularVelocity.y);
            var radius = comingRound > 0.001f ? speed / comingRound : float.PositiveInfinity;

            Assert.That(radius, Is.LessThan(30f),
                $"wound fully over at {speed:F1} m/s the tractor takes {radius:F1} m to come round, " +
                $"turning at {comingRound * Mathf.Rad2Deg:F1} degrees a second. A tractor that needs " +
                "half an apron to change direction is one a driver steers by stopping first");
            Assert.That(speed, Is.GreaterThan(10f),
                $"it came round in {radius:F1} m by scrubbing itself down to {speed:F1} m/s. A corner " +
                "that costs all the speed is a handbrake turn, not a corner");
        }

        [UnityTest]
        public IEnumerator AtSpeedTheTractorUsesMostOfTheGripItsTyresHave()
        {
            var tractor = ATractor();
            yield return UpToSpeed(tractor);

            tractor.IntentSource = new FixedIntent(steer: 1f, throttle: 1f);
            yield return Steps.Seconds(3f);

            var speed = tractor.Body.linearVelocity.magnitude;
            var comingRound = Mathf.Abs(tractor.Body.angularVelocity.y);

            var pullingSideways = speed * comingRound;

            const float onTheSteeredWheels = 0.5f;
            var theTyresCouldPull =
                WheelPhysics.MostGripPerKilogram(m_Profile) * onTheSteeredWheels;

            Assert.That(pullingSideways, Is.GreaterThan(theTyresCouldPull * 0.5f),
                $"steadied on the lock it settles at, the tractor is pulling {pullingSideways:F2} m/s^2 " +
                $"sideways at {speed:F1} m/s, and its front tyres between them have about " +
                $"{theTyresCouldPull:F2} to give. Throwing half of that away is a machine whose " +
                "wheels are turned and whose path is not: the driver winds on more lock, the tyres " +
                "scrub harder, and the tractor goes straighter");
        }
    }
}
