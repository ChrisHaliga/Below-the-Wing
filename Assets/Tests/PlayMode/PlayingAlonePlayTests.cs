using System.Collections;
using BelowTheWing.Net;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// Starting a game on this machine and nobody else's.
    ///
    /// Playing with other people means a session created through Unity's multiplayer service: an
    /// anonymous sign-in, a relay allocation and a lobby, which is several round trips to the
    /// internet before there is an apron to stand on. Playing alone needs none of that, and paying
    /// for it anyway is most of a minute every time somebody wants to try something out.
    ///
    /// What must not change is what kind of session it is. Every ownership rule in this game assumes
    /// distributed authority -- that no machine is a server, that objects change hands, that the
    /// session owner is simply one of the clients. A solo session that quietly ran as client-server
    /// would be a different game wearing the same scene.
    /// </summary>
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

            // The same configuration the apron scene ships: topology, scene management and player
            // spawning included. A session started with those switched off would prove that some
            // other session starts, not this one.
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
