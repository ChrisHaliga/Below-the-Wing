using System.Collections;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class CrouchingPlayTests
    {
        TestApron m_Apron;
        CrewProfile m_Profile;
        CrewCharacter m_Crew;
        HeldKeys m_Keys;
        GameObject m_Ceiling;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_Profile = TestProfiles.CrewMember();
            m_Crew = m_Apron.AddCrew(m_Profile, new Vector3(0f, 1f, 0f));

            m_Keys = new HeldKeys();
            m_Crew.IntentSource = m_Keys;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Ceiling != null)
            {
                Object.DestroyImmediate(m_Ceiling);
            }

            m_Apron.TearDown();
            Object.DestroyImmediate(m_Profile);
        }

        void PutACeilingAt(float heightMetres, Vector3 over)
        {
            m_Ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_Ceiling.name = "Ceiling";
            m_Ceiling.transform.localScale = new Vector3(4f, 0.15f, 4f);
            m_Ceiling.transform.position = new Vector3(over.x, heightMetres + 0.075f, over.z);
        }

        [UnityTest]
        public IEnumerator ACrouchedPersonIsShorterThanAStandingOne()
        {
            yield return Steps.Seconds(0.5f);

            var standing = m_Crew.HeightMetres;
            m_Keys.Crouch = true;
            yield return Steps.Seconds(0.5f);

            Assert.That(m_Crew.HeightMetres, Is.LessThan(standing));
            Assert.That(m_Crew.HeightMetres, Is.EqualTo(m_Profile.crouchedHeightMetres).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator ACrouchedPersonFitsUnderARoofAStandingOneDoesNot()
        {
            var clearance = TestShapes.Cart().InteriorLocal.size.y;

            yield return Steps.Seconds(0.5f);

            Assert.That(m_Profile.heightMetres, Is.GreaterThan(clearance),
                "this test means nothing unless a standing person genuinely does not fit");

            PutACeilingAt(m_Crew.transform.position.y - (m_Profile.heightMetres * 0.5f) + clearance,
                m_Crew.transform.position);

            m_Keys.Crouch = true;
            yield return Steps.Seconds(0.5f);

            var crown = m_Crew.transform.position.y + (m_Crew.HeightMetres * 0.5f);
            var underside = m_Ceiling.transform.position.y - (m_Ceiling.transform.localScale.y * 0.5f);

            Assert.That(crown, Is.LessThan(underside),
                $"crouched under a {clearance:F2} m roof the crown reached {crown:F2} m against an " +
                $"underside at {underside:F2} m. Two profile numbers compared to each other pass " +
                "with a capsule whose centre never moves");
        }

        [UnityTest]
        public IEnumerator CrouchingCostsSpeed()
        {
            yield return Steps.Seconds(0.5f);

            m_Keys.Move = new Vector2(0f, 1f);

            yield return Steps.Seconds(1.5f);
            var standingSpeed = new Vector2(m_Crew.Body.linearVelocity.x, m_Crew.Body.linearVelocity.z).magnitude;

            m_Keys.Crouch = true;
            yield return Steps.Seconds(1.5f);
            var crouchedSpeed = new Vector2(m_Crew.Body.linearVelocity.x, m_Crew.Body.linearVelocity.z).magnitude;

            Assert.That(standingSpeed, Is.GreaterThan(0.5f), "they have to have been walking");
            Assert.That(crouchedSpeed, Is.LessThan(standingSpeed * 0.9f),
                $"crouched at {crouchedSpeed:F2} m/s against {standingSpeed:F2} m/s standing. If " +
                "crouching costs nothing, nobody ever stands up and it stops being a choice");
        }

        [UnityTest]
        public IEnumerator SomebodyUnderSomethingLowStaysCrouched()
        {
            yield return Steps.Seconds(0.5f);

            m_Keys.Crouch = true;
            yield return Steps.Seconds(0.5f);

            PutACeilingAt(1.4f, m_Crew.transform.position);
            yield return Steps.Seconds(0.2f);

            m_Keys.Crouch = false;
            yield return Steps.Seconds(0.5f);

            Assert.That(m_Crew.Stance.Crouched, Is.True,
                "standing into a ceiling puts half a capsule inside a collider, and the solver's " +
                "answer to that is to fire the character through it");
            Assert.That(m_Crew.HeightMetres, Is.LessThan(1.4f));
        }

        [UnityTest]
        public IEnumerator SomebodyWhoWalksOutFromUnderItStandsUp()
        {
            yield return Steps.Seconds(0.5f);

            m_Keys.Crouch = true;
            yield return Steps.Seconds(0.3f);

            PutACeilingAt(1.4f, m_Crew.transform.position);
            yield return Steps.Seconds(0.2f);

            m_Keys.Crouch = false;
            yield return Steps.Seconds(0.3f);
            Assert.That(m_Crew.Stance.Crouched, Is.True, "still underneath it");

            m_Keys.Move = new Vector2(0f, 1f);
            yield return Steps.Seconds(2f);

            Assert.That(Vector3.Distance(m_Crew.transform.position, m_Ceiling.transform.position),
                Is.GreaterThan(2.5f),
                "they have to have actually walked out from under it");

            Assert.That(m_Crew.Stance.Crouched, Is.False,
                "asking to stand is not a one-off that gets thrown away when it cannot be granted. " +
                "A player who crouched into a cart and walked out of it would otherwise stay bent " +
                "double for the rest of the session");
        }

        [UnityTest]
        public IEnumerator CrouchingBringsTheEyesDownWithTheHead()
        {
            yield return Steps.Seconds(0.5f);
            var standing = m_Crew.EyeMetresAboveOrigin;
            Assert.That(standing, Is.GreaterThan(0.5f), "standing, the eyes are well up the capsule");

            m_Keys.Crouch = true;
            yield return Steps.Seconds(1f);

            var top = m_Crew.transform.position.y + m_Crew.EyeMetresAboveOrigin;
            var head = m_Crew.transform.position.y + (m_Crew.HeightMetres * 0.5f) - (m_Profile.heightMetres - m_Crew.HeightMetres) * 0.5f;
            Assert.That(m_Crew.EyeMetresAboveOrigin, Is.LessThan(standing - 0.3f),
                "eyes still at standing height inside a crouch look out through the roof of the cart " +
                "the crouch exists to fit under");
            Assert.That(top, Is.LessThan(head + 0.01f), "and not above the crouched head");
        }
    }
}
