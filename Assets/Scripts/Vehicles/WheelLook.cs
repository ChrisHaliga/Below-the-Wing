using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Turning a vehicle's visible wheels, and keeping them on the ground.
    ///
    /// Two jobs that look unrelated and are not. A vehicle's origin sits on the tarmac between its
    /// wheels, and the suspension moves the body relative to that, so a wheel parented rigidly to
    /// the bodywork is dragged into the ground by however far the springs compressed. On a cart
    /// with 0.157 m wheels and 0.08 m of travel, that is most of the wheel.
    ///
    /// Nothing here is simulated. The wheels the physics uses are raycasts from the shape's mount
    /// points; these are what a player sees, and they are moved to agree with what those rays
    /// found. Keeping the two apart is what lets the suspension be four springs rather than four
    /// more rigidbodies.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    [DisallowMultipleComponent]
    public sealed class WheelLook : MonoBehaviour
    {
        [SerializeField, Tooltip("The visible wheels, in the same order as the shape's wheel positions.")]
        Transform[] m_Wheels = new Transform[0];

        VehicleController m_Vehicle;
        Vector3[] m_RestingAt;

        /// <summary>How far the wheels have turned since the vehicle was built, in degrees.</summary>
        public float TurnedDegrees { get; private set; }

        /// <summary>Names the wheels this drives. Used when a prefab is built, and by tests.</summary>
        public void Watch(IReadOnlyList<Transform> wheels)
        {
            m_Wheels = new List<Transform>(wheels).ToArray();
            m_RestingAt = null;
        }

        void Awake() => m_Vehicle = GetComponent<VehicleController>();

        void FixedUpdate()
        {
            if (m_Vehicle == null || m_Vehicle.Profile == null || m_Wheels.Length == 0)
            {
                return;
            }

            Remember();
            TurnThem();
            HangThem();
        }

        /// <summary>
        /// Notes where each wheel was authored, once.
        ///
        /// Read off the model rather than taken from the shape, so that a wheel drawn slightly away
        /// from where its suspension probes -- a modelled offset, a hub that is not quite on the
        /// axle line -- stays where it was drawn. Only its height is the physics' business.
        /// </summary>
        void Remember()
        {
            if (m_RestingAt != null)
            {
                return;
            }

            m_RestingAt = new Vector3[m_Wheels.Length];
            for (var i = 0; i < m_Wheels.Length; i++)
            {
                m_RestingAt[i] = m_Wheels[i] != null ? m_Wheels[i].localPosition : Vector3.zero;
            }
        }

        /// <summary>
        /// Turns them at road speed.
        ///
        /// The angle is accumulated and assigned rather than added to the transform each frame.
        /// Adding a rotation repeatedly drifts -- every step carries a little floating-point error
        /// and there is nothing to correct it -- and the vehicle that suffers most is one driving in
        /// circles for a long time, which is exactly what a measuring rig does.
        /// </summary>
        void TurnThem()
        {
            var alongTheRoad = Vector3.Dot(m_Vehicle.Body.linearVelocity, transform.forward);
            var radius = Mathf.Max(m_Vehicle.Profile.wheelRadiusMetres, 1e-4f);

            TurnedDegrees += alongTheRoad / radius * Mathf.Rad2Deg * Time.fixedDeltaTime;
        }

        /// <summary>
        /// Puts each wheel where the ground is, rather than where the bodywork has sunk to.
        ///
        /// Its own suspension ray, not an average: a cart with one wheel up a kerb is leaning, and
        /// four wheels sharing one height would have three of them in the air or in the tarmac.
        /// </summary>
        void HangThem()
        {
            var steer = Quaternion.Euler(0f, m_Vehicle.SteerAngleDegrees, 0f);
            var spin = Quaternion.Euler(TurnedDegrees, 0f, 0f);

            for (var i = 0; i < m_Wheels.Length; i++)
            {
                var wheel = m_Wheels[i];
                if (wheel == null)
                {
                    continue;
                }

                var resting = m_RestingAt[i];
                var standing = m_Vehicle.WheelCentreLocal(i, resting.y);

                wheel.localPosition = new Vector3(resting.x, standing, resting.z);
                wheel.localRotation = (m_Vehicle.WheelSteers(i) ? steer : Quaternion.identity) * spin;
            }
        }
    }
}
