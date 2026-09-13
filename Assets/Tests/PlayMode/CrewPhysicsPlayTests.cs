using System.Collections;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// A ramp worker as a physical object.
    ///
    /// The last test here is the reason crew are rigidbodies at all. Everything else about the
    /// choice is an argument; being run over is the observable consequence.
    /// </summary>
    public sealed class CrewPhysicsPlayTests
    {
        TestApron m_Apron;
        CrewProfile m_CrewProfile;
        VehicleProfile m_TractorProfile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_CrewProfile = TestProfiles.CrewMember();
            m_TractorProfile = TestProfiles.Tractor();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_CrewProfile);
            Object.DestroyImmediate(m_TractorProfile);
        }

        [UnityTest]
        public IEnumerator ACharacterWithNothingPressedComesToRestAndStaysThere()
        {
            var crew = m_Apron.AddCrew(m_CrewProfile, new Vector3(0f, 1.5f, 0f));
            crew.IntentSource = new HeldKeys(new Vector2(0f, 1f));
            yield return Steps.Seconds(2f);

            crew.IntentSource = new HeldKeys();
            yield return Steps.Seconds(1.5f);
            var stopped = crew.transform.position;
            yield return Steps.Seconds(1.5f);

            Assert.That(crew.Body.linearVelocity.magnitude, Is.LessThan(0.15f), "the character is still sliding");
            Assert.That(Vector3.Distance(crew.transform.position, stopped), Is.LessThan(0.1f),
                "somebody standing still must stay where they are put");
        }

        [UnityTest]
        public IEnumerator ACharacterWalkingOffAnEdgeLandsOnTheApronRatherThanThroughIt()
        {
            var ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledge.transform.position = new Vector3(0f, 1f, 0f);
            ledge.transform.localScale = new Vector3(6f, 2f, 6f);
            m_Apron.Track(ledge.transform);

            var crew = m_Apron.AddCrew(m_CrewProfile, new Vector3(0f, 3f, 0f));
            yield return Steps.Seconds(1.5f);
            var onTheLedge = crew.transform.position.y;

            crew.IntentSource = new HeldKeys(new Vector2(0f, 1f));
            yield return Steps.Seconds(4f);

            Assert.That(onTheLedge, Is.GreaterThan(1.9f), "the character should have been standing on the ledge");
            Assert.That(crew.transform.position.y, Is.LessThan(onTheLedge - 1f), "they never walked off it");
            Assert.That(crew.transform.position.y, Is.GreaterThan(0f),
                "and they must land on the apron rather than fall through it");
        }

        [UnityTest]
        public IEnumerator ACharacterThisMachineIsNotSimulatingIsLeftToTheNetwork()
        {
            // Built the way a copy of somebody else's character is built: configured from its
            // profile, never given a seat. Setting Simulated false by hand would test a transition
            // that only ever happens in a test -- production goes straight from construction to
            // un-simulated and never passes through the setter at all.
            var crew = m_Apron.AddRemoteCrew(m_CrewProfile, new Vector3(0f, 1.5f, 0f));
            crew.IntentSource = new HeldKeys(new Vector2(0f, 1f));

            var leftAt = crew.transform.position;
            yield return Steps.Seconds(3f);

            Assert.That(crew.OursToMove, Is.False, "nobody gave this character a seat, so it is not ours");
            Assert.That(crew.Body.useGravity, Is.True,
                "a copy of somebody else keeps its weight, because players run each other over on " +
                "purpose and a weightless body has nothing for an impact to modify");
            // Sideways only. Falling is not walking: a copy of somebody else keeps its weight, so
            // it settles onto the apron like anything else, and that is the behaviour being asked
            // for rather than a failure to stay put.
            var wentSideways = Vector3.Distance(
                new Vector3(leftAt.x, 0f, leftAt.z),
                new Vector3(crew.transform.position.x, 0f, crew.transform.position.z));

            Assert.That(wentSideways, Is.LessThan(0.05f),
                "it walked itself across the apron. Two machines walking one character fight each " +
                "other, and the one that does not own them loses");
        }

        [UnityTest]
        public IEnumerator ATractorRunningIntoSomebodyKnocksThemOutOfTheWay()
        {
            var crew = m_Apron.AddCrew(m_CrewProfile, new Vector3(0f, 1.5f, 0f));
            var tractor = m_Apron.AddVehicle(
                m_TractorProfile, "Tug 1", new Vector3(0f, 1f, -14f), Quaternion.identity);
            yield return Steps.Seconds(2f);

            var stoodAt = crew.transform.position;
            tractor.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(6f);

            var shifted = Vector3.Distance(
                new Vector3(stoodAt.x, 0f, stoodAt.z),
                new Vector3(crew.transform.position.x, 0f, crew.transform.position.z));

            Assert.That(shifted, Is.GreaterThan(1f),
                "a person struck by three tonnes must move; a character that cannot be pushed is a bollard, " +
                "and the whole game is built on being able to shove and be shoved");
        }
    }
}
