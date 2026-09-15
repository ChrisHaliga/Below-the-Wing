using System;
using System.Collections.Generic;
using BelowTheWing.Cargo;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Crew
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [DisallowMultipleComponent]
    public sealed class CrewCharacter : MonoBehaviour, IDriveIntentSource, IMovedFromHere
    {
        [SerializeField, Tooltip("Profile this runs on")]
        CrewProfile m_Profile;

        [SerializeField, Tooltip("Reach for vehicles and couplings, m")]
        float m_ReachMetres = 3f;

        [SerializeField, Tooltip("Layers that count as something to stand on")]
        LayerMask m_StandsOn = ~0;

        [SerializeField, Tooltip("Layers that count as something overhead")]
        LayerMask m_FitsUnder = ~0;

        Rigidbody m_Body;
        CapsuleCollider m_Collider;
        PhysicsMaterial m_Feet;
        VehicleController m_Seated;
        float m_LastJumpedAt = -1f;

        public CrewProfile Profile => m_Profile;

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

        public ICrewIntentSource IntentSource { get; set; }

        public FollowCamera Camera { get; set; }

        public VehicleOccupancy Seat { get; private set; }

        public CouplingHand Hitching { get; set; }

        public Hands Handling { get; set; }

        public Crouching Stance { get; private set; }

        public Climbing Climb { get; } = new Climbing();

        public float HeightMetres => m_Collider != null ? m_Collider.height : 0f;

        public float EyeMetresAboveOrigin
            => m_Collider != null && m_Profile != null
                ? m_Collider.center.y + (m_Collider.height * 0.5f) - (m_Profile.heightMetres * EyesBelowTheTop)
                : 0f;

        const float EyesBelowTheTop = 0.08f;

        public DriveIntent Current
        {
            get
            {
                var asked = IntentSource?.Current ?? CrewIntent.Idle;
                return new DriveIntent(asked.Move.x, asked.Move.y, asked.Brake, asked.Sprint);
            }
        }

        public void ConfigureBody(CrewProfile profile)
        {
            m_Profile = profile ?? throw new ArgumentNullException(nameof(profile));

            m_Body = GetComponent<Rigidbody>();
            m_Body.mass = profile.massKg;
            m_Body.interpolation = RigidbodyInterpolation.Interpolate;

            m_Body.freezeRotation = true;

            m_Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            m_Collider = GetComponent<CapsuleCollider>();
            m_Collider.height = profile.heightMetres;
            m_Collider.radius = profile.radiusMetres;
            m_Collider.center = Vector3.zero;

            if (m_Feet == null)
            {
                m_Feet = new PhysicsMaterial("Feet")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounciness = 0f,
                    bounceCombine = PhysicsMaterialCombine.Minimum
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

        public void TakeTheSeat(IOwnershipBroker broker, Func<IReadOnlyList<VehicleController>> nearbyVehicles)
        {
            Seat = new VehicleOccupancy(transform, broker, nearbyVehicles, m_ReachMetres);
        }

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

        void ClimbIn(VehicleController vehicle)
        {
            m_Seated = vehicle;

            Handling?.LetGoOfEverything();

            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.isKinematic = true;

            if (m_Collider != null)
            {
                m_Collider.enabled = false;
            }

            transform.SetParent(vehicle.transform, worldPositionStays: false);
            transform.localPosition = SatIn(vehicle);
            transform.localRotation = Quaternion.identity;
        }

        Vector3 SatIn(VehicleController vehicle)
        {
            var seat = vehicle.Shape != null ? vehicle.Shape.SeatLocal : null;
            if (!seat.HasValue)
            {
                return Vector3.zero;
            }

            return seat.Value + (Vector3.up * (m_Profile.heightMetres * 0.5f));
        }

        void ClimbOut()
        {
            var left = m_Seated;
            m_Seated = null;

            transform.SetParent(null, worldPositionStays: true);
            Body.isKinematic = false;

            if (left != null && Seat != null)
            {
                var spot = Seat.DismountPosition(left, HeightMetres);
                transform.position = spot;
                Body.position = spot;
                Body.linearVelocity = left.Body.linearVelocity;
            }
            else
            {
                Body.linearVelocity = Vector3.zero;
            }

            Body.angularVelocity = Vector3.zero;

            if (m_Collider != null)
            {
                m_Collider.enabled = true;
            }
        }

        public bool Grounded => StandingOn(out _);

        Vector3 Feet => transform.position - (Vector3.up * ((m_Profile.heightMetres * 0.5f) - 0.05f));

        bool m_PushedOff;

        bool StandingOn(out Collider what)
        {
            if (m_PushedOff)
            {
                what = null;
                return false;
            }

            return Jumping.StandingOnSomething(Feet, 0.2f, m_StandsOn, out what);
        }

        void NoticeTheyHaveLanded()
        {
            if (!m_PushedOff || Body.linearVelocity.y > 0f)
            {
                return;
            }

            m_PushedOff = !Jumping.StandingOnSomething(Feet, 0.2f, m_StandsOn, out _);
        }

        static Vector3 MovingAt(Collider underfoot, Vector3 feet)
        {
            var body = underfoot.attachedRigidbody;
            return body != null ? body.GetPointVelocity(feet) : Vector3.zero;
        }

        readonly Collider[] m_ZonesInReach = new Collider[8];

        void LookForSomethingToClimb()
        {
            if (m_Seated != null || m_Collider == null)
            {
                Climb.NothingInReach();
                return;
            }

            var found = Physics.OverlapCapsuleNonAlloc(
                transform.position + (Vector3.up * (m_Collider.height * 0.5f)),
                transform.position - (Vector3.up * (m_Collider.height * 0.5f)),
                m_Profile.radiusMetres,
                m_ZonesInReach,
                ~0,
                QueryTriggerInteraction.Collide);

            Climb.NothingInReach();

            for (var i = 0; i < found; i++)
            {
                if (m_ZonesInReach[i].TryGetComponent<ClimbZone>(out var zone))
                {
                    Climb.Consider(
                        zone, Feet, m_Profile.climbReachMetres, m_Profile.crouchedHeightMetres);
                }
            }
        }

        void HaulThemselvesIn()
        {
            m_LastJumpedAt = Time.time;
            m_PushedOff = true;
            Body.linearVelocity = Climb.TakeHold(
                Feet, m_Profile.crouchedHeightMetres, Mathf.Abs(Physics.gravity.y));
            m_Footing.Reset();
        }

        void KeepHauling()
        {
            var pull = Climb.Haul(
                Feet, Body.linearVelocity, m_Profile.radiusMetres, Time.fixedDeltaTime,
                Mathf.Abs(Physics.gravity.y));

            if (pull != Vector3.zero)
            {
                Body.AddForce(pull, ForceMode.VelocityChange);
            }
        }

        public void Jump()
        {
            const float NoDoubleJumpsWithin = 0.2f;

            if (m_Profile == null || Time.time - m_LastJumpedAt < NoDoubleJumpsWithin || !Grounded)
            {
                return;
            }

            m_LastJumpedAt = Time.time;
            m_PushedOff = true;

            var up = Jumping.TakeOffSpeed(m_Profile.jumpHeightMetres, Mathf.Abs(Physics.gravity.y));
            var velocity = Body.linearVelocity;
            velocity.y = up;

            Body.linearVelocity = velocity;
        }

        void LateUpdate()
        {
            var ridingSomewhere = transform.parent != null;
            if (m_Collider != null && m_Collider.enabled == ridingSomewhere)
            {
                m_Collider.enabled = !ridingSomewhere;
            }
        }

        Footing m_Footing;

        void FixedUpdate()
        {
            if (m_Profile == null)
            {
                return;
            }

            Seat?.Refresh();

            Handling?.Tick();

            Stance.Settle();

            if (Hitching != null)
            {
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

            if (m_Seated != null)
            {
                return;
            }

            if (!OursToMove)
            {
                return;
            }

            var asked = IntentSource?.Current ?? CrewIntent.Idle;

            if (Camera != null)
            {
                transform.rotation = Quaternion.Euler(0f, Camera.YawDegrees, 0f);
            }

            NoticeTheyHaveLanded();

            Climb.Settle(Time.fixedDeltaTime);
            KeepHauling();

            LookForSomethingToClimb();

            if (asked.Jump)
            {
                if (Climb.Offered != null)
                {
                    HaulThemselvesIn();
                }
                else
                {
                    Jump();
                }
            }

            Stance.Want(asked.Crouch || Climb.Hauling != null);

            if (!StandingOn(out var underfoot))
            {
                m_Footing.Reset();
                return;
            }

            var deck = MovingAt(underfoot, Feet);
            var deckAcrossTheGround = new Vector3(deck.x, 0f, deck.z);

            var velocity = Body.linearVelocity;
            var acrossTheGround = new Vector3(velocity.x, 0f, velocity.z);

            m_Footing.Settle(
                underfoot,
                deckAcrossTheGround,
                acrossTheGround,
                m_Profile.footGripMetresPerSecondSquared,
                Time.fixedDeltaTime);

            var grip = m_Profile.footGripMetresPerSecondSquared * Time.fixedDeltaTime;

            if (m_Footing.Lost)
            {
                Body.AddForce(
                    Vector3.ClampMagnitude(deckAcrossTheGround - acrossTheGround, grip),
                    ForceMode.VelocityChange);
                return;
            }

            var cameraYaw = Camera != null ? Camera.YawDegrees : transform.eulerAngles.y;
            var walking = Climb.HandsFull
                ? Vector3.zero
                : CrewLocomotion.DesiredVelocity(asked.Move, cameraYaw, asked.Sprint, m_Profile)
                  * Stance.SpeedMultiplier;

            var wanted = deckAcrossTheGround + walking;
            var gait = m_Profile.gaitResponseMetresPerSecondSquared * Time.fixedDeltaTime;

            Body.AddForce(
                Vector3.ClampMagnitude(wanted - acrossTheGround, gait), ForceMode.VelocityChange);
        }
    }
}
