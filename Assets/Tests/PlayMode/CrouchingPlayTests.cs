using System.Collections;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// Getting down low enough to fit somewhere.
    ///
    /// A baggage cart is a container with a roof, and the clear space above its deck is shorter than
    /// a person. Somebody has to be able to get in there to load it by hand, so crouching exists.
    /// It is not a cart mechanic -- it is a person being shorter, and it works the same under a
    /// wing, in a hold, or anywhere else the world is low.
    ///
    /// The half of it that matters is standing back up. A character who stands into a ceiling is a
    /// capsule overlapping a collider by half a metre, and the solver's answer to that is to fire
    /// them through it.
    /// </summary>
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

            // Through what a player would be holding down, rather than by reaching into the
            // character and setting its stance. Crouching is asked for every step in the game, and a
            // test that asks once exercises a path nothing else uses.
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

        /// <summary>A slab low enough that a standing person does not fit under it.</summary>
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
        public IEnumerator ACrouchedPersonFitsWhereAStandingOneDoesNot()
        {
            // The clear space above a baggage cart's deck. A person has to get in there to load it.
            const float clearInsideACart = 1.626f;

            yield return Steps.Seconds(0.5f);

            Assert.That(m_Profile.heightMetres, Is.GreaterThan(clearInsideACart),
                "this test means nothing unless a standing person genuinely does not fit");
            Assert.That(m_Profile.crouchedHeightMetres, Is.LessThan(clearInsideACart));

            yield return null;
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

            // Out into the open, under their own steam. Straight ahead rather than sideways: with
            // no camera, which way is "forward" is the character's own facing, and a character
            // turns to face the way they are walking -- so holding strafe walks them in a circle
            // and they never leave the ceiling at all.
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
    }
}
