using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        const int TheOnlyTrain = 0;
        const int UpFront = 0;
        const int BehindTheTractor = 1;

        static readonly Regex SpawnedWithNoSession =
            new Regex("was spawned with no RampSession");

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

        IEnumerable<NetworkManager> AllMachines()
        {
            yield return m_ServerNetworkManager;

            foreach (var client in m_ClientNetworkManagers)
            {
                yield return client;
            }
        }

        void ExpectEveryMemberToComplainThatThisFixtureHasNoSession(int vehicles)
        {
            foreach (var unused in AllMachines())
            {
                for (var i = 0; i < vehicles; i++)
                {
                    LogAssert.Expect(LogType.Exception, SpawnedWithNoSession);
                }
            }
        }

        static TrainMember MemberOn(NetworkManager machine, ulong id)
            => machine.SpawnManager.SpawnedObjects[id].GetComponent<TrainMember>();

        bool EveryMachineHas(ulong id)
        {
            foreach (var machine in AllMachines())
            {
                if (!machine.SpawnManager.SpawnedObjects.ContainsKey(id))
                {
                    return false;
                }
            }

            return true;
        }

        bool EveryMachineHasHeardTheyAreInATrain(params ulong[] ids)
        {
            foreach (var machine in AllMachines())
            {
                foreach (var id in ids)
                {
                    if (MemberOn(machine, id).Membership.TrainIndex != TheOnlyTrain)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static TrainRegistry TheTrainsBuiltFromWhatReached(NetworkManager machine, params ulong[] ids)
        {
            var membership = new List<TrainMembership>(ids.Length);

            foreach (var id in ids)
            {
                membership.Add(MemberOn(machine, id).Membership);
            }

            var registry = new TrainRegistry(ChainJointSettings.Default);
            registry.Rebuild(membership);
            return registry;
        }

        IEnumerator ATrainEveryMachineHasHeardAbout(ulong tractor, ulong cart)
        {
            yield return WaitForConditionOrTimeOut(
                () => EveryMachineHas(tractor) && EveryMachineHas(cart));
            AssertOnTimeout("not every machine was given the two vehicles to begin with");

            MemberOn(m_ServerNetworkManager, tractor).Joins(TheOnlyTrain, UpFront);
            MemberOn(m_ServerNetworkManager, cart).Joins(TheOnlyTrain, BehindTheTractor);

            yield return WaitForConditionOrTimeOut(() => EveryMachineHasHeardTheyAreInATrain(tractor, cart));
            AssertOnTimeout("a machine never heard that these two vehicles are one train");
        }

        [UnityTest]
        public IEnumerator EveryMachineHooksUpTheTrainItsOwnerDescribed()
        {
            ExpectEveryMemberToComplainThatThisFixtureHasNoSession(vehicles: 2);

            var tractor = SpawnObject(m_TractorPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            var cart = SpawnObject(m_CartPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();

            yield return ATrainEveryMachineHasHeardAbout(tractor.NetworkObjectId, cart.NetworkObjectId);

            foreach (var machine in AllMachines())
            {
                var seen = TheTrainsBuiltFromWhatReached(machine, tractor.NetworkObjectId, cart.NetworkObjectId);

                Assert.That(seen.Trains.Count, Is.EqualTo(1),
                    $"machine {machine.LocalClientId} sees {seen.Trains.Count} trains where its owner " +
                    "described one. Vehicles it has not grouped are vehicles nothing holds together, " +
                    "so the cart is dragged along by corrections rather than towed");

                var train = seen.Trains[0];

                Assert.That(train.Leader.GetComponent<NetworkObject>().NetworkObjectId,
                    Is.EqualTo(tractor.NetworkObjectId),
                    $"machine {machine.LocalClientId} put the cart at the front. The place in the train " +
                    "decides which end tows, so a train assembled backwards is pushed by its cart");

                Assert.That(train.CouplingsEngaged, Is.True,
                    $"machine {machine.LocalClientId} is holding this train together with nothing. What " +
                    "a player watches is a train that stretches as it is driven");

                train.ReleaseCouplings();
            }
        }

        [UnityTest]
        public IEnumerator ACopyOfATowedCartIsLeftToItsCouplingRatherThanSteered()
        {
            ExpectEveryMemberToComplainThatThisFixtureHasNoSession(vehicles: 2);

            var tractor = SpawnObject(m_TractorPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            var cart = SpawnObject(m_CartPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();

            yield return ATrainEveryMachineHasHeardAbout(tractor.NetworkObjectId, cart.NetworkObjectId);

            foreach (var machine in m_ClientNetworkManagers)
            {
                var seen = TheTrainsBuiltFromWhatReached(machine, tractor.NetworkObjectId, cart.NetworkObjectId);
                var train = seen.Trains[0];

                var leader = train.Leader.GetComponent<VehicleMotion>();
                var towed = MemberOn(machine, cart.NetworkObjectId).GetComponent<VehicleMotion>();

                Assert.That(leader.TheOneWorthCorrecting, Is.True,
                    $"machine {machine.LocalClientId} corrects nothing in this train, so it is towed by " +
                    "a copy of a tractor that nobody is steering toward where the real one is");

                Assert.That(towed.TheOneWorthCorrecting, Is.False,
                    $"machine {machine.LocalClientId} is correcting a towed cart directly. A chain is a " +
                    "set of constraints and a correction is a force on one body: pushed toward a place " +
                    "its coupling forbids, the solver and the network take turns losing");

                train.ReleaseCouplings();
            }
        }
    }
}
