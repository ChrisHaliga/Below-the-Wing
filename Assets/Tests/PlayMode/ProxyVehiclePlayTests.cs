using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class ProxyVehiclePlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
        }

        VehicleController Tractor(string called, Vector3 where)
            => m_Apron.AddVehicle(m_TractorProfile, called, where, Quaternion.identity);

        [UnityTest]
        public IEnumerator AVehicleSomebodyElseOwnsStandsOnItsOwnSuspension()
        {
            var theirs = Tractor("Tug 1", new Vector3(0f, 1f, 0f));
            theirs.OursToMove = false;

            yield return Steps.Seconds(10f);

            const float resting = 0f;
            Assert.That(theirs.transform.position.y, Is.EqualTo(resting).Within(0.1f),
                "left to itself it must hold itself up on its springs. Sunk onto its bodywork it has " +
                "no suspension travel left to absorb anything, and a crash into it does nothing");
        }

        [UnityTest]
        public IEnumerator AVehicleKeepsItsMomentumWhenItStopsBeingOurs()
        {
            var vehicle = Tractor("Tug 1", new Vector3(0f, 1f, 0f));
            vehicle.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(4f);

            var carriedSpeed = vehicle.Body.linearVelocity;
            Assert.That(carriedSpeed.magnitude, Is.GreaterThan(2f), "it has to be moving for this to mean anything");

            vehicle.OursToMove = false;

            Assert.That(vehicle.Body.linearVelocity, Is.EqualTo(carriedSpeed),
                "changing who decides where a vehicle goes must not stop it. Three tonnes travelling " +
                "at speed does not become three tonnes standing still because a message arrived");
        }

        [UnityTest]
        public IEnumerator AVehicleSomebodyElseOwnsIsNotDrivenByControlsOnThisMachine()
        {
            var theirs = Tractor("Tug 1", new Vector3(0f, 1f, 0f));
            theirs.OursToMove = false;
            theirs.IntentSource = new FixedIntent(throttle: 1f);

            var from = theirs.transform.position;
            yield return Steps.Seconds(4f);

            var travelled = Vector3.Distance(
                new Vector3(from.x, 0f, from.z),
                new Vector3(theirs.transform.position.x, 0f, theirs.transform.position.z));

            Assert.That(travelled, Is.LessThan(0.5f),
                "two machines driving the same vehicle fight each other. Where it goes is the owner's " +
                "to say; running the throttle here as well is the one thing a copy must not do");
        }

        [UnityTest]
        public IEnumerator AVehicleSomebodyElseOwnsIsStillHeavyAndStillSolid()
        {
            var theirs = Tractor("Tug 1", new Vector3(0f, 1f, 0f));
            theirs.OursToMove = false;
            yield return Steps.Seconds(1f);

            Assert.That(theirs.Body.isKinematic, Is.False,
                "a kinematic body has infinite mass: nothing can shove it, and it shoves everything");
            Assert.That(theirs.Body.useGravity, Is.True,
                "a body with no weight on its springs has no grip and nothing holding it down");
            Assert.That(theirs.Body.mass, Is.EqualTo(m_TractorProfile.massKg).Within(0.01f),
                "it weighs what it weighs on every machine, or the same crash comes out differently on each");
        }

        [UnityTest]
        public IEnumerator AVehicleWeOwnCanShoveOneWeDoNotOwn()
        {
            var theirs = Tractor("Tug 2", new Vector3(0f, 1f, 8f));
            theirs.OursToMove = false;

            var ours = Tractor("Tug 1", new Vector3(0f, 1f, 0f));
            ours.IntentSource = new FixedIntent(throttle: 1f);

            yield return Steps.Seconds(1f);
            var whereTheyStood = theirs.transform.position;

            yield return Steps.Seconds(6f);

            var shovedBy = Vector3.Distance(
                new Vector3(whereTheyStood.x, 0f, whereTheyStood.z),
                new Vector3(theirs.transform.position.x, 0f, theirs.transform.position.z));

            Assert.That(shovedBy, Is.GreaterThan(0.5f),
                "if a vehicle somebody else owns cannot be moved by driving into it, two players can " +
                "never have a crash worth watching");

            const float resting = 0f;
            Assert.That(theirs.transform.position.y, Is.EqualTo(resting).Within(0.15f),
                "shoved off its suspension and left sitting on its bodywork, it can absorb nothing " +
                "further and every later impact against it reads as hitting a kerb");
        }
    }
}
