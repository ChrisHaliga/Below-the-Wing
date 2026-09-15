using System.Collections;
using BelowTheWing.Cargo;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class GrabbingWhatYouLookAtPlayTests
    {
        TestApron m_Apron;
        BagProfile m_BagProfile;
        GameObject m_Person;
        Rigidbody m_Body;
        Transform m_Anchor;
        Hand m_Hand;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron(60f);
            m_BagProfile = TestProfiles.CheckedBag();

            m_Person = new GameObject("Person");
            m_Person.transform.position = new Vector3(0f, 1f, 0f);
            m_Body = m_Person.AddComponent<Rigidbody>();
            m_Body.isKinematic = true;
            m_Apron.Track(m_Body);

            var anchor = new GameObject("Right Hand");
            anchor.transform.SetParent(m_Person.transform, worldPositionStays: false);
            anchor.transform.localPosition = new Vector3(0.3f, 0.2f, 0.6f);
            m_Anchor = anchor.transform;

            m_Hand = new Hand(m_Anchor, m_Body, HandSettings.Default);
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_BagProfile);
        }

        Bag ABagAt(Vector3 where)
        {
            var go = new GameObject("Bag");
            go.transform.position = where;
            go.AddComponent<BoxCollider>();
            go.AddComponent<HandUse>().As = HandUse.Category.Carry;
            var bag = go.AddComponent<Bag>();
            bag.Configure(m_BagProfile);
            m_Apron.Track(bag);
            return bag;
        }

        Ray Looking(Vector3 at) => new Ray(m_Person.transform.position, (at - m_Person.transform.position).normalized);

        [UnityTest]
        public IEnumerator TheBagBeingLookedAtIsTakenRatherThanTheNearestOne()
        {
            var nearer = ABagAt(new Vector3(-0.45f, 1f, 0.35f));
            var lookedAt = ABagAt(new Vector3(0.45f, 1f, 0.6f));

            yield return Steps.Seconds(0.3f);

            m_Hand.Press(Time.time, Looking(lookedAt.transform.position));

            Assert.That(m_Hand.Carrying, Is.SameAs(lookedAt.Body),
                $"the hand took {(m_Hand.Carrying != null ? m_Hand.Carrying.name : "nothing")}. " +
                "Picking by proximity hands a player whichever bag happens to be closest to their " +
                "shoulder, which is rarely the one under the crosshair and never the one they meant");

            Assert.That(m_Hand.Carrying, Is.Not.SameAs(nearer.Body));
        }

        [UnityTest]
        public IEnumerator ABagOutOfReachIsNotTakenHoweverSquarelyItIsLookedAt()
        {
            var far = ABagAt(new Vector3(0f, 1f, 8f));

            yield return Steps.Seconds(0.3f);

            m_Hand.Press(Time.time, Looking(far.transform.position));

            Assert.That(m_Hand.Carrying, Is.Null,
                "a bag eight metres away is a bag nobody can reach. Taken anyway, it flies to the " +
                "player's hand from across the apron");
        }

        [UnityTest]
        public IEnumerator ABagOffToTheSideIsTakenWhenNothingIsUnderTheCrosshair()
        {
            var aside = ABagAt(new Vector3(0f, 1f, 0.8f));

            yield return Steps.Seconds(0.5f);

            var pastIt = aside.transform.position + (Vector3.right * 0.4f);
            var offBy = Vector3.Angle(
                aside.transform.position - m_Person.transform.position,
                pastIt - m_Person.transform.position);

            m_Hand.Press(Time.time, Looking(pastIt));

            Assert.That(m_Hand.Carrying, Is.SameAs(aside.Body),
                $"looking {offBy:F0} degrees past a bag within reach still has to pick it up. A hand " +
                "that takes only what the crosshair is exactly on asks a player to aim at a bag by " +
                "the pixel before they can lift it");

            Assert.That(offBy, Is.LessThan(HandSettings.Default.reachesIntoConeDegrees),
                "and the miss has to be inside the cone, or this proves nothing about the cone");
        }

        [UnityTest]
        public IEnumerator ABagBehindThePlayerIsLeftAlone()
        {
            ABagAt(new Vector3(0f, 1f, -0.5f));

            yield return Steps.Seconds(0.3f);

            m_Hand.Press(Time.time, new Ray(m_Person.transform.position, Vector3.forward));

            Assert.That(m_Hand.Carrying, Is.Null,
                "a bag behind the player is not one they are reaching for. Picked up regardless, a " +
                "player walking forward drags whatever they just walked past");
        }

        [UnityTest]
        public IEnumerator AHandHoldsTheSpotItGrabbed()
        {
            var bag = ABagAt(new Vector3(0.35f, 1f, 0.6f));

            yield return Steps.Seconds(0.3f);

            m_Hand.Press(Time.time, Looking(bag.transform.position));

            Assert.That(m_Hand.Carrying, Is.Not.Null);
            Assert.That(m_Hand.HoldingAt.HasValue, Is.True,
                "a hand with nothing to say about where it took hold cannot be drawn on the bag, " +
                "and two hands on one bag both hold its middle");

            var held = bag.transform.InverseTransformPoint(m_Hand.HoldingAt.Value);
            var half = m_BagProfile.sizeMetres * 0.5f;

            Assert.That(Mathf.Abs(held.x), Is.LessThanOrEqualTo(half.x + 0.05f));
            Assert.That(Mathf.Abs(held.y), Is.LessThanOrEqualTo(half.y + 0.05f));
            Assert.That(Mathf.Abs(held.z), Is.LessThanOrEqualTo(half.z + 0.05f),
                $"the hold is at {held} on a bag half {half}. A grip outside the bag is not on it");
        }

        [UnityTest]
        public IEnumerator TwoHandsOnOneBagHoldTwoDifferentSpots()
        {
            var bag = ABagAt(new Vector3(0f, 1f, 0.6f));

            var otherAnchor = new GameObject("Left Hand");
            otherAnchor.transform.SetParent(m_Person.transform, worldPositionStays: false);
            otherAnchor.transform.localPosition = new Vector3(-0.3f, 0.2f, 0.6f);
            var other = new Hand(otherAnchor.transform, m_Body, HandSettings.Default);

            yield return Steps.Seconds(0.3f);

            m_Hand.Press(Time.time, Looking(bag.transform.position));
            other.Press(Time.time, Looking(bag.transform.position));

            Assert.That(m_Hand.Carrying, Is.SameAs(bag.Body));
            Assert.That(other.Carrying, Is.SameAs(bag.Body),
                "a bag big enough for two hands has to accept the second one, or the left hand is " +
                "decoration whenever the right is full");

            Assert.That(
                Vector3.Distance(m_Hand.HoldingAt.Value, other.HoldingAt.Value), Is.GreaterThan(0.1f),
                "both hands took hold of the same point, so the bag is carried by a single grip " +
                "drawn twice");
        }
    }
}
