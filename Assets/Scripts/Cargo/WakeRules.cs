using UnityEngine;

namespace BelowTheWing.Cargo
{
    /// <summary>
    /// Whether something riding on a carrier has been shaken loose.
    ///
    /// Three ways to lose your cargo, and they are different questions rather than three spellings
    /// of one. A hard corner throws it sideways. A tip drops it off the low side. A collision
    /// knocks it clear. A cart can take a corner that would never tip it, and tip on a kerb at
    /// walking pace, and neither is an impact.
    ///
    /// Plain arithmetic on purpose, so that whether a bag should have come off a cart in a given
    /// situation can be answered without driving anything.
    /// </summary>
    public static class WakeRules
    {
        /// <summary>How far from upright a carrier is leaning, in degrees.</summary>
        public static float TiltDegrees(Quaternion facing)
            => Vector3.Angle(facing * Vector3.up, Vector3.up);

        /// <summary>
        /// Whether any of the three has been crossed.
        ///
        /// Any one of them, not all: a bag on a cart that is tipping has come off whether or not it
        /// was also cornering hard.
        /// </summary>
        public static bool ShakenLoose(
            float lateralAcceleration, float tiltDegrees, float impulse, in WakeThresholds thresholds)
            => lateralAcceleration >= thresholds.LateralAcceleration
               || tiltDegrees >= thresholds.TiltDegrees
               || impulse >= thresholds.Impulse;
    }
}
