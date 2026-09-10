using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.Multiplayer
{
    /// <summary>
    /// Proves the multi-client harness itself works before anything is built on it.
    ///
    /// Every problem this project's external audit found in the networked layer was invisible with
    /// one client: carts configured as tractors, trains split by redistribution, a tractor taken
    /// from the person driving it. None of them can be tested without a second machine, and until
    /// now there was no way to have one without a cloud session.
    ///
    /// Netcode runs distributed authority in-process when it is not asked to use the cloud service,
    /// with one instance acting as session owner. That is what these tests use.
    /// </summary>
    public sealed class HarnessSmokeTest : RampMultiplayerTest
    {
        [UnityTest]
        public IEnumerator TwoClientsAndASessionOwnerAreConnectedToEachOther()
        {
            Assert.That(m_ServerNetworkManager.IsListening, Is.True);
            Assert.That(m_ServerNetworkManager.NetworkConfig.NetworkTopology,
                Is.EqualTo(NetworkTopologyTypes.DistributedAuthority),
                "the whole point of this harness is exercising the topology the game actually ships");

            Assert.That(m_ClientNetworkManagers.Length, Is.EqualTo(2));
            foreach (var client in m_ClientNetworkManagers)
            {
                Assert.That(client.IsConnectedClient, Is.True);
                Assert.That(client.LocalClientId, Is.Not.EqualTo(m_ServerNetworkManager.LocalClientId));
            }

            yield return null;
        }
    }
}
