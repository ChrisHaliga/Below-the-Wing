using System.Collections;
using System.Threading.Tasks;
using BelowTheWing.Net;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class ClosingDownPlayTests
    {
        GameObject m_Host;
        NetworkManager m_Netcode;
        UnityTransport m_Transport;

        [SetUp]
        public void SetUp()
        {
            m_Host = new GameObject("NetworkManager");
            m_Host.SetActive(false);

            m_Netcode = m_Host.AddComponent<NetworkManager>();
            m_Transport = m_Host.AddComponent<UnityTransport>();
            m_Transport.ConnectionData.Port = Ports.NobodyElseIsOn();

            m_Netcode.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = m_Transport,
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

        SessionLifetime AnEndingFor(ILeaveTheService leaving)
        {
            var lifetime = m_Host.AddComponent<SessionLifetime>();
            lifetime.Ends(m_Netcode, leaving);
            return lifetime;
        }

        sealed class NothingToLeave : ILeaveTheService
        {
            public bool InOne => false;

            public Task LeaveAsync() => Task.CompletedTask;
        }

        sealed class OneToLeave : ILeaveTheService
        {
            readonly TaskCompletionSource<bool> m_Left = new TaskCompletionSource<bool>();

            public bool Asked { get; private set; }

            public bool InOne => true;

            public Task LeaveAsync()
            {
                Asked = true;
                return m_Left.Task;
            }

            public void Finish() => m_Left.SetResult(true);
        }

        [UnityTest]
        public IEnumerator QuittingStopsTheSessionListening()
        {
            LocalSession.Start(m_Netcode);
            yield return null;

            Assert.That(m_Netcode.IsListening, Is.True, "there has to be a session to close");

            AnEndingFor(new NothingToLeave()).CloseBeforeQuitting();

            yield return null;

            Assert.That(m_Netcode.IsListening, Is.False,
                "nothing asked the session to stop. Netcode finishes a shutdown on its next update " +
                "rather than there and then, so this reads a frame later, but a quit that never " +
                "asks at all leaves peers waiting on a timeout to notice the player has gone");
        }

        [UnityTest]
        public IEnumerator QuittingWithNothingToLeaveIsNotHeldUp()
        {
            LocalSession.Start(m_Netcode);
            yield return null;

            Assert.That(AnEndingFor(new NothingToLeave()).CloseBeforeQuitting(), Is.True,
                "a quit with no service session to leave was held open. There is nothing to wait for");
        }

        [UnityTest]
        public IEnumerator QuittingWithASessionToLeaveWaitsForTheLeaveAndThenGoes()
        {
            LocalSession.Start(m_Netcode);
            yield return null;

            var service = new OneToLeave();
            var lifetime = AnEndingFor(service);

            Assert.That(lifetime.CloseBeforeQuitting(), Is.False,
                "the quit went through before the service was told the player had gone");
            Assert.That(service.Asked, Is.True, "nobody asked the service to be left");

            service.Finish();
            yield return null;

            Assert.That(lifetime.CloseBeforeQuitting(), Is.True,
                "the leave finished and the quit is still being refused. A game that cannot be " +
                "closed is worse than a session record the service times out on its own");
        }

        [UnityTest]
        public IEnumerator AQuitGoesThroughEvenWhenTheLeaveNeverAnswers()
        {
            LocalSession.Start(m_Netcode);
            yield return null;

            var lifetime = AnEndingFor(new OneToLeave());

            Assert.That(lifetime.CloseBeforeQuitting(), Is.False, "the first ask starts the leave");

            yield return Steps.Seconds(SessionLifetime.WaitsForTheServiceSeconds + 0.5f);

            Assert.That(lifetime.CloseBeforeQuitting(), Is.True,
                $"the leave never answered and the quit was still refused after " +
                $"{SessionLifetime.WaitsForTheServiceSeconds:F1} s. Nothing may hold a quit open " +
                "indefinitely");
        }

        [UnityTest]
        public IEnumerator LeavingPlayModeStopsTheSessionListening()
        {
            LocalSession.Start(m_Netcode);
            yield return null;

            var lifetime = AnEndingFor(new NothingToLeave());

            Object.DestroyImmediate(lifetime);
            yield return null;

            Assert.That(m_Netcode.IsListening, Is.False,
                "the thing that owns the session went away with the transport still bound. Leaving " +
                "play mode destroys it without quitting the process, so this is the path where a " +
                "socket outlives the session that opened it");
        }

        [UnityTest]
        public IEnumerator QuittingWithoutEverStartingIsHarmless()
        {
            var lifetime = AnEndingFor(new NothingToLeave());
            yield return null;

            Assert.That(lifetime.CloseBeforeQuitting(), Is.True,
                "a quit was held open over a session that was never started");
            Assert.That(m_Netcode.IsListening, Is.False);
        }

        [UnityTest]
        public IEnumerator TheGatewayBringsAnEndingWithIt()
        {
            var gateway = m_Host.AddComponent<SessionGateway>();
            yield return null;

            Assert.That(m_Host.GetComponent<SessionLifetime>(), Is.Not.Null,
                $"{nameof(SessionGateway)} is what holds a service session, and nothing built the " +
                "thing that ends one. A shutdown only tests construct is a shutdown that never runs");
            Assert.That(gateway, Is.InstanceOf<ILeaveTheService>(),
                "whatever ends a session has to be able to tell the service the player has gone");
        }

        [UnityTest]
        public IEnumerator ASoloSessionDoesNotTakeThePortItWasBuiltWith()
        {
            const ushort built = 7777;
            m_Transport.ConnectionData.Port = built;

            LocalSession.Start(m_Netcode);
            yield return null;

            Assert.That(m_Netcode.IsListening, Is.True, "there has to be a session to close");
            Assert.That(m_Transport.ConnectionData.Port, Is.Not.EqualTo(built),
                $"a solo session bound {built}, the port it was serialised with. Nothing dials into " +
                "a session running alone, so holding a fixed port buys nothing and costs a launch " +
                "whenever anything else already has it");
        }

        [UnityTest]
        public IEnumerator ASoloSessionStartsWhileSomethingElseHoldsThatPort()
        {
            var squatter = Squatting.On(out var taken);
            m_Transport.ConnectionData.Port = taken;

            LocalSession.Start(m_Netcode);
            yield return null;

            squatter.Dispose();

            Assert.That(m_Netcode.IsListening, Is.True,
                $"a solo session would not start because something else held port {taken}. That is " +
                "the case a player hits when a copy of the game is still alive, and no shutdown " +
                "code can help them because none of it ran");
        }
    }
}
