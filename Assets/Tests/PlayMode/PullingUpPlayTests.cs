using System.Collections;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class PullingUpPlayTests
    {
        TestApron m_Apron;
        CrewProfile m_Profile;
        VehicleProfile m_CartProfile;
        VehicleController m_Cart;
        CrewCharacter m_Crew;
        Hands m_Hands;
        HeldKeys m_Keys;
        Transform m_LeftAnchor;
        Transform m_RightAnchor;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_Profile = TestProfiles.CrewMember();
            m_CartProfile = TestProfiles.Cart();

            m_Cart = m_Apron.AddVehicle(
                m_CartProfile, "Cart 1", Vector3.zero, Quaternion.identity, TestShapes.Cart());

            m_Crew = m_Apron.AddCrew(
                m_Profile,
                m_Cart.transform.TransformPoint(new Vector3(-1.2f, m_Profile.heightMetres * 0.5f, 0f)));

            m_LeftAnchor = Anchor(Hands.LeftAnchorName, new Vector3(-0.3f, 0.3f, 0.45f));
            m_RightAnchor = Anchor(Hands.RightAnchorName, new Vector3(0.3f, 0.3f, 0.45f));

            m_Hands = new Hands(m_LeftAnchor, m_RightAnchor, m_Crew.Body, m_Profile.hands);
            m_Crew.Handling = m_Hands;

            m_Keys = new HeldKeys();
            m_Crew.IntentSource = m_Keys;
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_Profile);
            Object.DestroyImmediate(m_CartProfile);
        }

        Transform Anchor(string called, Vector3 local)
        {
            var anchor = new GameObject(called).transform;
            anchor.SetParent(m_Crew.transform, worldPositionStays: false);
            anchor.localPosition = local;
            return anchor;
        }

        Rigidbody ARailAt(Vector3 where)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Rail";
            go.transform.localScale = new Vector3(2f, 0.1f, 0.1f);
            go.transform.position = where;
            var body = go.AddComponent<Rigidbody>();
            body.mass = 2000f;
            body.isKinematic = true;
            go.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;
            m_Apron.Track(body);
            return body;
        }

        Ray Looking(Vector3 at)
            => new Ray(m_Crew.transform.position, (at - m_Crew.transform.position).normalized);

        IEnumerator BothHandsOn(Rigidbody rail)
        {
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time, Looking(rail.position));
            m_Hands.Right.Press(Time.time, Looking(rail.position));

            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator HoldingWithBothHandsAndPressingLiftsThem()
        {
            var rail = ARailAt(m_Crew.transform.position + new Vector3(0f, 0.55f, 0.5f));

            yield return BothHandsOn(rail);

            Assert.That(m_Hands.BothHoldingAt.HasValue, Is.True,
                "both hands have to be holding before a pull up is worth testing");

            var startedAt = m_Crew.transform.position.y;

            m_Keys.Hoist = true;
            yield return Steps.Seconds(1.5f);
            m_Keys.Hoist = false;

            Assert.That(m_Crew.transform.position.y, Is.GreaterThan(startedAt + 0.2f),
                $"they went from {startedAt:F2} m to {m_Crew.transform.position.y:F2} m. A pull up " +
                "that lifts nobody is a key that does nothing while both hands are full");
        }

        [UnityTest]
        public IEnumerator TheyStopWithTheirFeetAboutLevelWithTheirHands()
        {
            var rail = ARailAt(m_Crew.transform.position + new Vector3(0f, 0.55f, 0.5f));

            yield return BothHandsOn(rail);

            m_Keys.Hoist = true;
            yield return Steps.Seconds(2.5f);

            var hands = m_Hands.BothHoldingAt.Value;
            var feet = m_Crew.transform.position.y - (m_Crew.HeightMetres * 0.5f);

            m_Keys.Hoist = false;

            Assert.That(feet, Is.GreaterThan(hands.y - 0.3f),
                $"their feet stopped at {feet:F2} m with their hands at {hands.y:F2} m. A pull up " +
                "that stops short leaves them hanging below what they are holding");
            Assert.That(feet, Is.LessThan(hands.y + 0.6f),
                $"their feet reached {feet:F2} m against hands at {hands.y:F2} m. A pull up with no " +
                "top to it is a lift that keeps going for as long as the key is held");
        }

        float AcrossFrom(Vector3 point)
        {
            var gap = point - m_Crew.transform.position;
            return new Vector2(gap.x, gap.z).magnitude;
        }

        [UnityTest]
        public IEnumerator ThePullCarriesThemInOverTheirHands()
        {
            var rail = ARailAt(m_Crew.transform.position + new Vector3(0f, 0.55f, 0.6f));

            yield return BothHandsOn(rail);

            var reachedAcross = AcrossFrom(m_Hands.BothHoldingAt.Value);

            m_Keys.Hoist = true;
            yield return Steps.Seconds(2.5f);

            var finishedAcross = AcrossFrom(m_Hands.BothHoldingAt ?? rail.position);
            m_Keys.Hoist = false;

            Assert.That(finishedAcross, Is.LessThan(0.15f),
                $"they reached {reachedAcross:F2} m across to their hands and finished {finishedAcross:F2} m " +
                "away from them. A pull up that only goes straight up leaves them hanging alongside " +
                "the cart rather than standing over the deck");
        }

        [UnityTest]
        public IEnumerator TheyStayTuckedUpUntilTheyAreDownOnSomething()
        {
            var rail = ARailAt(m_Crew.transform.position + new Vector3(0f, 0.55f, 0.6f));

            yield return BothHandsOn(rail);

            m_Keys.Hoist = true;
            yield return Steps.Seconds(2.5f);
            m_Keys.Hoist = false;

            yield return new WaitForFixedUpdate();

            Assume.That(m_Crew.Grounded, Is.False,
                "this only says anything while they are still off the ground");
            Assert.That(m_Crew.HeightMetres, Is.LessThan(m_Profile.heightMetres - 0.01f),
                $"the key came up and they stretched back to {m_Crew.HeightMetres:F2} m while still " +
                "in the air. Standing up under a cart roof is how a climb ends with a head against it");
        }

        [UnityTest]
        public IEnumerator TheyDuckWhileTheyPull()
        {
            var rail = ARailAt(m_Crew.transform.position + new Vector3(0f, 0.55f, 0.5f));

            yield return BothHandsOn(rail);

            m_Keys.Hoist = true;
            yield return Steps.Seconds(0.5f);

            var tall = m_Crew.HeightMetres;
            m_Keys.Hoist = false;

            Assert.That(tall, Is.LessThan(m_Profile.heightMetres - 0.01f),
                $"they stayed {tall:F2} m tall through the pull. Hauling yourself onto something " +
                "means tucking up under it, and a body at full height meets whatever is overhead");
        }

        [UnityTest]
        public IEnumerator OneHandIsNotEnoughToPullUpOn()
        {
            var rail = ARailAt(m_Crew.transform.position + new Vector3(0f, 0.55f, 0.5f));

            yield return Steps.Seconds(0.5f);

            m_Hands.Right.Press(Time.time, Looking(rail.position));
            yield return new WaitForFixedUpdate();

            Assert.That(m_Hands.Right.HoldingOnto, Is.SameAs(rail));
            Assert.That(m_Hands.BothHoldingAt.HasValue, Is.False);

            var startedAt = m_Crew.transform.position.y;

            m_Keys.Hoist = true;
            yield return Steps.Seconds(1.5f);
            m_Keys.Hoist = false;

            Assert.That(m_Crew.transform.position.y, Is.LessThan(startedAt + 0.2f),
                $"one hand hauled them from {startedAt:F2} m to {m_Crew.transform.position.y:F2} m. " +
                "A pull up is a two handed thing, and one hand on a rail is a person hanging from it");
        }

        [UnityTest]
        public IEnumerator PressingWithNothingHeldStillJumps()
        {
            yield return Steps.Seconds(0.7f);

            var stoodAt = m_Crew.transform.position.y;

            m_Keys.Jump = true;
            m_Keys.Hoist = true;
            yield return new WaitForFixedUpdate();
            m_Keys.Jump = false;

            yield return Steps.Seconds(0.25f);
            m_Keys.Hoist = false;

            Assert.That(m_Crew.transform.position.y, Is.GreaterThan(stoodAt + 0.2f),
                "with empty hands the same key is the jump it always was");
        }
    }
}
