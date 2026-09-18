using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class SlidingDoor
    {
        public const float FullyOpenWeight = 100f;

        public static float ShapeWeight(float openness) => Mathf.Clamp01(openness) * FullyOpenWeight;

        public static float OpennessAt(float travelledMetres, float travelMetres)
            => travelMetres <= 0f ? 0f : Mathf.Clamp01(travelledMetres / travelMetres);

        public static float CoveredMetres(float poleAt, float fixedPoleAt)
            => Mathf.Abs(fixedPoleAt - poleAt);

        public static float CoverSitsAt(float poleAt, float fixedPoleAt)
            => (poleAt + fixedPoleAt) * 0.5f;
    }
}
