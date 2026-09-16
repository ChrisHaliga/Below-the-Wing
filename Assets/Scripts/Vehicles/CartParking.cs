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

        public float BrakingForce(float speedMetresPerSecond, float massKg, float holdsAt)
            => Parked ? Mathf.Min(holdsAt * massKg, Mathf.Abs(speedMetresPerSecond) * massKg * 20f) : 0f;
    }
}
