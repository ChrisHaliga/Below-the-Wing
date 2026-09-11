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
    public sealed class CrewCharacter : MonoBehaviour, IDriveIntentSource
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

        Rigidbody m_Body;
        CapsuleCollider m_Collider;
        VehicleController m_RidingIn;
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
        /// Whatever this character is riding on, if anything. Present on every character, not only
        /// the local one, because a rider has to be carried on every machine that can see them.
        /// </summary>
        public Carried Riding { get; set; }

        /// <summary>
        /// This player's hands. Only the character belonging to the person at this machine has
        /// them, for the same reason only they have a seat: nobody else's is operated from here.
        /// </summary>
        public Hands Handling { get; set; }

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
            m_RidingIn = vehicle;

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
            var left = m_RidingIn;
            m_RidingIn = null;

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
        public bool Grounded
        {
            get
            {
                var feet = transform.position - (Vector3.up * ((m_Profile.heightMetres * 0.5f) - 0.05f));
                return Jumping.StandingOnSomething(feet, 0.2f, m_StandsOn, out _);
            }
        }

        /// <summary>
        /// Pushes off whatever is underneath.
        ///
        /// Jumping off a carrier lets go of it, so the jump carries wherever the carrier was going.
        /// Somebody springing off a cart doing six metres a second lands well ahead of where they
        /// left, which is both correct and the funnier outcome.
        /// </summary>
        public void Jump()
        {
            const float NoDoubleJumpsWithin = 0.2f;

            if (m_Profile == null || Time.time - m_LastJumpedAt < NoDoubleJumpsWithin || !Grounded)
            {
                return;
            }

            m_LastJumpedAt = Time.time;

            var carriedAt = Vector3.zero;
            if (Riding != null && Riding.Attached)
            {
                carriedAt = Riding.On.VelocityAt(transform.position);
                Riding.Wake(Time.time);
            }

            var up = Jumping.TakeOffSpeed(m_Profile.jumpHeightMetres, Mathf.Abs(Physics.gravity.y));
            var velocity = Body.linearVelocity + carriedAt;
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

            if (Hitching != null)
            {
                // Only meaningful while driving, and the train being driven is what it acts on.
                Hitching.Driving = Seat?.Driving != null ? Seat.Driving.Chain : null;
                Hitching.Refresh();
            }

            var drivingNow = Seat?.Driving;
            if (drivingNow != m_RidingIn)
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
            if (m_RidingIn != null)
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

            var cameraYaw = Camera != null ? Camera.YawDegrees : transform.eulerAngles.y;
            var wanted = CrewLocomotion.DesiredVelocity(asked.Move, cameraYaw, asked.Sprint, m_Profile);

            var velocity = Body.linearVelocity;
            var acrossTheGround = new Vector3(velocity.x, 0f, velocity.z);

            var canChangeThisStep = m_Profile.accelerationMetresPerSecondSquared * Time.fixedDeltaTime;
            var change = Vector3.ClampMagnitude(wanted - acrossTheGround, canChangeThisStep);
            Body.AddForce(change, ForceMode.VelocityChange);

            if (acrossTheGround.magnitude > WalkingPaceMetresPerSecond)
            {
                transform.rotation = CrewLocomotion.FaceTravel(
                    transform.rotation, acrossTheGround, Time.fixedDeltaTime, m_Profile);
            }
        }
    }
}
