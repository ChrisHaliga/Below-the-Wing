using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Session
{
    [RequireComponent(typeof(VehicleController))]
    [DisallowMultipleComponent]
    public sealed class VehicleMotion : MotionReplication
    {
        VehicleController m_Vehicle;

        void Awake() => m_Vehicle = GetComponent<VehicleController>();

        protected override Rigidbody Body => m_Vehicle.Body;

        void OnCollisionEnter(Collision other)
        {
            if (other.rigidbody != null && !other.rigidbody.isKinematic)
            {
                m_Vehicle.Chain.NoteCollision();
            }
        }

        public bool TheOneWorthCorrecting => m_Vehicle.Chain != null && m_Vehicle.Chain.Leader == m_Vehicle;

        void FixedUpdate()
        {
            if (Body == null)
            {
                return;
            }

            m_Vehicle.OursToMove = IsOwner;

            if (TheOneWorthCorrecting)
            {
                m_Vehicle.Chain.TickBlackout(Time.fixedDeltaTime);
            }

            if (IsOwner)
            {
                Report();
            }
            else if (TheOneWorthCorrecting)
            {
                KeepUp(m_Vehicle.Chain.CorrectionAuthority);
            }
        }
    }
}
