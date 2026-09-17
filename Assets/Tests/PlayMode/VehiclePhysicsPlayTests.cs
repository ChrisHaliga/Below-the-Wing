using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class VehiclePhysicsPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;
        VehicleProfile m_CartProfile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
            m_CartProfile = TestProfiles.Cart();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
            Object.DestroyImmediate(m_CartProfile);
        }

        [UnityTest]
        public IEnumerator ATractorDroppedOntoTheApronSettlesAndStaysThere()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 2f, 0f), Quaternion.identity);

            yield return Steps.Seconds(4f);
            var settled = tractor.transform.position.y;
            yield return Steps.Seconds(1f);
            var later = tractor.transform.position.y;

            Assert.That(settled, Is.GreaterThan(0f), "the tractor has sunk through the apron");
            Assert.That(settled, Is.LessThan(2f), "the tractor never came down");
            Assert.That(later, Is.EqualTo(settled).Within(0.02f),
                "a suspension still bouncing after four seconds is undamped, and the vehicle will never feel settled");
        }

        [UnityTest]
        public IEnumerator ATractorStandsLevelRatherThanLeaningOnOneCorner()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);

            yield return Steps.Seconds(4f);

            var lean = Vector3.Angle(tractor.transform.up, Vector3.up);
            Assert.That(lean, Is.LessThan(3f), "four equal corners on flat ground should hold the body level");
        }

        [UnityTest]
        public IEnumerator OpeningTheThrottleMovesATractorForward()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);
            yield return Steps.Seconds(2f);
            var from = tractor.transform.position;

            tractor.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(3f);

            var travelled = Vector3.Dot(tractor.transform.position - from, tractor.transform.forward);
            Assert.That(travelled, Is.GreaterThan(3f), "three seconds at full throttle should get a tug moving");
        }

        [UnityTest]
        public IEnumerator ATractorReleasedFromTheThrottleCoastsToAStopOnItsOwn()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);
            yield return Steps.Seconds(2f);

            tractor.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(3f);
            Assert.That(tractor.Body.linearVelocity.magnitude, Is.GreaterThan(2f), "it should be moving by now");

            tractor.IntentSource = new FixedIntent();
            yield return Steps.Seconds(10f);

            Assert.That(tractor.Body.linearVelocity.magnitude, Is.LessThan(0.1f),
                "letting go of the throttle has to bring a tractor to rest without touching the brake; " +
                "one that coasts for ever never settles and never stops costing solver time");
        }

        [UnityTest]
        public IEnumerator BrakingBringsAMovingTractorToAStopWithoutDrivingItBackwards()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);
            yield return Steps.Seconds(2f);
            tractor.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(3f);

            var whereItStartedBraking = tractor.transform.position;
            tractor.IntentSource = new FixedIntent(brake: 1f);
            yield return Steps.Seconds(4f);

            var afterBraking = Vector3.Dot(tractor.transform.position - whereItStartedBraking, tractor.transform.forward);
            Assert.That(tractor.Body.linearVelocity.magnitude, Is.LessThan(0.5f), "it should have stopped");
            Assert.That(afterBraking, Is.GreaterThan(-0.5f),
                "braking must not reverse the vehicle back past where it began braking");
        }

        [UnityTest]
        public IEnumerator AVehicleSomebodyElseOwnsCanStillBeWalkedInto()
        {
            var crewProfile = TestProfiles.CrewMember();
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);
            var crew = m_Apron.AddCrew(crewProfile, new Vector3(0f, 1.5f, -4f));
            yield return Steps.Seconds(2f);

            tractor.OursToMove = false;
            yield return Steps.Seconds(1f);

            crew.IntentSource = new HeldKeys(new Vector2(0f, 1f));
            yield return Steps.Seconds(4f);

            var reachedTheTractor = crew.transform.position.z;
            Assert.That(reachedTheTractor, Is.LessThan(tractor.transform.position.z),
                "the character walked clean through a vehicle somebody else owns. A copy that cannot " +
                "be collided with -- or one made kinematic, which has infinite mass -- is the whole " +
                "reason this game does not use NetworkRigidbody");

            Object.DestroyImmediate(crewProfile);
        }

        [UnityTest]
        public IEnumerator ACartAndATractorAreTheSameComponentBehavingDifferentlyBecauseOfTheirProfiles()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);
            var cart = m_Apron.AddVehicle(m_CartProfile, "Cart 1-1", new Vector3(20f, 1f, 0f), Quaternion.identity);
            yield return Steps.Seconds(2f);

            Assert.That(cart.Body.mass, Is.EqualTo(m_CartProfile.massKg).Within(0.01f));
            Assert.That(tractor.Body.mass, Is.EqualTo(m_TractorProfile.massKg).Within(0.01f));

            var tractorFrom = tractor.transform.position;
            var cartFrom = cart.transform.position;
            tractor.IntentSource = new FixedIntent(throttle: 1f);
            cart.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(3f);

            Assert.That(Vector3.Distance(tractor.transform.position, tractorFrom), Is.GreaterThan(3f));
            Assert.That(Vector3.Distance(cart.transform.position, cartFrom), Is.LessThan(0.5f),
                "a cart has no engine, and flooring a throttle it does not have should move it nowhere");
        }

        [UnityTest]
        public IEnumerator FullThrottleGetsATractorToEightMetresASecondInABitOverASecond()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);
            yield return Steps.Seconds(2f);

            tractor.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(1.2f);

            Assert.That(tractor.Body.linearVelocity.magnitude, Is.GreaterThan(8f),
                $"{tractor.Body.linearVelocity.magnitude:F1} m/s after 1.2 s of full throttle. A tractor " +
                "that pulls away like a loaded lorry makes the whole apron feel like treacle");
        }

        [UnityTest]
        public IEnumerator ATractorIsStillBeingShovedHardHalfASecondOffTheLine()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);
            yield return Steps.Seconds(2f);

            tractor.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(0.5f);

            var atHalfASecond = tractor.Body.linearVelocity.magnitude;

            yield return Steps.Seconds(0.3f);

            var reached = tractor.Body.linearVelocity.magnitude;

            Assert.That(atHalfASecond, Is.GreaterThan(10f),
                $"half a second of full throttle put it at {atHalfASecond:F1} m/s. A shove that is " +
                "spent by the time a driver has noticed it is a number in a profile rather than " +
                "something anybody feels");
            Assert.That(reached, Is.GreaterThan(14f),
                $"{reached:F1} m/s after eight tenths of a second, against a top speed of " +
                $"{m_TractorProfile.topSpeedMetresPerSecond:F0}. The shove has to carry into the " +
                "speed a driver actually crosses the apron at, not fade out at walking pace");
        }

        [UnityTest]
        public IEnumerator ATractorKicksHardEnoughToThrowTheDriverBackInTheSeat()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);
            yield return Steps.Seconds(2f);

            tractor.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(0.1f);

            var afterATenth = tractor.Body.linearVelocity.magnitude;

            Assert.That(afterATenth / 0.1f, Is.GreaterThan(28f),
                $"a tenth of a second of full throttle gained {afterATenth:F2} m/s, which is " +
                $"{afterATenth / 0.1f:F0} m/s^2. This figure is chosen for how it feels to press " +
                "the throttle and nothing else, and no tug on any apron accelerates like this");
        }

        [UnityTest]
        public IEnumerator ATractorNeverExceedsItsTopSpeedAndGetsCloseToIt()
        {
            m_Apron.TearDown();
            m_Apron = new TestApron(sizeMetres: 1000f);
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);
            yield return Steps.Seconds(2f);

            tractor.IntentSource = new FixedIntent(throttle: 1f);
            var fastest = 0f;
            for (var i = 0; i < 500; i++)
            {
                yield return new WaitForFixedUpdate();
                fastest = Mathf.Max(fastest, tractor.Body.linearVelocity.magnitude);
            }

            var top = m_TractorProfile.topSpeedMetresPerSecond;
            Assert.That(fastest, Is.LessThanOrEqualTo(top + 0.1f), "the top speed is a top speed");
            Assert.That(tractor.Body.linearVelocity.magnitude, Is.GreaterThan(top * 0.95f),
                $"settled at {tractor.Body.linearVelocity.magnitude:F1} m/s against a top speed of {top}: " +
                "the dial on the profile has to be the speed you actually get");
        }

        [UnityTest]
        public IEnumerator SprintingTakesATractorPastItsOrdinaryTopSpeed()
        {
            m_Apron.TearDown();
            m_Apron = new TestApron(sizeMetres: 1000f);
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);
            yield return Steps.Seconds(2f);

            tractor.IntentSource = new FixedIntent(throttle: 1f) { Current = new DriveIntent(0f, 1f, 0f, true) };
            yield return Steps.Seconds(10f);

            var top = m_TractorProfile.topSpeedMetresPerSecond * m_TractorProfile.sprintDriveMultiplier;
            Assert.That(tractor.Body.linearVelocity.magnitude, Is.GreaterThan(m_TractorProfile.topSpeedMetresPerSecond * 1.2f));
            Assert.That(tractor.Body.linearVelocity.magnitude, Is.LessThanOrEqualTo(top + 0.1f));
        }

        [UnityTest]
        public IEnumerator ATractorReleasedAtTenMetresASecondIsBelowSixTwoSecondsLater()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);
            yield return Steps.Seconds(2f);

            tractor.IntentSource = new FixedIntent(throttle: 1f);
            while (tractor.Body.linearVelocity.magnitude < 10f)
            {
                yield return new WaitForFixedUpdate();
            }

            tractor.IntentSource = new FixedIntent();
            yield return Steps.Seconds(2f);

            Assert.That(tractor.Body.linearVelocity.magnitude, Is.LessThan(6f),
                "off the throttle the driveline drags: a tractor that coasts on at speed is one " +
                "nobody can stop in time");
        }
    }
}
