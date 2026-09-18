using UnityEngine;

namespace BelowTheWing.Menu
{
    public static class Stepping
    {
        public static int Next(int index, int count, int by)
            => count <= 0 ? 0 : (((index + by) % count) + count) % count;

        public static float Nudge(float value, float least, float most, int by, int steps)
            => Mathf.Clamp(value + (by * (most - least) / Mathf.Max(steps, 1)), least, most);
    }
}
