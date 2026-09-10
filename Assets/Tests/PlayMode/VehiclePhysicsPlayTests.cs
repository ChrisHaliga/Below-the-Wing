using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// What a vehicle does once physics is actually running.
    ///
    /// The forces are checked one at a time elsewhere. These are the claims that only become true
    /// after several hundred steps of a solver: that a vehicle stands up, stays up, and behaves
    /// according to the profile it was given rather than to anything written into the controller.
    /// </summary>
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

        static IEnumerator Step(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator ATractorDroppedOntoTheApronSettlesAndStaysThere()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 2f, 0f), Quaternion.identity);

            yield return Step(4f);
            var settled = tractor.transform.position.y;
            yield return Step(1f);
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

            yield return Step(4f);

            var lean = Vector3.Angle(tractor.transform.up, Vector3.up);
            Assert.That(lean, Is.LessThan(3f), "four equal corners on flat ground should hold the body level");
        }

        [UnityTest]
        public IEnumerator OpeningTheThrottleMovesATractorForward()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);
            yield return Step(2f);
            var from = tractor.transform.position;

            tractor.IntentSource = new FixedIntent(throttle: 1f);
            yield return Step(3f);

            var travelled = Vector3.Dot(tractor.transform.position - from, tractor.transform.forward);
            Assert.That(travelled, Is.GreaterThan(3f), "three seconds at full throttle should get a tug moving");
        }

        [UnityTest]
        public IEnumerator ATractorReleasedFromTheThrottleCoastsToAStopOnItsOwn()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);
            yield return Step(2f);

            tractor.IntentSource = new FixedIntent(throttle: 1f);
            yield return Step(3f);
            Assert.That(tractor.Body.linearVelocity.magnitude, Is.GreaterThan(2f), "it should be moving by now");

            tractor.IntentSource = new FixedIntent();
            yield return Step(10f);

            Assert.That(tractor.Body.linearVelocity.magnitude, Is.LessThan(0.1f),
                "letting go of the throttle has to bring a tractor to rest without touching the brake; " +
                "one that coasts for ever never settles and never stops costing solver time");
        }

        [UnityTest]
        public IEnumerator BrakingBringsAMovingTractorToAStopWithoutDrivingItBackwards()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);
            yield return Step(2f);
            tractor.IntentSource = new FixedIntent(throttle: 1f);
            yield return Step(3f);

            var whereItStartedBraking = tractor.transform.position;
            tractor.IntentSource = new FixedIntent(brake: 1f);
            yield return Step(4f);

            var afterBraking = Vector3.Dot(tractor.transform.position - whereItStartedBraking, tractor.transform.forward);
            Assert.That(tractor.Body.linearVelocity.magnitude, Is.LessThan(0.5f), "it should have stopped");
            Assert.That(afterBraking, Is.GreaterThan(-0.5f),
                "braking must not reverse the vehicle back past where it began braking");
        }

        [UnityTest]
        public IEnumerator ACartAndATractorAreTheSameComponentBehavingDifferentlyBecauseOfTheirProfiles()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);
            var cart = m_Apron.AddVehicle(m_CartProfile, "Cart 1-1", new Vector3(20f, 1f, 0f), Quaternion.identity);
            yield return Step(2f);

            Assert.That(cart.GetType(), Is.EqualTo(tractor.GetType()),
                "a cart is not a different class, it is the same one with different numbers");
            Assert.That(cart.Body.mass, Is.EqualTo(m_CartProfile.massKg).Within(0.01f));
            Assert.That(tractor.Body.mass, Is.EqualTo(m_TractorProfile.massKg).Within(0.01f));

            var tractorFrom = tractor.transform.position;
            var cartFrom = cart.transform.position;
            tractor.IntentSource = new FixedIntent(throttle: 1f);
            cart.IntentSource = new FixedIntent(throttle: 1f);
            yield return Step(3f);

            Assert.That(Vector3.Distance(tractor.transform.position, tractorFrom), Is.GreaterThan(3f));
            Assert.That(Vector3.Distance(cart.transform.position, cartFrom), Is.LessThan(0.5f),
                "a cart has no engine, and flooring a throttle it does not have should move it nowhere");
        }
    }
}
