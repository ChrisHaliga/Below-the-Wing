using System.Collections;
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
    /// What happens to "somebody is driving this" when the vehicle changes hands anyway.
    ///
    /// Only a vehicle's owner may say who is sitting in it. That means a driver who loses the
    /// vehicle -- a session owner reclaiming an orphaned train, or the driver's machine leaving
    /// altogether -- can never record that they got out. Left alone, the tractor stays occupied on
    /// every machine for the rest of the session: never offered to anybody, and refusing every
    /// request made for it. Five vehicles quietly become unusable and nothing can unstick them.
    /// </summary>
    public sealed class OccupancyHandoverMultiplayerTests : RampMultiplayerTest
    {
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
        public IEnumerator ATractorTakenFromItsDriverStopsBeingOccupied()
        {
            var driver = m_ClientNetworkManagers[0];
            var nextOwner = m_ClientNetworkManagers[1];

            var tractor = SpawnObject(m_TractorPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            ApronBuilder.OnlyByAsking(tractor);
            var id = tractor.NetworkObjectId;

            yield return WaitForConditionOrTimeOut(() => Everywhere(id));
            AssertOnTimeout("the tractor never reached every machine");

            // One player takes it and gets in.
            tractor.ChangeOwnership(driver.LocalClientId);
            yield return WaitForConditionOrTimeOut(() => Owner(driver, id) == driver.LocalClientId);
            AssertOnTimeout("the first client never got the tractor");

            Vehicle(driver, id).Occupied = true;
            yield return WaitForConditionOrTimeOut(() => Vehicle(nextOwner, id).Occupied);
            AssertOnTimeout("the second client never learned it was occupied");

            // Handed on by whoever currently holds it. Only the owner or the session owner may move
            // ownership of a request-only vehicle, and the session owner gave this one up a moment
            // ago. What matters is the state the receiving machine ends up in: owning a vehicle
            // whose seat is recorded as taken by somebody else, which is exactly what a reclaim
            // after a disconnect looks like from where it is standing.
            driver.SpawnManager.SpawnedObjects[id].ChangeOwnership(nextOwner.LocalClientId);
            yield return WaitForConditionOrTimeOut(() => Owner(nextOwner, id) == nextOwner.LocalClientId);
            AssertOnTimeout("the second client never got the tractor");

            yield return WaitForConditionOrTimeOut(() => !Vehicle(nextOwner, id).Occupied);
            AssertOnTimeout(
                "the tractor is still occupied by a driver who no longer owns it. Nobody but the owner " +
                "may clear that, and the old driver is not the owner any more, so it stays true for the " +
                "rest of the session: never offered to anybody and refusing every request for it");

            yield return WaitForConditionOrTimeOut(() => !Vehicle(m_ServerNetworkManager, id).Occupied);
            AssertOnTimeout("the session owner still believes somebody is driving it");

            Assert.That(Vehicle(nextOwner, id).AcceptsDriver, Is.True,
                "and with nobody in it, it has to be offered again");
        }

        [UnityTest]
        public IEnumerator ATractorHandedToItsOwnDriverStaysOccupied()
        {
            var driver = m_ClientNetworkManagers[0];

            var tractor = SpawnObject(m_TractorPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            ApronBuilder.OnlyByAsking(tractor);
            var id = tractor.NetworkObjectId;

            yield return WaitForConditionOrTimeOut(() => Everywhere(id));
            AssertOnTimeout("the tractor never reached every machine");

            tractor.ChangeOwnership(driver.LocalClientId);
            yield return WaitForConditionOrTimeOut(() => Owner(driver, id) == driver.LocalClientId);
            AssertOnTimeout("the driver never got the tractor");

            Vehicle(driver, id).Occupied = true;
            yield return WaitForConditionOrTimeOut(() => Vehicle(m_ServerNetworkManager, id).Occupied);
            AssertOnTimeout("nobody else learned it was occupied");

            // Ownership churn that ends up back with the same driver must not throw them out.
            driver.SpawnManager.SpawnedObjects[id].ChangeOwnership(driver.LocalClientId);
            yield return s_ShortWait;

            Assert.That(Vehicle(driver, id).Occupied, Is.True,
                "clearing the seat on every ownership change would eject a driver whose own machine " +
                "was simply confirmed as the owner");
        }

        static readonly WaitForSeconds s_ShortWait = new WaitForSeconds(0.5f);

        static VehicleController Vehicle(NetworkManager on, ulong id)
            => on.SpawnManager.SpawnedObjects[id].GetComponent<VehicleController>();

        static ulong Owner(NetworkManager on, ulong id) => on.SpawnManager.SpawnedObjects[id].OwnerClientId;

        bool Everywhere(ulong id)
        {
            if (!m_ServerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(id))
            {
                return false;
            }

            foreach (var client in m_ClientNetworkManagers)
            {
                if (!client.SpawnManager.SpawnedObjects.ContainsKey(id))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
