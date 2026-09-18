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
        public const int BadgePixels = 64;

        public static Texture2D Badge(bool ready)
            => ready ? MenuLook.Icons.Ready : MenuLook.Icons.Unready;

        public static bool Visible(Vector3 viewportPoint)
            => viewportPoint.z > 0f
               && viewportPoint.x >= 0f && viewportPoint.x <= 1f
               && viewportPoint.y >= 0f && viewportPoint.y <= 1f;

        public static Vector2 OnPanel(Vector3 viewportPoint, Vector2 panelSize)
            => new Vector2(viewportPoint.x * panelSize.x, (1f - viewportPoint.y) * panelSize.y);
    }
}
