using UnityEngine;

namespace BelowTheWing.Crew
{
    [DisallowMultipleComponent]
    public sealed class OccupancyPromptView : MonoBehaviour
    {
        VehicleOccupancy m_Seat;

        public void Watch(VehicleOccupancy seat) => m_Seat = seat;

        void OnGUI()
        {
            if (m_Seat == null || string.IsNullOrEmpty(m_Seat.Message))
            {
                return;
            }

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18
            };

            style.normal.textColor = m_Seat.Prompt == OccupancyPrompt.VehicleTaken
                ? new Color(1f, 0.6f, 0.45f)
                : Color.white;

            const float width = 520f;
            const float height = 30f;
            var box = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.72f, width, height);

            GUI.Label(box, m_Seat.Message, style);
        }
    }
}
