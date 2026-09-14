using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class Steering
    {
        public static float Step(float currentAngleDegrees, float steerInput, float deltaTime, VehicleProfile profile)
        {
            var asked = Mathf.Clamp(steerInput, -1f, 1f) * profile.maxSteerAngleDegrees;
            var asFarAsItCanTurnThisStep = profile.steerRateDegreesPerSecond * deltaTime;

            return Mathf.MoveTowards(currentAngleDegrees, asked, asFarAsItCanTurnThisStep);
        }
    }
}
