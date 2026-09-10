using System;
using System.Collections.Generic;
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
    /// While its owner is driving a vehicle the character stops steering itself and rides along.
    /// It also acts as the driver of that vehicle: what the player presses becomes steering and
    /// throttle rather than footsteps.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class CrewCharacter : MonoBehaviour, IDriveIntentSource
    {
        /// <summary>Below this speed a character is treated as standing still and stops turning.</summary>
        const float WalkingPaceMetresPerSecond = 0.1f;

        [SerializeField, Tooltip("Mass, size and speeds for a person.")]
        CrewProfile m_Profile;

        [SerializeField, Tooltip("How close this character must be to a vehicle to be offered it.")]
        float m_ReachMetres = 3f;

        Rigidbody m_Body;
        CapsuleCollider m_Collider;
        VehicleController m_RidingIn;

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

        /// <summary>The camera this character moves relative to, and which follows it.</summary>
        public FollowCamera Camera { get; set; }

        /// <summary>Getting in and out of vehicles.</summary>
        public VehicleOccupancy Seat { get; private set; }

        /// <summary>
        /// What this character is asking a vehicle to do. Meaningful only while it is driving one:
        /// the same stick that walks a character forward opens a throttle once they are in a seat.
        /// </summary>
        public DriveIntent Current
        {
            get
            {
                var asked = IntentSource?.Current ?? CrewIntent.Idle;
                return new DriveIntent(asked.Move.x, asked.Move.y, asked.Brake);
            }
        }

        /// <summary>
        /// Wires this character up once it has been built: the profile it runs on, how ownership of
        /// vehicles is asked for, and how it finds vehicles worth being offered.
        /// </summary>
        public void Configure(CrewProfile profile, IOwnershipBroker broker, Func<IReadOnlyList<IDriveable>> nearbyVehicles)
        {
            m_Profile = profile;

            m_Body = GetComponent<Rigidbody>();
            if (m_Body == null)
            {
                m_Body = gameObject.AddComponent<Rigidbody>();
            }

            m_Body.mass = profile.massKg;
            m_Body.interpolation = RigidbodyInterpolation.Interpolate;

            // People do not topple over when nudged, and a capsule left free to rotate spends its
            // life lying down. Facing is set directly instead.
            m_Body.freezeRotation = true;

            // Crew are light, get launched hard, and are the thing it is least acceptable to see
            // pass through a wall, so they sweep rather than step.
            m_Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            m_Collider = GetComponent<CapsuleCollider>();
            if (m_Collider == null)
            {
                m_Collider = gameObject.AddComponent<CapsuleCollider>();
            }

            m_Collider.height = profile.heightMetres;
            m_Collider.radius = profile.radiusMetres;
            m_Collider.center = Vector3.zero;

            Seat = new VehicleOccupancy(transform, broker, nearbyVehicles, m_ReachMetres);
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

            if (left != null)
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

        void FixedUpdate()
        {
            if (m_Profile == null)
            {
                return;
            }

            Seat?.Refresh();

            var drivingNow = Seat != null ? Seat.Driving : null;
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

            var asked = IntentSource?.Current ?? CrewIntent.Idle;
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
