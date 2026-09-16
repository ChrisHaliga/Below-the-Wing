using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class ArcadeHandling
    {
        public static float YawDegreesPerSecond(
            float steerInput, float forwardSpeedMetresPerSecond, VehicleProfile profile)
        {
            var asked = Mathf.Clamp(steerInput, -1f, 1f);
            var radius = Mathf.Max(profile.tightestTurnRadiusMetres, 0.01f);

            var toHoldThatRadius = Mathf.Abs(forwardSpeedMetresPerSecond) / radius * Mathf.Rad2Deg;
            var allowed = Mathf.Min(toHoldThatRadius, profile.fastestTurnDegreesPerSecond);

            return asked * Mathf.Sign(forwardSpeedMetresPerSecond) * allowed;
        }

        public static Vector3 HeldToItsHeading(
            Vector3 velocity, Vector3 heading, float holdsPerSecond, float deltaTime)
        {
            var along = Vector3.Project(velocity, heading);
            var sideways = velocity - along;

            return along + (sideways * Mathf.Exp(-Mathf.Max(holdsPerSecond, 0f) * deltaTime));
        }
    }
}
