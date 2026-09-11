using System.Collections;
using System.Collections.Generic;
using BelowTheWing.Cargo;
using BelowTheWing.Session;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// Numbering the carriers on an object, so that one machine can tell another which one a bag is
    /// riding on.
    ///
    /// The number is a position in a list rather than anything stored in the scene, which is what
    /// makes it work on a prefab nobody remembered to label. What has to hold for that to be safe is
    /// that both machines build the same list, and the thing that would break it is one object
    /// ending up inside another: a player sitting in a tractor is a child of it, and a player has
    /// hands, and hands are a carrier. Counted as the tractor's, every number after theirs shifts,
    /// and cargo appears to jump between carriers for no reason anybody could trace.
    /// </summary>
    public sealed class CarrierSlotsPlayTests
    {
        readonly List<Carrier> m_Found = new List<Carrier>();

        GameObject m_Cart;
        Carrier m_Deck;
        GameObject m_Player;

        [SetUp]
        public void SetUp()
        {
            m_Cart = new GameObject("Cart");
            m_Cart.AddComponent<NetworkObject>();

            var deck = new GameObject("Deck");
            deck.transform.SetParent(m_Cart.transform, worldPositionStays: false);
            m_Deck = deck.AddComponent<Carrier>();

            // Already inside the cart, the way somebody who has climbed aboard is. Built this way
            // rather than re-parented during the test, because the networking layer treats moving a
            // networked object between parents as an operation only its owner may perform and says
            // so loudly when nothing is connected.
            m_Player = new GameObject("Player");
            m_Player.transform.SetParent(m_Cart.transform, worldPositionStays: true);
            m_Player.AddComponent<NetworkObject>();

            var hands = new GameObject("Hands");
            hands.transform.SetParent(m_Player.transform, worldPositionStays: false);
            hands.AddComponent<Carrier>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Player != null)
            {
                Object.DestroyImmediate(m_Player);
            }

            if (m_Cart != null)
            {
                Object.DestroyImmediate(m_Cart);
            }
        }

        [UnityTest]
        public IEnumerator ACarrierIsNumberedByWhereItSitsOnItsOwnObject()
        {
            Assert.That(CarrierSlots.SlotOf(m_Deck, m_Found), Is.Zero);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SomebodyAboardDoesNotTakeUpANumberOnTheThingTheyAreAboard()
        {
            CarrierSlots.CarriersOf(m_Cart.GetComponent<NetworkObject>(), m_Found);

            Assert.That(m_Found, Has.No.Member(m_Player.GetComponentInChildren<Carrier>()),
                "a player's hands belong to the player, not to whatever they happen to be aboard");
            Assert.That(m_Found, Has.Member(m_Deck));
            Assert.That(CarrierSlots.SlotOf(m_Deck, m_Found), Is.Zero,
                "counting somebody else's carriers as this object's means every number after theirs " +
                "shifts the moment they climb aboard, and every bag already on the deck is suddenly " +
                "reported as riding something else");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ACarrierOnNothingNetworkedHasNoNumberAtAll()
        {
            var scenery = new GameObject("Shelf");
            var shelf = scenery.AddComponent<Carrier>();

            Assert.That(CarrierSlots.SlotOf(shelf, m_Found), Is.EqualTo(CarrierSlots.None),
                "there is nothing another machine could do with the answer, so there must not be " +
                "one to send");

            Object.DestroyImmediate(scenery);

            yield return null;
        }
    }
}
