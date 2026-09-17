using UnityEngine;

namespace BelowTheWing.Settings
{
    public static class DisplayIdentity
    {
        public static bool Same(DisplayInfo a, DisplayInfo b)
            => a.name == b.name
               && a.width == b.width
               && a.height == b.height
               && a.workArea == b.workArea;
    }
}
