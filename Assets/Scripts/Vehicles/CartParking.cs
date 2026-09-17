using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public sealed class CartParking
    {
        public const float TooFastToParkMetresPerSecond = 1.5f;

        public const float HitchRisesToDegrees = 55f;

        public bool Parked { get; private set; }

        public bool Toggle(float speedMetresPerSecond)
        {
            if (!Parked && speedMetresPerSecond > TooFastToParkMetresPerSecond)
            {
                return false;
            }

            Parked = !Parked;
            return true;
        }

        public float HitchDegrees(float deltaTime, float current, float swingsAtDegreesPerSecond)
            => Mathf.MoveTowards(
                current, Parked ? HitchRisesToDegrees : 0f, swingsAtDegreesPerSecond * deltaTime);

        public float BrakingForce(float speedMetresPerSecond, float massKg, float holdsAt, float deltaTime)
        {
            if (!Parked)
            {
                return 0f;
            }

            var enoughToStopItThisStep = Mathf.Abs(speedMetresPerSecond) * massKg / Mathf.Max(deltaTime, 1e-5f);

            return Mathf.Min(holdsAt * massKg, enoughToStopItThisStep);
        }
    }
}
