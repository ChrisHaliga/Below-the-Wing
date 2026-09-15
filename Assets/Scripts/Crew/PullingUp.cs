using UnityEngine;

namespace BelowTheWing.Crew
{
    public sealed class PullingUp
    {
        public const float RisesAtMetresPerSecond = 1.6f;

        public const float PullsInAtMetresPerSecond = 1.6f;

        public const float FeetAboveTheHandsMetres = 0.05f;

        public const float OverTheHandsWithinMetres = 0.1f;

        public bool Pulling { get; private set; }

        public bool Ducking { get; private set; }

        public Vector3 Haul(bool asked, Vector3? hands, Vector3 feet)
        {
            Pulling = asked && hands.HasValue && NotOverThemYet(hands.Value, feet);

            if (!Pulling)
            {
                return Vector3.zero;
            }

            Ducking = true;

            if (StillBelowThem(hands.Value, feet))
            {
                return Vector3.up * RisesAtMetresPerSecond;
            }

            return Across(hands.Value, feet).normalized * PullsInAtMetresPerSecond;
        }

        public void Landed() => Ducking = false;

        static bool NotOverThemYet(Vector3 hands, Vector3 feet)
            => StillBelowThem(hands, feet)
               || Across(hands, feet).magnitude > OverTheHandsWithinMetres;

        static bool StillBelowThem(Vector3 hands, Vector3 feet)
            => feet.y < hands.y + FeetAboveTheHandsMetres;

        static Vector3 Across(Vector3 hands, Vector3 feet)
            => new Vector3(hands.x - feet.x, 0f, hands.z - feet.z);
    }
}
