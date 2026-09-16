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

        public PullingUp Hoisting { get; } = new PullingUp();

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

        public Ray LookingAlong()
            => Camera != null
                ? new Ray(Camera.transform.position, Camera.transform.forward)
                : new Ray(transform.position, transform.forward);

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

        void HoistThemselves(bool asked)
        {
            var hauling = Hoisting.Haul(asked, Handling != null ? Handling.BothHoldingAt : null, Feet);

            if (!Hoisting.Pulling)
            {
                if (Grounded)
                {
                    Hoisting.Landed();
                }

                return;
            }

            Body.linearVelocity = hauling;

            m_Footing.Reset();
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

            KeepTheirGearInStep();
            TakeTheSeatOrLeaveIt();

            if (m_Seated != null || !OursToMove)
            {
                return;
            }

            var asked = IntentSource?.Current ?? CrewIntent.Idle;

            FaceWhereTheyAreLooking();
            NoticeTheyHaveLanded();
            HoistThemselves(asked.Hoist);

            if (asked.Jump && !Hoisting.Pulling)
            {
                Jump();
            }

            Stance.Want(asked.Crouch || Hoisting.Ducking);

            if (!StandingOn(out var underfoot))
            {
                m_Footing.Reset();
                return;
            }

            CarryThemAlongTheDeck(asked, underfoot);
        }

        void KeepTheirGearInStep()
        {
            Seat?.Refresh();
            Handling?.Tick();
            Stance.Settle();

            if (Hitching == null)
            {
                return;
            }

            Hitching.Driving = Seat?.Driving != null ? Seat.Driving.Chain : null;
            Hitching.Refresh();
        }

        void TakeTheSeatOrLeaveIt()
        {
            var drivingNow = Seat?.Driving;

            if (drivingNow == m_Seated)
            {
                return;
            }

            if (drivingNow != null)
            {
                ClimbIn(drivingNow);
            }
            else
            {
                ClimbOut();
            }
        }

        void FaceWhereTheyAreLooking()
        {
            if (Camera != null)
            {
                transform.rotation = Quaternion.Euler(0f, Camera.YawDegrees, 0f);
            }
        }

        void CarryThemAlongTheDeck(CrewIntent asked, Collider underfoot)
        {
            var deckAcrossTheGround = Flat(MovingAt(underfoot, Feet));
            var acrossTheGround = Flat(Body.linearVelocity);

            m_Footing.Settle(
                underfoot,
                deckAcrossTheGround,
                acrossTheGround,
                m_Profile.footGripMetresPerSecondSquared,
                Time.fixedDeltaTime);

            if (m_Footing.Lost)
            {
                Shove(deckAcrossTheGround - acrossTheGround,
                    m_Profile.footGripMetresPerSecondSquared * Time.fixedDeltaTime);
                return;
            }

            Shove(deckAcrossTheGround + TheirOwnStride(asked) - acrossTheGround,
                m_Profile.gaitResponseMetresPerSecondSquared * Time.fixedDeltaTime);
        }

        Vector3 TheirOwnStride(CrewIntent asked)
        {
            if (Hoisting.Pulling)
            {
                return Vector3.zero;
            }

            var cameraYaw = Camera != null ? Camera.YawDegrees : transform.eulerAngles.y;

            return CrewLocomotion.DesiredVelocity(asked.Move, cameraYaw, asked.Sprint, m_Profile)
                   * Stance.SpeedMultiplier;
        }

        void Shove(Vector3 towards, float mostItMayChangeBy)
            => Body.AddForce(
                Vector3.ClampMagnitude(towards, mostItMayChangeBy), ForceMode.VelocityChange);

        static Vector3 Flat(Vector3 velocity) => new Vector3(velocity.x, 0f, velocity.z);
    }
}
