using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [RequireComponent(typeof(VehicleController))]
    [DisallowMultipleComponent]
    public sealed class WheelLook : MonoBehaviour
    {
        [SerializeField, Tooltip("Wheels: where each sits and how big it is")]
        Transform[] m_Wheels = new Transform[0];

        VehicleController m_Vehicle;

        Vector3[] m_RestingAt;

        Quaternion[] m_AuthoredFacing;

        float[] m_TurnedDegrees;

        public float TurnedDegrees(int corner)
            => m_TurnedDegrees != null && corner >= 0 && corner < m_TurnedDegrees.Length
                ? m_TurnedDegrees[corner]
                : 0f;

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

        void Remember()
        {
            if (m_RestingAt != null)
            {
                return;
            }

            m_RestingAt = new Vector3[m_Wheels.Length];
            m_AuthoredFacing = new Quaternion[m_Wheels.Length];
            m_TurnedDegrees = new float[m_Wheels.Length];

            var toVehicle = Quaternion.Inverse(transform.rotation);
            for (var i = 0; i < m_Wheels.Length; i++)
            {
                var wheel = m_Wheels[i];
                m_RestingAt[i] = wheel != null ? transform.InverseTransformPoint(wheel.position) : Vector3.zero;
                m_AuthoredFacing[i] = wheel != null ? toVehicle * wheel.rotation : Quaternion.identity;
            }
        }

        void TurnThem()
        {
            var alongTheRoad = Vector3.Dot(m_Vehicle.Body.linearVelocity, transform.forward);

            for (var i = 0; i < m_TurnedDegrees.Length; i++)
            {
                var radius = m_Vehicle.WheelRadiusMetres(i);
                if (radius <= 0f)
                {
                    continue;
                }

                m_TurnedDegrees[i] += alongTheRoad / radius * Mathf.Rad2Deg * Time.fixedDeltaTime;
            }
        }

        void HangThem()
        {
            var steer = Quaternion.Euler(0f, m_Vehicle.SteerAngleDegrees, 0f);

            for (var i = 0; i < m_Wheels.Length; i++)
            {
                var wheel = m_Wheels[i];
                if (wheel == null)
                {
                    continue;
                }

                var spin = Quaternion.Euler(m_TurnedDegrees[i], 0f, 0f);

                var resting = m_RestingAt[i];
                var standing = m_Vehicle.WheelCentreLocal(i, resting.y);

                wheel.position = transform.TransformPoint(new Vector3(resting.x, standing, resting.z));
                wheel.rotation = transform.rotation
                                 * (m_Vehicle.WheelSteers(i) ? steer : Quaternion.identity)
                                 * spin
                                 * m_AuthoredFacing[i];
            }
        }
    }
}
