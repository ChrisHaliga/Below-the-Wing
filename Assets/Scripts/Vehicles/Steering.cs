using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class Steering
    {
        public static float Step(
            float currentAngleDegrees, float steerInput, float speedMetresPerSecond, float deltaTime,
            VehicleProfile profile)
        {
            var asked = Mathf.Clamp(steerInput, -1f, 1f) * profile.maxSteerAngleDegrees;
            var canBite = AsFarAsTheTyresStillBite(speedMetresPerSecond, profile);

            var wanted = Mathf.Sign(asked) * Mathf.Min(Mathf.Abs(asked), canBite);
            var asFarAsItCanTurnThisStep = profile.steerRateDegreesPerSecond * deltaTime;

            return Mathf.MoveTowards(currentAngleDegrees, wanted, asFarAsItCanTurnThisStep);
        }

        public static float AsFarAsTheTyresStillBite(float speedMetresPerSecond, VehicleProfile profile)
        {
            var slidingItCanTake = profile.SlipItGripsHardestAt;
            var speed = Mathf.Abs(speedMetresPerSecond);

            if (slidingItCanTake <= 0f || speed <= slidingItCanTake)
            {
                return profile.maxSteerAngleDegrees;
            }

            return Mathf.Min(
                profile.maxSteerAngleDegrees,
                Mathf.Asin(Mathf.Clamp01(slidingItCanTake / speed)) * Mathf.Rad2Deg);
        }
    }
}
