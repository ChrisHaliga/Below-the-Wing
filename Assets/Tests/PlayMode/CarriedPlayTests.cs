using System.Collections;
using BelowTheWing.Cargo;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// Riding on something, and coming off it.
    ///
    /// The mechanism the whole slice rests on. An object at rest on a carrier is attached to it
    /// rather than simulated against it, because a loose rigidbody on a moving platform jitters,
    /// drifts, and eventually launches for no reason a player can read -- the floor moves into it
    /// every step and contact resolution pushes it back out.
    ///
    /// What has to be true is that it rides perfectly still while attached, and leaves with the
    /// speed it actually had when it stops being attached. Both of those are about velocity at a
    /// point rather than velocity overall, because the far end of a turning cart is travelling
    /// faster than the near end.
    /// </summary>
    public sealed class CarriedPlayTests
    {
        GameObject m_CartObject;
        Carrier m_Deck;
        GameObject m_BagObject;
        Carried m_Bag;

        [SetUp]
        public void SetUp()
        {
            m_CartObject = new GameObject("Cart");
            var cartBody = m_CartObject.AddComponent<Rigidbody>();
            cartBody.useGravity = false;
            cartBody.mass = 550f;
            m_Deck = m_CartObject.AddComponent<Carrier>();
            m_Deck.Covers(Vector3.zero, new Vector3(2f, 1f, 4f));

            m_BagObject = new GameObject("Bag");
            var bagBody = m_BagObject.AddComponent<Rigidbody>();
            bagBody.useGravity = false;
            bagBody.mass = 20f;
            m_Bag = m_BagObject.AddComponent<Carried>();
            m_Bag.CannotSettleForSeconds = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_BagObject != null)
            {
                Object.DestroyImmediate(m_BagObject);
            }

            if (m_CartObject != null)
            {
                Object.DestroyImmediate(m_CartObject);
            }
        }

        [UnityTest]
        public IEnumerator SomethingRidingACarrierDoesNotMoveRelativeToIt()
        {
            m_BagObject.transform.position = new Vector3(0.5f, 0.4f, 1f);
            m_Bag.AttachTo(m_Deck);

            // Where the bag sits on the deck, in the deck's own frame -- which is the thing that
            // has to stay the same however far the cart drives.
            var satAt = m_CartObject.transform.InverseTransformPoint(m_BagObject.transform.position);

            m_CartObject.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, 8f);
            yield return Steps.Seconds(3f);

            var sitsAt = m_CartObject.transform.InverseTransformPoint(m_BagObject.transform.position);
            Assert.That(Vector3.Distance(sitsAt, satAt), Is.LessThan(0.001f),
                "a bag that creeps about its deck while the cart drives is the jitter this mechanism " +
                "exists to remove");
        }

        [UnityTest]
        public IEnumerator SomethingRidingACarrierCostsNothingToSimulate()
        {
            m_Bag.AttachTo(m_Deck);
            yield return null;

            Assert.That(m_Bag.Body.isKinematic, Is.True,
                "an attached object is part of its carrier, so there is nothing for the solver to " +
                "work out about it and nothing for it to get wrong");
        }

        [UnityTest]
        public IEnumerator SomethingShakenLooseLeavesWithTheSpeedItHad()
        {
            m_BagObject.transform.position = new Vector3(0f, 0.4f, 1f);
            m_Bag.AttachTo(m_Deck);

            m_CartObject.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, 8f);
            yield return Steps.Seconds(0.5f);

            m_Bag.Wake(Time.time);

            Assert.That(m_Bag.Body.isKinematic, Is.False, "it is its own object again");
            Assert.That(m_Bag.Body.linearVelocity.z, Is.EqualTo(8f).Within(0.5f),
                "a bag that loses the cart's speed on release drops straight down instead of carrying " +
                "on, which reads as the throw being swallowed");
        }

        [UnityTest]
        public IEnumerator SomethingThrownAddsItsOwnEffortToTheCarriersSpeed()
        {
            m_Bag.AttachTo(m_Deck);
            m_CartObject.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, 6f);
            yield return Steps.Seconds(0.3f);

            m_Bag.Wake(Time.time, ofItsOwn: new Vector3(0f, 0f, 5f));

            Assert.That(m_Bag.Body.linearVelocity.z, Is.EqualTo(11f).Within(0.5f),
                "throwing forward from a moving cart has to go further than throwing from standing, " +
                "which is the whole reason throwing belongs in a physics slice");
        }

        [UnityTest]
        public IEnumerator SomethingOnTheFarEndOfATurningCarrierLeavesFasterThanTheNearEnd()
        {
            var cartBody = m_CartObject.GetComponent<Rigidbody>();
            cartBody.angularVelocity = new Vector3(0f, 2f, 0f);

            m_BagObject.transform.position = new Vector3(0f, 0f, 1.8f);
            m_Bag.AttachTo(m_Deck);
            yield return null;

            var far = m_Deck.VelocityAt(m_BagObject.transform.position).magnitude;
            var near = m_Deck.VelocityAt(m_CartObject.transform.position).magnitude;

            Assert.That(far, Is.GreaterThan(near + 0.5f),
                "the far end of a turning cart is travelling faster, and a bag thrown off it should " +
                "go where that corner of the deck was going rather than where the middle was");
        }

        [UnityTest]
        public IEnumerator SomethingJustShakenLooseDoesNotSettleStraightBackDown()
        {
            m_Bag.AttachTo(m_Deck);
            m_Bag.Wake(Time.time);

            Assert.That(m_Bag.WouldSettle(Time.time), Is.False,
                "a bag flung off on a corner sticking straight back down before the corner has " +
                "finished is the mechanism visibly fighting the player");
            Assert.That(m_Bag.WouldSettle(Time.time + 2f), Is.True, "but it has to be allowed back eventually");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ACarrierGoingAwayLeavesItsCargoTravellingRatherThanHanging()
        {
            m_BagObject.transform.position = new Vector3(0f, 0.4f, 1f);
            m_Bag.AttachTo(m_Deck);

            m_CartObject.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0f, 6f);
            yield return Steps.Seconds(0.5f);

            // The cart quits the session mid-drive.
            Object.DestroyImmediate(m_CartObject);
            m_CartObject = null;

            Assert.That(m_BagObject, Is.Not.Null, "cargo must not be deleted with whatever was carrying it");
            Assert.That(m_Bag.Attached, Is.False);
            Assert.That(m_Bag.Body.linearVelocity.z, Is.EqualTo(6f).Within(0.5f),
                "read after the carrier has gone there is nothing left to ask, and the bag hangs in " +
                "mid-air exactly where the cart used to be");
        }

        [UnityTest]
        public IEnumerator ACarrierGoingAwayDoesNotCountAsBeingShakenLoose()
        {
            m_Bag.AttachTo(m_Deck);

            Object.DestroyImmediate(m_CartObject);
            m_CartObject = null;

            Assert.That(m_Bag.WouldSettle(Time.time), Is.True,
                "nothing was crossed, so nothing should behave as though it was. A bag dropped by a " +
                "cart that quit has no reason to refuse the next cart it lands in");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ACarrierKnowsWhatIsRidingOnIt()
        {
            m_Bag.AttachTo(m_Deck);
            Assert.That(m_Deck.Riding, Has.Count.EqualTo(1));

            m_Bag.Wake(Time.time);
            Assert.That(m_Deck.Riding, Is.Empty,
                "a carrier still listing something it let go of will try to let go of it twice, and " +
                "the second time there is nothing there");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ACarrierOnlyTakesThingsInsideItsOwnSpace()
        {
            Assert.That(m_Deck.Reaches(new Vector3(0f, 0f, 1f)), Is.True, "on the deck");
            Assert.That(m_Deck.Reaches(new Vector3(0f, 0f, 9f)), Is.False,
                "a deck that reaches the whole apron would pick up bags lying beside the cart");

            yield return null;
        }
    }
}
