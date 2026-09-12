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
    [RequireComponent(typeof(Carried))]
    [DisallowMultipleComponent]
    public sealed class Bag : MonoBehaviour
    {
        [SerializeField, Tooltip("What this bag weighs and what it takes to shake it loose.")]
        BagProfile m_Profile;

        Rigidbody m_Body;
        BoxCollider m_Collider;

        /// <summary>What this bag's weight, size and thresholds come from.</summary>
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

        /// <summary>
        /// Gives this bag the weight and size its profile describes, and tells the thing that
        /// carries it what it takes to shake this bag loose.
        ///
        /// The thresholds are applied here rather than left on the carried component's defaults,
        /// because a profile whose numbers reach nothing is a set of dials that turn nothing. The
        /// shipped bag ran on hardcoded thresholds that happened to match its profile, and the first
        /// retune would have been made, saved, and had no effect at all.
        /// </summary>
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

            var carried = GetComponent<Carried>();
            carried.ComesOffAt = new WakeThresholds(
                profile.wakeAtLateralAcceleration, profile.wakeAtTiltDegrees, profile.wakeAtImpactImpulse);
            carried.CannotSettleForSeconds = profile.cannotSettleForSeconds;
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
