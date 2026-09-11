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
    /// Three states, and the third is the one that gets forgotten. <b>Attached</b>: kinematic, not
    /// simulated, moved each step to keep the spot it took on its carrier. <b>Woken</b>: shaken
    /// loose because a threshold was crossed, thrown into the world with the speed it actually had.
    /// <b>Orphaned</b>: let go because the carrier itself ceased to exist. Orphaning is deliberately
    /// not waking -- nothing was crossed, nobody should be told a threshold fired, and anything
    /// later hung off waking must not run.
    ///
    /// Riding is a pose held each step rather than a change of parent. Parenting looks like the
    /// obvious way to do it and cannot be used: everything that gets carried here is a networked
    /// object, and the networking layer refuses to let any machine but the object's owner re-parent
    /// one. Since the whole point is that a bag rides the same cart on every screen, and only one of
    /// those machines owns it, re-parenting would fail on all the others -- and fail loudly, in a
    /// log, while the bag carried on lying in the road.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class Carried : MonoBehaviour
    {
        Rigidbody m_Body;
        float m_CannotSettleUntil;

        /// <summary>Where on its carrier this sits, in the carrier's own frame.</summary>
        Vector3 m_SpotTaken;

        /// <summary>Which way it faces relative to its carrier.</summary>
        Quaternion m_FacingTaken = Quaternion.identity;

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

        /// <summary>
        /// What it takes to shake this particular thing loose.
        ///
        /// Carried by the thing itself rather than by whatever is carrying it, because a bag and a
        /// person riding the same cart do not come off at the same moment -- and neither should a
        /// light case and a heavy one.
        /// </summary>
        public WakeThresholds ComesOffAt { get; set; } =
            new WakeThresholds(lateralAcceleration: 6f, tiltDegrees: 25f, impulse: 400f);

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

            m_SpotTaken = carrier.HoldsAtItsCentre
                ? Vector3.zero
                : carrier.transform.InverseTransformPoint(transform.position);
            m_FacingTaken = Quaternion.Inverse(carrier.transform.rotation) * transform.rotation;

            // Put there outright rather than moved there. A kinematic body asked to move arrives at
            // the next physics step, and anything that looks at where the thing is before then --
            // a pair of hands checking what it is holding, a deck checking its load is on it --
            // would be told it is still lying where it was picked up from.
            PlaceOnTheSpotTaken();
        }

        /// <summary>Where this sits right now, given where its carrier has got to.</summary>
        Vector3 SpotInTheWorld => On.transform.TransformPoint(m_SpotTaken);

        /// <summary>Which way it faces right now.</summary>
        Quaternion FacingInTheWorld => On.transform.rotation * m_FacingTaken;

        void PlaceOnTheSpotTaken()
        {
            transform.SetPositionAndRotation(SpotInTheWorld, FacingInTheWorld);

            // The body and the transform are two records of the same thing and writing one does not
            // update the other until physics next runs.
            Body.position = transform.position;
            Body.rotation = transform.rotation;
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
                RideAlong();
            }
        }

        /// <summary>
        /// Puts this back on the spot it took, wherever its carrier has got to.
        ///
        /// Moved rather than placed, so the physics engine carries it there over the step and
        /// anything it runs into is pushed rather than passed through.
        ///
        /// Aimed at where the carrier is about to be, not where it is. Every script's fixed step
        /// runs before the solver moves anything, so the carrier is still standing at the start of
        /// the step when this is read; aiming there would land this object exactly one step behind
        /// for as long as the carrier keeps moving. At eight metres a second that is a bag sitting
        /// sixteen centimetres back from where it was put, sliding further back the faster the cart
        /// goes and snapping into place whenever it stops.
        /// </summary>
        void RideAlong()
        {
            Body.MovePosition(SpotInTheWorld + (m_RidingAt * Time.fixedDeltaTime));
            Body.MoveRotation(FacingInTheWorld);
        }

        void LetGo()
        {
            On.NoLongerCarrying(this);
            On = null;

            if (Body != null)
            {
                Body.isKinematic = false;
            }
        }
    }
}
