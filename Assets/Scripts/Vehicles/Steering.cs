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
            var canBite = AsFarAsItMayTurnAt(speedMetresPerSecond, profile);

            var wanted = Mathf.Sign(asked) * Mathf.Min(Mathf.Abs(asked), canBite);
            var asFarAsItCanTurnThisStep = profile.steerRateDegreesPerSecond * deltaTime;

            return Mathf.MoveTowards(currentAngleDegrees, wanted, asFarAsItCanTurnThisStep);
        }

        public static float AsFarAsItMayTurnAt(float speedMetresPerSecond, VehicleProfile profile)
        {
            var top = profile.topSpeedMetresPerSecond;

            if (top <= 0f)
            {
                return profile.maxSteerAngleDegrees;
            }

            var atTheTop = Mathf.Min(profile.steerLockAtTopSpeedDegrees, profile.maxSteerAngleDegrees);
            var through = Mathf.Clamp01(Mathf.Abs(speedMetresPerSecond) / top);

            return Mathf.Lerp(profile.maxSteerAngleDegrees, atTheTop, through);
        }
    }
}
