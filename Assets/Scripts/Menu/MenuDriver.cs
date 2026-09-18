using System.Collections.Generic;
using BelowTheWing.Net;
using BelowTheWing.Wiring;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BelowTheWing.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuDriver : MonoBehaviour
    {
        public const string ShiftScene = "Apron";

        [Header("Typefaces")]
        [SerializeField] Font m_Display;
        [SerializeField] Font m_Body;
        [SerializeField] Font m_Data;

        [Header("Icons")]
        [SerializeField] Texture2D m_ReadyIcon;
        [SerializeField] Texture2D m_UnreadyIcon;

        [Header("Wiring")]
        [SerializeField] SessionGateway m_Gateway;
        [SerializeField] MenuCamera m_Camera;
        [SerializeField] MenuBackdrop m_Backdrop;

        readonly MenuFlow m_Flow = new MenuFlow();

        static readonly CrewOnStage[] NobodyYet = new CrewOnStage[0];

        readonly List<CrewOnStage> m_OnStage = new List<CrewOnStage>();

        MenuChrome m_Chrome;
        LobbyRoster m_Roster;
        bool m_Hosting;

        void Awake()
        {
            FindWhatIsAlreadyInTheScene();

            MenuLook.Typeface = new MenuTypeface
            {
                Display = m_Display,
                Body = m_Body,
                Data = m_Data
            };

            MenuLook.Icons = new MenuIcons
            {
                Ready = m_ReadyIcon,
                Unready = m_UnreadyIcon
            };

            m_Chrome = new MenuChrome(GetComponent<UIDocument>().rootVisualElement);

            m_Chrome.Hosted += Host;
            m_Chrome.Joining += () => m_Flow.Show(MenuScreen.Join);
            m_Chrome.JoinedWith += JoinWith;
            m_Chrome.SettingsOpened += () => m_Flow.Show(MenuScreen.Settings);
            m_Chrome.Quit += Leave;
            m_Chrome.Backed += m_Flow.Back;
            m_Chrome.ReadyToggled += ReadyUp;
            m_Chrome.ShiftStarted += StartTheShift;

            m_Flow.Changed += WentTo;
            m_Flow.LeftTheSession += LeaveTheSession;

            WentTo(MenuScreen.Title);
        }

        void FindWhatIsAlreadyInTheScene()
        {
            m_Gateway = m_Gateway != null ? m_Gateway : FindAnyObjectByType<SessionGateway>();
            m_Camera = m_Camera != null ? m_Camera : FindAnyObjectByType<MenuCamera>();
            m_Backdrop = m_Backdrop != null ? m_Backdrop : FindAnyObjectByType<MenuBackdrop>();

            if (m_Gateway == null || m_Camera == null || m_Backdrop == null)
            {
                throw MisbuiltException.Refuse(
                    this,
                    "cannot find a SessionGateway, a MenuCamera and a MenuBackdrop in the scene, " +
                    "and the menu drives all three");
            }
        }

        void Update()
        {
            if (!m_Flow.Open)
            {
                return;
            }

            if (m_Flow.Showing == MenuScreen.Title)
            {
                if (SomethingWasPressed())
                {
                    m_Flow.AnyButtonPressed();
                }

                return;
            }

            Steer();

            if (m_Flow.Showing == MenuScreen.Lobby)
            {
                PaintTheLobby();
            }
        }

        void Steer()
        {
            var keys = Keyboard.current;

            if (keys == null)
            {
                return;
            }

            // A join code is typed into a field on the same screen as the buttons, and every letter
            // in one is also a movement key.
            if (m_Chrome.TypingACode)
            {
                if (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame)
                {
                    m_Chrome.Chose();
                }

                if (keys.escapeKey.wasPressedThisFrame)
                {
                    m_Flow.Back();
                }

                return;
            }

            if (keys.downArrowKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame)
            {
                m_Chrome.Moved(1);
            }

            if (keys.upArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame)
            {
                m_Chrome.Moved(-1);
            }

            if (keys.rightArrowKey.wasPressedThisFrame || keys.dKey.wasPressedThisFrame)
            {
                m_Chrome.Nudged(1);
            }

            if (keys.leftArrowKey.wasPressedThisFrame || keys.aKey.wasPressedThisFrame)
            {
                m_Chrome.Nudged(-1);
            }

            if (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame)
            {
                m_Chrome.Chose();
            }

            if (keys.escapeKey.wasPressedThisFrame)
            {
                m_Flow.Back();
            }
        }

        static bool SomethingWasPressed()
            => (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
               || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
               || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        void WentTo(MenuScreen screen)
        {
            m_Chrome.Show(screen);

            var staging = MenuStaging.For(screen);

            if (!staging.Staged)
            {
                return;
            }

            m_Backdrop.CartDoors.Open(staging.DoorsOpen);

            if (staging.Station == MenuStation.AsPlaced)
            {
                m_Camera.StartWhereItIs();
                return;
            }

            m_Camera.TravelTo(m_Backdrop.StandingAt(staging.Station), staging.TravelSeconds);
        }

        void Host()
        {
            if (m_Hosting)
            {
                return;
            }

            m_Hosting = true;

            m_Chrome.SayTheCodeIs("");
            m_Flow.Show(MenuScreen.Lobby);

            OpenTheSession();
        }

        async void OpenTheSession()
        {
            await m_Gateway.HostAsync();

            m_Hosting = false;

            if (m_Gateway.Phase == SessionPhase.InSession)
            {
                m_Chrome.SayTheCodeIs(m_Gateway.JoinCode);
                return;
            }

            m_Chrome.SayTheCodeIs(Why("no code"));
        }

        async void JoinWith(string code)
        {
            m_Chrome.SayTheJoinFailed("Joining...");

            await m_Gateway.JoinAsync(code);

            if (m_Gateway.Phase != SessionPhase.InSession)
            {
                m_Chrome.SayTheJoinFailed(Why("That did not work. Check the code and try again."));
                return;
            }

            m_Chrome.SayTheCodeIs(m_Gateway.JoinCode);
            m_Flow.Show(MenuScreen.Lobby);
        }

        string Why(string otherwise)
            => string.IsNullOrEmpty(m_Gateway.FailureReason) ? otherwise : m_Gateway.FailureReason;

        void ReadyUp()
        {
            if (m_Roster != null && m_Roster.IsSpawned)
            {
                m_Roster.ReadyUp(!m_Roster.AmIReady);
                return;
            }

            m_Chrome.ReadyOnYourOwn(!m_Chrome.AloneAndReady);
        }

        void PaintTheLobby()
        {
            m_Roster ??= FindAnyObjectByType<LobbyRoster>();

            var spawned = m_Roster != null && m_Roster.IsSpawned;

            m_Chrome.ShowTheLobby(
                spawned ? m_Roster.AmIReady : m_Chrome.AloneAndReady,
                spawned ? m_Roster.CanStart : m_Chrome.AloneAndReady);

            StandTheCrewUp(spawned);

            m_Backdrop.ShowThisManyCrew(m_OnStage.Count);

            // A nameplate over somebody the doors have not uncovered yet reads as a label floating
            // on the outside of the cart, so they wait for the second set of doors to be thrown.
            m_Chrome.ShowCrewOnStage(
                m_Backdrop.CartDoors.BothSetsAreMoving ? m_OnStage : NobodyYet,
                m_Camera.Eye);
        }

        void StandTheCrewUp(bool spawned)
        {
            m_OnStage.Clear();

            if (!spawned)
            {
                m_OnStage.Add(new CrewOnStage("You", m_Chrome.AloneAndReady, m_Backdrop.PlateOver(0)));
                return;
            }

            var seats = m_Roster.Seats;
            var room = Mathf.Min(seats.Count, m_Backdrop.CrewCount);

            for (var seat = 0; seat < room; seat++)
            {
                m_OnStage.Add(new CrewOnStage(
                    seats[seat].Called.ToString(), seats[seat].Ready, m_Backdrop.PlateOver(seat)));
            }
        }

        void StartTheShift()
        {
            if (m_Roster != null && m_Roster.IsSpawned && !m_Roster.CanStart)
            {
                return;
            }

            if (m_Roster == null && !m_Chrome.AloneAndReady)
            {
                return;
            }

            m_Flow.ShiftStarted();
            GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.None;

            var netcode = NetworkManager.Singleton;

            if (netcode != null && netcode.IsListening && netcode.SceneManager != null)
            {
                netcode.SceneManager.LoadScene(ShiftScene, LoadSceneMode.Single);
                return;
            }

            SceneManager.LoadScene(ShiftScene, LoadSceneMode.Single);
        }

        async void LeaveTheSession()
        {
            m_Roster = null;
            m_Chrome.ReadyOnYourOwn(false);

            await m_Gateway.LeaveAsync();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        void Leave()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
