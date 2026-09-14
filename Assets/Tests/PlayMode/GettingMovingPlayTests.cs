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
    /// How quickly somebody gets going, and how quickly they stop.
    ///
    /// A person's legs really can only push about as hard as their feet grip, which is around
    /// 8 m/s^2 and half a second of shuffling before they are up to walking pace. That is honest and
    /// it feels like wading, because a player is not a person: they are somebody holding a key, and
    /// the delay between pressing it and moving is the whole of what the controls feel like.
    ///
    /// So how fast a player's own walking answers is a feel dial, kept apart from how hard their
    /// feet can hold what they are standing on, which is a real figure and is what decides whether
    /// a corner throws them.
    /// </summary>
    public sealed class GettingMovingPlayTests
    {
        TestApron m_Apron;
        CrewProfile m_Profile;
        CrewCharacter m_Crew;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_Profile = TestProfiles.CrewMember();
            m_Crew = m_Apron.AddCrew(m_Profile, new Vector3(0f, 0.9f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_Profile);
        }

        float AcrossTheGround => new Vector2(m_Crew.Body.linearVelocity.x, m_Crew.Body.linearVelocity.z).magnitude;

        [UnityTest]
        public IEnumerator WalkingStartsAtOnceRatherThanWindingUp()
        {
            yield return Steps.Seconds(0.5f);

            m_Crew.IntentSource = new HeldKeys(new Vector2(0f, 1f));
            yield return Steps.Seconds(0.2f);

            Assert.That(AcrossTheGround, Is.GreaterThan(m_Profile.walkSpeedMetresPerSecond * 0.9f),
                $"a fifth of a second after the key went down they are doing {AcrossTheGround:F2} m/s " +
                $"of {m_Profile.walkSpeedMetresPerSecond:F1}. Walking that winds up is the single " +
                "thing a player feels most about moving around");
        }

        [UnityTest]
        public IEnumerator LettingGoStopsThemJustAsQuickly()
        {
            yield return Steps.Seconds(0.5f);
            m_Crew.IntentSource = new HeldKeys(new Vector2(0f, 1f));
            yield return Steps.Seconds(1f);

            m_Crew.IntentSource = new HeldKeys(Vector2.zero);
            yield return Steps.Seconds(0.2f);

            Assert.That(AcrossTheGround, Is.LessThan(0.4f),
                $"a fifth of a second after letting go they are still doing {AcrossTheGround:F2} m/s. " +
                "Sliding to a halt is the same fault as winding up, felt on the way out");
        }

        [UnityTest]
        public IEnumerator SomebodyStandingOnADeckKeepsTheirFootingWhenItPullsAway()
        {
            var tractorProfile = TestProfiles.Tractor();
            var cartProfile = TestProfiles.Cart();

            var train = m_Apron.AddTrain(
                tractorProfile, cartProfile, cartCount: 1, new Vector3(0f, 0f, 20f), "Tug 1",
                tractorShape: TestShapes.Tractor(), cartShape: TestShapes.Cart());

            var cart = train.Members[1];
            var shape = cart.GetComponent<VehicleShape>();

            // Standing on the deck, inside the cart.
            var deck = cart.transform.TransformPoint(new Vector3(0f, shape.InteriorLocal.min.y + 0.95f, 0f));
            m_Crew.transform.position = deck;
            m_Crew.Body.position = deck;

            yield return Steps.Seconds(1.5f);
            var aboardAt = cart.transform.InverseTransformPoint(m_Crew.transform.position);

            train.Leader.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(3f);

            var nowAt = cart.transform.InverseTransformPoint(m_Crew.transform.position);
            var slid = new Vector2(nowAt.x - aboardAt.x, nowAt.z - aboardAt.z).magnitude;

            Assert.That(slid, Is.LessThan(0.5f),
                $"they slid {slid:F2} m about the deck while the tractor pulled away. Feet that get " +
                "a player moving instantly are also feet that hold them on a deck: that is the trade " +
                "this figure makes, and it is made deliberately");

            Object.DestroyImmediate(tractorProfile);
            Object.DestroyImmediate(cartProfile);
        }
    }
}
