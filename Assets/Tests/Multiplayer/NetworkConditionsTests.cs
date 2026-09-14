using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.Multiplayer
{
    public sealed class PingBeacon : NetworkBehaviour
    {
        public readonly NetworkVariable<int> Count =
            new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    }

    public sealed class NetworkConditionsTests : RampMultiplayerTest
    {
        GameObject m_Prefab;

        protected override NetworkCondition Conditions => NetworkConditions.Clean;

        protected override void OnServerAndClientsCreated()
        {
            m_Prefab = CreateNetworkObjectPrefab("Ping beacon");
            m_Prefab.AddComponent<PingBeacon>();

            base.OnServerAndClientsCreated();
        }

        static IEnumerator Until(Func<bool> done, float timeoutSeconds, string what)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!done())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail($"timed out waiting for {what}");
                }

                yield return null;
            }
        }

        static PingBeacon CopyOn(NetworkManager instance, ulong networkObjectId)
            => instance.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var found)
                ? found.GetComponent<PingBeacon>()
                : null;

        IEnumerator MeasureCrossing(int crossings, List<double> into)
        {
            var owner = m_ClientNetworkManagers[0];
            var watcher = m_ClientNetworkManagers[1];

            var networkObject = owner.SpawnManager.InstantiateAndSpawn(
                m_Prefab.GetComponent<NetworkObject>(), owner.LocalClientId);

            var id = networkObject.NetworkObjectId;
            yield return Until(() => CopyOn(watcher, id) != null, 10f, "the beacon to reach the watching instance");

            var here = CopyOn(owner, id);
            var there = CopyOn(watcher, id);

            for (var i = 1; i <= crossings; i++)
            {
                var sentAt = Time.realtimeSinceStartupAsDouble;
                here.Count.Value = i;

                yield return Until(() => there.Count.Value == i, 10f, $"crossing {i}");
                into.Add((Time.realtimeSinceStartupAsDouble - sentAt) * 1000d);
            }

            networkObject.Despawn();
        }

        static double Average(List<double> values)
        {
            var total = 0d;
            foreach (var value in values)
            {
                total += value;
            }

            return total / values.Count;
        }

        [UnityTest]
        public IEnumerator ABadNetworkTakesMeasurablyLongerThanACleanOne()
        {
            var onAPerfectConnection = new List<double>();
            yield return MeasureCrossing(crossings: 8, into: onAPerfectConnection);

            ApplyConditions(NetworkConditions.Bad);
            var onABadOne = new List<double>();
            yield return MeasureCrossing(crossings: 8, into: onABadOne);

            var clean = Average(onAPerfectConnection);
            var bad = Average(onABadOne);

            Assert.That(bad - clean, Is.GreaterThan(50d),
                $"clean crossed in {clean:F0} ms and {NetworkConditions.Bad} crossed in {bad:F0} ms. " +
                "Conditions that make no difference to how long anything takes are not being applied, " +
                "and every test claiming to run on a bad network is running on a perfect one");
        }

        [UnityTest]
        public IEnumerator AskingForACleanNetworkAsksForNoDelayAndNoLoss()
        {
            var clean = NetworkConditions.Clean.AsPreset();

            Assert.That(clean.PacketDelayMs, Is.Zero);
            Assert.That(clean.PacketJitterMs, Is.Zero);
            Assert.That(clean.PacketLossPercent, Is.Zero);

            yield return null;
        }
    }

    public sealed class DefaultConditionsTests : RampMultiplayerTest
    {
        [UnityTest]
        public IEnumerator ATestThatAsksForNothingRunsOnAnOrdinaryNetwork()
        {
            Assert.That(Conditions.RoundTripMilliseconds,
                Is.EqualTo(NetworkConditions.Typical.RoundTripMilliseconds),
                "a test that does not mention the network must not quietly get a perfect one");
            Assert.That(Conditions.LossPercent, Is.GreaterThan(0),
                "an ordinary network loses packets, and that is the point of testing on one");

            yield return null;
        }
    }
}
