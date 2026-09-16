using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class ArcadeHandling
    {
        public static float YawDegreesPerSecond(
            float steerInput, float forwardSpeedMetresPerSecond, float wheelbaseMetres,
            VehicleProfile profile)
        {
            var asked = Mathf.Clamp(steerInput, -1f, 1f);
            var wheel = Mathf.Abs(asked) * profile.maxSteerAngleDegrees;

            if (Mathf.Approximately(wheel, 0f) || wheelbaseMetres <= 0f)
            {
                return 0f;
            }

            var radius = wheelbaseMetres / Mathf.Tan(wheel * Mathf.Deg2Rad);
            var toHoldThatRadius = Mathf.Abs(forwardSpeedMetresPerSecond) / radius * Mathf.Rad2Deg;

            return Mathf.Sign(asked) * Mathf.Sign(forwardSpeedMetresPerSecond)
                   * Mathf.Min(toHoldThatRadius, profile.fastestTurnDegreesPerSecond);
        }

        public static Vector3 HeldToItsHeading(
            Vector3 velocity, Vector3 heading, float holdsPerSecond,
            float mostSideGripMetresPerSecondSquared, float deltaTime)
        {
            var speed = velocity.magnitude;

            if (speed < 1e-3f || heading.sqrMagnitude < 1e-6f)
            {
                return velocity;
            }

            var off = Vector3.Angle(velocity, heading) * Mathf.Deg2Rad;
            var wanted = off * (1f - Mathf.Exp(-Mathf.Max(holdsPerSecond, 0f) * deltaTime));
            var afforded = Mathf.Max(mostSideGripMetresPerSecondSquared, 0f) * deltaTime / speed;

            return Vector3.RotateTowards(
                velocity, heading.normalized * speed, Mathf.Min(wanted, afforded), 0f);
        }
    }
}
