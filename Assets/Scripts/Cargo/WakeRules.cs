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
    /// Plain arithmetic on purpose. Whether a bag should have come off a cart at a given speed is
    /// exactly the sort of thing that is argued about during tuning, and it is worth being able to
    /// answer without driving anything.
    /// </summary>
    public static class WakeRules
    {
        /// <summary>
        /// Sideways acceleration at a point on a carrier turning at a given radius and speed, in
        /// metres per second squared.
        ///
        /// The everyday version of the rule: going twice as fast round the same corner throws
        /// things four times as hard, and it is why speed rather than steering is what loses a load.
        /// </summary>
        public static float LateralAcceleration(float speedMetresPerSecond, float turnRadiusMetres)
            => turnRadiusMetres <= 1e-4f
                ? 0f
                : speedMetresPerSecond * speedMetresPerSecond / turnRadiusMetres;

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

        /// <summary>
        /// How fast a carrier may go round a corner of this radius before it starts losing things,
        /// in metres per second.
        ///
        /// The inverse of the acceleration rule, and the number worth having: it turns a threshold
        /// nobody can picture into a speed that can be driven at and checked against.
        /// </summary>
        public static float FastestSafeSpeed(float turnRadiusMetres, in WakeThresholds thresholds)
            => Mathf.Sqrt(Mathf.Max(0f, thresholds.LateralAcceleration * Mathf.Max(0f, turnRadiusMetres)));
    }
}
