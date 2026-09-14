using UnityEngine;

namespace BelowTheWing.Cargo
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public sealed class Bag : MonoBehaviour
    {
        [SerializeField, Tooltip("Profile this runs on")]
        BagProfile m_Profile;

        Rigidbody m_Body;
        BoxCollider m_Collider;
        PhysicsMaterial m_Grip;

        public BagProfile Profile => m_Profile;

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

        public void Configure(BagProfile profile)
        {
            m_Profile = profile != null ? profile : throw new System.ArgumentNullException(nameof(profile));

            m_Body = GetComponent<Rigidbody>();
            m_Body.mass = profile.massKg;

            m_Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            m_Body.interpolation = RigidbodyInterpolation.Interpolate;

            m_Body.sleepThreshold = 0f;

            m_Collider = GetComponent<BoxCollider>();
            m_Collider.size = profile.sizeMetres;
            m_Collider.center = Vector3.zero;

            if (m_Grip == null)
            {
                m_Grip = new PhysicsMaterial($"{profile.name} grip");
            }

            m_Grip.dynamicFriction = profile.frictionCoefficient;
            m_Grip.staticFriction = profile.frictionCoefficient;
            m_Grip.frictionCombine = PhysicsMaterialCombine.Minimum;

            m_Grip.bounciness = 0f;
            m_Grip.bounceCombine = PhysicsMaterialCombine.Minimum;
            m_Collider.material = m_Grip;
        }

        void OnDestroy()
        {
            if (m_Grip != null)
            {
                Destroy(m_Grip);
            }
        }

        void Awake()
        {
            if (m_Profile != null)
            {
                Configure(m_Profile);
            }
        }

        void Start()
        {
            if (m_Profile == null)
            {
                Debug.LogError(
                    $"'{name}' is a bag with no profile, so it weighs whatever its prefab happened to " +
                    "say -- usually one kilogram -- and will be thrown across the apron by anything " +
                    "that touches it.", this);
                enabled = false;
            }
        }
    }
}
