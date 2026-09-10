using UnityEngine;

namespace BelowTheWing.Apron
{
    /// <summary>
    /// The aircraft as a physical object on the apron.
    ///
    /// Kinematic, permanently. Forty tonnes of airframe barely moves under anything that happens on
    /// the ramp, and simulating it would cost a great deal to achieve almost nothing. It is here to
    /// occupy space, to be driven into, and eventually to be loaded -- not to be pushed about.
    ///
    /// Shaped as a capsule lying on its side, which at this stage is exactly as much fuselage as
    /// anybody needs.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class AircraftBody : MonoBehaviour
    {
        [SerializeField, Tooltip("The real aircraft this stands for.")]
        AircraftProfile m_Profile;

        /// <summary>The real aircraft this stands for, and where its size comes from.</summary>
        public AircraftProfile Profile => m_Profile;

        void Awake()
        {
            if (m_Profile == null)
            {
                return;
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

            // Direction 2 is the local z axis, which lays the capsule down nose to tail rather than
            // stood on its end.
            fuselage.direction = 2;
            fuselage.center = Vector3.zero;
        }
    }
}
