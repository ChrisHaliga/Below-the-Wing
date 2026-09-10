using UnityEngine;

namespace BelowTheWing.Net
{
    /// <summary>
    /// The screen a player sees before they are on the apron: host a session, or join one by code.
    ///
    /// Drawn with immediate-mode GUI rather than built out of interface assets. Everything on the
    /// apron is a grey box at this stage and this screen is in the same spirit -- it exists so the
    /// game can be got into and tested, and it will be replaced wholesale rather than grown into
    /// the real thing, so there is nothing here worth keeping in a prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SessionEntryScreen : MonoBehaviour
    {
        [SerializeField, Tooltip("The gateway this screen drives and reports on.")]
        SessionGateway m_Gateway;

        string m_TypedJoinCode = "";

        void OnGUI()
        {
            if (m_Gateway == null)
            {
                return;
            }

            // Once a player is on the apron the only thing worth keeping on screen is the code
            // their friends need.
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

            GUI.enabled = !m_Gateway.Busy;

            if (GUILayout.Button("Host a session", GUILayout.Height(32f)))
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
