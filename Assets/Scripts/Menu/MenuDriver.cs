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

        [Header("Wiring")]
        [SerializeField] SessionGateway m_Gateway;
        [SerializeField] MenuCamera m_Camera;
        [SerializeField] MenuBackdrop m_Backdrop;

        readonly MenuFlow m_Flow = new MenuFlow();

        MenuChrome m_Chrome;
        LobbyRoster m_Roster;
        bool m_Hosting;

        void Awake()
        {
            FindWhatIsAlreadyInTheScene();

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

            if (m_Flow.Showing == MenuScreen.Title && SomethingWasPressed())
            {
                m_Flow.AnyButtonPressed();
            }

            if (m_Flow.Showing == MenuScreen.Lobby)
            {
                PaintTheLobby();
            }
        }

        static bool SomethingWasPressed()
            => (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
               || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
               || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        void WentTo(MenuScreen screen)
        {
            m_Chrome.Show(screen);

            if (screen == MenuScreen.None)
            {
                return;
            }

            var shot = screen switch
            {
                MenuScreen.Title => m_Backdrop.TitleShot,
                MenuScreen.Lobby => m_Backdrop.LobbyShot,
                _ => m_Backdrop.PanelShot
            };

            if (screen == MenuScreen.Title)
            {
                m_Camera.StartOn(shot);
                return;
            }

            m_Camera.TravelTo(shot);
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

            if (m_Roster != null && m_Roster.IsSpawned)
            {
                m_Chrome.ShowTheSeats(m_Roster.Seats, m_Roster.AmIReady, m_Roster.CanStart);
                m_Backdrop.ShowThisManyCrew(m_Roster.Filled);
                return;
            }

            m_Chrome.ShowOneSeatWaitingOnTheService();
            m_Backdrop.ShowThisManyCrew(1);
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
