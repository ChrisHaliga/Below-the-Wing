using System.Collections;
using System.Collections.Generic;
using BelowTheWing.Cargo;
using BelowTheWing.Session;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.Multiplayer
{
    /// <summary>
    /// Every machine agreeing about which cart a bag is riding on.
    ///
    /// This is the one thing about cargo that cannot be worked out locally. Where a loose bag has
    /// tumbled to is a difference of centimetres that nobody notices; whether it is on the cart in
    /// front of you or lying on the tarmac is a difference players argue about. Left to each machine
    /// to decide, the two answers never reconcile -- there is no force that pulls a bag from a deck
    /// on one screen onto a deck on another.
    ///
    /// So it travels as a fact rather than being recomputed, and these tests are about that fact
    /// arriving. Nothing here measures physics: three copies of every object share one physics world
    /// in this harness, so the bags are standing inside each other and what the solver makes of that
    /// means nothing. What the wire carries is real regardless.
    /// </summary>
    public sealed class CargoMultiplayerTests : RampMultiplayerTest
    {
        GameObject m_BagPrefab;
        GameObject m_CartPrefab;

        protected override void OnServerAndClientsCreated()
        {
            m_BagPrefab = CreateNetworkObjectPrefab("Bag");
            var bagBody = m_BagPrefab.AddComponent<Rigidbody>();
            bagBody.useGravity = false;
            m_BagPrefab.AddComponent<Carried>();
            m_BagPrefab.AddComponent<SettlesOntoCarriers>();
            m_BagPrefab.AddComponent<CargoMotion>();

            m_CartPrefab = CreateNetworkObjectPrefab("Cart");
            var cartBody = m_CartPrefab.AddComponent<Rigidbody>();
            cartBody.useGravity = false;
            cartBody.isKinematic = true;

            var deck = new GameObject("Deck");
            deck.transform.SetParent(m_CartPrefab.transform, worldPositionStays: false);
            deck.AddComponent<Carrier>().Covers(Vector3.zero, new Vector3(2f, 2f, 4f));

            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// Puts one object on the apron the way the game does.
        ///
        /// Through the production call rather than by repeating what it does. Ownership that moves
        /// only when somebody asks for it is the whole basis of taking hold of a bag, and written
        /// out here these tests would pass with that line deleted from the builder.
        /// </summary>
        NetworkObject Place(GameObject prefab)
        {
            var placed = SpawnObject(prefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            ApronBuilder.OnlyByAsking(placed);

            return placed;
        }

        /// <summary>The copy of a spawned object that lives on one particular machine.</summary>
        static NetworkObject CopyOn(NetworkManager machine, ulong id)
            => machine.SpawnManager.SpawnedObjects.TryGetValue(id, out var copy) ? copy : null;

        static Carried BagOn(NetworkManager machine, ulong id)
        {
            var copy = CopyOn(machine, id);
            return copy != null ? copy.GetComponent<Carried>() : null;
        }

        static Carrier DeckOn(NetworkManager machine, ulong id)
        {
            var copy = CopyOn(machine, id);
            return copy != null ? copy.GetComponentInChildren<Carrier>() : null;
        }

        IEnumerable<NetworkManager> EveryMachine()
        {
            yield return m_ServerNetworkManager;
            foreach (var client in m_ClientNetworkManagers)
            {
                yield return client;
            }
        }

        bool EverybodyHas(params ulong[] ids)
        {
            foreach (var machine in EveryMachine())
            {
                foreach (var id in ids)
                {
                    if (CopyOn(machine, id) == null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        [UnityTest]
        public IEnumerator ABagPutOnACartIsOnThatCartEverywhere()
        {
            var bag = Place(m_BagPrefab);
            var cart = Place(m_CartPrefab);

            yield return WaitForConditionOrTimeOut(() => EverybodyHas(bag.NetworkObjectId, cart.NetworkObjectId));
            AssertOnTimeout("the bag and the cart never reached every machine");

            // A player on the first client picks the bag up and puts it on the cart. Done the way
            // the game does it -- on that machine, straight away, without asking anybody first.
            var reachingClient = m_ClientNetworkManagers[0];
            BagOn(reachingClient, bag.NetworkObjectId)
                .AttachTo(DeckOn(reachingClient, cart.NetworkObjectId));

            yield return WaitForConditionOrTimeOut(() =>
            {
                foreach (var machine in EveryMachine())
                {
                    var here = BagOn(machine, bag.NetworkObjectId);
                    if (here == null || here.On != DeckOn(machine, cart.NetworkObjectId))
                    {
                        return false;
                    }
                }

                return true;
            });

            AssertOnTimeout(
                "a bag on a cart for the player who put it there and lying on the tarmac for everybody " +
                "else is the disagreement cargo replication exists to prevent");
        }

        [UnityTest]
        public IEnumerator TheMachineThatPutItThereEndsUpOwningIt()
        {
            var bag = Place(m_BagPrefab);
            var cart = Place(m_CartPrefab);

            yield return WaitForConditionOrTimeOut(() => EverybodyHas(bag.NetworkObjectId, cart.NetworkObjectId));
            AssertOnTimeout("the bag and the cart never reached every machine");

            var reachingClient = m_ClientNetworkManagers[0];
            Assert.That(CopyOn(reachingClient, bag.NetworkObjectId).OwnerClientId,
                Is.Not.EqualTo(reachingClient.LocalClientId),
                "this test is about a machine taking hold of a bag it does not already own");

            BagOn(reachingClient, bag.NetworkObjectId)
                .AttachTo(DeckOn(reachingClient, cart.NetworkObjectId));

            yield return WaitForConditionOrTimeOut(
                () => CopyOn(reachingClient, bag.NetworkObjectId).OwnerClientId == reachingClient.LocalClientId);

            AssertOnTimeout(
                "whoever has hold of a bag has to be the machine deciding what happens to it, or " +
                "the machine that still owns it goes on reporting where it thinks the bag is and " +
                "pulls it back out of their hands");
        }

        [UnityTest]
        public IEnumerator OnlyTheMachineThatOwnsABagDecidesWhereItSettles()
        {
            var bag = Place(m_BagPrefab);

            yield return WaitForConditionOrTimeOut(() => EverybodyHas(bag.NetworkObjectId));
            AssertOnTimeout("the bag never reached every machine");

            yield return WaitForConditionOrTimeOut(() =>
            {
                foreach (var machine in EveryMachine())
                {
                    var copy = CopyOn(machine, bag.NetworkObjectId);
                    var settling = copy.GetComponent<SettlesOntoCarriers>();

                    if (settling.OursToDecide != copy.IsOwner)
                    {
                        return false;
                    }
                }

                return true;
            });

            AssertOnTimeout(
                "every machine judging for itself that a bag has come to rest is how the same bag " +
                "ends up aboard a different cart on each screen");
        }
    }
}
