using UnityEngine;

namespace BelowTheWing.Menu
{
    public readonly struct CrewOnStage
    {
        public CrewOnStage(string called, bool ready, Vector3 head)
        {
            Called = called;
            Ready = ready;
            Head = head;
        }

        public string Called { get; }

        public bool Ready { get; }

        public Vector3 Head { get; }
    }

    public static class CrewPlate
    {
        public const int RingPixels = 46;

        public static Color Ring(bool ready) => ready ? MenuLook.Good : MenuLook.InkSoft;

        public static Color Fill(bool ready) => ready ? MenuLook.Good : Color.clear;

        public static bool TickShows(bool ready) => ready;

        public static bool Visible(Vector3 viewportPoint)
            => viewportPoint.z > 0f
               && viewportPoint.x >= 0f && viewportPoint.x <= 1f
               && viewportPoint.y >= 0f && viewportPoint.y <= 1f;

        public static Vector2 OnPanel(Vector3 viewportPoint, Vector2 panelSize)
            => new Vector2(viewportPoint.x * panelSize.x, (1f - viewportPoint.y) * panelSize.y);
    }
}
