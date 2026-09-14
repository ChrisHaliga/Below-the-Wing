using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [CreateAssetMenu(menuName = "Below the Wing/Vehicle Profile", fileName = "VehicleProfile")]
    public sealed class VehicleProfile : ScriptableObject
    {
        [Header("What this represents")]
        [Tooltip("What this profile stands for")]
        [TextArea(2, 4)]
        public string equipmentNote = "";

        [Header("Mass (real-world)")]
        [Tooltip("Mass, kg")]
        public float massKg = 1000f;

        [Tooltip("Centre of mass above the origin, m, local")]
        public Vector3 centerOfMassOffset = new Vector3(0f, 0.4f, 0f);

        [Header("Suspension")]
        [Tooltip("Suspension travel, m")]
        public float suspensionRestLengthMetres = 0.35f;

        [Tooltip("Newtons at full compression")]
        public float springStrengthNewtons = 40000f;

        [Tooltip("Suspension damping, N/(m/s)")]
        public float damperNewtonsPerMetrePerSecond = 4000f;

        [Header("Tire")]
        [Tooltip("Sideways grip, N/kg against sideways slip, m/s")]
        public AnimationCurve lateralGripCurve = AnimationCurve.Linear(0f, 0f, 10f, 10f);

        [Tooltip("Rolling resistance, fraction of the weight carried")]
        public float rollingResistanceCoefficient = 0.02f;

        [Tooltip("Driveline drag, fraction of speed shed per second")]
        public float coastingDragPerSecond = 0.7f;

        [Header("Drive")]
        [Tooltip("Drive force at full throttle, N")]
        public float maxDriveForceNewtons = 12000f;

        [Tooltip("Sprint multiplier on drive force and top speed")]
        public float sprintDriveMultiplier = 1.5f;

        [Tooltip("Top speed, m/s. Zero for no drive")]
        public float topSpeedMetresPerSecond = 20f;

        [Tooltip("Braking force, N")]
        public float maxBrakeForceNewtons = 20000f;

        [Tooltip("Steering lock, degrees from centre")]
        public float maxSteerAngleDegrees = 45f;

        [Tooltip("Steering rate, degrees/s")]
        public float steerRateDegreesPerSecond = 120f;

        [Header("Bodywork")]
        [Tooltip("Bounce of the bodywork, 0 to 1")]
        public float bounciness = 0.4f;

        [Header("Control")]
        [Tooltip("Whether a player may drive it")]
        public bool driveable = true;
    }
}
