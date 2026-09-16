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

        [Tooltip("Drive force multiplier off the line, 1 for none")]
        public float launchDriveMultiplier = 3f;

        [Tooltip("Sprint multiplier on drive force and top speed")]
        public float sprintDriveMultiplier = 1.5f;

        [Tooltip("Top speed, m/s. Zero for no drive")]
        public float topSpeedMetresPerSecond = 20f;

        [Tooltip("Braking force, N")]
        public float maxBrakeForceNewtons = 20000f;

        [Tooltip("Fraction of top speed the launch shove fades across")]
        public float launchFadesByFractionOfTopSpeed = 0.35f;

        [Tooltip("Steering lock, degrees from centre")]
        public float maxSteerAngleDegrees = 60f;

        [Tooltip("Steering lock still allowed at top speed, degrees")]
        public float steerLockAtTopSpeedDegrees = 30f;

        [Tooltip("Handles as an arcade vehicle rather than through its tyres")]
        public bool arcadeHandling;

        [Tooltip("Fastest it may come round, degrees/s")]
        public float fastestTurnDegreesPerSecond = 240f;

        [Tooltip("How hard it picks itself back up when it leans, per second squared. Zero lets it roll over")]
        public float staysUprightPerSecond = 6f;

        [Tooltip("Most sideways grip before it slides, m/s^2")]
        public float mostSideGripMetresPerSecondSquared = 20f;

        [Tooltip("How fast grip pulls a slide back onto the heading, per second")]
        public float gripHoldsHeadingPerSecond = 3f;

        [Tooltip("Steering rate, degrees/s")]
        public float steerRateDegreesPerSecond = 120f;

        [Header("Bodywork")]
        [Tooltip("Bounce of the bodywork, 0 to 1")]
        public float bounciness = 0.4f;

        [Header("Control")]
        [Tooltip("Whether a player may drive it")]
        public bool driveable = true;

        const int SamplesAcrossTheCurve = 256;

        AnimationCurve m_Measured;
        float m_MostLateralGrip;

        float m_SlipItGripsHardestAt;

        public float MostLateralGripPerKilogram
        {
            get
            {
                Measure();
                return m_MostLateralGrip;
            }
        }

        public float SlipItGripsHardestAt
        {
            get
            {
                Measure();
                return m_SlipItGripsHardestAt;
            }
        }

        void Measure()
        {
            if (ReferenceEquals(m_Measured, lateralGripCurve))
            {
                return;
            }

            m_Measured = lateralGripCurve;
            m_MostLateralGrip = 0f;
            m_SlipItGripsHardestAt = 0f;

            var curve = lateralGripCurve;
            if (curve == null || curve.length == 0)
            {
                return;
            }

            var from = curve[0].time;
            var to = curve[curve.length - 1].time;

            for (var i = 0; i <= SamplesAcrossTheCurve; i++)
            {
                var slip = Mathf.Lerp(from, to, i / (float)SamplesAcrossTheCurve);
                var grip = curve.Evaluate(slip);

                if (grip <= m_MostLateralGrip)
                {
                    continue;
                }

                m_MostLateralGrip = grip;
                m_SlipItGripsHardestAt = slip;
            }
        }

        void OnValidate() => m_Measured = null;
    }
}
