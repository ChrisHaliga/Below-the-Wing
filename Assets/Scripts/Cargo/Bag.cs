using UnityEngine;

namespace BelowTheWing.Cargo
{
    /// <summary>
    /// One piece of baggage.
    ///
    /// The simplest object in the game and the most numerous, which is the whole design of it. Forty
    /// of these end up in carts, in a pit and on the concrete, on every machine in the session, so
    /// everything about a bag is chosen to cost as little as possible: a single box, one profile
    /// asset, no components it can do without.
    ///
    /// A bag configures itself from its profile the way a vehicle does, on every machine, rather
    /// than trusting whatever the prefab was saved with. A bag that arrives at the wrong weight on
    /// one machine is thrown a different distance there, and nothing about it looks wrong until two
    /// players disagree about where it landed.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public sealed class Bag : MonoBehaviour
    {
        [SerializeField, Tooltip("What this bag weighs and how big it is.")]
        BagProfile m_Profile;

        Rigidbody m_Body;
        BoxCollider m_Collider;
        PhysicsMaterial m_Grip;

        /// <summary>What this bag's weight and size come from.</summary>
        public BagProfile Profile => m_Profile;

        /// <summary>The body this bag is thrown about as.</summary>
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

        /// <summary>Gives this bag the weight, size and grip its profile describes.</summary>
        public void Configure(BagProfile profile)
        {
            m_Profile = profile != null ? profile : throw new System.ArgumentNullException(nameof(profile));

            m_Body = GetComponent<Rigidbody>();
            m_Body.mass = profile.massKg;

            // A bag is small, light, and thrown hard at things much heavier than itself, which is
            // the exact case a discrete collision check misses: at twelve metres a second it moves
            // most of its own length in one step.
            m_Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            m_Body.interpolation = RigidbodyInterpolation.Interpolate;

            m_Collider = GetComponent<BoxCollider>();
            m_Collider.size = profile.sizeMetres;
            m_Collider.center = Vector3.zero;

            // The bag's grip wins over the deck's. A steel deck has a friction of its own, and
            // averaging the two would make every bag half as slippery as its profile says.
            if (m_Grip == null)
            {
                m_Grip = new PhysicsMaterial($"{profile.name} grip");
            }

            m_Grip.dynamicFriction = profile.frictionCoefficient;
            m_Grip.staticFriction = profile.frictionCoefficient;
            m_Grip.frictionCombine = PhysicsMaterialCombine.Minimum;

            // A soft bag does not bounce, whatever it lands on. Said with the mode that beats a
            // vehicle's averaging, or a bag thrown into a cart springs off the far lip and out.
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
