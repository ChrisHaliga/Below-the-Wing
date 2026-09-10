using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// Turning what a player pressed into where their character is trying to go.
    ///
    /// Movement is relative to the camera rather than the world: pressing forward means "away from
    /// me", so swinging the camera round changes where forward is. Keeping that translation here,
    /// apart from the component that reads the keyboard and pushes the rigidbody, means it can be
    /// checked one direction at a time.
    /// </summary>
    public static class CrewLocomotion
    {
        /// <summary>
        /// The velocity a character is asking for, in world space, given what is being pressed and
        /// which way the camera is facing.
        ///
        /// The result is flat: characters walk across the apron, they do not walk up into the air,
        /// so the camera's pitch has no bearing on it.
        /// </summary>
        public static Vector3 DesiredVelocity(Vector2 moveInput, float cameraYawDegrees, bool sprinting, CrewProfile profile)
        {
            // Clamping rather than normalising, so that a stick pushed halfway asks for half speed
            // while two keys held at once still ask for one speed rather than the square root of two.
            var asked = Vector2.ClampMagnitude(moveInput, 1f);
            if (asked.sqrMagnitude < 1e-6f)
            {
                return Vector3.zero;
            }

            var speed = sprinting ? profile.sprintSpeedMetresPerSecond : profile.walkSpeedMetresPerSecond;
            var awayFromTheCamera = Quaternion.Euler(0f, cameraYawDegrees, 0f);

            return awayFromTheCamera * new Vector3(asked.x, 0f, asked.y) * speed;
        }

        /// <summary>
        /// Which way the character should be facing one step later: turning toward the direction it
        /// is travelling, no faster than the profile allows, and staying put when it is not moving.
        /// </summary>
        public static Quaternion FaceTravel(Quaternion current, Vector3 travelDirection, float deltaTime, CrewProfile profile)
        {
            var acrossTheGround = new Vector3(travelDirection.x, 0f, travelDirection.z);
            if (acrossTheGround.sqrMagnitude < 1e-6f)
            {
                return current;
            }

            var facingTravel = Quaternion.LookRotation(acrossTheGround.normalized, Vector3.up);
            return Quaternion.RotateTowards(current, facingTravel, profile.turnRateDegreesPerSecond * deltaTime);
        }
    }
}
