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
                    "has no aircraft profile, so it is not the size of an aircraft and everything parks relative to it");
            }

            var body = GetComponent<Rigidbody>();
            body.mass = m_Profile.massKg;
            body.isKinematic = true;

            var fuselage = GetComponent<CapsuleCollider>();
            if (fuselage == null)
            {
                fuselage = gameObject.AddComponent<CapsuleCollider>();
            }

            fuselage.radius = m_Profile.fuselageDiameterMetres * 0.5f;
            fuselage.height = m_Profile.lengthMetres;

            fuselage.direction = 2;
            fuselage.center = Vector3.zero;
        }
    }
}
