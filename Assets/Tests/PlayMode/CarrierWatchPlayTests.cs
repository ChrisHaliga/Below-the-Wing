using System.Collections;
using BelowTheWing.Cargo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// A carrier noticing that it can no longer hold what is riding on it.
    ///
    /// The moment the whole slice is built around. A bag leaving a cart is the game's central joke,
    /// and it only works if the boundary is somewhere a player can learn -- which means it has to be
    /// measured from what the cart is actually doing, not from what somebody asked it to do. A cart
    /// shoved sideways by another vehicle is throwing its load about just as surely as one taking a
    /// corner too fast, and nobody steered it.
    /// </summary>
    public sealed class CarrierWatchPlayTests
    {
        GameObject m_CartObject;
        Rigidbody m_CartBody;
        Carrier m_Deck;
        CarrierWatch m_Watch;

        GameObject m_BagObject;
        Carried m_Bag;

        [SetUp]
        public void SetUp()
        {
            m_CartObject = new GameObject("Cart");
            m_CartBody = m_CartObject.AddComponent<Rigidbody>();
            m_CartBody.useGravity = false;
            m_CartBody.mass = 550f;
            m_Deck = m_CartObject.AddComponent<Carrier>();
            m_Deck.Covers(Vector3.zero, new Vector3(2f, 2f, 4f));
            m_Watch = m_CartObject.AddComponent<CarrierWatch>();

            m_BagObject = new GameObject("Bag");
            var bagBody = m_BagObject.AddComponent<Rigidbody>();
            bagBody.useGravity = false;
            bagBody.mass = 20f;
            m_Bag = m_BagObject.AddComponent<Carried>();
            m_Bag.ComesOffAt = new WakeThresholds(lateralAcceleration: 6f, tiltDegrees: 25f, impulse: 400f);
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

        static IEnumerator Step(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator ACartDrivingGentlyKeepsItsLoad()
        {
            m_Bag.AttachTo(m_Deck);
            m_CartBody.linearVelocity = new Vector3(0f, 0f, 4f);

            yield return Step(3f);

            Assert.That(m_Bag.Attached, Is.True,
                "driving in a straight line has to be uneventful, or nobody will ever move a bag " +
                "anywhere");
        }

        [UnityTest]
        public IEnumerator ACartTippedOverDropsItsLoad()
        {
            m_Bag.AttachTo(m_Deck);
            m_CartObject.transform.rotation = Quaternion.Euler(0f, 0f, 40f);

            yield return Step(0.2f);

            Assert.That(m_Bag.Attached, Is.False,
                "a cart on its side holding onto its bags is the most obviously wrong thing this " +
                "mechanism could do");
        }

        [UnityTest]
        public IEnumerator ACartThrownSidewaysDropsItsLoad()
        {
            m_Bag.AttachTo(m_Deck);
            yield return Step(0.2f);

            // Swung hard sideways, which is what a corner taken too fast does to a deck.
            for (var i = 0; i < 6; i++)
            {
                m_CartBody.linearVelocity += new Vector3(4f, 0f, 0f);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(m_Bag.Attached, Is.False, "this is the corner the whole game is about");
        }

        [UnityTest]
        public IEnumerator SomethingThatHoldsOnHarderStaysOnLonger()
        {
            var gripping = new GameObject("Rider");
            var grippingBody = gripping.AddComponent<Rigidbody>();
            grippingBody.useGravity = false;
            var rider = gripping.AddComponent<Carried>();
            rider.ComesOffAt = new WakeThresholds(lateralAcceleration: 40f, tiltDegrees: 70f, impulse: 4000f);

            m_Bag.AttachTo(m_Deck);
            rider.AttachTo(m_Deck);
            yield return Step(0.2f);

            // Hard enough to empty the deck of bags, not hard enough to tear off somebody gripping
            // a rail. A fifth of a metre a second per step is about fifteen metres per second
            // squared, which sits between the two thresholds on purpose.
            for (var i = 0; i < 6; i++)
            {
                m_CartBody.linearVelocity += new Vector3(0.3f, 0f, 0f);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(m_Bag.Attached, Is.False, "the bag goes");
            Assert.That(rider.Attached, Is.True,
                "and whoever is holding on does not, which is what makes holding on worth doing");

            Object.DestroyImmediate(gripping);
        }

        [UnityTest]
        public IEnumerator AMachineThatIsNotInChargeDecidesNothing()
        {
            m_Watch.OursToDecide = false;
            m_Bag.AttachTo(m_Deck);
            m_CartObject.transform.rotation = Quaternion.Euler(0f, 0f, 60f);

            yield return Step(0.3f);

            Assert.That(m_Bag.Attached, Is.True,
                "every machine deciding for itself means a bag that flies on one screen and rides on " +
                "another, which is worse than one that flies slightly late everywhere");
        }
    }
}
