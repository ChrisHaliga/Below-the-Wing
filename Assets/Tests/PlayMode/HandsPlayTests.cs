using System.Collections;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class HandsPlayTests
    {
        TestApron m_Apron;
        CrewProfile m_Profile;
        CrewCharacter m_Crew;
        Hands m_Hands;
        Transform m_LeftAnchor;
        Transform m_RightAnchor;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_Profile = TestProfiles.CrewMember();
            m_Crew = m_Apron.AddCrew(m_Profile, new Vector3(0f, 0.9f, 0f));

            m_LeftAnchor = Anchor("Left Hand", new Vector3(-0.35f, 0.3f, 0.6f));
            m_RightAnchor = Anchor("Right Hand", new Vector3(0.35f, 0.3f, 0.6f));
            m_Hands = new Hands(m_LeftAnchor, m_RightAnchor, m_Crew.Body, HandSettings.Default);
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_Profile);
        }

        Ray Looking(Vector3 at)
            => new Ray(m_Crew.transform.position, (at - m_Crew.transform.position).normalized);

        Transform Anchor(string name, Vector3 local)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(m_Crew.transform, worldPositionStays: false);
            anchor.localPosition = local;
            return anchor;
        }

        Rigidbody ABagAt(Vector3 where)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Bag";
            go.transform.localScale = new Vector3(0.4f, 0.25f, 0.6f);
            go.transform.position = new Vector3(where.x, 0.13f, where.z);
            var body = go.AddComponent<Rigidbody>();
            body.mass = 20f;
            go.AddComponent<HandUse>().As = HandUse.Category.Carry;
            m_Apron.Track(body);
            return body;
        }

        Rigidbody ARailAt(Vector3 where)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Cart";
            go.transform.position = where;
            var body = go.AddComponent<Rigidbody>();
            body.mass = 550f;
            body.useGravity = false;
            body.isKinematic = true;
            go.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;
            m_Apron.Track(body);
            return body;
        }

        [UnityTest]
        public IEnumerator TheLeftHandPicksUpABagAndTheRightIsUnchanged()
        {
            var bag = ABagAt(m_LeftAnchor.position + Vector3.forward * 0.5f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time, Looking(bag.position));

            Assert.That(m_Hands.Left.Carrying, Is.SameAs(bag));
            Assert.That(m_Hands.Right.Empty, Is.True);
        }

        [UnityTest]
        public IEnumerator BothHandsCanCarryABagEach()
        {
            var left = ABagAt(m_LeftAnchor.position + Vector3.forward * 0.4f);
            var right = ABagAt(m_RightAnchor.position + Vector3.forward * 0.4f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time, Looking(left.position));
            m_Hands.Right.Press(Time.time, Looking(right.position));

            Assert.That(m_Hands.Left.Carrying, Is.SameAs(left));
            Assert.That(m_Hands.Right.Carrying, Is.SameAs(right),
                "two hands, two bags. Hands with a capacity of one between them would make the " +
                "second press drop the first bag or do nothing");
        }

        [UnityTest]
        public IEnumerator OneHandCanHoldOnWhileTheOtherCarries()
        {
            var bag = ABagAt(m_LeftAnchor.position + Vector3.forward * 0.4f);
            var rail = ARailAt(m_RightAnchor.position + Vector3.right * 1.0f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time, Looking(bag.position));
            m_Hands.Right.Press(Time.time, Looking(rail.position));

            Assert.That(m_Hands.Left.Carrying, Is.SameAs(bag));
            Assert.That(m_Hands.Right.HoldingOnto, Is.SameAs(rail),
                "a rail in one hand and a bag in the other is the whole reason to have two");
        }

        [UnityTest]
        public IEnumerator EachHandReachesFromItsOwnAnchor()
        {
            var bag = ABagAt(m_LeftAnchor.position + Vector3.left * 0.6f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Right.Press(Time.time, Looking(bag.position));
            Assert.That(m_Hands.Right.Empty, Is.True, "too far for the right hand");

            m_Hands.Left.Press(Time.time, Looking(bag.position));
            Assert.That(m_Hands.Left.Carrying, Is.SameAs(bag));
        }

        [UnityTest]
        public IEnumerator AHandTakesTheNearestThingWhateverItIs()
        {
            var bag = ABagAt(m_RightAnchor.position + Vector3.forward * 0.3f);
            var placed1 = ARailAt(m_RightAnchor.position + Vector3.right * 1.6f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Right.Press(Time.time, Looking(bag.position));

            Assert.That(m_Hands.Right.Carrying, Is.SameAs(bag));
            Assert.That(m_Hands.Right.HoldingOnto, Is.Null);
        }

        [UnityTest]
        public IEnumerator TheReleaseOfThePressThatPickedUpDoesNotDrop()
        {
            var bag = ABagAt(m_LeftAnchor.position + Vector3.forward * 0.4f);
            yield return Steps.Seconds(0.5f);

            var now = Time.time;
            m_Hands.Left.Press(now, Looking(bag.position));
            m_Hands.Left.Release(now + 0.05f, Vector3.forward);

            Assert.That(m_Hands.Left.Carrying, Is.SameAs(bag),
                "a click that picks something up and drops it on the way back up is a click that " +
                "does nothing");
        }

        [UnityTest]
        public IEnumerator ATapSetsItDownGently()
        {
            var bag = ABagAt(m_LeftAnchor.position + Vector3.forward * 0.4f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time, Looking(bag.position));
            m_Hands.Left.Release(Time.time, Vector3.forward);
            yield return Steps.Seconds(1f);
            Assert.That(m_Hands.Left.Carrying, Is.SameAs(bag), "held, before the tap");

            var now = Time.time;
            m_Hands.Left.Press(now, Looking(bag.position));
            m_Hands.Left.Release(now + 0.05f, Vector3.forward);
            yield return new WaitForFixedUpdate();

            Assert.That(m_Hands.Left.Empty, Is.True);
            Assert.That(bag.linearVelocity.magnitude, Is.LessThan(1f),
                "a tap is putting it down, not throwing it. Without that, stacking a cart is a " +
                "game of not flinching");
        }

        [UnityTest]
        public IEnumerator AFullWindUpThrowsHardWhereThePlayerIsLooking()
        {
            var bag = ABagAt(m_LeftAnchor.position + Vector3.forward * 0.4f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time, Looking(bag.position));
            m_Hands.Left.Release(Time.time, Vector3.forward);
            yield return Steps.Seconds(0.5f);

            var now = Time.time;
            m_Hands.Left.Press(now, Looking(bag.position));
            m_Hands.Left.Release(now + HandSettings.Default.fullChargeSeconds + 0.1f, Vector3.forward);
            yield return new WaitForFixedUpdate();

            Assert.That(m_Hands.Left.Empty, Is.True);
            Assert.That(bag.linearVelocity.z, Is.GreaterThan(8f),
                $"thrown at {bag.linearVelocity.magnitude:F1} m/s. A full wind-up is the hardest " +
                "throw there is");
        }

        [UnityTest]
        public IEnumerator ACarriedBagComesAlongWithThePlayer()
        {
            var placed2 = ABagAt(m_LeftAnchor.position + Vector3.forward * 0.4f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time, Looking(placed2.position));
            var bag = m_Hands.Left.Carrying;
            var startedAt = m_Crew.transform.position;

            m_Crew.IntentSource = new HeldKeys(new Vector2(0f, 1f));
            yield return Steps.Seconds(2f);

            Assert.That(Vector3.Distance(m_Crew.transform.position, startedAt), Is.GreaterThan(3f),
                "the player walked for two seconds");
            Assert.That(Vector3.Distance(bag.position, m_LeftAnchor.position), Is.LessThan(0.5f),
                "the bag is a body pulled to the hand, so it goes where the hand goes");
        }

        [UnityTest]
        public IEnumerator AHardHitKnocksABagOutOfTheHand()
        {
            var placed3 = ABagAt(m_LeftAnchor.position + Vector3.forward * 0.4f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time, Looking(placed3.position));
            var bag = m_Hands.Left.Carrying;
            yield return Steps.Seconds(0.5f);

            bag.AddForce(new Vector3(600f, 0f, 0f), ForceMode.Impulse);
            yield return Steps.Seconds(0.2f);
            m_Hands.Tick();

            Assert.That(m_Hands.Left.Empty, Is.True,
                "a hand that cannot be emptied by a three tonne tractor is a hand that is not " +
                $"taking part in the physics. Bag {Vector3.Distance(bag.position, m_LeftAnchor.position):F2} m " +
                $"from the hand moving {bag.linearVelocity}, holder moving {m_Crew.Body.linearVelocity}");
        }

        [UnityTest]
        public IEnumerator TheHolderIsNotPushedByWhatTheyCarry()
        {
            var bag = ABagAt(m_LeftAnchor.position + Vector3.forward * 0.35f);
            bag.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time, Looking(bag.position));
            Assert.That(m_Hands.Left.Carrying, Is.SameAs(bag), "held, so that the spring is real");
            yield return Steps.Seconds(1.5f);

            Assert.That(m_Crew.Body.linearVelocity.magnitude, Is.LessThan(0.3f),
                $"the holder is moving at {m_Crew.Body.linearVelocity.magnitude:F1} m/s while standing " +
                "still holding a bag. A held thing overlapping its holder is the solver's cue to " +
                "shove the holder out, every step, for as long as it is held");
        }

        [UnityTest]
        public IEnumerator ACarriedBagComesToRestAtTheHand()
        {
            var placed4 = ABagAt(m_LeftAnchor.position + Vector3.forward * 0.4f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time, Looking(placed4.position));
            var bag = m_Hands.Left.Carrying;
            yield return Steps.Seconds(2f);

            var held = m_Hands.Left.HoldingAt.Value;

            Assert.That(Vector3.Distance(held, m_LeftAnchor.position), Is.LessThan(0.2f),
                $"the spot the hand took hold of is at {held}, the hand at {m_LeftAnchor.position}, " +
                $"the bag centre at {bag.position} moving {bag.linearVelocity}. A hand draws the " +
                "grip it took, so it is the grip that has to arrive at the hand rather than the " +
                "middle of whatever was grabbed");
            Assert.That(bag.linearVelocity.magnitude, Is.LessThan(0.2f),
                "a bag that is still being hauled toward the hand two seconds later is a spring " +
                "with no damping, and it never stops");
        }

        [UnityTest]
        public IEnumerator ACarriedBagSettlesIntoOnePoseAndKeepsItWhenThePlayerTurns()
        {
            var bag = ABagAt(m_LeftAnchor.position + Vector3.forward * 0.4f);
            bag.transform.rotation = Quaternion.Euler(30f, 40f, 50f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time, Looking(bag.position));
            yield return Steps.Seconds(1f);

            Assert.That(bag.angularVelocity.magnitude, Is.LessThan(0.2f),
                $"still turning at {bag.angularVelocity.magnitude:F2} rad/s a second after being picked " +
                "up. A hand holds a bag still; one that only pulls on its middle leaves it spinning");
            Assert.That(Quaternion.Angle(bag.rotation, m_Crew.transform.rotation), Is.LessThan(5f),
                "and it sits the way the hand does, whatever angle it was lying at");

            m_Crew.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            yield return Steps.Seconds(1f);

            Assert.That(Quaternion.Angle(bag.rotation, m_Crew.transform.rotation), Is.LessThan(5f),
                "turning the player turns the bag with them");
        }

        [UnityTest]
        public IEnumerator AThrowGoesWhereThePlayerIsLookingPitchIncluded()
        {
            var bag = ABagAt(m_LeftAnchor.position + Vector3.forward * 0.4f);
            yield return Steps.Seconds(0.5f);

            m_Hands.Left.Press(Time.time, Looking(bag.position));
            m_Hands.Left.Release(Time.time, Vector3.forward);
            yield return Steps.Seconds(1f);

            var lookingUp = Quaternion.Euler(-30f, 0f, 0f) * Vector3.forward;
            var now = Time.time;
            m_Hands.Left.Press(now, Looking(bag.position));
            m_Hands.Left.Release(now + HandSettings.Default.fullChargeSeconds + 0.1f, lookingUp);
            yield return new WaitForFixedUpdate();

            Assert.That(Vector3.Angle(bag.linearVelocity, lookingUp), Is.LessThan(5f),
                $"looking 30 degrees up, the bag left at {Vector3.Angle(bag.linearVelocity, Vector3.forward):F0} " +
                "degrees. In first person the lob is aimed by looking up; a fixed lift is a throw " +
                "nobody can aim");
        }
    }
}
