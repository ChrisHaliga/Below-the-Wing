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

        static IEnumerator Step(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator ACharacterWithNothingPressedComesToRestAndStaysThere()
        {
            var crew = m_Apron.AddCrew(m_CrewProfile, new Vector3(0f, 1.5f, 0f));
            crew.IntentSource = new FixedCrewIntent(new Vector2(0f, 1f));
            yield return Step(2f);

            crew.IntentSource = new FixedCrewIntent(Vector2.zero);
            yield return Step(1.5f);
            var stopped = crew.transform.position;
            yield return Step(1.5f);

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
            yield return Step(1.5f);
            var onTheLedge = crew.transform.position.y;

            crew.IntentSource = new FixedCrewIntent(new Vector2(0f, 1f));
            yield return Step(4f);

            Assert.That(onTheLedge, Is.GreaterThan(1.9f), "the character should have been standing on the ledge");
            Assert.That(crew.transform.position.y, Is.LessThan(onTheLedge - 1f), "they never walked off it");
            Assert.That(crew.transform.position.y, Is.GreaterThan(0f),
                "and they must land on the apron rather than fall through it");
        }

        [UnityTest]
        public IEnumerator ACharacterThisMachineIsNotSimulatingIsLeftToTheNetwork()
        {
            var crew = m_Apron.AddCrew(m_CrewProfile, new Vector3(0f, 1.5f, 0f));
            yield return Step(2f);

            // Every character except the local player's is a copy whose position arrives over the
            // network. Running its movement here as well means a full-mass body being braked toward
            // a standstill locally while replication drags it somewhere else -- so it shoves your
            // character on your screen, and on its owner's screen it never touched you.
            crew.Simulated = false;
            crew.IntentSource = new FixedCrewIntent(new Vector2(0f, 1f));

            var leftAt = crew.transform.position;
            yield return Step(3f);

            Assert.That(Vector3.Distance(crew.transform.position, leftAt), Is.LessThan(0.05f),
                "a copy of somebody else's character walked itself across the apron. Where it goes is " +
                "the network's business, not this machine's");
        }

        [UnityTest]
        public IEnumerator ATractorRunningIntoSomebodyKnocksThemOutOfTheWay()
        {
            var crew = m_Apron.AddCrew(m_CrewProfile, new Vector3(0f, 1.5f, 0f));
            var tractor = m_Apron.AddVehicle(
                m_TractorProfile, "Tug 1", new Vector3(0f, 1f, -14f), Quaternion.identity);
            yield return Step(2f);

            var stoodAt = crew.transform.position;
            tractor.IntentSource = new FixedIntent(throttle: 1f);
            yield return Step(6f);

            var shifted = Vector3.Distance(
                new Vector3(stoodAt.x, 0f, stoodAt.z),
                new Vector3(crew.transform.position.x, 0f, crew.transform.position.z));

            Assert.That(shifted, Is.GreaterThan(1f),
                "a person struck by three tonnes must move; a character that cannot be pushed is a bollard, " +
                "and the whole game is built on being able to shove and be shoved");
        }
    }

    /// <summary>A player who is always pressing the same thing.</summary>
    sealed class FixedCrewIntent : ICrewIntentSource
    {
        public CrewIntent Current { get; }

        public FixedCrewIntent(Vector2 move, bool sprint = false) => Current = new CrewIntent(move, sprint);
    }
}
