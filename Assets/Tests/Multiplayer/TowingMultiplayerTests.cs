using System.Collections;
using System.Collections.Generic;
using BelowTheWing.Session;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.Multiplayer
{
    public sealed class TowingMultiplayerTests : RampMultiplayerTest
    {
        GameObject m_TractorPrefab;
        GameObject m_CartPrefab;
        VehicleProfile m_TractorProfile;
        VehicleProfile m_CartProfile;

        protected override void OnServerAndClientsCreated()
        {
            m_TractorProfile = TestProfiles.Tractor();
            m_CartProfile = TestProfiles.Cart();

            m_TractorPrefab = CreateNetworkObjectPrefab("Tractor");
            TestShapes.On(m_TractorPrefab, TestShapes.Tractor());
            m_TractorPrefab.AddComponent<VehicleController>().Configure(m_TractorProfile, "Tug 1");
            m_TractorPrefab.AddComponent<VehicleMotion>();
            m_TractorPrefab.AddComponent<TrainMember>();
            m_TractorPrefab.GetComponent<Rigidbody>().isKinematic = true;

            m_CartPrefab = CreateNetworkObjectPrefab("Cart");
            TestShapes.On(m_CartPrefab, TestShapes.Cart());
            m_CartPrefab.AddComponent<VehicleController>().Configure(m_CartProfile, "Cart 1");
            m_CartPrefab.AddComponent<VehicleMotion>();
            m_CartPrefab.AddComponent<TrainMember>();
            m_CartPrefab.GetComponent<Rigidbody>().isKinematic = true;

            base.OnServerAndClientsCreated();
        }

        protected override void OnOneTimeTearDown()
        {
            if (m_TractorProfile != null)
            {
                Object.DestroyImmediate(m_TractorProfile);
            }

            if (m_CartProfile != null)
            {
                Object.DestroyImmediate(m_CartProfile);
            }

            base.OnOneTimeTearDown();
        }

        IEnumerable<NetworkManager> AllManagers()
        {
            yield return m_ServerNetworkManager;

            foreach (var client in m_ClientNetworkManagers)
            {
                yield return client;
            }
        }

        bool EveryMachineHas(ulong id)
        {
            foreach (var manager in AllManagers())
            {
                if (!manager.SpawnManager.SpawnedObjects.ContainsKey(id))
                {
                    return false;
                }
            }

            return true;
        }

        CartChain TrainOn(NetworkManager manager, ulong tractor, ulong cart)
        {
            var members = new List<VehicleController>
            {
                manager.SpawnManager.SpawnedObjects[tractor].GetComponent<VehicleController>(),
                manager.SpawnManager.SpawnedObjects[cart].GetComponent<VehicleController>()
            };

            var train = CartChain.Couple(members, ChainJointSettings.Default);
            train.EngageCouplings();
            return train;
        }

        [UnityTest]
        public IEnumerator ATrainIsHookedTogetherOnEveryMachineNotOnlyItsOwners()
        {
            var tractor = SpawnObject(m_TractorPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            var cart = SpawnObject(m_CartPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();

            yield return WaitForConditionOrTimeOut(
                () => EveryMachineHas(tractor.NetworkObjectId) && EveryMachineHas(cart.NetworkObjectId));
            AssertOnTimeout("not every machine received the train");

            foreach (var manager in AllManagers())
            {
                var train = TrainOn(manager, tractor.NetworkObjectId, cart.NetworkObjectId);

                Assert.That(train.CouplingsEngaged, Is.True,
                    $"client {manager.LocalClientId} is holding this train together with nothing. A " +
                    "cart nobody here has hitched up is a cart being dragged into place by a " +
                    "correction rather than towed, and what a player watches is a train stretching");

                train.ReleaseCouplings();
            }
        }

        [UnityTest]
        public IEnumerator OnlyTheFrontOfATrainIsSteeredTowardsWhatItsOwnerSays()
        {
            var tractor = SpawnObject(m_TractorPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            var cart = SpawnObject(m_CartPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();

            yield return WaitForConditionOrTimeOut(
                () => EveryMachineHas(tractor.NetworkObjectId) && EveryMachineHas(cart.NetworkObjectId));
            AssertOnTimeout("not every machine received the train");

            foreach (var manager in m_ClientNetworkManagers)
            {
                var train = TrainOn(manager, tractor.NetworkObjectId, cart.NetworkObjectId);

                var leader = train.Leader.GetComponent<VehicleMotion>();
                var follower = train.Members[1].GetComponent<VehicleMotion>();

                Assert.That(leader.TheOneWorthCorrecting, Is.True,
                    $"client {manager.LocalClientId} corrects nothing in this train, so it is towed " +
                    "by a copy of a tractor that nobody is steering toward where the real one is");
                Assert.That(follower.TheOneWorthCorrecting, Is.False,
                    $"client {manager.LocalClientId} is correcting a towed cart directly. A chain is " +
                    "a set of constraints and a correction is a force on one body: pushed toward a " +
                    "place its coupling forbids, the solver and the network take turns losing");

                train.ReleaseCouplings();
            }
        }
    }
}
