using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.Multiplayer
{
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
