using UnityEngine;

namespace BelowTheWing.Net
{
    [DisallowMultipleComponent]
    public sealed class SessionEntryScreen : MonoBehaviour
    {
        [SerializeField, Tooltip("Session this screen drives")]
        SessionGateway m_Gateway;

        string m_TypedJoinCode = "";

        void OnGUI()
        {
            if (m_Gateway == null)
            {
                return;
            }

            if (m_Gateway.Phase == SessionPhase.InSession)
            {
                DrawJoinCodeBanner();
                return;
            }

            const float width = 340f;
            const float height = 220f;
            var panel = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            GUI.Box(panel, "Below the Wing");
            GUILayout.BeginArea(new Rect(panel.x + 20f, panel.y + 30f, panel.width - 40f, panel.height - 45f));

            GUILayout.Label(Status());
            GUILayout.Space(8f);

            if (GUILayout.Button("Play alone", GUILayout.Height(32f)))
            {
                m_Gateway.PlayAlone();
            }

            GUILayout.Space(6f);

            GUI.enabled = !m_Gateway.Busy;

            if (GUILayout.Button("Host a session for others", GUILayout.Height(32f)))
            {
                _ = m_Gateway.HostAsync();
            }

            GUILayout.Space(12f);
            GUILayout.Label("Join code");
            m_TypedJoinCode = GUILayout.TextField(m_TypedJoinCode, 12);

            if (GUILayout.Button("Join", GUILayout.Height(28f)))
            {
                _ = m_Gateway.JoinAsync(m_TypedJoinCode);
            }

            GUI.enabled = true;
            GUILayout.EndArea();
        }

        void DrawJoinCodeBanner()
        {
            if (string.IsNullOrEmpty(m_Gateway.JoinCode))
            {
                return;
            }

            GUI.Box(new Rect(Screen.width - 260f, 10f, 250f, 46f), "");
            GUILayout.BeginArea(new Rect(Screen.width - 250f, 18f, 230f, 40f));
            GUILayout.Label($"Join code: {m_Gateway.JoinCode}");
            GUILayout.Label(m_Gateway.IsSessionOwner ? "You are the session owner" : "");
            GUILayout.EndArea();
        }

        string Status() => m_Gateway.Phase switch
        {
            SessionPhase.Offline => "Starting up.",
            SessionPhase.SigningIn => "Signing in.",
            SessionPhase.Ready => "Ready. Host a session, or join one with a code.",
            SessionPhase.Connecting => "Connecting.",
            SessionPhase.InSession => "On the apron.",
            SessionPhase.Failed => m_Gateway.FailureReason,
            _ => ""
        };
    }
}
