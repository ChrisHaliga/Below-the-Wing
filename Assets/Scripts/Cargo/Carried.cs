using UnityEngine;

namespace BelowTheWing.Cargo
{
    /// <summary>What it takes to shake one particular thing off whatever it is riding on.</summary>
    public readonly struct WakeThresholds
    {
        /// <summary>Sideways acceleration at the object's own position, in metres per second squared.</summary>
        public readonly float LateralAcceleration;

        /// <summary>How far the carrier may lean, in degrees.</summary>
        public readonly float TiltDegrees;

        /// <summary>How hard the carrier has to be hit, in newton seconds.</summary>
        public readonly float Impulse;

        public WakeThresholds(float lateralAcceleration, float tiltDegrees, float impulse)
        {
            LateralAcceleration = lateralAcceleration;
            TiltDegrees = tiltDegrees;
            Impulse = impulse;
        }

        /// <summary>The same thresholds with the acceleration one raised, for a rider holding on.</summary>
        public WakeThresholds HoldingOn(float multiple)
            => new WakeThresholds(LateralAcceleration * multiple, TiltDegrees * multiple, Impulse);
    }

    /// <summary>
    /// Whether a thing is riding on something, and what it takes to shake it loose.
    ///
    /// One mechanism for everything that is ever carried: bags, players, loose equipment. They
    /// differ by the numbers in their thresholds and by what they may do while attached, not by
    /// having systems of their own -- three systems for the same idea is three sets of rules that
    /// will disagree with each other at the worst moment.
    ///
    /// Three states, and the third is the one that gets forgotten. <b>Attached</b>: kinematic,
    /// parented, not simulated, costing nothing. <b>Woken</b>: shaken loose because a threshold was
    /// crossed, thrown into the world with the speed it actually had. <b>Orphaned</b>: let go
    /// because the carrier itself ceased to exist. Orphaning is deliberately not waking -- nothing
    /// was crossed, nobody should be told a threshold fired, and anything later hung off waking must
    /// not run.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class Carried : MonoBehaviour
    {
        Rigidbody m_Body;
        float m_CannotSettleUntil;

        /// <summary>
        /// How fast the carrier was moving, here, as of the last step.
        ///
        /// Remembered rather than asked for at the moment it is wanted. A carrier that is going
        /// away has usually gone by the time anything notices -- its rigidbody is destroyed before
        /// or during the same call -- and asking a destroyed body how fast it was moving answers
        /// zero. Cargo then stops dead in mid-air exactly where the cart used to be, which reads as
        /// a physics glitch rather than as a cart leaving.
        /// </summary>
        Vector3 m_RidingAt;

        /// <summary>What this is riding on, or null if it is loose in the world.</summary>
        public Carrier On { get; private set; }

        /// <summary>Whether this is currently riding on something.</summary>
        public bool Attached => On != null;

        /// <summary>How long after being shaken loose before this may settle onto a carrier again.</summary>
        public float CannotSettleForSeconds { get; set; } = 1f;

        /// <summary>The body this is thrown about as.</summary>
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
        /// Whether this would settle onto a carrier right now.
        ///
        /// The wait after being shaken loose is what stops a bag flung off on a corner sticking
        /// straight back down before the same corner has finished.
        /// </summary>
        public bool WouldSettle(float now) => !Attached && now >= m_CannotSettleUntil;

        /// <summary>Starts riding on this carrier, at whatever pose it currently has.</summary>
        public void AttachTo(Carrier carrier)
        {
            if (carrier == null || Attached)
            {
                return;
            }

            On = carrier;
            m_RidingAt = carrier.VelocityAt(transform.position);
            carrier.NowCarrying(this);

            // Not simulated while carried. It is part of the carrier, so the solver has nothing to
            // work out about it and nothing to get wrong.
            Body.isKinematic = true;
            transform.SetParent(carrier.transform, worldPositionStays: true);
        }

        /// <summary>
        /// Shaken loose, with the speed it had where it was standing plus anything of its own --
        /// a throw, or a jump.
        /// </summary>
        public void Wake(float now, Vector3 ofItsOwn = default)
        {
            if (!Attached)
            {
                return;
            }

            var carriedAt = On.VelocityAt(transform.position);

            LetGo();

            Body.linearVelocity = carriedAt + ofItsOwn;
            m_CannotSettleUntil = now + CannotSettleForSeconds;
        }

        /// <summary>
        /// Let go because the carrier is going away, rather than because anything was crossed.
        ///
        /// Keeps the speed the carrier had, so a cart despawning at six metres a second leaves its
        /// bags travelling at six metres a second rather than hanging where it was. Deliberately
        /// silent: no threshold fired, so nothing that listens for one should hear anything.
        /// </summary>
        public void Orphan()
        {
            if (!Attached)
            {
                return;
            }

            // The remembered speed, not a fresh reading. By now the carrier is usually already
            // gone, and a destroyed body reports nothing.
            var carriedAt = m_RidingAt;

            LetGo();

            Body.linearVelocity = carriedAt;
        }

        void FixedUpdate()
        {
            if (Attached)
            {
                m_RidingAt = On.VelocityAt(transform.position);
            }
        }

        void LetGo()
        {
            On.NoLongerCarrying(this);
            On = null;

            transform.SetParent(null, worldPositionStays: true);

            if (Body != null)
            {
                Body.isKinematic = false;
            }
        }
    }
}
