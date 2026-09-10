using System.Collections;
using System.Collections.Generic;
using BelowTheWing.Session;
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
    /// Who may take a train, and when.
    ///
    /// Two rules, both of which were broken and neither of which could be seen with one client.
    ///
    /// A train changes hands only when somebody asks for all of it and is granted all of it. It was
    /// marked distributable, which told the netcode layer it could hand vehicles out one at a time
    /// whenever anybody joined or left -- splitting trains across machines behind the back of every
    /// rule written to prevent exactly that.
    ///
    /// And a tractor somebody is driving is not available. Nothing refused anything: an ownership
    /// request is approved unless something objects, so a second player could take a tractor out
    /// from under the person driving it.
    /// </summary>
    public sealed class OwnershipRulesMultiplayerTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        protected override NetworkTopologyTypes OnGetNetworkTopologyType()
            => NetworkTopologyTypes.DistributedAuthority;

        GameObject m_TractorPrefab;
        VehicleProfile m_TractorProfile;

        protected override void OnServerAndClientsCreated()
        {
            m_TractorProfile = TestProfiles.Tractor();

            m_TractorPrefab = CreateNetworkObjectPrefab("Tractor");
            m_TractorPrefab.AddComponent<VehicleController>().Configure(m_TractorProfile, "Tug 1");
            m_TractorPrefab.AddComponent<VehicleOccupant>();

            base.OnServerAndClientsCreated();
        }

        protected override void OnOneTimeTearDown()
        {
            if (m_TractorProfile != null)
            {
                Object.DestroyImmediate(m_TractorProfile);
            }

            base.OnOneTimeTearDown();
        }

        [UnityTest]
        public IEnumerator NobodyIsHandedAVehicleJustBecauseTheyJoined()
        {
            var placed = new List<NetworkObject>();
            for (var i = 0; i < 5; i++)
            {
                var vehicle = SpawnObject(m_TractorPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();

                // Through the production call, not by repeating what it does. Written out here, this
                // test would pass even with that line deleted from the builder.
                ApronBuilder.OnlyByAsking(vehicle);
                placed.Add(vehicle);
            }

            yield return WaitForConditionOrTimeOut(() => EveryMachineHasAll(placed));
            AssertOnTimeout("the vehicles never reached every machine");

            var ownerBefore = new Dictionary<ulong, ulong>();
            foreach (var vehicle in placed)
            {
                ownerBefore[vehicle.NetworkObjectId] = vehicle.OwnerClientId;
            }

            yield return CreateAndStartNewClient();

            foreach (var vehicle in placed)
            {
                Assert.That(vehicle.OwnerClientId, Is.EqualTo(ownerBefore[vehicle.NetworkObjectId]),
                    $"vehicle {vehicle.NetworkObjectId} changed hands purely because somebody joined. " +
                    "Handing vehicles out one at a time takes no notice of which train they belong to, " +
                    "so a train ends up simulated by two machines with couplings spanning the gap");
            }
        }

        [UnityTest]
        public IEnumerator ATractorSomebodyIsDrivingCannotBeTakenBySomebodyElse()
        {
            var driver = m_ClientNetworkManagers[0];
            var wouldBeThief = m_ClientNetworkManagers[1];

            var tractor = SpawnObject(m_TractorPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            ApronBuilder.OnlyByAsking(tractor);
            var id = tractor.NetworkObjectId;

            yield return WaitForConditionOrTimeOut(() => EveryMachineHasAll(new List<NetworkObject> { tractor }));
            AssertOnTimeout("the tractor never reached every machine");

            // The first client takes it and gets in.
            tractor.ChangeOwnership(driver.LocalClientId);
            yield return WaitForConditionOrTimeOut(
                () => driver.SpawnManager.SpawnedObjects[id].OwnerClientId == driver.LocalClientId);
            AssertOnTimeout("the first client never got the tractor");

            driver.SpawnManager.SpawnedObjects[id].GetComponent<VehicleController>().Occupied = true;

            yield return WaitForConditionOrTimeOut(
                () => wouldBeThief.SpawnManager.SpawnedObjects[id].GetComponent<VehicleController>().Occupied);
            AssertOnTimeout(
                "the second client never learned the tractor was occupied. Whether somebody is driving " +
                "has to reach every machine, or an occupied tractor looks free to everyone but its driver");

            var theirs = wouldBeThief.SpawnManager.SpawnedObjects[id];
            Assert.That(theirs.GetComponent<VehicleController>().AcceptsDriver, Is.False,
                "an occupied tractor must not be offered to a second player");

            var answer = NetworkObject.OwnershipRequestResponseStatus.Approved;
            var answered = false;
            theirs.OnOwnershipRequestResponse += response =>
            {
                answer = response;
                answered = true;
            };

            theirs.RequestOwnership();

            yield return WaitForConditionOrTimeOut(() => answered);
            AssertOnTimeout("no answer came back to the ownership request");

            Assert.That(answer, Is.Not.EqualTo(NetworkObject.OwnershipRequestResponseStatus.Approved),
                "the tractor was taken out from under the person driving it, whose character is now " +
                "parented inside a vehicle they no longer own and whose controls reach nothing");
            Assert.That(theirs.OwnerClientId, Is.EqualTo(driver.LocalClientId),
                "and it must still belong to the driver");
        }

        bool EveryMachineHasAll(IReadOnlyList<NetworkObject> objects)
        {
            foreach (var manager in AllManagers())
            {
                foreach (var each in objects)
                {
                    if (!manager.SpawnManager.SpawnedObjects.ContainsKey(each.NetworkObjectId))
                    {
                        return false;
                    }
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
