using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class VehicleController : MonoBehaviour, IMovedFromHere
    {
        readonly struct Wheel
        {
            public readonly Vector3 MountLocal;
            public readonly float RadiusMetres;
            public readonly bool Steers;
            public readonly float DriveShare;
            public readonly float BrakeShare;

            public Wheel(Vector3 mountLocal, float radiusMetres, bool steers, float driveShare, float brakeShare)
            {
                MountLocal = mountLocal;
                RadiusMetres = radiusMetres;
                Steers = steers;
                DriveShare = driveShare;
                BrakeShare = brakeShare;
            }
        }

        [SerializeField, Tooltip("Profile this runs on")]
        VehicleProfile m_Profile;

        [SerializeField, Tooltip("Layers the suspension may find ground on")]
        LayerMask m_GroundMask = ~0;

        [SerializeField, Tooltip("Name shown above it")]
        string m_DisplayName = "";

        Rigidbody m_Body;
        VehicleShape m_Shape;
        Wheel[] m_Wheels;

        float[] m_HangingBy;
        float m_SteerAngleDegrees;
        bool m_Occupied;

        public VehicleProfile Profile => m_Profile;

        public VehicleShape Shape
        {
            get
            {
                if (m_Shape == null)
                {
                    m_Shape = GetComponent<VehicleShape>();
                }

                return m_Shape;
            }
        }

        public Rigidbody Body
        {
            get
            {
                if (m_Body == null)
                {
                    m_Body = GetComponent<Rigidbody>();
                }

                return m_Body;
            }
        }

        public IDriveIntentSource IntentSource { get; set; }

        public float SteerAngleDegrees => m_SteerAngleDegrees;

        public bool WheelSteers(int corner)
            => m_Wheels != null && corner >= 0 && corner < m_Wheels.Length && m_Wheels[corner].Steers;

        public float WheelRadiusMetres(int corner)
            => m_Wheels != null && corner >= 0 && corner < m_Wheels.Length ? m_Wheels[corner].RadiusMetres : 0f;

        public float WheelCentreLocal(int corner, float drawnAt)
        {
            if (m_Wheels == null || m_HangingBy == null || corner < 0 || corner >= m_Wheels.Length)
            {
                return drawnAt;
            }

            return m_Wheels[corner].MountLocal.y - m_HangingBy[corner];
        }

        public CartChain Chain { get; internal set; }

        public bool OursToMove { get; set; } = true;

        public string DisplayName => m_DisplayName;

        public void Rename(string displayName) => m_DisplayName = displayName;

        public bool Occupied
        {
            get => m_Occupied;
            set
            {
                if (m_Occupied == value)
                {
                    return;
                }

                m_Occupied = value;
                OccupiedChanged?.Invoke(value);
            }
        }

        public event System.Action<bool> OccupiedChanged;

        public bool AcceptsDriver => m_Profile != null && m_Profile.driveable && !m_Occupied;

        public Vector3? FrontHitchLocal => Shape?.FrontCouplingLocal;

        public Vector3? RearHitchLocal => Shape?.RearCouplingLocal;

        public bool CanBeTowed => FrontHitchLocal.HasValue;

        public float RearReach => Shape != null ? Shape.RearReachMetres : 0f;

        public static float SuspensionCompressionAtRest(VehicleProfile profile, int wheelCount)
        {
            var weightOnEachWheel = profile.massKg * Physics.gravity.magnitude / Mathf.Max(wheelCount, 1);

            return Mathf.Clamp01(weightOnEachWheel / profile.springStrengthNewtons);
        }

        public static float SuspensionMountHeightMetres(VehicleProfile profile, float wheelRadiusMetres, int wheelCount)
            => wheelRadiusMetres
               + (profile.suspensionRestLengthMetres * (1f - SuspensionCompressionAtRest(profile, wheelCount)));

        public void Configure(VehicleProfile profile, string displayName)
        {
            m_Profile = profile;
            m_DisplayName = displayName;

            m_Body = GetComponent<Rigidbody>();
            if (m_Body == null)
            {
                m_Body = gameObject.AddComponent<Rigidbody>();
            }

            m_Body.mass = profile.massKg;

            m_Body.sleepThreshold = 0f;
            if (profile.centerOfMassOffset.y <= 0f)
            {
                throw MisbuiltException.Refuse(
                    this,
                    "carries its centre of mass at or below its own origin, which is on the ground; " +
                    "weight transfer then works backwards and nothing can tip it over");
            }

            m_Body.centerOfMass = profile.centerOfMassOffset;

            m_Body.interpolation = RigidbodyInterpolation.Interpolate;

            m_Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            DiscardBodywork();
            m_Bodywork = VehicleBody.Build(gameObject, Shape, profile.bounciness);

            SlidingDoors.Build(gameObject, Shape, m_Bodywork);

            if (!profile.driveable)
            {
                var brake = GetComponent<CartBrake>() ?? gameObject.AddComponent<CartBrake>();
                brake.SwingsThis(Drawbar.Build(gameObject, Shape, m_Bodywork));
            }

            BuildWheels(profile);

            if (GetComponent<ContactTally>() == null)
            {
                gameObject.AddComponent<ContactTally>();
            }

            Chain ??= CartChain.Couple(new[] { this }, ChainJointSettings.Default);
        }

        void BuildWheels(VehicleProfile profile)
        {
            var wheels = Shape != null ? Shape.Wheels : null;
            if (wheels == null || wheels.Count == 0)
            {
                throw MisbuiltException.Refuse(
                    this, "has no shape, so there is nowhere to hang its suspension from");
            }

            var roles = WheelShares.Of(wheels);

            m_Wheels = new Wheel[wheels.Count];
            for (var i = 0; i < wheels.Count; i++)
            {
                var wheel = wheels[i];

                m_Wheels[i] = new Wheel(
                    new Vector3(
                        wheel.CentreLocal.x,
                        SuspensionMountHeightMetres(profile, wheel.RadiusMetres, wheels.Count),
                        wheel.CentreLocal.z),
                    wheel.RadiusMetres,
                    roles[i].Steers,
                    roles[i].DriveShare,
                    roles[i].BrakeShare);
            }

            m_HangingBy = new float[m_Wheels.Length];
            for (var i = 0; i < m_HangingBy.Length; i++)
            {
                m_HangingBy[i] = profile.suspensionRestLengthMetres;
            }
        }

        void Awake()
        {
            if (m_Profile != null && m_Wheels == null)
            {
                Configure(m_Profile, string.IsNullOrEmpty(m_DisplayName) ? name : m_DisplayName);
            }
        }

        void Start()
        {
            if (m_Profile == null)
            {
                throw MisbuiltException.Refuse(
                    this, "has no vehicle profile, so it has no mass, no wheels and no size");
            }
        }

        PhysicsMaterial m_Bodywork;

        void OnDestroy() => DiscardBodywork();

        void DiscardBodywork()
        {
            if (m_Bodywork == null)
            {
                return;
            }

            Discard.Now(m_Bodywork);
            m_Bodywork = null;
        }

        void FixedUpdate()
        {
            if (m_Profile == null || m_Wheels == null)
            {
                return;
            }

            var intent = OursToMove ? IntentSource?.Current ?? DriveIntent.Idle : DriveIntent.Idle;

            if (OursToMove)
            {
                m_SteerAngleDegrees = Steering.Step(
                    m_SteerAngleDegrees, intent.Steer,
                    Vector3.Dot(Body.linearVelocity, transform.forward), Time.fixedDeltaTime,
                    m_Profile);
            }

            if (!m_Profile.driveable)
            {
                m_SteerAngleDegrees = WhereItsDrawbarPoints();
            }

            var steerRotation = Quaternion.AngleAxis(m_SteerAngleDegrees, transform.up);

            for (var i = 0; i < m_Wheels.Length; i++)
            {
                StandOnAndPushWith(i, steerRotation, intent);
            }

            if (m_Profile.arcadeHandling && OursToMove)
            {
                TurnItLikeAnArcadeVehicle(intent);
            }
        }

        float WhereItsDrawbarPoints()
        {
            var hitch = GetComponent<Joint>();

            if (hitch == null || hitch.connectedBody == null || FrontHitchLocal == null)
            {
                return 0f;
            }

            var pullingFrom = hitch.connectedBody.worldCenterOfMass;
            var ourEnd = transform.TransformPoint(FrontHitchLocal.Value);

            return FrontAxle.PointsAlongDegrees(
                transform.InverseTransformDirection(pullingFrom - ourEnd), m_Profile.maxSteerAngleDegrees);
        }

        void StandOnAndPushWith(int corner, Quaternion steerRotation, DriveIntent intent)
        {
            var wheel = m_Wheels[corner];

            var up = transform.up;
            var forward = wheel.Steers ? steerRotation * transform.forward : transform.forward;
            var right = wheel.Steers ? steerRotation * transform.right : transform.right;

            var mount = transform.TransformPoint(wheel.MountLocal);
            var probe = WhatIsUnder(mount, up, wheel.RadiusMetres);

            m_HangingBy[corner] = probe.HitGround
                ? probe.DistanceToGround - wheel.RadiusMetres
                : m_Profile.suspensionRestLengthMetres;

            var atTheContactPatch = Body.GetPointVelocity(mount);

            var force = WheelPhysics.Evaluate(
                probe,
                new ContactVelocity(
                    Vector3.Dot(atTheContactPatch, up),
                    Vector3.Dot(atTheContactPatch, right),
                    Vector3.Dot(atTheContactPatch, forward)),
                new WheelLoad(m_Profile.massKg / m_Wheels.Length, wheel.DriveShare, wheel.BrakeShare),
                intent,
                m_Profile.massKg,
                wheel.RadiusMetres,
                Time.fixedDeltaTime,
                m_Profile);

            if (!force.Grounded)
            {
                return;
            }

            var sideways = m_Profile.arcadeHandling ? 0f : force.Lateral;

            Body.AddForceAtPosition(
                (up * force.AlongSuspension) + (right * sideways) + (forward * force.Forward),
                mount);
        }

        void TurnItLikeAnArcadeVehicle(DriveIntent intent)
        {
            var forwardSpeed = Vector3.Dot(Body.linearVelocity, transform.forward);

            var wound = m_Profile.maxSteerAngleDegrees > 0f
                ? m_SteerAngleDegrees / m_Profile.maxSteerAngleDegrees
                : 0f;

            var wanted = ArcadeHandling.YawDegreesPerSecond(
                wound, forwardSpeed, Shape != null ? Shape.WheelbaseMetres : 0f, m_Profile)
                * Mathf.Deg2Rad;

            var itsOwnUp = transform.up;
            var coming = Vector3.Dot(Body.angularVelocity, itsOwnUp);

            Body.AddTorque(
                itsOwnUp * ((wanted - coming) * m_Profile.turnsIntoItPerSecond),
                ForceMode.Acceleration);

            HoldItToItsHeading();
        }

        void HoldItToItsHeading()
        {
            var flat = new Vector3(Body.linearVelocity.x, 0f, Body.linearVelocity.z);
            var heading = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

            if (flat.magnitude < 0.1f || heading.sqrMagnitude < 0.5f)
            {
                return;
            }

            var held = ArcadeHandling.HeldToItsHeading(
                flat, heading, m_Profile.gripHoldsHeadingPerSecond,
                m_Profile.mostSideGripMetresPerSecondSquared, Time.fixedDeltaTime);

            Body.AddForce((held - flat) / Time.fixedDeltaTime, ForceMode.Acceleration);
        }


        GroundProbe WhatIsUnder(Vector3 mount, Vector3 up, float wheelRadiusMetres)
            => Physics.Raycast(
                mount, -up, out var hit, wheelRadiusMetres + m_Profile.suspensionRestLengthMetres,
                m_GroundMask, QueryTriggerInteraction.Ignore)
                ? new GroundProbe(true, hit.distance)
                : GroundProbe.Airborne;
    }
}
