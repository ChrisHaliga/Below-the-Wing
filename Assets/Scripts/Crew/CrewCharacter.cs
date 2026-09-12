using System;
using System.Collections.Generic;
using BelowTheWing.Cargo;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// One ramp worker on the apron: the capsule you walk around, and the thing that gets run over.
    ///
    /// A rigidbody rather than a character controller, because being shoved is part of the game.
    /// A three-tonne tractor running into somebody is supposed to send them flying, and an object
    /// with infinite mass cannot be sent anywhere. Movement is applied as changes to velocity that
    /// are capped per step, so a shove survives for a moment rather than being overwritten on the
    /// frame it lands.
    ///
    /// Every character configures its own body from the profile on its prefab, on every machine.
    /// A copy of somebody else's character left unconfigured has whatever the prefab defaults to --
    /// a kilogram, the wrong shape, free to topple -- and the first thing that touches it sends it
    /// across the apron.
    ///
    /// Only the character belonging to the person at this machine is given a seat, a camera and a
    /// keyboard. See <c>LocalPlayerRig</c>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [DisallowMultipleComponent]
    public sealed class CrewCharacter : MonoBehaviour, IDriveIntentSource, IMovedFromHere
    {
        /// <summary>Below this speed a character is treated as standing still and stops turning.</summary>
        const float WalkingPaceMetresPerSecond = 0.1f;

        [SerializeField, Tooltip("Mass, size and speeds for a person. Carried on the prefab.")]
        CrewProfile m_Profile;

        [SerializeField, Tooltip("How close this character must be to a vehicle to be offered it.")]
        float m_ReachMetres = 3f;

        [SerializeField, Tooltip("What counts as something to stand on. Everything, by default: a " +
                                 "cart deck is as good a floor as the apron.")]
        LayerMask m_StandsOn = ~0;

        [SerializeField, Tooltip("What counts as being over somebody's head, so they cannot stand " +
                                 "up into it. Separate from what they can stand on: narrowing one " +
                                 "so players cannot climb onto cart roofs must not quietly let a " +
                                 "crouched player stand up through one.")]
        LayerMask m_FitsUnder = ~0;

        Rigidbody m_Body;
        CapsuleCollider m_Collider;
        PhysicsMaterial m_Feet;
        VehicleController m_Seated;
        float m_LastJumpedAt = -1f;

        /// <summary>The profile this character's mass, size and speeds come from.</summary>
        public CrewProfile Profile => m_Profile;

        /// <summary>The rigidbody this character is pushed around as.</summary>
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

        /// <summary>Where this character's movement comes from. Null means it does not move itself.</summary>
        public ICrewIntentSource IntentSource { get; set; }

        /// <summary>The camera this character moves relative to. Only the local player has one.</summary>
        public FollowCamera Camera { get; set; }

        /// <summary>
        /// Getting in and out of vehicles. Null on every character except the one belonging to the
        /// person at this machine, because nobody else's character is driven from here.
        /// </summary>
        public VehicleOccupancy Seat { get; private set; }

        /// <summary>
        /// Hooking a cart on and dropping one off. Null for the same reason as the seat: only the
        /// player at this machine reshapes trains from here.
        /// </summary>
        public CouplingHand Hitching { get; set; }

        /// <summary>
        /// This player's hands. Only the character belonging to the person at this machine has
        /// them, for the same reason only they have a seat: nobody else's is operated from here.
        /// </summary>
        public Hands Handling { get; set; }

        /// <summary>
        /// Whether this character is crouched, and getting them up and down.
        ///
        /// Present on every character rather than only the local one, because how tall somebody is
        /// has to be right on every machine that can see them -- a person drawn standing while they
        /// are crouched inside a cart has their head through its roof.
        /// </summary>
        public Crouching Stance { get; private set; }

        /// <summary>How tall this character is right now, in metres.</summary>
        public float HeightMetres => m_Collider != null ? m_Collider.height : 0f;

        /// <summary>
        /// What this character is asking a vehicle to do. Meaningful only while it is driving one:
        /// the same stick that walks a character forward opens a throttle once they are in a seat.
        /// </summary>
        public DriveIntent Current
        {
            get
            {
                var asked = IntentSource?.Current ?? CrewIntent.Idle;
                return new DriveIntent(asked.Move.x, asked.Move.y, asked.Brake, asked.Sprint);
            }
        }

        /// <summary>
        /// Gives this character the body its profile describes: mass, size, and how it behaves when
        /// something hits it. Runs on every machine, for every character.
        /// </summary>
        public void ConfigureBody(CrewProfile profile)
        {
            m_Profile = profile ?? throw new ArgumentNullException(nameof(profile));

            m_Body = GetComponent<Rigidbody>();
            m_Body.mass = profile.massKg;
            m_Body.interpolation = RigidbodyInterpolation.Interpolate;

            // People do not topple over when nudged, and a capsule left free to rotate spends its
            // life lying down. Facing is set directly instead.
            m_Body.freezeRotation = true;

            // Crew are light, get launched hard, and are the thing it is least acceptable to see
            // pass through a wall, so they sweep rather than step.
            m_Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            m_Collider = GetComponent<CapsuleCollider>();
            m_Collider.height = profile.heightMetres;
            m_Collider.radius = profile.radiusMetres;
            m_Collider.center = Vector3.zero;

            // The feet are the grip, and the legs are the only thing that pushes against the
            // ground. A capsule with friction of its own fights every step the legs take, and holds
            // a rider on a deck through a corner the legs could not.
            if (m_Feet == null)
            {
                m_Feet = new PhysicsMaterial("Feet")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum
                };
            }

            m_Collider.material = m_Feet;

            Stance = new Crouching(profile, m_Collider, m_FitsUnder);
        }

        void OnDestroy()
        {
            if (m_Feet != null)
            {
                Destroy(m_Feet);
            }
        }

        /// <summary>
        /// Gives this character a seat, so its owner can get into vehicles. Only the local player's
        /// character gets one.
        /// </summary>
        public void TakeTheSeat(IOwnershipBroker broker, Func<IReadOnlyList<VehicleController>> nearbyVehicles)
        {
            Seat = new VehicleOccupancy(transform, broker, nearbyVehicles, m_ReachMetres);
        }

        /// <summary>
        /// Whether this machine is the one that says where this character goes.
        ///
        /// Authority, and nothing else. A copy of somebody else's character still has their weight,
        /// still falls, still keeps whatever speed it was given, and can still be run over -- it
        /// simply is not walked from here. Two machines walking one character fight each other, and
        /// the one that does not own them loses.
        ///
        /// It has to keep its weight and its momentum because players run each other over on
        /// purpose. A body with gravity switched off and its velocity zeroed has nothing for an
        /// impact to modify, so a tractor driven into somebody passes through them on the driver's
        /// screen while their own screen shows them standing untouched.
        /// </summary>
        public bool OursToMove { get; set; } = true;

        void Awake()
        {
            if (m_Profile != null)
            {
                ConfigureBody(m_Profile);
            }
        }

        void Start()
        {
            if (m_Profile == null)
            {
                Debug.LogError(
                    $"'{name}' has no crew profile, so it keeps whatever mass and shape its prefab " +
                    "happened to have -- typically one kilogram and the wrong collider -- and the " +
                    "first thing that touches it sends it across the apron.", this);
                enabled = false;
            }
        }

        /// <summary>
        /// Puts the body away while its owner drives.
        ///
        /// A character left standing where they got in is an obstacle: another player runs into an
        /// invisible person, and the driver reverses into their own body. Riding along inside the
        /// vehicle rather than beside it is what makes getting in look like getting in.
        /// </summary>
        void ClimbIn(VehicleController vehicle)
        {
            m_Seated = vehicle;

            // A driver's hands are on the wheel. A tether left running from a body parked inside
            // the tractor would haul whatever it held after the tractor.
            Handling?.LetGoOfEverything();

            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.isKinematic = true;

            if (m_Collider != null)
            {
                m_Collider.enabled = false;
            }

            transform.SetParent(vehicle.transform, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Puts the body back on the apron, clear of the vehicle it came out of.</summary>
        void ClimbOut()
        {
            var left = m_Seated;
            m_Seated = null;

            transform.SetParent(null, worldPositionStays: true);

            if (left != null && Seat != null)
            {
                transform.position = Seat.DismountPosition(left);
            }

            Body.isKinematic = false;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;

            if (m_Collider != null)
            {
                m_Collider.enabled = true;
            }
        }

        /// <summary>Whether there is something underneath close enough to push off.</summary>
        public bool Grounded => StandingOn(out _);

        /// <summary>Where the feet are, just above the bottom of the capsule.</summary>
        Vector3 Feet => transform.position - (Vector3.up * ((m_Profile.heightMetres * 0.5f) - 0.05f));

        /// <summary>What is underfoot, if anything is close enough to push against.</summary>
        bool StandingOn(out Collider what) => Jumping.StandingOnSomething(Feet, 0.2f, m_StandsOn, out what);

        /// <summary>
        /// How fast the thing underfoot is moving where the feet touch it. The tarmac is not going
        /// anywhere; a cart deck is.
        /// </summary>
        static Vector3 MovingAt(Collider underfoot, Vector3 feet)
        {
            var body = underfoot.attachedRigidbody;
            return body != null ? body.GetPointVelocity(feet) : Vector3.zero;
        }

        /// <summary>
        /// Pushes off whatever is underneath.
        ///
        /// The body already carries whatever speed the thing under it gave it, because standing on
        /// a moving deck is friction and nothing else. Somebody springing off a cart doing six
        /// metres a second therefore lands well ahead of where they left, which is both correct and
        /// the funnier outcome.
        /// </summary>
        public void Jump()
        {
            const float NoDoubleJumpsWithin = 0.2f;

            if (m_Profile == null || Time.time - m_LastJumpedAt < NoDoubleJumpsWithin || !Grounded)
            {
                return;
            }

            m_LastJumpedAt = Time.time;

            var up = Jumping.TakeOffSpeed(m_Profile.jumpHeightMetres, Mathf.Abs(Physics.gravity.y));
            var velocity = Body.linearVelocity;
            velocity.y = up;

            Body.linearVelocity = velocity;
        }

        void LateUpdate()
        {
            // Parenting a character into a vehicle is replicated; switching its collider off is not,
            // and a copy of somebody else's character never runs ClimbIn because it is not simulated
            // here. Left alone, every other machine has a live 80 kg capsule buried inside a
            // tractor's bodywork, which the solver reads as deep interpenetration and shoves apart
            // every step against a transform replication keeps snapping back.
            //
            // Riding in a vehicle is visible from the parenting, so every machine can act on it.
            var ridingSomewhere = transform.parent != null;
            if (m_Collider != null && m_Collider.enabled == ridingSomewhere)
            {
                m_Collider.enabled = !ridingSomewhere;
            }
        }

        void FixedUpdate()
        {
            if (m_Profile == null)
            {
                return;
            }

            Seat?.Refresh();

            // Before anything else moves this step, so that a bag torn out of a hand or a grip
            // broken by a crash is known about before the next press is read.
            Handling?.Tick();

            // Every step, and on every machine. How tall somebody is has to be right everywhere
            // that can see them -- drawn standing while they are crouched inside a cart puts their
            // head through its roof -- and standing up is something the world has to be able to
            // refuse, which it can only do while they are still under whatever is over them.
            Stance.Settle();

            if (Hitching != null)
            {
                // Only meaningful while driving, and the train being driven is what it acts on.
                Hitching.Driving = Seat?.Driving != null ? Seat.Driving.Chain : null;
                Hitching.Refresh();
            }

            var drivingNow = Seat?.Driving;
            if (drivingNow != m_Seated)
            {
                if (drivingNow != null)
                {
                    ClimbIn(drivingNow);
                }
                else
                {
                    ClimbOut();
                }
            }

            // Somebody in a seat is cargo. Their controls are going to the vehicle, and walking at
            // the same time would drag the capsule out through the bodywork.
            if (m_Seated != null)
            {
                return;
            }

            // Walked only where this machine is in charge. The body keeps its weight and its
            // momentum either way, so a copy of somebody else's character is still something that
            // can be run over -- it is simply not being walked from here as well. Two machines
            // walking one character fight, and the one that does not own them loses.
            if (!OursToMove)
            {
                return;
            }

            var asked = IntentSource?.Current ?? CrewIntent.Idle;

            if (asked.Jump)
            {
                Jump();
            }

            Stance.Want(asked.Crouch);

            // Walking is pushing against whatever is underfoot. Nothing there, nothing to push
            // against: somebody in the air keeps the speed they left the ground with.
            if (!StandingOn(out var underfoot))
            {
                return;
            }

            var cameraYaw = Camera != null ? Camera.YawDegrees : transform.eulerAngles.y;
            var walking = CrewLocomotion.DesiredVelocity(asked.Move, cameraYaw, asked.Sprint, m_Profile)
                          * Stance.SpeedMultiplier;

            // Relative to what is underfoot. Standing still on a moving deck is moving with it, and
            // walking forward on one is that and a bit more -- which is the whole of riding a cart,
            // and needs nothing to attach the rider to it.
            var deck = MovingAt(underfoot, Feet);
            var deckAcrossTheGround = new Vector3(deck.x, 0f, deck.z);
            var wanted = deckAcrossTheGround + walking;

            var velocity = Body.linearVelocity;
            var acrossTheGround = new Vector3(velocity.x, 0f, velocity.z);

            // Capped at what the feet can push before they slip. That cap is also what lets a
            // corner taken hard enough take a deck out from under somebody.
            var canChangeThisStep = m_Profile.accelerationMetresPerSecondSquared * Time.fixedDeltaTime;
            var change = Vector3.ClampMagnitude(wanted - acrossTheGround, canChangeThisStep);
            Body.AddForce(change, ForceMode.VelocityChange);

            // Facing follows where they are trying to go, not where they are being taken. A
            // passenger dragged along by a deck, or yanked by the bag they just picked up, is not
            // walking anywhere -- and turning them to face the drag would, with the camera's yaw
            // as their own, turn "forward" round with them.
            if (walking.magnitude > WalkingPaceMetresPerSecond)
            {
                transform.rotation = CrewLocomotion.FaceTravel(
                    transform.rotation, walking, Time.fixedDeltaTime, m_Profile);
            }
        }
    }
}
