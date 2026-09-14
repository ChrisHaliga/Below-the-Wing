using System.Collections;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class WalkingOnWhatMovesPlayTests
    {
        TestApron m_Apron;
        CrewProfile m_Profile;
        CrewCharacter m_Crew;
        Rigidbody m_Deck;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_Profile = TestProfiles.CrewMember();

            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.localScale = new Vector3(3f, 0.2f, 30f);
            deck.transform.position = new Vector3(0f, 0.1f, 0f);
            m_Deck = deck.AddComponent<Rigidbody>();
            m_Deck.mass = 2000f;
            m_Deck.freezeRotation = true;
            m_Apron.Track(m_Deck);

            m_Crew = m_Apron.AddCrew(m_Profile, new Vector3(0f, 1.15f, -8f));
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_Profile);
        }

        IEnumerator DriveTheDeck(Vector3 velocity, float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                m_Deck.linearVelocity = velocity;
                yield return new WaitForFixedUpdate();
            }
        }

        Vector3 OnTheDeck() => m_Deck.transform.InverseTransformPoint(m_Crew.transform.position);

        string Where()
            => $"crew at {m_Crew.transform.position} moving {m_Crew.Body.linearVelocity}, deck at " +
               $"{m_Deck.position} moving {m_Deck.linearVelocity}, grounded {m_Crew.Grounded}";

        [UnityTest]
        public IEnumerator StandingStillOnAMovingDeckMeansMovingWithIt()
        {
            yield return Steps.Seconds(1f);
            var stoodAt = OnTheDeck();

            yield return DriveTheDeck(new Vector3(0f, 0f, 3f), seconds: 3f);

            Assert.That(m_Crew.transform.position.z, Is.GreaterThan(-3f),
                $"the deck went nine metres and the person standing on it has to have come along. {Where()}");
            Assert.That(Vector3.Distance(OnTheDeck(), stoodAt), Is.LessThan(1f),
                "and stayed roughly where they were standing on it, rather than sliding off the back");
        }

        [UnityTest]
        public IEnumerator WalkingForwardIsRelativeToTheDeck()
        {
            yield return Steps.Seconds(1f);
            m_Crew.IntentSource = new HeldKeys(new Vector2(0f, 1f));

            yield return DriveTheDeck(new Vector3(0f, 0f, 3f), seconds: 1.5f);

            Assert.That(m_Crew.Body.linearVelocity.z, Is.GreaterThan(3f + (m_Profile.walkSpeedMetresPerSecond * 0.6f)),
                $"walking forward on a deck doing 3 m/s, they are doing {m_Crew.Body.linearVelocity.z:F1} " +
                "m/s. Walking that is relative to the world instead of the deck means standing " +
                $"still is walking backward at the deck's speed. {Where()}");
        }

        [UnityTest]
        public IEnumerator JumpingOffCarriesTheDecksSpeed()
        {
            yield return Steps.Seconds(1f);
            yield return DriveTheDeck(new Vector3(0f, 0f, 3f), seconds: 1f);

            m_Crew.IntentSource = new HeldKeys { Jump = true };
            yield return DriveTheDeck(new Vector3(0f, 0f, 3f), seconds: 0.1f);
            m_Crew.IntentSource = new HeldKeys();

            Assert.That(m_Crew.Body.linearVelocity.y, Is.GreaterThan(1f), "they have to have left the deck");
            Assert.That(m_Crew.Body.linearVelocity.z, Is.EqualTo(3f).Within(1f),
                "somebody springing off a cart doing three metres a second is doing three metres a " +
                "second in the air, which is why they land well ahead of where they left");
        }

        IEnumerator AHardCorner()
        {
            for (var i = 1; i <= 50; i++)
            {
                m_Deck.linearVelocity = new Vector3(0.4f * i, 0f, 3f);
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator AHardCornerSlidesAPersonOffTheDeck()
        {
            yield return Steps.Seconds(1f);

            var stoodAt = OnTheDeck();
            yield return DriveTheDeck(new Vector3(0f, 0f, 3f), seconds: 1.5f);
            Assert.That(Vector3.Distance(OnTheDeck(), stoodAt), Is.LessThan(1f), $"aboard, before the corner. {Where()}");

            yield return AHardCorner();
            yield return Steps.Seconds(1f);

            Assert.That(Mathf.Abs(OnTheDeck().x), Is.GreaterThan(1.5f),
                $"there is no lip for a person, and a corner that would throw a bag throws them " +
                $"too. {Where()}");
        }

        [UnityTest]
        public IEnumerator SomebodySlidingCannotWalkThemselvesBackAboard()
        {
            yield return Steps.Seconds(1f);
            yield return DriveTheDeck(new Vector3(0f, 0f, 3f), seconds: 1f);

            m_Crew.IntentSource = new HeldKeys(new Vector2(1f, 0f));

            yield return AHardCorner();
            yield return Steps.Seconds(1f);

            Assert.That(Mathf.Abs(OnTheDeck().x), Is.GreaterThan(1.5f),
                $"they walked out of a skid. Feet that can push while they are sliding are feet " +
                $"that never lose the deck, and the corner costs a rider nothing. {Where()}");
        }
    }
}
