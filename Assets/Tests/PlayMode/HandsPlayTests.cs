using System.Collections;
using System.Collections.Generic;
using BelowTheWing.Cargo;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// Picking a bag up, winding up, and letting go of it.
    ///
    /// Hands are a carrier like any other, which is the point: a bag carried by somebody riding a
    /// cart is a bag on a player on a cart, and all of it comes apart through the same mechanism.
    /// The case that proves the chain works is a bag thrown from a moving cart, which has to carry
    /// the cart's motion as well as the throw.
    /// </summary>
    public sealed class HandsPlayTests
    {
        readonly List<Carried> m_Loose = new List<Carried>();

        GameObject m_PlayerObject;
        Carrier m_Hands;
        Hands m_Holding;
        GameObject m_BagObject;
        Carried m_Bag;

        [SetUp]
        public void SetUp()
        {
            m_Loose.Clear();

            m_PlayerObject = new GameObject("Player");
            var playerBody = m_PlayerObject.AddComponent<Rigidbody>();
            playerBody.useGravity = false;
            playerBody.mass = 80f;
            m_Hands = m_PlayerObject.AddComponent<Carrier>();
            m_Hands.Covers(Vector3.zero, new Vector3(1f, 1f, 1f), holdsAtItsCentre: true);

            m_BagObject = new GameObject("Bag");
            var bagBody = m_BagObject.AddComponent<Rigidbody>();
            bagBody.useGravity = false;
            bagBody.mass = 20f;
            m_Bag = m_BagObject.AddComponent<Carried>();
            m_Loose.Add(m_Bag);

            m_Holding = new Hands(m_Hands, () => m_Loose, ThrowSettings.Default);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_BagObject != null)
            {
                Object.DestroyImmediate(m_BagObject);
            }

            if (m_PlayerObject != null)
            {
                Object.DestroyImmediate(m_PlayerObject);
            }
        }

        [UnityTest]
        public IEnumerator HandsDoNotPickUpTheBodyTheyBelongTo()
        {
            // What the game actually hands to a pair of hands: everything on the apron that can be
            // carried -- and the player is one of those things, since they can ride a cart. Their
            // own body is half a metre from their own hands, closer than any bag will ever be.
            var self = m_PlayerObject.AddComponent<Carried>();
            m_Loose.Add(self);

            m_BagObject.transform.position = new Vector3(0f, 0f, 1f);
            yield return null;

            Assert.That(m_Holding.PickUp(Time.time), Is.True);
            Assert.That(self.Attached, Is.False,
                "a player who picks themselves up is kinematic, riding their own hands, and the " +
                "hands move with them -- so they slide off across the apron and cannot walk");
            Assert.That(m_Holding.Carrying, Is.SameAs(m_Bag));
        }

        [UnityTest]
        public IEnumerator ABagWithinReachCanBePickedUp()
        {
            m_BagObject.transform.position = new Vector3(0f, 0f, 1f);

            Assert.That(m_Holding.PickUp(Time.time), Is.True);
            Assert.That(m_Holding.Full, Is.True);
            Assert.That(m_Bag.On, Is.SameAs(m_Hands), "the hands are the carrier it is riding on");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ABagAcrossTheApronCannotBePickedUp()
        {
            m_BagObject.transform.position = new Vector3(0f, 0f, 20f);

            Assert.That(m_Holding.PickUp(Time.time), Is.False,
                "reaching twenty metres would have players hoovering up the apron from where they stand");

            yield return null;
        }

        [UnityTest]
        public IEnumerator FullHandsCannotPickUpAnythingElse()
        {
            m_BagObject.transform.position = new Vector3(0f, 0f, 1f);
            m_Holding.PickUp(Time.time);

            var second = new GameObject("Second bag");
            second.AddComponent<Rigidbody>().useGravity = false;
            var secondBag = second.AddComponent<Carried>();
            second.transform.position = new Vector3(0f, 0f, 1f);
            m_Loose.Add(secondBag);

            Assert.That(m_Holding.PickUp(Time.time), Is.False, "there are two hands and they are both full");

            Object.DestroyImmediate(second);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ATapPutsABagDownRatherThanThrowingIt()
        {
            m_BagObject.transform.position = new Vector3(0f, 0f, 1f);
            m_Holding.PickUp(Time.time);

            m_Holding.StartWindingUp(Time.time);
            var dropped = m_Holding.LetGo(Time.time, Vector3.forward);

            Assert.That(dropped, Is.SameAs(m_Bag));
            Assert.That(m_Bag.Body.linearVelocity.magnitude, Is.LessThan(0.5f),
                "stacking a cart would be a game of not flinching if every press launched the bag");

            yield return null;
        }

        [UnityTest]
        public IEnumerator AFullWindUpThrowsABagProperly()
        {
            m_BagObject.transform.position = new Vector3(0f, 0f, 1f);
            m_Holding.PickUp(Time.time);

            m_Holding.StartWindingUp(Time.time);
            yield return Steps.Seconds(1.5f);

            m_Holding.LetGo(Time.time, Vector3.forward);

            Assert.That(m_Bag.Body.linearVelocity.magnitude, Is.GreaterThan(8f),
                "a full wind-up has to be worth winding up for");
            Assert.That(m_Bag.Body.linearVelocity.y, Is.GreaterThan(0f),
                "thrown flat a bag skids along the floor rather than travelling anywhere");
        }

        [UnityTest]
        public IEnumerator AHalfWindUpThrowsLessHardThanAFullOne()
        {
            m_BagObject.transform.position = new Vector3(0f, 0f, 1f);
            m_Holding.PickUp(Time.time);

            m_Holding.StartWindingUp(Time.time);
            yield return Steps.Seconds(0.4f);
            var halfWay = m_Holding.Charge(Time.time);

            yield return Steps.Seconds(1.2f);

            Assert.That(halfWay, Is.GreaterThan(0f).And.LessThan(1f), "part way is part way");
            Assert.That(m_Holding.Charge(Time.time), Is.EqualTo(1f).Within(1e-3f),
                "and holding it long enough gets there");
        }

        [UnityTest]
        public IEnumerator ABagThrownFromAMovingCarrierCarriesItsMotion()
        {
            // The player is riding something: a cart doing six metres a second.
            var cartObject = new GameObject("Cart");
            var cartBody = cartObject.AddComponent<Rigidbody>();
            cartBody.useGravity = false;
            cartBody.mass = 550f;
            var deck = cartObject.AddComponent<Carrier>();
            deck.Covers(Vector3.zero, new Vector3(2f, 2f, 4f));

            var riding = m_PlayerObject.AddComponent<Carried>();
            riding.AttachTo(deck);

            m_BagObject.transform.position = new Vector3(0f, 0f, 0.5f);
            m_Holding.PickUp(Time.time);

            cartBody.linearVelocity = new Vector3(0f, 0f, 6f);
            yield return Steps.Seconds(0.5f);

            m_Holding.StartWindingUp(Time.time);
            yield return Steps.Seconds(1.5f);
            m_Holding.LetGo(Time.time, Vector3.forward);

            Assert.That(m_Bag.Body.linearVelocity.z, Is.GreaterThan(12f),
                "thrown forward from a cart doing six, a bag has to go further than the same throw " +
                "standing still. That is the whole chain -- bag on player on cart -- coming apart in " +
                "the right order");

            Object.DestroyImmediate(cartObject);
        }

        [UnityTest]
        public IEnumerator ABagJustThrownCannotBeSnatchedStraightBack()
        {
            m_BagObject.transform.position = new Vector3(0f, 0f, 1f);
            m_Holding.PickUp(Time.time);
            m_Holding.LetGo(Time.time, Vector3.forward);

            Assert.That(m_Holding.PickUp(Time.time), Is.False,
                "a bag that can be re-grabbed the instant it leaves your hands never lands");

            yield return null;
        }
    }
}
