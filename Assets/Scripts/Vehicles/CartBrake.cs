using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [RequireComponent(typeof(VehicleController))]
    [DisallowMultipleComponent]
    public sealed class CartBrake : MonoBehaviour
    {
        [SerializeField, Tooltip("How hard the brake holds, m/s^2")]
        float m_HoldsAtMetresPerSecondSquared = 6f;

        [SerializeField, Tooltip("How fast the hitch swings, degrees/s")]
        float m_HitchSwingsDegreesPerSecond = 120f;

        readonly CartParking m_Parking = new CartParking();

        VehicleController m_Cart;
        Transform m_Hitch;
        Collider m_PullOn;
        float m_HitchAt;

        public bool Parked => m_Parking.Parked;

        public bool Toggle() => m_Parking.Toggle(Body.linearVelocity.magnitude);

        Rigidbody Body => m_Cart.Body;

        public void SwingsThis(Transform hitch)
        {
            m_Hitch = hitch;
            m_PullOn = hitch != null ? hitch.GetComponentInChildren<Collider>() : null;
        }

        void Awake() => m_Cart = GetComponent<VehicleController>();

        bool HitchedToSomething()
        {
            var joint = GetComponent<Joint>();

            return joint != null && joint.connectedBody != null;
        }

        void FixedUpdate()
        {
            m_HitchAt = m_Parking.HitchDegrees(
                Time.fixedDeltaTime, m_HitchAt, m_HitchSwingsDegreesPerSecond);

            if (m_Hitch != null)
            {
                m_Hitch.localRotation = Quaternion.Euler(-m_HitchAt, 0f, 0f);
            }

            if (m_PullOn != null)
            {
                m_PullOn.enabled = Drawbar.CanBePulled(m_Parking.Parked, HitchedToSomething());
            }

            if (!m_Parking.Parked)
            {
                return;
            }

            var rolling = new Vector3(Body.linearVelocity.x, 0f, Body.linearVelocity.z);

            if (rolling.sqrMagnitude < 1e-6f)
            {
                return;
            }

            var holding = m_Parking.BrakingForce(
                rolling.magnitude, Body.mass, m_HoldsAtMetresPerSecondSquared);

            Body.AddForce(-rolling.normalized * holding, ForceMode.Force);
        }
    }
}
