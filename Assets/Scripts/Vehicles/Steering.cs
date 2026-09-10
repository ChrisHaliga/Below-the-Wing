using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// How the steered wheels get from where they are pointing to where the driver wants them.
    ///
    /// The wheels of a three-tonne tractor take time to come round, and a controller that snapped
    /// them to full lock in a single step would make the vehicle turn in ways no mass on wheels can.
    /// Every change of steering angle goes through here so that it stays rate limited.
    /// </summary>
    public static class Steering
    {
        /// <summary>
        /// The steer angle one step later, in degrees from centre. Moves toward the angle the
        /// driver is asking for, by no more than the profile's steering rate allows, and never
        /// past the profile's lock.
        /// </summary>
        public static float Step(float currentAngleDegrees, float steerInput, float deltaTime, VehicleProfile profile)
        {
            var asked = Mathf.Clamp(steerInput, -1f, 1f) * profile.maxSteerAngleDegrees;
            var asFarAsItCanTurnThisStep = profile.steerRateDegreesPerSecond * deltaTime;

            return Mathf.MoveTowards(currentAngleDegrees, asked, asFarAsItCanTurnThisStep);
        }
    }
}
