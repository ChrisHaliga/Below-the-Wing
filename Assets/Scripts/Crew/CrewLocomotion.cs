using UnityEngine;

namespace BelowTheWing.Crew
{
    public static class CrewLocomotion
    {
        public static Vector3 DesiredVelocity(Vector2 moveInput, float cameraYawDegrees, bool sprinting, CrewProfile profile)
        {
            var asked = Vector2.ClampMagnitude(moveInput, 1f);
            if (asked.sqrMagnitude < 1e-6f)
            {
                return Vector3.zero;
            }

            var speed = sprinting ? profile.sprintSpeedMetresPerSecond : profile.walkSpeedMetresPerSecond;
            var awayFromTheCamera = Quaternion.Euler(0f, cameraYawDegrees, 0f);

            return awayFromTheCamera * new Vector3(asked.x, 0f, asked.y) * speed;
        }
    }
}
