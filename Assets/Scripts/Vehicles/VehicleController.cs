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
    public sealed class VehicleController : MonoBehaviour
    {
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
        VehicleShape m_Shape;
        Wheel[] m_Wheels;

        /// <summary>How far each wheel currently hangs below its mount, in metres.</summary>
        float[] m_HangingBy;
        float m_SteerAngleDegrees;
        bool m_Occupied;

        /// <summary>The tuning and real-world mass this vehicle runs on.</summary>
        public VehicleProfile Profile => m_Profile;

        /// <summary>
        /// Where this vehicle's parts physically are: its wheels, its couplings, the room it takes
        /// up and which parts of it are solid.
        /// </summary>
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

        /// <summary>Whether the wheel in that corner turns with the steering.</summary>
        public bool WheelSteers(int corner)
            => m_Wheels != null && corner >= 0 && corner < m_Wheels.Length && m_Wheels[corner].Steers;

        /// <summary>
        /// How high the centre of the wheel in that corner is sitting right now, in this vehicle's
        /// own space.
        ///
        /// Where it is standing rather than where it was drawn. The body moves up and down on its
        /// springs relative to an origin that is on the tarmac, so a wheel that keeps the height it
        /// was modelled at is buried by however far the suspension compressed.
        /// </summary>
        public float WheelCentreLocal(int corner, float drawnAt)
        {
            if (m_Wheels == null || m_HangingBy == null || corner < 0 || corner >= m_Wheels.Length)
            {
                return drawnAt;
            }

            return m_Wheels[corner].MountLocal.y - m_HangingBy[corner];
        }

        /// <summary>
        /// The train this vehicle belongs to. Every vehicle is in one; an uncoupled vehicle is in a
        /// train of itself alone. Having no empty case means that taking over a vehicle is the same
        /// operation whether or not it happens to be towing anything.
        /// </summary>
        public CartChain Chain { get; internal set; }

        /// <summary>
        /// Whether this machine is the one that says where this vehicle goes.
        ///
        /// Authority, and nothing else. Physics runs on every vehicle on every machine either way:
        /// a copy of one somebody else owns holds itself up on its own suspension, grips the ground
        /// with its own tires, keeps its weight and keeps its speed. The single difference is that
        /// nobody here is driving it, so no throttle and no brake are applied.
        ///
        /// It has to be that way for contact between two players to mean anything. A crash is an
        /// exchange of momentum, and a body held still, or held up by nothing, has no momentum to
        /// exchange -- there is nothing for an impulse to write into and nothing that survives to
        /// the next update. What the owner says is blended in on top of the physics rather than
        /// replacing it, which is why the physics has to be running underneath.
        ///
        /// Deliberately a plain flag rather than a question about networking, so that the vehicle
        /// still knows nothing about how -- or whether -- the game is networked.
        /// </summary>
        public bool OursToMove { get; set; } = true;

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

        /// <summary>
        /// Where a vehicle in front of this one attaches, in this vehicle's local space.
        ///
        /// Read off the shape, which for a modelled vehicle read it off the model. A real drawbar
        /// is nothing like symmetric -- a baggage cart reaches 3.16 m forward and 1.82 m back, and
        /// the two halves sit at different heights so that they do not try to occupy the same
        /// space -- so neither end can be worked out from the other.
        /// </summary>
        public Vector3 FrontHitchLocal => Shape != null ? Shape.FrontCouplingLocal : Vector3.zero;

        /// <summary>Where a vehicle behind this one attaches, in this vehicle's local space.</summary>
        public Vector3 RearHitchLocal => Shape != null ? Shape.RearCouplingLocal : Vector3.zero;

        /// <summary>How far this vehicle's front coupling reaches past its origin, in metres.</summary>
        public float FrontReach => Shape != null ? Shape.FrontReachMetres : 0f;

        /// <summary>How far this vehicle's rear coupling reaches past its origin, in metres.</summary>
        public float RearReach => Shape != null ? Shape.RearReachMetres : 0f;

        /// <summary>
        /// How far each corner's spring is squashed by the weight standing on it, from 0 to 1.
        ///
        /// Read from the numbers rather than written down again. A vehicle resting at full
        /// extension has nothing left to absorb a bump with; one resting bottomed out has nothing
        /// left to give. Somewhere in between is the whole reason to have springs.
        /// </summary>
        public static float SuspensionCompressionAtRest(VehicleProfile profile)
        {
            const int corners = 4;

            var weightOnEachCorner = profile.massKg * Physics.gravity.magnitude / corners;

            return Mathf.Clamp01(weightOnEachCorner / profile.springStrengthNewtons);
        }

        /// <summary>
        /// How high above the ground the top of the suspension sits once it has settled, in metres.
        ///
        /// This is where a vehicle's body hangs from. With the origin on the ground between the
        /// wheels, it is also how far above that origin the mounts have to be for the vehicle to
        /// stand at the right height with nothing floating and nothing buried.
        /// </summary>
        public static float SuspensionMountHeightMetres(VehicleProfile profile)
            => profile.wheelRadiusMetres
               + (profile.suspensionRestLengthMetres * (1f - SuspensionCompressionAtRest(profile)));

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
            if (profile.centerOfMassOffset.y <= 0f)
            {
                Debug.LogError(
                    $"'{name}' carries its centre of mass at or below its own origin, which is on " +
                    "the ground. Weight transfer then works backwards -- braking pitches the nose " +
                    "up -- and nothing can tip the vehicle over.", this);
            }

            m_Body.centerOfMass = profile.centerOfMassOffset;
            m_Body.interpolation = RigidbodyInterpolation.Interpolate;

            // Vehicles are fast, heavy, and the thing most often driven hard at something else --
            // an apron edge, a jetway leg, or another train. Sweeping against moving bodies as well
            // as static ones costs more than sweeping against static alone, and a tractor passing
            // clean through a cart is the failure nobody would accept.
            m_Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Not one box the size of the vehicle. A cart is a container, and a box that size fills
            // the space bags are supposed to go in -- so what a vehicle is solid where comes from
            // its shape, which for a modelled vehicle is a floor, some walls and a roof.
            VehicleBody.Build(gameObject, Shape);

            BuildWheels(profile);

            // Counting what is touching this vehicle. Physics reports contacts as they begin and
            // end and keeps no running total, so the only way to have the number is to keep it.
            if (GetComponent<ContactTally>() == null)
            {
                gameObject.AddComponent<ContactTally>();
            }

            Chain ??= CartChain.Couple(new[] { this }, ChainJointSettings.Default);
        }

        /// <summary>
        /// Hangs the suspension from where the wheels actually are.
        ///
        /// The positions come from the shape, which for a modelled vehicle read them off the model
        /// itself. They used to be worked out from a wheelbase and a track written into the profile,
        /// and that second copy of a measurement is what put the invisible probes fourteen
        /// centimetres from the visible wheels, with one side of a cart sitting permanently
        /// compressed and nobody able to see why.
        ///
        /// Only the height is decided here, because it is a suspension figure rather than a
        /// geometric one: the mount sits at whatever height leaves the vehicle standing correctly
        /// once its springs have taken its weight.
        ///
        /// Front wheels steer, rear wheels drive. A baggage tractor is a small rear-drive unit, and
        /// pulling a loaded train from the front axle would spin the wheels up rather than move it.
        /// Braking is shared evenly because every wheel has a brake on it.
        /// </summary>
        void BuildWheels(VehicleProfile profile)
        {
            var wheels = Shape != null ? Shape.WheelCentresLocal : null;
            if (wheels == null || wheels.Count == 0)
            {
                Debug.LogError(
                    $"'{name}' has no shape, so there is nowhere to hang its suspension from and it " +
                    "will fall through the apron. Every vehicle prefab needs a VehicleShape.", this);

                m_Wheels = new Wheel[0];
                m_HangingBy = new float[0];
                return;
            }

            var mountHeight = SuspensionMountHeightMetres(profile);
            var middleOfTheWheelbase = 0f;

            foreach (var wheel in wheels)
            {
                middleOfTheWheelbase += wheel.z;
            }

            middleOfTheWheelbase /= wheels.Count;

            m_Wheels = new Wheel[wheels.Count];
            for (var i = 0; i < wheels.Count; i++)
            {
                var atTheFront = wheels[i].z > middleOfTheWheelbase;

                m_Wheels[i] = new Wheel(
                    new Vector3(wheels[i].x, mountHeight, wheels[i].z),
                    steers: atTheFront,
                    driveShare: atTheFront ? 0f : 2f / wheels.Count,
                    brakeShare: 1f / wheels.Count);
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
            if (m_Profile == null || m_Wheels == null)
            {
                return;
            }

            // Controls are read only where they count. On a copy of a vehicle somebody else owns
            // the wheels still hold it up and still grip, but nobody here is driving: two machines
            // opening the same throttle would fight each other, and the one that does not own it
            // would lose anyway.
            var intent = OursToMove ? IntentSource?.Current ?? DriveIntent.Idle : DriveIntent.Idle;
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

            // Sleep is a decision for a whole train, taken once, by the vehicle at the front of it.
            // A vehicle on its own is a train of itself, so there is no separate case for one.
            if (Chain != null && Chain.Leader == this
                && Chain.SettleOnceEverythingHasStopped(
                    idle, Time.fixedDeltaTime, SecondsOfStillnessBeforeSleeping,
                    StillnessMetresPerSecond, StillnessRadiansPerSecond))
            {
                // Just put to sleep. Applying a single wheel force now would wake it again, and the
                // train would spend the rest of the session settling and being roused by itself.
                return;
            }

            if (OursToMove)
            {
                m_SteerAngleDegrees = Steering.Step(m_SteerAngleDegrees, intent.Steer, Time.fixedDeltaTime, m_Profile);
            }

            var massPerWheel = m_Profile.massKg / m_Wheels.Length;
            var rayLength = m_Profile.wheelRadiusMetres + m_Profile.suspensionRestLengthMetres;
            var up = transform.up;
            var steerRotation = Quaternion.AngleAxis(m_SteerAngleDegrees, up);

            for (var i = 0; i < m_Wheels.Length; i++)
            {
                var wheel = m_Wheels[i];
                var mount = transform.TransformPoint(wheel.MountLocal);
                var forward = wheel.Steers ? steerRotation * transform.forward : transform.forward;
                var right = wheel.Steers ? steerRotation * transform.right : transform.right;

                var probe = Physics.Raycast(mount, -up, out var hit, rayLength, m_GroundMask, QueryTriggerInteraction.Ignore)
                    ? new GroundProbe(true, hit.distance)
                    : GroundProbe.Airborne;

                // Kept so the visible wheels can be hung where the ground actually is. Measured
                // once, here, rather than probed a second time by whatever draws them: two rays a
                // frame apart find different ground on a moving vehicle, and the wheel you see
                // would sit somewhere the wheel holding the cart up is not.
                m_HangingBy[i] = probe.HitGround
                    ? probe.DistanceToGround - m_Profile.wheelRadiusMetres
                    : m_Profile.suspensionRestLengthMetres;

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


        static bool NobodyIsAskingForAnything(in DriveIntent intent)
            => Mathf.Approximately(intent.Throttle, 0f)
               && Mathf.Approximately(intent.Brake, 0f)
               && Mathf.Approximately(intent.Steer, 0f);
    }
}
