using UnityEngine;

namespace BelowTheWing.Wiring
{
    public static class Discard
    {
        public static void Now(Object thing)
        {
            if (thing == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(thing);
            }
            else
            {
                Object.DestroyImmediate(thing);
            }
        }
    }
}
