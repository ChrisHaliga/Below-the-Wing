using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.Multiplayer
{
    /// <summary>
    /// A cart is a cart on every machine.
    ///
    /// It was not. The machine that built the apron configured every cart as a three-tonne tractor,
    /// while a client joining later configured the same cart correctly at 550 kg. The same vehicle
    /// had two different masses and two different answers to "can I drive this", depending on who
    /// was looking, and its physics changed the moment it was taken over.
    ///
    /// The cause was that what a vehicle *is* was replicated data it learned about itself shortly
    /// after coming into existence, and its default value happened to name a real kind of vehicle.
    /// A cart is now a cart because it came from the cart prefab, so there is no window in which it
    /// is anything else and nothing to get in the wrong order.
    /// </summary>
    public sealed class VehicleIdentityMultiplayerTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        protected override NetworkTopologyTypes OnGetNetworkTopologyType()
            => NetworkTopologyTypes.DistributedAuthority;

        GameObject m_CartPrefab;
        VehicleProfile m_CartProfile;

        protected override void OnServerAndClientsCreated()
        {
            m_CartProfile = TestProfiles.Cart();

            m_CartPrefab = CreateNetworkObjectPrefab("Cart");
            m_CartPrefab.AddComponent<VehicleController>().Configure(m_CartProfile, "Cart 1-1");

            base.OnServerAndClientsCreated();
        }

        protected override void OnOneTimeTearDown()
        {
            if (m_CartProfile != null)
            {
                Object.DestroyImmediate(m_CartProfile);
            }

            base.OnOneTimeTearDown();
        }

        [UnityTest]
        public IEnumerator ACartIsACartOnEveryMachineIncludingTheOneThatSpawnedIt()
        {
            var spawned = SpawnObject(m_CartPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            var id = spawned.NetworkObjectId;

            yield return WaitForConditionOrTimeOut(() => EveryMachineHas(id));
            AssertOnTimeout($"not every machine received cart {id}");

            foreach (var manager in AllManagers())
            {
                var vehicle = manager.SpawnManager.SpawnedObjects[id].GetComponent<VehicleController>();

                Assert.That(vehicle.Profile, Is.Not.Null,
                    $"client {manager.LocalClientId} left the cart with no profile at all");
                Assert.That(vehicle.Profile.massKg, Is.EqualTo(m_CartProfile.massKg).Within(0.01f),
                    $"client {manager.LocalClientId} has this cart at {vehicle.Profile.massKg} kg. " +
                    "A cart configured from the tractor's profile weighs five times what it should, " +
                    "and everyone watching a train sees it behave as if it did");
                Assert.That(vehicle.Body.mass, Is.EqualTo(m_CartProfile.massKg).Within(0.01f));
            }
        }

        [UnityTest]
        public IEnumerator ACartIsNeverOfferedToADriverOnAnyMachine()
        {
            var spawned = SpawnObject(m_CartPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            var id = spawned.NetworkObjectId;

            yield return WaitForConditionOrTimeOut(() => EveryMachineHas(id));
            AssertOnTimeout($"not every machine received cart {id}");

            foreach (var manager in AllManagers())
            {
                var vehicle = manager.SpawnManager.SpawnedObjects[id].GetComponent<VehicleController>();
                Assert.That(vehicle.AcceptsDriver, Is.False,
                    $"client {manager.LocalClientId} would offer this cart to a player. Carts are towed, " +
                    "and a cart wearing a tractor's profile says it can be driven");
            }
        }

        [UnityTest]
        public IEnumerator AVehicleHasTheSameNameEverywhereAndItIsNotBlank()
        {
            var spawned = SpawnObject(m_CartPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            var id = spawned.NetworkObjectId;

            yield return WaitForConditionOrTimeOut(() => EveryMachineHas(id));
            AssertOnTimeout($"not every machine received cart {id}");

            foreach (var manager in AllManagers())
            {
                var vehicle = manager.SpawnManager.SpawnedObjects[id].GetComponent<VehicleController>();
                Assert.That(vehicle.DisplayName, Is.Not.Null.And.Not.Empty,
                    $"client {manager.LocalClientId} has a nameless vehicle. The drive prompt reads this, " +
                    "so a blank one offers a prompt ending in nothing at all");
            }
        }

        bool EveryMachineHas(ulong networkObjectId)
        {
            foreach (var manager in AllManagers())
            {
                if (!manager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                {
                    return false;
                }
            }

            return true;
        }

        NetworkManager[] AllManagers()
        {
            var all = new NetworkManager[m_ClientNetworkManagers.Length + 1];
            all[0] = m_ServerNetworkManager;
            m_ClientNetworkManagers.CopyTo(all, 1);
            return all;
        }
    }
}
