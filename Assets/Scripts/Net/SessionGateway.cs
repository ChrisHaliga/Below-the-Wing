using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace BelowTheWing.Net
{
    /// <summary>Where a client has got to in the business of joining other people.</summary>
    public enum SessionPhase
    {
        /// <summary>Nothing has been attempted yet.</summary>
        Offline,

        /// <summary>Talking to the backend for the first time and identifying this player.</summary>
        SigningIn,

        /// <summary>Signed in, not in a session, able to host or join one.</summary>
        Ready,

        /// <summary>A host or join attempt is in flight.</summary>
        Connecting,

        /// <summary>In a session with other players.</summary>
        InSession,

        /// <summary>The last attempt failed. <see cref="SessionGateway.FailureReason"/> says why.</summary>
        Failed
    }

    /// <summary>
    /// The only thing in the game that knows the backend exists.
    ///
    /// Joining other players is a single operation here: signing in, creating or finding a session,
    /// and connecting the netcode layer all happen together, because in this setup a session is
    /// what starts the netcode. There is no separate "start hosting" step to call afterwards.
    ///
    /// Sessions are created with distributed authority, which means there is no host machine that
    /// owns everything. Each client simulates the objects it owns, and one client is nominated as
    /// session owner to look after state that belongs to nobody in particular. That nomination
    /// moves automatically if the current session owner leaves.
    ///
    /// Nothing on the apron exists until this reports <see cref="SessionPhase.InSession"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SessionGateway : MonoBehaviour
    {
        [SerializeField, Tooltip("Identifies this game's sessions. Must match between players.")]
        string m_SessionType = "below-the-wing";

        [SerializeField, Tooltip("Most players allowed in one session.")]
        int m_MaxPlayers = 5;

        ISession m_Session;

        /// <summary>How far along this client is.</summary>
        public SessionPhase Phase { get; private set; } = SessionPhase.Offline;

        /// <summary>Why the last attempt failed, in words a player can read. Empty if it did not.</summary>
        public string FailureReason { get; private set; } = "";

        /// <summary>The code other players type in to join this session. Empty until in one.</summary>
        public string JoinCode => m_Session?.Code ?? "";

        /// <summary>True while an attempt is in flight, so the screen can lock its buttons.</summary>
        public bool Busy => Phase is SessionPhase.SigningIn or SessionPhase.Connecting;

        /// <summary>Whether this client is the one looking after session-wide state.</summary>
        public bool IsSessionOwner
        {
            get
            {
                var manager = NetworkManager.Singleton;
                return manager != null && manager.IsListening && manager.LocalClient.IsSessionOwner;
            }
        }

        async void Start()
        {
            await SignInAsync();
        }

        /// <summary>Creates a new session and starts netcode in it.</summary>
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

        /// <summary>Joins an existing session by its code and starts netcode in it.</summary>
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

                // The screen has to come back to life after this, or a mistyped code leaves the
                // player looking at a button that no longer does anything.
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
