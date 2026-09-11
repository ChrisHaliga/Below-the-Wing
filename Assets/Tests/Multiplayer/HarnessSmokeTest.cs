using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.Multiplayer
{
    /// <summary>
    /// That the harness every other multiplayer test stands on is really there.
    ///
    /// Two things have to be true before any of those tests mean anything, and neither is visible
    /// from inside a test that assumes them. There have to be three separate instances, because a
    /// train split across machines or a tractor taken from its driver simply cannot happen with
    /// one. And the topology has to be distributed authority, because that is what the game ships
    /// and it is the reason no instance here is a server.
    ///
    /// Netcode runs distributed authority between instances in a single process when it is not
    /// asked to use the cloud service, which is what makes testing either of them possible at all.
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
