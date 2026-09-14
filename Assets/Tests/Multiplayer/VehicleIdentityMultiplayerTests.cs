using System.Collections;
using BelowTheWing.Apron;
using BelowTheWing.Session;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.Multiplayer
{
    public sealed class VehicleIdentityMultiplayerTests : RampMultiplayerTest
    {
        GameObject m_CartPrefab;
        VehicleProfile m_CartProfile;

        protected override void OnServerAndClientsCreated()
        {
            m_CartProfile = TestProfiles.Cart();

            m_CartPrefab = CreateNetworkObjectPrefab("Cart");
            TestShapes.On(m_CartPrefab, TestShapes.Cart());
            m_CartPrefab.AddComponent<VehicleController>().Configure(m_CartProfile, "");

            m_CartPrefab.AddComponent<ApronAppearance>().DescribeAs(
                ApronAppearance.Shape.Box, TestShapes.StandInSizeMetres, Color.grey, 1.2f);
            m_CartPrefab.AddComponent<ApronIdentity>();

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
            var spawned = Object.Instantiate(m_CartPrefab).GetComponent<NetworkObject>();
            spawned.GetComponent<ApronIdentity>().Called("Cart 1-1");
            spawned.SpawnWithOwnership(m_ServerNetworkManager.LocalClientId);
            var id = spawned.NetworkObjectId;

            yield return WaitForConditionOrTimeOut(() => EveryMachineHas(id));
            AssertOnTimeout($"not every machine received cart {id}");

            yield return WaitForConditionOrTimeOut(() => EveryMachineNamesIt(id, "Cart 1-1"));
            AssertOnTimeout("the name never reached every machine");

            foreach (var manager in AllManagers())
            {
                var vehicle = manager.SpawnManager.SpawnedObjects[id].GetComponent<VehicleController>();
                Assert.That(vehicle.DisplayName, Is.EqualTo("Cart 1-1"),
                    $"client {manager.LocalClientId} has this vehicle as '{vehicle.DisplayName}'. The " +
                    "drive prompt reads this, so an unnamed one offers \"Press E to drive " +
                    "BaggageCart(Clone)\" -- and a machine that never received the name draws no " +
                    "shape and no label at all, leaving an invisible collider on an empty apron");

                Assert.That(manager.SpawnManager.SpawnedObjects[id].GetComponentInChildren<MeshRenderer>(),
                    Is.Not.Null,
                    $"client {manager.LocalClientId} has nothing to look at. Appearance is built when " +
                    "the name arrives, so a machine that is never told sees an empty grey plane");
            }
        }

        bool EveryMachineNamesIt(ulong networkObjectId, string called)
        {
            foreach (var manager in AllManagers())
            {
                if (!manager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var each)
                    || each.GetComponent<VehicleController>().DisplayName != called)
                {
                    return false;
                }
            }

            return true;
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
