using System.Collections;
using BelowTheWing.Session;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.Multiplayer
{
    public sealed class CorrectionMultiplayerTests : RampMultiplayerTest
    {
        GameObject m_TractorPrefab;
        VehicleProfile m_TractorProfile;

        protected override void OnServerAndClientsCreated()
        {
            m_TractorProfile = TestProfiles.Tractor();

            m_TractorPrefab = CreateNetworkObjectPrefab("Tractor");
            TestShapes.On(m_TractorPrefab, TestShapes.Tractor());
            m_TractorPrefab.AddComponent<VehicleController>().Configure(m_TractorProfile, "Tug 1");
            m_TractorPrefab.AddComponent<VehicleMotion>();

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

        static T CopyOn<T>(NetworkManager instance, ulong id) where T : Component
            => instance.SpawnManager.SpawnedObjects.TryGetValue(id, out var found)
                ? found.GetComponent<T>()
                : null;

        [UnityTest]
        public IEnumerator ExactlyOneMachineIsInChargeOfEachVehicle()
        {
            var driver = m_ClientNetworkManagers[0];
            var spawned = SpawnObject(m_TractorPrefab, driver).GetComponent<NetworkObject>();
            var id = spawned.NetworkObjectId;

            yield return WaitForSpawnedOnAllOrTimeOut(spawned);
            AssertOnTimeout("the tractor never reached every machine");

            yield return WaitForConditionOrTimeOut(
                () => CopyOn<NetworkObject>(driver, id).OwnerClientId == driver.LocalClientId);
            AssertOnTimeout("the machine that spawned the tractor never ended up owning it");

            Assert.That(CopyOn<VehicleController>(driver, id).OursToMove, Is.True,
                "the machine netcode says owns it has to be the one moving it");

            foreach (var watcher in new[] { m_ServerNetworkManager, m_ClientNetworkManagers[1] })
            {
                var here = CopyOn<NetworkObject>(watcher, id);
                Assert.That(CopyOn<VehicleController>(watcher, id).OursToMove, Is.False,
                    $"on client {watcher.LocalClientId}: netcode says the owner is {here.OwnerClientId}, " +
                    $"IsOwner is {here.IsOwner}, and the driving machine is {driver.LocalClientId}. " +
                    "two machines that both believe a vehicle is theirs drive it in two directions and " +
                    "both try to broadcast where it went. Netcode refuses the second write, so the only " +
                    "trace is an error in a log nobody is reading while the vehicle quietly diverges");
            }
        }

        [UnityTest]
        public IEnumerator WhereTheOwnerDrivesItIsWhatTheOtherMachinesAreTold()
        {
            var driver = m_ClientNetworkManagers[0];
            var watcher = m_ClientNetworkManagers[1];

            var spawned = SpawnObject(m_TractorPrefab, driver).GetComponent<NetworkObject>();
            var id = spawned.NetworkObjectId;

            yield return WaitForSpawnedOnAllOrTimeOut(spawned);
            AssertOnTimeout("the tractor never reached every machine");

            var theirs = CopyOn<VehicleController>(driver, id);
            var ours = CopyOn<VehicleMotion>(watcher, id);

            yield return WaitForConditionOrTimeOut(() => ours.HeardFromTheOwner);
            AssertOnTimeout("nothing was ever heard from the machine that owns the tractor");

            var firstHeard = ours.WhereTheOwnerSaysItIs;

            theirs.Body.position += new Vector3(0f, 0f, 25f);

            yield return WaitForConditionOrTimeOut(
                () => Vector3.Distance(ours.WhereTheOwnerSaysItIs, firstHeard) > 20f);

            AssertOnTimeout(
                $"the owner moved 25 m and the watching machine is still told it is at {firstHeard}. " +
                "A copy that is never told where its vehicle went has nothing to steer toward, so it " +
                "carries on with whatever its own physics is doing and never comes back");
        }

        [UnityTest]
        public IEnumerator AMachineThatOwnsNothingKnowsHowFarOutItIs()
        {
            var driver = m_ClientNetworkManagers[0];
            var watcher = m_ClientNetworkManagers[1];

            var spawned = SpawnObject(m_TractorPrefab, driver).GetComponent<NetworkObject>();
            var id = spawned.NetworkObjectId;

            yield return WaitForSpawnedOnAllOrTimeOut(spawned);
            AssertOnTimeout("the tractor never reached every machine");

            var ours = CopyOn<VehicleMotion>(watcher, id);

            yield return WaitForConditionOrTimeOut(() => ours.HeardFromTheOwner);
            AssertOnTimeout("nothing was ever heard from the machine that owns the tractor");

            ours.enabled = false;

            var body = CopyOn<VehicleController>(watcher, id).Body;
            body.position += new Vector3(0f, 0f, 40f);

            Assert.That(ours.MetresOutOfPlace, Is.GreaterThan(30f),
                "a copy forty metres from where its owner says it is, reporting no error at all. This " +
                "number is the only way to tell a tuning problem from an architectural one: without it " +
                "both screens look perfectly reasonable on their own");

            Assert.That(CopyOn<VehicleMotion>(driver, id).MetresOutOfPlace, Is.Zero,
                "a machine that owns a vehicle is not out of place with itself");
        }
    }
}
