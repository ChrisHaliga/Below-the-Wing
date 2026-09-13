using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Everything that makes one vehicle drive differently from another.
    ///
    /// Every vehicle in the game runs the same <see cref="VehicleController"/>; the only thing
    /// that distinguishes a baggage tractor from a baggage cart is which profile it was given.
    /// Adding a new kind of vehicle should therefore mean authoring a new asset, not writing
    /// a new class.
    ///
    /// Masses and dimensions are real-world figures for the equipment being modelled, so that
    /// suspension and drive values can be reasoned about as physical quantities rather than
    /// tuned as arbitrary numbers.
    ///
    /// Where a vehicle's parts are is not in here. That is <see cref="VehicleShape"/>, which for a
    /// modelled vehicle reads them off the model itself. A wheelbase written down in two places is
    /// how the invisible suspension probes ended up fourteen centimetres from the visible wheels.
    /// </summary>
    [CreateAssetMenu(menuName = "Below the Wing/Vehicle Profile", fileName = "VehicleProfile")]
    public sealed class VehicleProfile : ScriptableObject
    {
        [Header("What this represents")]
        [Tooltip("The real equipment this profile stands for, and where its mass comes from.")]
        [TextArea(2, 4)]
        public string equipmentNote = "";

        [Header("Mass and scale (real-world)")]
        [Tooltip("Kerb mass in kilograms, unloaded.")]
        public float massKg = 1000f;

        [Tooltip("Overall size of the body in metres: width (x), height (y), length (z). Only " +
                 "the size of the stand-in shape drawn for a vehicle that has no model yet. What a " +
                 "vehicle collides as comes from its VehicleShape.")]
        public Vector3 bodySizeMetres = new Vector3(1.5f, 1.5f, 3f);

        [Tooltip("Centre of mass in the vehicle's own space, in metres. The origin is on the " +
                 "ground between the wheels, so this is how high the weight sits above the tarmac " +
                 "and is always positive. Low is stable; at or below zero puts the mass under the " +
                 "contact patches and weight transfer inverts.")]
        public Vector3 centerOfMassOffset = new Vector3(0f, 0.4f, 0f);

        [Header("Wheels")]
        [Tooltip("Wheel radius in metres. The suspension ray reaches this far past its rest length.")]
        public float wheelRadiusMetres = 0.3f;

        [Header("Suspension")]
        [Tooltip("Travel of the suspension in metres, from fully extended to fully compressed.")]
        public float suspensionRestLengthMetres = 0.35f;

        [Tooltip("Newtons applied at full compression. Roughly (massKg * 9.81) / wheels, divided by " +
                 "the compression you want it to settle at.")]
        public float springStrengthNewtons = 40000f;

        [Tooltip("Newtons per metre-per-second of suspension travel, opposing that travel.")]
        public float damperNewtonsPerMetrePerSecond = 4000f;

        [Header("Tire")]
        [Tooltip("Sideways force one wheel can generate, in newtons per kilogram it carries, " +
                 "as a function of how fast the contact patch is sliding sideways in metres per second. " +
                 "A curve that rises to a peak and then falls away is what lets a vehicle break traction.")]
        public AnimationCurve lateralGripCurve = AnimationCurve.Linear(0f, 0f, 10f, 10f);

        [Tooltip("Rolling resistance: how much force a tire costs simply to roll, as a fraction of " +
                 "the weight it carries. Pneumatic tires on concrete are around 0.01 to 0.02. This is " +
                 "what finally brings a coasting vehicle to a complete stop.")]
        public float rollingResistanceCoefficient = 0.02f;

        [Tooltip("Driveline and bearing drag, as the fraction of its speed a vehicle sheds each " +
                 "second when nobody is on the throttle. 0.7 means a tractor released at walking " +
                 "pace has almost stopped a couple of seconds later.")]
        public float coastingDragPerSecond = 0.7f;

        [Header("Drive")]
        [Tooltip("Newtons of forward force at full throttle, shared across the driven wheels.")]
        public float maxDriveForceNewtons = 12000f;

        [Tooltip("How much more force the driven wheels get while sprinting. 1 means sprinting does " +
                 "nothing; 1.5 is half as much again.")]
        public float sprintDriveMultiplier = 1.5f;

        [Tooltip("Newtons of braking force available, shared across the wheels.")]
        public float maxBrakeForceNewtons = 20000f;

        [Tooltip("How far the steered wheels can turn from centre, in degrees.")]
        public float maxSteerAngleDegrees = 45f;

        [Tooltip("How fast the steer angle can change, in degrees per second. This is what stops " +
                 "steering snapping instantly from lock to lock.")]
        public float steerRateDegreesPerSecond = 120f;

        /// <summary>
        /// Whether a player can take control of a vehicle carrying this profile. Baggage carts are
        /// towed rather than driven, so their profile says no and no prompt is offered for them.
        /// </summary>
        [Header("Control")]
        [Tooltip("Whether a player standing next to this vehicle is offered the chance to drive it.")]
        public bool driveable = true;
    }
}
