using System;
using System.Threading.Tasks;
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

        [SerializeField, Tooltip("Players allowed in a session")]
        int m_MaxPlayers = 5;

        ISession m_Session;

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
                Debug.LogWarning(
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
            await SignInAsync();
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
                    Name = $"Below the Wing {DateTime.Now:HH:mm}",
                    MaxPlayers = m_MaxPlayers
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
            }
            catch (Exception e)
            {
                Fail($"Could not reach the multiplayer service: {e.Message}");
            }
        }

        async Task<bool> ReadyToConnectAsync()
        {
            if (Phase == SessionPhase.Failed || Phase == SessionPhase.Offline)
            {
                await SignInAsync();
            }

            return Phase == SessionPhase.Ready;
        }

        async Task Attempt(Func<Task> connect)
        {
            Move(SessionPhase.Connecting);

            try
            {
                await connect();
                Move(SessionPhase.InSession);
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
