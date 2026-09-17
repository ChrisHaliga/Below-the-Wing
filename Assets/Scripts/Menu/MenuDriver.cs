using System.Collections.Generic;
using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Net;
using BelowTheWing.Session;
using BelowTheWing.Wiring;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BelowTheWing.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuDriver : MonoBehaviour
    {
        [SerializeField, Tooltip("Where everything stands on the menu apron")]
        ApronLayoutSettings m_Layout = ApronLayoutSettings.Default;

        [Header("Wiring")]
        [SerializeField] SessionGateway m_Gateway;
        [SerializeField] RampSession m_Session;
        [SerializeField] MenuCamera m_Camera;
        [SerializeField] Camera m_PlayingCamera;

        [Header("What the background is made of")]
        [SerializeField] AircraftProfile m_AircraftProfile;
        [SerializeField] CrewProfile m_CrewProfile;
        [SerializeField] GameObject m_TractorPrefab;
        [SerializeField] GameObject m_CartPrefab;
        [SerializeField] GameObject m_AircraftPrefab;

        readonly MenuFlow m_Flow = new MenuFlow();
        readonly List<Transform> m_Standing = new List<Transform>();

        MenuChrome m_Chrome;
        MenuApron m_Apron;
        LobbyRoster m_Roster;

        void Awake()
        {
            if (m_Gateway == null || m_Session == null || m_Camera == null)
            {
                throw MisbuiltException.Refuse(
                    this, "has no gateway, session or menu camera, so the menu can drive nothing");
            }

            m_Apron = new MenuApron(
                m_Layout, m_AircraftProfile, m_CrewProfile,
                m_TractorPrefab, m_CartPrefab, m_AircraftPrefab);

            m_Chrome = new MenuChrome(GetComponent<UIDocument>().rootVisualElement);

            m_Chrome.Hosted += Host;
            m_Chrome.Joining += () => m_Flow.Show(MenuScreen.Join);
            m_Chrome.JoinedWith += JoinWith;
            m_Chrome.SettingsOpened += () => m_Flow.Show(MenuScreen.Settings);
            m_Chrome.Quit += Leave;
            m_Chrome.Backed += m_Flow.Back;
            m_Chrome.ReadyToggled += () => m_Roster?.ReadyUp(!m_Roster.AmIReady);
            m_Chrome.ShiftStarted += StartTheShift;

            m_Flow.Changed += WentTo;
            m_Flow.LeftTheSession += LeaveTheSession;

            m_Session.HoldTheCrewBack();

            WentTo(MenuScreen.Title);
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
                MenuScreen.Title => MenuSubjects.WholeApron(m_Apron.Plan),
                MenuScreen.Lobby => MenuSubjects.Crew(m_Apron.Plan, CrewStandingAbout()),
                _ => MenuSubjects.Airliner(m_Apron.Plan)
            };

            if (screen == MenuScreen.Title)
            {
                m_Camera.StartOn(shot);
                return;
            }

            m_Camera.TravelTo(shot);
        }

        async void Host()
        {
            await m_Gateway.HostAsync();

            AfterConnecting();
        }

        async void JoinWith(string code)
        {
            await m_Gateway.JoinAsync(code);

            AfterConnecting();
        }

        void AfterConnecting()
        {
            if (m_Gateway.Phase != SessionPhase.InSession)
            {
                m_Chrome.SayTheJoinFailed(
                    string.IsNullOrEmpty(m_Gateway.FailureReason)
                        ? "That did not work. Check the code and try again."
                        : m_Gateway.FailureReason);
                return;
            }

            m_Chrome.SayTheCodeIs(m_Gateway.JoinCode);
            m_Flow.Show(MenuScreen.Lobby);
        }

        void PaintTheLobby()
        {
            m_Roster ??= FindAnyObjectByType<LobbyRoster>();

            if (m_Roster == null)
            {
                return;
            }

            m_Chrome.ShowTheSeats(m_Roster.Seats, m_Roster.AmIReady, m_Roster.CanStart);
        }

        IReadOnlyList<Transform> CrewStandingAbout()
        {
            m_Standing.Clear();

            foreach (var crew in FindObjectsByType<CrewCharacter>(FindObjectsSortMode.None))
            {
                m_Standing.Add(crew.transform);
            }

            return m_Standing;
        }

        void StartTheShift()
        {
            if (m_Roster == null || !m_Roster.CanStart)
            {
                return;
            }

            m_Session.StartTheShift();

            m_Apron.TakeItDown();
            m_Flow.ShiftStarted();

            if (m_PlayingCamera != null)
            {
                m_PlayingCamera.enabled = true;
            }

            m_Camera.gameObject.SetActive(false);
            GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.None;
        }

        async void LeaveTheSession()
        {
            await m_Gateway.LeaveAsync();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            m_Roster = null;
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
