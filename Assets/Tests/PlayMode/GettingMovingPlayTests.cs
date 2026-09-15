using System.Collections;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
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
        public IEnumerator SomebodyStandingOnADeckIsThrownAboutWhenTheTractorLaunches()
        {
            var tractorProfile = TestProfiles.Tractor();
            var cartProfile = TestProfiles.Cart();

            var train = m_Apron.AddTrain(
                tractorProfile, cartProfile, cartCount: 1, new Vector3(0f, 0f, 20f), "Tug 1",
                tractorShape: TestShapes.Tractor(), cartShape: TestShapes.Cart());

            var cart = train.Members[1];
            var shape = cart.GetComponent<VehicleShape>();

            var deck = cart.transform.TransformPoint(new Vector3(0f, shape.InteriorLocal.min.y + 0.95f, 0f));
            m_Crew.transform.position = deck;
            m_Crew.Body.position = deck;

            yield return Steps.Seconds(1.5f);
            var aboardAt = cart.transform.InverseTransformPoint(m_Crew.transform.position);

            train.Leader.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(3f);

            var nowAt = cart.transform.InverseTransformPoint(m_Crew.transform.position);
            var slid = new Vector2(nowAt.x - aboardAt.x, nowAt.z - aboardAt.z).magnitude;

            var launching = tractorProfile.maxDriveForceNewtons * tractorProfile.launchDriveMultiplier
                            / tractorProfile.massKg;

            Assert.That(launching, Is.GreaterThan(m_Profile.footGripMetresPerSecondSquared),
                $"a standing start pulls {launching:F1} m/s^2 and a pair of feet hold at " +
                $"{m_Profile.footGripMetresPerSecondSquared:F1}. These two figures decide between " +
                "them whether a rider survives a launch, and nothing else does");

            Assert.That(slid, Is.GreaterThan(0.5f),
                $"they slid {slid:F2} m about the deck while the tractor pulled away. A launch that " +
                "out-pulls what feet can hold has to take a rider off their feet, or the grip figure " +
                "is doing nothing and a corner cannot throw them either");

            Object.DestroyImmediate(tractorProfile);
            Object.DestroyImmediate(cartProfile);
        }
    }
}
