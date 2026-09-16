using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class SlidingDoor
    {
        public static float OpennessAt(float poleMetres, float trackMetres)
            => trackMetres <= 0f ? 0f : Mathf.Clamp01(poleMetres / trackMetres);

        public static float StillCoveredMetres(float openness, float openingMetres)
            => openingMetres * Mathf.Clamp01(1f - openness);

        public static float CoverSitsAt(float openness, float openingMetres)
            => (StillCoveredMetres(openness, openingMetres) - openingMetres) * 0.5f;
    }
}
