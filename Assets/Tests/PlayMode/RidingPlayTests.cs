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

            // Where they are standing on the deck, in the deck's own frame -- which is what has to
            // stay the same however far the cart drives.
            var stoodAt = m_CartObject.transform.InverseTransformPoint(m_Crew.transform.position);

            m_CartObject.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, 8f);
            yield return Step(3f);

            var standsAt = m_CartObject.transform.InverseTransformPoint(m_Crew.transform.position);
            Assert.That(Vector3.Distance(standsAt, stoodAt), Is.LessThan(0.01f),
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
    
        [UnityTest]
        public IEnumerator SomebodyOnTheRoofRidesAlongWithTheCart()
        {
            // A second place to stand, on top rather than inside. Riding never knew what a deck
            // was, so a roof needs a carrier and nothing else.
            var roof = new GameObject("Roof");
            roof.transform.SetParent(m_CartObject.transform, worldPositionStays: false);
            roof.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            var onTop = roof.AddComponent<Carrier>();
            onTop.Covers(Vector3.zero, new Vector3(1.7f, 2f, 3.2f));

            m_Crew.transform.position = roof.transform.position;
            yield return null;

            m_Riding.LandedOn(onTop, Time.time);
            Assert.That(m_Riding.Attached, Is.True, "they have to be up there for this to mean anything");

            var stoodAt = m_CartObject.transform.InverseTransformPoint(m_Crew.transform.position);

            m_CartObject.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, 8f);
            yield return Step(3f);

            var standsAt = m_CartObject.transform.InverseTransformPoint(m_Crew.transform.position);

            Assert.That(Vector3.Distance(standsAt, stoodAt), Is.LessThan(0.01f),
                "the roof is somewhere to ride, which is the whole reason a carrier is a carrier " +
                "and not a deck");
        }

        [UnityTest]
        public IEnumerator AGripOnTheRoofSurvivesACornerThatEmptiesTheDeck()
        {
            var roof = new GameObject("Roof");
            roof.transform.SetParent(m_CartObject.transform, worldPositionStays: false);
            roof.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            var onTop = roof.AddComponent<Carrier>();
            onTop.Covers(Vector3.zero, new Vector3(1.7f, 2f, 3.2f));

            var bagObject = new GameObject("Bag");
            var bagBody = bagObject.AddComponent<Rigidbody>();
            bagBody.useGravity = false;
            var bag = bagObject.AddComponent<Carried>();
            bag.ComesOffAt = new WakeThresholds(lateralAcceleration: 6f, tiltDegrees: 25f, impulse: 400f);
            m_Apron.Track(bagObject.transform);

            var watch = m_CartObject.AddComponent<CarrierWatch>();
            Assert.That(watch, Is.Not.Null);

            m_Crew.transform.position = roof.transform.position;
            yield return null;

            m_Riding.LandedOn(onTop, Time.time);
            m_Rider.ComesOffAt = m_Riding.Thresholds(
                new WakeThresholds(lateralAcceleration: 6f, tiltDegrees: 25f, impulse: 400f),
                holdingOn: true);

            bag.AttachTo(m_Deck);
            yield return Step(0.2f);

            for (var i = 0; i < 6; i++)
            {
                m_CartObject.GetComponent<Rigidbody>().linearVelocity += new Vector3(0.3f, 0f, 0f);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(bag.Attached, Is.False, "the load goes");
            Assert.That(m_Riding.Attached, Is.True,
                "and somebody gripping the roof rails does not, which is what makes holding on " +
                "worth the hands it costs");
        }
    }
}
