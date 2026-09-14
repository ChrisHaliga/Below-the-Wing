using System.Collections;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class ClimbingAboardPlayTests
    {
        const float DeckTopMetres = 0.4727f;

        TestApron m_Apron;
        CrewProfile m_CrewProfile;
        VehicleProfile m_CartProfile;
        VehicleController m_Cart;
        CrewCharacter m_Crew;
        HeldKeys m_Keys;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_CrewProfile = TestProfiles.CrewMember();
            m_CartProfile = TestProfiles.Cart();

            m_Cart = m_Apron.AddVehicle(
                m_CartProfile, "Cart 1", Vector3.zero, Quaternion.identity, TestShapes.Cart());
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_CrewProfile);
            Object.DestroyImmediate(m_CartProfile);
        }

        void StandThePersonAt(Vector3 position)
        {
            m_Crew = m_Apron.AddCrew(m_CrewProfile, position);
            m_Keys = new HeldKeys();
            m_Crew.IntentSource = m_Keys;
        }

        Vector3 BesideTheLeftLip(float alongTheCart = 0f)
            => m_Cart.transform.TransformPoint(
                new Vector3(-1.15f, m_CrewProfile.heightMetres * 0.5f, alongTheCart));

        Vector3 BeyondTheRearWall()
            => m_Cart.transform.TransformPoint(
                new Vector3(0f, m_CrewProfile.heightMetres * 0.5f, -2.1f));

        Vector3 InTheCartsOwnSpace() => m_Cart.transform.InverseTransformPoint(m_Crew.transform.position);

        string Where()
            => $"person at {m_Crew.transform.position} ({InTheCartsOwnSpace()} in the cart's own " +
               $"space) moving {m_Crew.Body.linearVelocity}; cart at {m_Cart.transform.position}";

        bool StandingOnTheDeck()
        {
            var local = InTheCartsOwnSpace();
            var interior = m_Cart.Shape.InteriorLocal;
            var feet = local.y - (m_Crew.HeightMetres * 0.5f);

            return Mathf.Abs(local.x) < interior.extents.x
                   && Mathf.Abs(local.z) < interior.extents.z
                   && feet > DeckTopMetres - 0.1f
                   && feet < DeckTopMetres + 0.4f;
        }

        [UnityTest]
        public IEnumerator StandingByASideOfTheCartOffersAClimb()
        {
            StandThePersonAt(BesideTheLeftLip());
            yield return Steps.Seconds(0.5f);

            Assert.That(m_Crew.Climb.Offered, Is.Not.Null,
                $"a person within arm's length of the lip has to be offered the way in. {Where()}");
            Assert.That(m_Crew.Climb.Message, Is.EqualTo("Press space to climb in"),
                "and told which key does it, because nothing else on screen says so");
        }

        [UnityTest]
        public IEnumerator StandingByTheCartsEndWallOffersNothing()
        {
            StandThePersonAt(BeyondTheRearWall());
            yield return Steps.Seconds(0.5f);

            Assert.That(m_Crew.Climb.Offered, Is.Null,
                "the end of a cart is a wall to the roof. Offering a climb there sends a player " +
                $"into the bodywork and leaves them stuck in it. {Where()}");
            Assert.That(m_Crew.Climb.Message, Is.Empty);
        }

        [UnityTest]
        public IEnumerator StandingNowhereNearACartOffersNothing()
        {
            StandThePersonAt(new Vector3(20f, m_CrewProfile.heightMetres * 0.5f, 0f));
            yield return Steps.Seconds(0.5f);

            Assert.That(m_Crew.Climb.Offered, Is.Null, Where());
        }

        [UnityTest]
        public IEnumerator APressBesideTheCartPutsThemOnTheDeck()
        {
            StandThePersonAt(BesideTheLeftLip());
            yield return Steps.Seconds(0.5f);

            Assert.That(m_Crew.Climb.Offered, Is.Not.Null,
                $"nothing was on offer to climb when the press came. {Where()}");
            Assert.That(m_Crew.Grounded, Is.True,
                $"and they have to be standing on something to push off it. {Where()}");

            m_Keys.Jump = true;
            yield return new WaitForFixedUpdate();
            m_Keys.Jump = false;

            yield return Steps.Seconds(2f);

            Assert.That(StandingOnTheDeck(), Is.True,
                $"pressing beside the cart has to end with them standing in it. {Where()}");
        }

        [UnityTest]
        public IEnumerator APushOffIsNotWalkedBackOffThemWhileTheGroundIsStillInReach()
        {
            StandThePersonAt(BesideTheLeftLip());
            yield return Steps.Seconds(0.5f);

            m_Keys.Jump = true;
            yield return new WaitForFixedUpdate();
            m_Keys.Jump = false;

            var leftTheGroundAt = m_Crew.Body.linearVelocity;

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            var stillGoing = m_Crew.Body.linearVelocity;

            Assert.That(leftTheGroundAt.y, Is.GreaterThan(1f),
                $"they pushed off at {leftTheGroundAt} and have to be going up");
            Assert.That(stillGoing.y, Is.GreaterThan(leftTheGroundAt.y * 0.75f),
                $"two steps later they are going up at {stillGoing.y:F2} m/s, from {leftTheGroundAt.y:F2}. " +
                "The ground is still within reach of their feet for the first few steps of any leap, " +
                "so a person on their way up who still counts as standing has their legs walk the " +
                "push-off straight back off them. What a player sees is a climb that goes nowhere");
        }

        [UnityTest]
        public IEnumerator APressNowhereNearACartIsStillAJump()
        {
            StandThePersonAt(new Vector3(20f, m_CrewProfile.heightMetres * 0.5f, 0f));
            yield return Steps.Seconds(0.7f);

            var stoodAt = m_Crew.transform.position;

            m_Keys.Jump = true;
            yield return new WaitForFixedUpdate();
            m_Keys.Jump = false;

            yield return Steps.Seconds(0.25f);

            Assert.That(m_Crew.transform.position.y, Is.GreaterThan(stoodAt.y + 0.2f),
                $"space is still the jump key everywhere else on the apron. {Where()}");
            Assert.That(
                Vector3.Distance(
                    new Vector3(m_Crew.transform.position.x, 0f, m_Crew.transform.position.z),
                    new Vector3(stoodAt.x, 0f, stoodAt.z)),
                Is.LessThan(0.3f),
                "and a jump goes straight up rather than carrying them anywhere");
        }

        [UnityTest]
        public IEnumerator OnceTheyAreOnTheDeckTheyAreNotOfferedTheWayInAgain()
        {
            StandThePersonAt(m_Cart.transform.TransformPoint(
                new Vector3(0f, DeckTopMetres + (m_CrewProfile.heightMetres * 0.5f) + 0.05f, 0f)));

            yield return Steps.Seconds(1f);

            Assert.That(m_Crew.Climb.Offered, Is.Null,
                "standing on the deck, a press has to be the jump it always was. Offered the climb " +
                $"again, a player in a cart can never jump. {Where()}");
        }

        [UnityTest]
        public IEnumerator ClimbingIntoARollingCartLandsThemOnItAndKeepsThem()
        {
            StandThePersonAt(BesideTheLeftLip());
            yield return Steps.Seconds(0.5f);

            m_Cart.Body.linearVelocity = new Vector3(0f, 0f, 4f);

            m_Keys.Jump = true;
            yield return new WaitForFixedUpdate();
            m_Keys.Jump = false;

            var steps = Mathf.CeilToInt(2f / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                m_Cart.Body.linearVelocity = new Vector3(0f, 0f, 4f);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(StandingOnTheDeck(), Is.True,
                "a climb has to carry the cart's own motion, or a player aiming at a rolling cart " +
                $"lands where it used to be and watches it leave. {Where()}");
        }

        [UnityTest]
        public IEnumerator ACartDrivenAwayMidClimbLeavesThemBehind()
        {
            StandThePersonAt(BesideTheLeftLip());
            yield return Steps.Seconds(0.5f);

            m_Keys.Jump = true;
            yield return new WaitForFixedUpdate();
            m_Keys.Jump = false;

            yield return Steps.Seconds(0.1f);

            var steps = Mathf.CeilToInt(2f / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                m_Cart.Body.linearVelocity = new Vector3(0f, 0f, 14f);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(StandingOnTheDeck(), Is.False,
                "nothing carries a person to a cart that has gone. A climb is a launch and then " +
                $"gravity, and this one has to end on the tarmac. {Where()}");
        }
    }
}
