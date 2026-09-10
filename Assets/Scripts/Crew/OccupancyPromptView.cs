using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// Tells the player they can press E, and tells them when they cannot.
    ///
    /// The seat has always worked out what to say. Nothing ever said it: a player walked up to a
    /// tractor and was offered nothing, and a player refused a tractor somebody else was driving
    /// was told so for one physics step, twenty milliseconds, before the next refresh replaced it
    /// with the offer again.
    ///
    /// Drawn with immediate-mode GUI, in the same spirit as everything else here while the apron is
    /// grey boxes. It is meant to be replaced along with the rest of the interface.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OccupancyPromptView : MonoBehaviour
    {
        VehicleOccupancy m_Seat;

        /// <summary>Starts showing what this seat has to say.</summary>
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

            // Refusals are the ones worth noticing, so they are not the same colour as an offer.
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
