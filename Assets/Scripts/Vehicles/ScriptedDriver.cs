using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Driving a vehicle from numbers rather than from a keyboard.
    ///
    /// The same thing a player's input is: something a vehicle asks what it should be doing. The
    /// vehicle cannot tell the difference and gains no branch for it, which is the point -- anything
    /// that can be driven by hand can be driven by this, and anything this can do a player could
    /// have done.
    ///
    /// It holds a <b>turn radius</b> rather than a steering angle. That sounds like a convenience
    /// and is not: a fixed radius is what makes sideways acceleration a number you can compute
    /// rather than one you can only measure afterwards, so a vehicle driving a circle becomes an
    /// instrument. It is also what any scripted or automatic vehicle would want later, because a
    /// path is a sequence of radii and a steering angle is not.
    ///
    /// It knows nothing about what it is being used for. No circles, no tests, no speeds with names.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    [DisallowMultipleComponent]
    public sealed class ScriptedDriver : MonoBehaviour, IDriveIntentSource
    {
        [SerializeField, Tooltip("How fast to try to go, in metres per second. Negative reverses.")]
        float m_TargetSpeed;

        [SerializeField, Tooltip("Radius of the turn to hold, in metres. Zero drives straight.")]
        float m_TurnRadiusMetres;

        [SerializeField, Tooltip("How quickly the target speed may change, in metres per second per " +
                                 "second. Stepping it instantly is an acceleration spike that throws " +
                                 "cargo off for a reason nobody caused.")]
        float m_RampMetresPerSecondSquared = 2f;

        VehicleController m_Vehicle;
        float m_Approaching;

        void Awake() => m_Vehicle = GetComponent<VehicleController>();

        /// <summary>How fast this is currently trying to go, after ramping.</summary>
        public float Approaching => m_Approaching;

        /// <summary>The speed being aimed for. Changing it ramps rather than jumps.</summary>
        public float TargetSpeed
        {
            get => m_TargetSpeed;
            set => m_TargetSpeed = value;
        }

        /// <summary>The radius of turn being held, in metres. Zero is a straight line.</summary>
        public float TurnRadiusMetres
        {
            get => m_TurnRadiusMetres;
            set => m_TurnRadiusMetres = value;
        }

        /// <summary>How quickly the target speed may change.</summary>
        public float RampMetresPerSecondSquared
        {
            get => m_RampMetresPerSecondSquared;
            set => m_RampMetresPerSecondSquared = value;
        }

        /// <summary>
        /// Sideways acceleration this vehicle is currently pulling, in metres per second squared.
        ///
        /// Computed from what it is actually doing rather than from what it was asked for, so it
        /// stays honest while the speed is still ramping up to the target.
        /// </summary>
        public float LateralAcceleration
            => m_TurnRadiusMetres <= 1e-4f || m_Vehicle == null || m_Vehicle.Body == null
                ? 0f
                : Speed() * Speed() / m_TurnRadiusMetres;

        /// <summary>What the vehicle should be doing this step.</summary>
        public DriveIntent Current { get; private set; } = DriveIntent.Idle;

        float Speed()
        {
            var body = m_Vehicle.Body;
            return body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward);
        }

        void FixedUpdate()
        {
            if (m_Vehicle == null || m_Vehicle.Profile == null)
            {
                return;
            }

            m_Approaching = Mathf.MoveTowards(
                m_Approaching, m_TargetSpeed, m_RampMetresPerSecondSquared * Time.fixedDeltaTime);

            Current = new DriveIntent(
                steer: SteerToHoldRadius(),
                throttle: Throttle(),
                brake: 0f);
        }

        /// <summary>
        /// Throttle enough to reach the speed being approached, and nothing once there.
        ///
        /// Deliberately blunt. Holding a speed precisely is a control problem worth solving when
        /// something depends on it; what is wanted here is a vehicle that gets to roughly the right
        /// speed and stays near it without the throttle chattering.
        /// </summary>
        float Throttle()
        {
            var wanted = m_Approaching - Speed();
            return Mathf.Clamp(wanted, -1f, 1f);
        }

        /// <summary>
        /// The steering angle that holds the radius being asked for, as a fraction of full lock.
        ///
        /// A vehicle steers around a circle whose size depends on its wheelbase and its steering
        /// angle, so holding a radius means changing the angle as the vehicle's own geometry
        /// dictates rather than picking an angle and hoping. Sign follows the radius, so a negative
        /// radius turns the other way.
        /// </summary>
        float SteerToHoldRadius()
        {
            if (Mathf.Abs(m_TurnRadiusMetres) <= 1e-4f)
            {
                return 0f;
            }

            var profile = m_Vehicle.Profile;
            var wanted = Mathf.Atan(profile.wheelbaseMetres / Mathf.Abs(m_TurnRadiusMetres)) * Mathf.Rad2Deg;
            var asFraction = wanted / Mathf.Max(profile.maxSteerAngleDegrees, 1e-3f);

            return Mathf.Clamp(asFraction, 0f, 1f) * Mathf.Sign(m_TurnRadiusMetres);
        }
    }
}
