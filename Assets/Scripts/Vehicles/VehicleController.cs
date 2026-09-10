using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Every vehicle in the game.
    ///
    /// A baggage tractor and a baggage cart are the same component with different
    /// <see cref="VehicleProfile"/> assets; nothing here knows which one it is running as. Each
    /// fixed step it probes the ground under all four wheels, asks <see cref="WheelPhysics"/> what
    /// force each one produces, and applies the results to the rigidbody. Wheels are raycasts and
    /// springs rather than Unity's WheelCollider, so that one model covers everything on the apron
    /// and every vehicle behaves like a sibling of the others.
    ///
    /// The vehicle never reads input. It asks <see cref="IntentSource"/> what is being requested,
    /// which is null on any vehicle nobody is driving -- including one being simulated on another
    /// player's machine.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class VehicleController : MonoBehaviour, IDriveable
    {
        /// <summary>
        /// How far up inside the bodywork the top of the suspension sits, in metres.
        ///
        /// Two reasons for it being inside rather than flush with the underside. It gives the
        /// suspension somewhere to hang from, and a ray that starts within a vehicle's own collider
        /// cannot find that collider, so a wheel can never mistake the vehicle it belongs to for
        /// the ground.
        /// </summary>
        const float SuspensionMountInsetMetres = 0.2f;

        /// <summary>
        /// How high above the apron every coupling sits, in metres.
        ///
        /// A fixed height above the ground, exactly like the standard tow height that lets any cart
        /// hitch to any tractor. Measuring it from the ground rather than from a vehicle's own
        /// bodywork is what makes two vehicles of different heights meet at precisely the same
        /// point once both are standing on their suspension.
        ///
        /// Getting this wrong is not a cosmetic matter. A coupling that holds two hitches even
        /// slightly apart vertically leaves both vehicles leaning, and a leaning vehicle has its
        /// suspension pushing partly sideways -- so a parked train wanders off across the apron
        /// under a force nobody applied.
        /// </summary>
        const float CouplingHeightAboveGroundMetres = 1.05f;

        /// <summary>One corner of the vehicle: where its suspension hangs and what it does.</summary>
        readonly struct Wheel
        {
            public readonly Vector3 MountLocal;
            public readonly bool Steers;
            public readonly float DriveShare;
            public readonly float BrakeShare;

            public Wheel(Vector3 mountLocal, bool steers, float driveShare, float brakeShare)
            {
                MountLocal = mountLocal;
                Steers = steers;
                DriveShare = driveShare;
                BrakeShare = brakeShare;
            }
        }

        [SerializeField, Tooltip("The tuning and real-world mass this vehicle runs on.")]
        VehicleProfile m_Profile;

        [SerializeField, Tooltip("Layers the suspension rays are allowed to find ground on.")]
        LayerMask m_GroundMask = ~0;

        [SerializeField, Tooltip("Name shown on this vehicle's label and in the prompt to drive it.")]
        string m_DisplayName = "";

        /// <summary>
        /// Below this speed, and this rate of turn, a vehicle counts as having stopped moving.
        /// </summary>
        const float StillnessMetresPerSecond = 0.05f;

        const float StillnessRadiansPerSecond = 0.05f;

        /// <summary>
        /// How long a vehicle must sit still, with nobody driving it, before it is put to sleep.
        /// </summary>
        const float SecondsOfStillnessBeforeSleeping = 1f;

        Rigidbody m_Body;
        BoxCollider m_Collider;
        Wheel[] m_Wheels;
        float m_SteerAngleDegrees;
        float m_SecondsStill;
        bool m_Occupied;

        /// <summary>The tuning and real-world mass this vehicle runs on.</summary>
        public VehicleProfile Profile => m_Profile;

        /// <summary>The rigidbody the wheel forces are applied to.</summary>
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

        /// <summary>
        /// Where this vehicle's steering, throttle and braking come from, or null when nobody is
        /// driving it. Setting it is how a player takes the wheel; clearing it is how they get out.
        /// </summary>
        public IDriveIntentSource IntentSource { get; set; }

        /// <summary>How far the steered wheels are currently turned from centre, in degrees.</summary>
        public float SteerAngleDegrees => m_SteerAngleDegrees;

        /// <summary>
        /// The train this vehicle belongs to. Every vehicle is in one; an uncoupled vehicle is in a
        /// train of itself alone. Having no empty case means that taking over a vehicle is the same
        /// operation whether or not it happens to be towing anything.
        /// </summary>
        public CartChain Chain { get; internal set; }

        /// <summary>
        /// Whether this machine is the one working out where this vehicle goes.
        ///
        /// False on a copy of a vehicle somebody else is simulating. Such a copy still collides and
        /// can still be shoved, but it does not run its own suspension, grip or drive: its position
        /// comes from its owner, and anything computed here would be overwritten the moment the
        /// next update arrived. Running it anyway costs four raycasts and four forces per vehicle
        /// per step for a result that is thrown away.
        ///
        /// Deliberately a plain flag rather than a question about networking, so that the vehicle
        /// still knows nothing about how -- or whether -- the game is networked.
        /// </summary>
        public bool Simulated
        {
            get => m_Simulated;
            set
            {
                if (m_Simulated == value)
                {
                    return;
                }

                m_Simulated = value;

                // Gravity goes with it. A copy that is not running its own suspension has nothing
                // holding it up, so left falling it sinks onto its bodywork between network updates
                // and is snapped back the moment the next one arrives -- which reads as every remote
                // vehicle juddering, twenty times a second.
                //
                // It stays a dynamic body either way. A vehicle somebody else owns still has to be
                // something you can walk into and be shoved by.
                Body.useGravity = value;

                if (!value)
                {
                    Body.linearVelocity = Vector3.zero;
                    Body.angularVelocity = Vector3.zero;
                }
            }
        }

        bool m_Simulated = true;

        public string DisplayName => m_DisplayName;

        /// <summary>
        /// Gives this vehicle the name a player sees when offered it. Separate from configuring it,
        /// because a vehicle is built from its prefab before anybody has said which tractor it is.
        /// </summary>
        public void Rename(string displayName)
        {
            m_DisplayName = displayName;
            name = displayName;
        }

        public Vector3 Position => transform.position;

        /// <summary>
        /// Whether somebody is in this vehicle's seat, on whichever machine they are playing from.
        ///
        /// Deliberately not "does this instance have an intent source", which is only ever true on
        /// the driver's own machine. Every other machine would look at an occupied tractor, see no
        /// local driver, and offer it to somebody else.
        /// </summary>
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

        /// <summary>Raised when somebody gets in or out, so the change can be told to other machines.</summary>
        public event System.Action<bool> OccupiedChanged;

        public bool AcceptsDriver => m_Profile != null && m_Profile.driveable && !m_Occupied;

        /// <summary>Where a vehicle in front of this one attaches, in this vehicle's local space.</summary>
        public Vector3 FrontHitchLocal => new Vector3(0f, CouplingHeightLocal, HitchReach);

        /// <summary>Where a vehicle behind this one attaches, in this vehicle's local space.</summary>
        public Vector3 RearHitchLocal => new Vector3(0f, CouplingHeightLocal, -HitchReach);

        /// <summary>How far the coupling reaches past this vehicle's origin, in metres.</summary>
        public float HitchReach => HitchReachMetres(m_Profile);

        /// <summary>
        /// How far a coupling reaches past a vehicle's origin, in metres.
        ///
        /// Two hitched vehicles stand exactly the sum of their two reaches apart, which is what puts
        /// their hitches on the same spot and leaves the coupling with nothing to pull against.
        /// Anything placing a train needs this to space it correctly.
        /// </summary>
        public static float HitchReachMetres(VehicleProfile profile)
            => (profile.bodySizeMetres.z * 0.5f) + profile.drawbarLengthMetres;

        float CouplingHeightLocal => CouplingHeightAboveGroundMetres - RestingHeightMetres(m_Profile);

        /// <summary>
        /// How high above the ground a vehicle's origin sits once it has settled on its suspension,
        /// in metres.
        ///
        /// Worked out from where the springs balance the weight they are holding. Anything placing
        /// a vehicle needs this, because dropping one in at an arbitrary height either buries it in
        /// the apron or leaves it to fall.
        /// </summary>
        public static float RestingHeightMetres(VehicleProfile profile)
        {
            const int corners = 4;

            // Read from physics rather than written down again. This figure decides where a
            // vehicle's coupling point ends up, and two vehicles whose coupling points do not meet
            // lean into the difference until a parked train wanders off across the apron.
            var weightOnEachCorner = profile.massKg * Physics.gravity.magnitude / corners;
            var compression = Mathf.Clamp01(weightOnEachCorner / profile.springStrengthNewtons);
            var mountAboveGround = profile.wheelRadiusMetres
                                   + (profile.suspensionRestLengthMetres * (1f - compression));

            return mountAboveGround + (profile.bodySizeMetres.y * 0.5f) - SuspensionMountInsetMetres;
        }

        /// <summary>
        /// Applies a profile to this vehicle: its mass, its centre of mass, the size of its
        /// collider, and the wheel positions its suspension probes from.
        ///
        /// Called when a vehicle is built rather than left to the inspector, so that the profile is
        /// the single description of what this vehicle physically is.
        /// </summary>
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
            m_Body.centerOfMass = profile.centerOfMassOffset;
            m_Body.interpolation = RigidbodyInterpolation.Interpolate;

            // Vehicles are fast, heavy, and the thing most often driven hard at something else --
            // an apron edge, a jetway leg, or another train. Sweeping against moving bodies as well
            // as static ones costs more than sweeping against static alone, and a tractor passing
            // clean through a cart is the failure nobody would accept.
            m_Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            m_Collider = GetComponent<BoxCollider>();
            if (m_Collider == null)
            {
                m_Collider = gameObject.AddComponent<BoxCollider>();
            }

            m_Collider.size = profile.bodySizeMetres;
            m_Collider.center = Vector3.zero;

            BuildWheels(profile);

            Chain ??= CartChain.Couple(new[] { this }, ChainJointSettings.Default);
        }

        void BuildWheels(VehicleProfile profile)
        {
            var halfTrack = profile.trackMetres * 0.5f;
            var halfWheelbase = profile.wheelbaseMetres * 0.5f;
            var mountHeight = (-profile.bodySizeMetres.y * 0.5f) + SuspensionMountInsetMetres;

            // Front wheels steer, rear wheels drive: a baggage tractor is a small rear-drive unit,
            // and pulling a loaded train from the front axle would spin the wheels up rather than
            // move it. Braking is shared evenly because every wheel has a brake on it.
            m_Wheels = new[]
            {
                new Wheel(new Vector3(-halfTrack, mountHeight, halfWheelbase), steers: true, driveShare: 0f, brakeShare: 0.25f),
                new Wheel(new Vector3(halfTrack, mountHeight, halfWheelbase), steers: true, driveShare: 0f, brakeShare: 0.25f),
                new Wheel(new Vector3(-halfTrack, mountHeight, -halfWheelbase), steers: false, driveShare: 0.5f, brakeShare: 0.25f),
                new Wheel(new Vector3(halfTrack, mountHeight, -halfWheelbase), steers: false, driveShare: 0.5f, brakeShare: 0.25f)
            };
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
            // Checked here rather than in Awake because a vehicle built in code is configured
            // immediately after the component is added, which is still before the first frame.
            if (m_Profile == null)
            {
                Debug.LogError(
                    $"'{name}' has no vehicle profile, so it has no mass, no wheels and no size. It " +
                    "will sit where it was put and do nothing. A vehicle without a profile is a piece " +
                    "of wiring that was missed, not a vehicle that happens to be idle.", this);
                enabled = false;
            }
        }

        void FixedUpdate()
        {
            if (m_Profile == null || m_Wheels == null || !Simulated)
            {
                return;
            }

            var intent = IntentSource?.Current ?? DriveIntent.Idle;
            var idle = NobodyIsAskingForAnything(intent);

            // A body that has gone to sleep costs nothing until something wakes it, and a parked
            // train that never sleeps burns solver time and bandwidth for the rest of the session.
            //
            // Getting there takes an explicit push. Applying any force wakes a rigidbody, so a
            // vehicle holding itself up on its own suspension can never drop off on its own: it is
            // woken every step by the very force keeping it standing. So a vehicle that has been
            // still long enough with nobody driving it is put to sleep deliberately, and then left
            // alone until something disturbs it.
            if (Body.IsSleeping() && idle)
            {
                return;
            }

            if (idle && IsBarelyMoving())
            {
                m_SecondsStill += Time.fixedDeltaTime;
                if (m_SecondsStill >= SecondsOfStillnessBeforeSleeping)
                {
                    Body.Sleep();
                    return;
                }
            }
            else
            {
                m_SecondsStill = 0f;
            }

            m_SteerAngleDegrees = Steering.Step(m_SteerAngleDegrees, intent.Steer, Time.fixedDeltaTime, m_Profile);

            var massPerWheel = m_Profile.massKg / m_Wheels.Length;
            var rayLength = m_Profile.wheelRadiusMetres + m_Profile.suspensionRestLengthMetres;
            var up = transform.up;
            var steerRotation = Quaternion.AngleAxis(m_SteerAngleDegrees, up);

            foreach (var wheel in m_Wheels)
            {
                var mount = transform.TransformPoint(wheel.MountLocal);
                var forward = wheel.Steers ? steerRotation * transform.forward : transform.forward;
                var right = wheel.Steers ? steerRotation * transform.right : transform.right;

                var probe = Physics.Raycast(mount, -up, out var hit, rayLength, m_GroundMask, QueryTriggerInteraction.Ignore)
                    ? new GroundProbe(true, hit.distance)
                    : GroundProbe.Airborne;

                var atTheContactPatch = Body.GetPointVelocity(mount);
                var velocity = new ContactVelocity(
                    Vector3.Dot(atTheContactPatch, up),
                    Vector3.Dot(atTheContactPatch, right),
                    Vector3.Dot(atTheContactPatch, forward));

                var force = WheelPhysics.Evaluate(
                    probe,
                    velocity,
                    new WheelLoad(massPerWheel, wheel.DriveShare, wheel.BrakeShare),
                    intent,
                    m_Profile.massKg,
                    Time.fixedDeltaTime,
                    m_Profile);

                if (!force.Grounded)
                {
                    continue;
                }

                var total = (up * force.AlongSuspension)
                            + (right * force.Lateral)
                            + (forward * force.Forward);

                Body.AddForceAtPosition(total, mount);
            }
        }

        bool IsBarelyMoving()
            => Body.linearVelocity.sqrMagnitude < StillnessMetresPerSecond * StillnessMetresPerSecond
               && Body.angularVelocity.sqrMagnitude < StillnessRadiansPerSecond * StillnessRadiansPerSecond;

        static bool NobodyIsAskingForAnything(in DriveIntent intent)
            => Mathf.Approximately(intent.Throttle, 0f)
               && Mathf.Approximately(intent.Brake, 0f)
               && Mathf.Approximately(intent.Steer, 0f);
    }
}
