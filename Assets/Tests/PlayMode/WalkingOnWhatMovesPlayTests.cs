using System.Collections;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// A person standing on something that moves.
    ///
    /// Nothing attaches them and nothing freezes them. Friction carries them, and walking is
    /// relative to whatever is under their feet: standing still on a moving deck means moving with
    /// it, walking forward means moving with it and a bit more. That is the whole of riding, and
    /// it is also why somebody who jumps off a moving deck keeps the speed it gave them. Where a
    /// bag and a person part company is grip: a bag has the friction its profile gives it and slides
    /// off a deck thrown about hard enough, while a pair of feet hold on to anything this apron can
    /// do -- see GettingMovingPlayTests for the figure that decides that and what it costs.
    /// </summary>
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

            // A deck: a slab lying on the tarmac that can be driven about. Long, so that somebody
            // walking forward on it for a while is still on it.
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

    }
}
