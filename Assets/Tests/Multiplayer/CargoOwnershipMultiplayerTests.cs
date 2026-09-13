using System.Collections;
using BelowTheWing.Cargo;
using BelowTheWing.Session;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.Multiplayer
{
    /// <summary>
    /// Whether a bag actually changes hands between machines when the rules say it should.
    ///
    /// The rules themselves are arithmetic and are checked without a network. What is checked here
    /// is the part nothing else can see: that a hand closing on a bag on one machine ends with that
    /// machine simulating it, and that a bag coming to rest on somebody else's cart ends up theirs.
    /// The three instances share one physics world, so everything here is placed by hand and held
    /// still rather than left to physics.
    /// </summary>
    public sealed class CargoOwnershipMultiplayerTests : RampMultiplayerTest
    {
        /// <summary>A body this machine moves, standing in for a player's.</summary>
        sealed class MovedHere : MonoBehaviour, IMovedFromHere
        {
            public bool OursToMove => true;
        }

        GameObject m_BagPrefab;
        GameObject m_CartPrefab;
        BagProfile m_BagProfile;
        VehicleProfile m_CartProfile;

        protected override void OnServerAndClientsCreated()
        {
            m_BagProfile = TestProfiles.CheckedBag();
            m_CartProfile = TestProfiles.Cart();

            m_BagPrefab = CreateNetworkObjectPrefab("Bag");
            m_BagPrefab.AddComponent<Rigidbody>().isKinematic = true;
            m_BagPrefab.AddComponent<BoxCollider>();
            m_BagPrefab.AddComponent<Bag>().Configure(m_BagProfile);
            m_BagPrefab.AddComponent<CargoMotion>();

            // Held still and out of the way. Three instances share one physics world, and a
            // dynamic cart in it -- the template included -- is shoved about by its own copies.
            m_CartPrefab = CreateNetworkObjectPrefab("Cart");
            TestShapes.On(m_CartPrefab, TestShapes.BoxVehicle(m_CartProfile.bodySizeMetres));
            m_CartPrefab.AddComponent<VehicleController>().Configure(m_CartProfile, "Cart 1");
            m_CartPrefab.AddComponent<VehicleMotion>();
            m_CartPrefab.GetComponent<Rigidbody>().isKinematic = true;
            m_CartPrefab.transform.position = new Vector3(-40f, 0f, 0f);

            base.OnServerAndClientsCreated();
        }

        protected override void OnOneTimeTearDown()
        {
            Object.DestroyImmediate(m_BagProfile);
            Object.DestroyImmediate(m_CartProfile);
            base.OnOneTimeTearDown();
        }

        static T CopyOn<T>(NetworkManager instance, ulong id) where T : Component
            => instance.SpawnManager.SpawnedObjects.TryGetValue(id, out var found)
                ? found.GetComponent<T>()
                : null;

        IEnumerator ABagOwnedBy(NetworkManager owner, Vector3 at, System.Action<ulong> spawned)
        {
            var bag = SpawnObject(m_BagPrefab, owner).GetComponent<NetworkObject>();
            bag.transform.position = at;
            ApronBuilder.OnlyByAsking(bag);

            yield return WaitForSpawnedOnAllOrTimeOut(bag);
            AssertOnTimeout("the bag never reached every machine");

            yield return WaitForConditionOrTimeOut(
                () => CopyOn<NetworkObject>(owner, bag.NetworkObjectId).OwnerClientId == owner.LocalClientId);
            AssertOnTimeout("the machine that spawned the bag never ended up owning it");

            spawned(bag.NetworkObjectId);
        }

        [UnityTest]
        public IEnumerator TakingHoldOfABagMakesItThisMachines()
        {
            var thrower = m_ClientNetworkManagers[0];
            var grabber = m_ClientNetworkManagers[1];

            ulong id = 0;
            yield return ABagOwnedBy(thrower, new Vector3(0f, 5f, 0f), spawned => id = spawned);

            // A hand closing on the bag is a joint from it to a body this machine moves.
            var holder = new GameObject("Holder", typeof(Rigidbody), typeof(MovedHere));
            holder.GetComponent<Rigidbody>().isKinematic = true;
            holder.transform.position = new Vector3(0f, 5f, 0.5f);

            var theirCopy = CopyOn<Bag>(grabber, id);
            theirCopy.gameObject.AddComponent<SpringJoint>().connectedBody = holder.GetComponent<Rigidbody>();

            yield return WaitForConditionOrTimeOut(
                () => CopyOn<NetworkObject>(grabber, id).OwnerClientId == grabber.LocalClientId);
            AssertOnTimeout(
                $"a player on client {grabber.LocalClientId} has hold of the bag and it still belongs to " +
                $"client {CopyOn<NetworkObject>(grabber, id).OwnerClientId}. The throw that comes next " +
                "would be simulated on a machine that does not know about the hand, and everybody " +
                "would watch the bag sit there");

            Object.Destroy(holder);
        }

        [UnityTest]
        public IEnumerator ABagAtRestOnSomebodyElsesCartBecomesTheirs()
        {
            var thrower = m_ClientNetworkManagers[0];
            var driver = m_ClientNetworkManagers[1];

            var cart = SpawnObject(m_CartPrefab, driver).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(cart);
            AssertOnTimeout("the cart never reached every machine");

            yield return WaitForConditionOrTimeOut(
                () => CopyOn<NetworkObject>(driver, cart.NetworkObjectId).OwnerClientId == driver.LocalClientId);
            AssertOnTimeout("the machine that spawned the cart never ended up owning it");

            // Every copy of the cart in a place of its own, still. What matters is that whichever
            // copy the bag lies on says the same owner, and every copy does.
            var machines = new[] { m_ServerNetworkManager, thrower, driver };
            for (var i = 0; i < machines.Length; i++)
            {
                var copy = CopyOn<VehicleController>(machines[i], cart.NetworkObjectId);
                copy.Body.position = new Vector3(i * 20f, 0f, 0f);
                copy.transform.position = copy.Body.position;
            }

            var deck = CopyOn<VehicleController>(thrower, cart.NetworkObjectId).Body.position;
            yield return new WaitForFixedUpdate();

            var shape = m_CartPrefab.GetComponent<VehicleShape>();
            var deckTop = shape.EnvelopeCentreLocal.y + (shape.EnvelopeSizeMetres.y * 0.5f);

            // Thrown from elsewhere and landing on the cart: the thrower's for as long as it is in
            // the air, and then set down on the deck, still.
            ulong id = 0;
            yield return ABagOwnedBy(thrower, deck + new Vector3(0f, 10f, 0f), spawned => id = spawned);

            CopyOn<Bag>(thrower, id).transform.position =
                deck + new Vector3(0f, deckTop + (m_BagProfile.sizeMetres.y * 0.5f) + 0.02f, 0f);

            yield return WaitForConditionOrTimeOut(
                () => CopyOn<NetworkObject>(thrower, id).OwnerClientId == driver.LocalClientId);
            AssertOnTimeout(
                $"the bag is lying on a cart client {driver.LocalClientId} is simulating and still " +
                $"belongs to client {CopyOn<NetworkObject>(thrower, id).OwnerClientId}. Bag and cart " +
                "have to be one machine's physics, or a corner throws the bag on one screen and not " +
                "the other");
        }
    }
}
