using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Apron
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class AircraftBody : MonoBehaviour
    {
        [SerializeField, Tooltip("Profile this runs on")]
        AircraftProfile m_Profile;

        public AircraftProfile Profile => m_Profile;

        void Awake()
        {
            if (m_Profile == null)
            {
                throw MisbuiltException.Refuse(
                    this,
                    "has no aircraft profile, so nothing says what it weighs");
            }

            if (GetComponentInChildren<Collider>(true) == null)
            {
                throw MisbuiltException.Refuse(
                    this,
                    "has no collider anywhere inside it, so crew and vehicles would drive straight " +
                    "through the aircraft they are loading");
            }

            var body = GetComponent<Rigidbody>();
            body.mass = m_Profile.massKg;
            body.isKinematic = true;
        }
    }
}
