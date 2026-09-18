using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BelowTheWing.Wiring;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace BelowTheWing.Net
{
    public enum SessionPhase
    {
        Offline,

        SigningIn,

        Ready,

        Connecting,

        InSession,

        Failed
    }

    [DisallowMultipleComponent]
    public sealed class SessionGateway : MonoBehaviour, ILeaveTheService
    {
        [SerializeField, Tooltip("Session type name")]
        string m_SessionType = "below-the-wing";

        ISession m_Session;
        Task m_SigningIn;

        readonly ServiceLeave m_Leave = new ServiceLeave();

        public SessionPhase Phase { get; private set; } = SessionPhase.Offline;

        public string FailureReason { get; private set; } = "";

        public string JoinCode => m_Session?.Code ?? "";

        public bool Busy => Phase is SessionPhase.SigningIn or SessionPhase.Connecting;

        public bool InOne => m_Session != null || m_Leave.Pending;

        public async Task LeaveAsync()
        {
            var leaving = m_Session;
            m_Session = null;

            if (leaving == null)
            {
                return;
            }

            try
            {
                await m_Leave.Run(leaving.LeaveAsync);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning(
                    $"Could not tell the service this player had left: {e.Message}. The session " +
                    "record stays until the service times it out.", this);
            }
        }

        public bool IsSessionOwner
        {
            get
            {
                var manager = NetworkManager.Singleton;
                return manager != null && manager.IsListening && manager.LocalClient.IsSessionOwner;
            }
        }

        void Awake()
            => gameObject.AddComponent<SessionLifetime>()
                .Ends(GetComponent<NetworkManager>(), this);

        async void Start()
        {
            await SignedIn();
        }

        // Signing in starts as the menu opens and takes a round trip to the service. Host used to
        // read the phase while that was still in flight, see something other than Ready, and give
        // up without saying anything, which left the lobby waiting on a code that was never asked
        // for. One task is shared instead, and a second caller waits on the first.
        Task SignedIn()
        {
            if (m_SigningIn == null || (m_SigningIn.IsCompleted && Phase != SessionPhase.Ready))
            {
                m_SigningIn = SignInAsync();
            }

            return m_SigningIn;
        }

        public void PlayAlone()
        {
            if (!LocalSession.Start(NetworkManager.Singleton, out var refusal))
            {
                Fail(refusal);
                return;
            }

            Move(SessionPhase.InSession);
        }

        public async Task HostAsync()
        {
            if (!await ReadyToConnectAsync())
            {
                return;
            }

            await Attempt(async () =>
            {
                var options = new SessionOptions
                {
                    Type = m_SessionType,
                    Name = $"{Shift.Title} {DateTime.Now:HH:mm}",
                    MaxPlayers = Shift.MostCrew
                }.WithDistributedAuthorityNetwork();

                m_Session = await MultiplayerService.Instance.CreateSessionAsync(options);
            });
        }

        public async Task JoinAsync(string joinCode)
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Fail("Enter a join code first.");
                return;
            }

            if (!await ReadyToConnectAsync())
            {
                return;
            }

            await Attempt(async () =>
            {
                var options = new JoinSessionOptions();
                m_Session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode.Trim(), options);
            });
        }

        async Task SignInAsync()
        {
            Move(SessionPhase.SigningIn);

            var clock = Stopwatch.StartNew();

            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                Move(SessionPhase.Ready);

                UnityEngine.Debug.Log($"Signed in to the multiplayer service in {clock.ElapsedMilliseconds} ms.", this);
            }
            catch (Exception e)
            {
                Fail($"Could not reach the multiplayer service: {e.Message}");
            }
        }

        async Task<bool> ReadyToConnectAsync()
        {
            await SignedIn();

            return Phase == SessionPhase.Ready;
        }

        async Task Attempt(Func<Task> connect)
        {
            Move(SessionPhase.Connecting);

            var clock = Stopwatch.StartNew();

            try
            {
                await connect();
                Move(SessionPhase.InSession);

                UnityEngine.Debug.Log($"Session open in {clock.ElapsedMilliseconds} ms.", this);
            }
            catch (Exception e)
            {
                m_Session = null;

                Fail(Readable(e));
            }
        }

        static string Readable(Exception e)
            => e is SessionException session
                ? $"{session.Error}: {session.Message}"
                : e.Message;

        void Fail(string reason)
        {
            FailureReason = reason;
            Move(SessionPhase.Failed);
        }

        void Move(SessionPhase phase)
        {
            if (phase != SessionPhase.Failed)
            {
                FailureReason = "";
            }

            Phase = phase;
        }
    }
}
