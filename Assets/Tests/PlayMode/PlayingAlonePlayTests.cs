using System.Collections;
using BelowTheWing.Net;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class PlayingAlonePlayTests
    {
        GameObject m_Host;
        NetworkManager m_Netcode;

        [SetUp]
        public void SetUp()
        {
            m_Host = new GameObject("NetworkManager");
            m_Host.SetActive(false);

            m_Netcode = m_Host.AddComponent<NetworkManager>();
            var transport = m_Host.AddComponent<UnityTransport>();
            transport.ConnectionData.Port = Ports.NobodyElseIsOn();

            m_Netcode.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                NetworkTopology = NetworkTopologyTypes.DistributedAuthority,
                AutoSpawnPlayerPrefabClientSide = true,
                EnableSceneManagement = true,
                ConnectionApproval = false,
                PlayerPrefab = null
            };

            m_Host.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Netcode != null && m_Netcode.IsListening)
            {
                m_Netcode.Shutdown();
            }

            Object.DestroyImmediate(m_Host);
        }

        [UnityTest]
        public IEnumerator ASessionStartsOnThisMachineWithoutTheMultiplayerService()
        {
            var started = LocalSession.Start(m_Netcode);

            yield return null;

            Assert.That(started, Is.True,
                "netcode refused to start a session on this machine alone. Everything about playing " +
                "alone rests on this: no sign-in, no relay, no lobby");
            Assert.That(m_Netcode.IsListening, Is.True, "the session is not running");
        }

        [UnityTest]
        public IEnumerator PlayingAloneIsStillADistributedAuthoritySession()
        {
            LocalSession.Start(m_Netcode);
            yield return null;

            Assert.That(m_Netcode.DistributedAuthorityMode, Is.True,
                "a solo session that runs client-server instead is a different game: ownership " +
                "requests are answered by a machine that should not be answering them, and the " +
                "disconnect handling waits for an event that never arrives");
            Assert.That(m_Netcode.CMBServiceConnection, Is.False,
                "it is meant to be talking to nothing at all");
            Assert.That(m_Netcode.LocalClient.IsSessionOwner, Is.True,
                "somebody has to be looking after session-wide state, and alone that is this machine");
        }

        [UnityTest]
        public IEnumerator TheApronIsReadyAsSoonAsTheSessionStarts()
        {
            var began = Time.realtimeSinceStartup;

            LocalSession.Start(m_Netcode);
            yield return null;

            Assert.That(m_Netcode.IsConnectedClient, Is.True,
                "the local client is not connected to its own session, so nothing would ever be " +
                "spawned onto the apron");
            Assert.That(Time.realtimeSinceStartup - began, Is.LessThan(1f),
                "starting alone waited on something. There is nothing here to wait for: no sign-in, " +
                "no relay allocation, no lobby -- that is the whole point of it");
        }
    }
}
