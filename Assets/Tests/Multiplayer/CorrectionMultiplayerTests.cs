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
    /// <summary>
    /// Whether what the owner of a vehicle says about it reaches the other machines.
    ///
    /// Only the wiring is checked here, and that is on purpose. The three instances this harness runs
    /// share a single physics world, so the owner's tractor and every copy of it are solid bodies
    /// standing in the same place -- they interpenetrate, shove each other across the apron, and any
    /// measurement of how closely a copy follows its owner is measuring that instead. What the
    /// correction forces do to a vehicle is checked against a real body elsewhere, without a network.
    ///
    /// What remains here is what nothing else can see: that authority lands on exactly one machine,
    /// that the owner actually reports, and that the report arrives. Every serious defect in this
    /// project so far has been of that kind -- correct logic that nothing called, or called on the
    /// wrong instance.
    /// </summary>
    public sealed class CorrectionMultiplayerTests : RampMultiplayerTest
    {
        GameObject m_TractorPrefab;
        VehicleProfile m_TractorProfile;

        protected override void OnServerAndClientsCreated()
        {
            m_TractorProfile = TestProfiles.Tractor();

            m_TractorPrefab = CreateNetworkObjectPrefab("Tractor");
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

        const float Correction_SnapMetres = 2f;

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

            Assert.That(CopyOn<VehicleController>(driver, id).OursToMove, Is.True,
                "the machine netcode says owns it has to be the one moving it");

            foreach (var watcher in new[] { m_ServerNetworkManager, m_ClientNetworkManagers[1] })
            {
                Assert.That(CopyOn<VehicleController>(watcher, id).OursToMove, Is.False,
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

            // Move the owner's copy outright rather than driving it. Driving would have both copies
            // pushing each other around this shared physics world, and what is being checked is the
            // reporting, not the forces.
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

            // Generous, and it has to be. Both copies of this tractor are solid bodies standing in
            // the same spot of one shared physics world, so they shove each other apart no matter
            // what correction does. What matters here is only that the reading is a real measurement
            // rather than a constant, which the forty-metre check below establishes.
            Assert.That(ours.MetresOutOfPlace, Is.LessThan(Correction_SnapMetres),
                "before anything is moved the copy should be roughly where its owner says it is");

            // Correction is switched off before the copy is moved. Left running it would put the
            // vehicle straight back -- a gap this size is past the point where blending is given up
            // and the vehicle is moved outright -- and the error would be gone before it could be
            // read. That is correct behaviour and it makes the number impossible to observe.
            ours.enabled = false;

            // The body rather than the transform. Writing a transform leaves the rigidbody's own
            // pose untouched until physics next syncs, so the reading taken here would be of where
            // the vehicle used to be.
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
