using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class SlidingDoor
    {
        public const float FullyOpenWeight = 100f;

        public const float SeatingStiffnessNewtonsPerMetre = 40000f;

        public const float Shut = 0f;

        public const float Open = 1f;

        public static float ShapeWeight(float openness) => Mathf.Clamp01(openness) * FullyOpenWeight;

        public static float OpennessAt(float travelledMetres, float travelMetres)
            => travelMetres <= 0f ? 0f : Mathf.Clamp01(travelledMetres / travelMetres);

        public static float EndItSettlesTo(float openness)
            => Mathf.Clamp01(openness) < 0.5f ? Shut : Open;

        public static bool Seated(float openness, float withinFraction)
        {
            var howOpen = Mathf.Clamp01(openness);
            var band = Mathf.Clamp(withinFraction, 0f, 0.5f);

            return howOpen <= band || howOpen >= 1f - band;
        }

        public static float TargetAlongTheRail(float end, float travelMetres, float towardsTheEnd)
            => -(end - 0.5f) * travelMetres * towardsTheEnd;

        public static float CoveredMetres(float poleAt, float fixedPoleAt)
            => Mathf.Abs(fixedPoleAt - poleAt);

        public static float CoverSitsAt(float poleAt, float fixedPoleAt)
            => (poleAt + fixedPoleAt) * 0.5f;
    }
}
