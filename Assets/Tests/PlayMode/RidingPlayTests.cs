using System.Collections;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// A player standing on something that moves.
    ///
    /// Riding is half of what this slice is for. The other half is coming off, at a moment the
    /// driver caused and the passenger can see coming.
    /// </summary>
    public sealed class RidingPlayTests
    {
        TestApron m_Apron;
        CrewProfile m_CrewProfile;

        GameObject m_CartObject;
        Carrier m_Deck;
        CrewCharacter m_Crew;
        Carried m_Rider;
        Riding m_Riding;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_CrewProfile = TestProfiles.CrewMember();

            m_CartObject = new GameObject("Cart");
            var cartBody = m_CartObject.AddComponent<Rigidbody>();
            cartBody.useGravity = false;
            cartBody.mass = 550f;
            m_Deck = m_CartObject.AddComponent<Carrier>();
            m_Deck.Covers(Vector3.zero, new Vector3(2f, 2f, 4f));
            m_Apron.Track(m_CartObject.transform);

            m_Crew = m_Apron.AddCrew(m_CrewProfile, new Vector3(0f, 0.5f, 0f));
            m_Rider = m_Crew.gameObject.AddComponent<Carried>();
            m_Crew.Riding = m_Rider;
            m_Riding = new Riding(m_Crew, m_Rider);
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_CrewProfile);
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
        public IEnumerator LandingOnADeckStartsRidingItImmediately()
        {
            m_Riding.LandedOn(m_Deck, Time.time);

            Assert.That(m_Riding.Attached, Is.True,
                "a person is not going to oblige by standing still first, and being flung off for " +
                "moving is not a rule anybody would accept");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ARiderIsNotFlungOffByTheCartSimplyDriving()
        {
            m_Riding.LandedOn(m_Deck, Time.time);
            var stoodAt = m_Crew.transform.localPosition;

            m_CartObject.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, 8f);
            yield return Step(3f);

            Assert.That(Vector3.Distance(m_Crew.transform.localPosition, stoodAt), Is.LessThan(0.01f),
                "riding a cart in a straight line has to be uneventful, or nobody will ever do it");
        }

        [UnityTest]
        public IEnumerator SteppingOffKeepsWhateverTheCartWasDoing()
        {
            m_Riding.LandedOn(m_Deck, Time.time);
            m_CartObject.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, 6f);
            yield return Step(0.5f);

            m_Riding.SteppedOff(Time.time);

            Assert.That(m_Crew.Body.linearVelocity.z, Is.EqualTo(6f).Within(0.5f),
                "somebody who walks off the back of a moving cart is already moving, and stopping " +
                "dead in mid-air is not something a body does");
        }

        [UnityTest]
        public IEnumerator WanderingOffTheDeckIsNoticed()
        {
            m_Riding.LandedOn(m_Deck, Time.time);
            Assert.That(m_Riding.WalkedOffTheEdge(), Is.False, "standing in the middle of it");

            m_Crew.transform.position = new Vector3(0f, 0.5f, 9f);

            Assert.That(m_Riding.WalkedOffTheEdge(), Is.True,
                "a rider still attached to a cart they are standing nine metres from is being towed " +
                "through the air behind it");

            yield return null;
        }

        [UnityTest]
        public IEnumerator HoldingOnMakesARiderHarderToThrowOff()
        {
            var standing = new WakeThresholds(lateralAcceleration: 5f, tiltDegrees: 20f, impulse: 600f);

            var loose = m_Riding.Thresholds(standing, holdingOn: false);
            var gripping = m_Riding.Thresholds(standing, holdingOn: true);

            Assert.That(WakeRules.ShakenLoose(8f, 0f, 0f, loose), Is.True, "a hard corner throws a standing rider");
            Assert.That(WakeRules.ShakenLoose(8f, 0f, 0f, gripping), Is.False,
                "and the same corner is survivable by one holding on, which is the trade being offered");

            yield return null;
        }

        [UnityTest]
        public IEnumerator HoldingOnDoesNotMakeARiderImmuneToBeingHit()
        {
            var standing = new WakeThresholds(lateralAcceleration: 5f, tiltDegrees: 20f, impulse: 600f);
            var gripping = m_Riding.Thresholds(standing, holdingOn: true);

            Assert.That(WakeRules.ShakenLoose(0f, 0f, 900f, gripping), Is.True,
                "surviving a head-on collision by holding a rail would read as the game ignoring the " +
                "crash, which is the one thing all of this was built to avoid");

            yield return null;
        }

        [UnityTest]
        public IEnumerator SomebodyStandingNowhereNearADeckDoesNotRideIt()
        {
            m_Crew.transform.position = new Vector3(0f, 0.5f, 30f);

            m_Riding.LandedOn(m_Deck, Time.time);

            Assert.That(m_Riding.Attached, Is.False, "a deck that reaches the far side of the apron is not a deck");

            yield return null;
        }
    }
}
