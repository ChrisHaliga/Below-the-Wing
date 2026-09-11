using UnityEngine;

namespace BelowTheWing.Cargo
{
    /// <summary>
    /// Watches what a carrier is doing to the things riding on it, and lets go of the ones it can
    /// no longer hold.
    ///
    /// Put on the same object as a <see cref="Carrier"/>. Each step it works out how hard the
    /// carrier is throwing things sideways, how far it is leaning, and whether it has just been hit,
    /// then asks each rider whether that is more than it can take. Riders answer differently: a
    /// person holding a rail survives a corner that empties the deck of bags.
    ///
    /// Only where this machine is in charge. Waking has to be one machine's decision and everybody
    /// else's news, or a bag flies on one screen and rides on another -- which is a worse bug than a
    /// bag that flies two hundred milliseconds late.
    /// </summary>
    [RequireComponent(typeof(Carrier))]
    [DisallowMultipleComponent]
    public sealed class CarrierWatch : MonoBehaviour
    {
        /// <summary>How long an impact still counts as having just happened, in seconds.</summary>
        const float ImpactCountsForSeconds = 0.1f;

        Carrier m_Carrier;
        Rigidbody m_Body;
        Vector3 m_MovingAt;
        float m_LastImpact;
        float m_ImpactAt = -1f;

        /// <summary>
        /// Whether this machine decides what comes off. Set from whoever owns the carrier.
        ///
        /// Defaults to true so that a carrier with no networking above it -- a test, or a single
        /// player -- simply works.
        /// </summary>
        public bool OursToDecide { get; set; } = true;

        /// <summary>Sideways acceleration the carrier is pulling right now, in metres per second squared.</summary>
        public float LateralAcceleration { get; private set; }

        /// <summary>How far the carrier is leaning, in degrees.</summary>
        public float TiltDegrees => WakeRules.TiltDegrees(transform.rotation);

        void Awake()
        {
            m_Carrier = GetComponent<Carrier>();
            m_Body = m_Carrier.Body;
        }

        void OnCollisionEnter(Collision other)
        {
            m_LastImpact = other.impulse.magnitude;
            m_ImpactAt = Time.time;
        }

        void FixedUpdate()
        {
            MeasureWhatItIsDoing();

            if (!OursToDecide || m_Carrier.Riding.Count == 0)
            {
                return;
            }

            var impact = Time.time - m_ImpactAt <= ImpactCountsForSeconds ? m_LastImpact : 0f;
            var tilt = TiltDegrees;

            // Backwards, because waking a rider takes it out of the list.
            for (var i = m_Carrier.Riding.Count - 1; i >= 0; i--)
            {
                var riding = m_Carrier.Riding[i];
                if (riding == null)
                {
                    continue;
                }

                if (WakeRules.ShakenLoose(LateralAcceleration, tilt, impact, riding.ComesOffAt))
                {
                    riding.Wake(Time.time);
                }
            }
        }

        /// <summary>
        /// How hard the carrier is throwing things sideways, measured from how its own motion is
        /// changing rather than from what anybody asked it to do.
        ///
        /// Measured rather than asked for, because a cart being shoved sideways by another vehicle
        /// is throwing its load about just as surely as one taking a corner too fast, and nobody
        /// steered it.
        /// </summary>
        void MeasureWhatItIsDoing()
        {
            if (m_Body == null)
            {
                m_Body = m_Carrier.Body;
                return;
            }

            var now = m_Body.linearVelocity;
            var change = (now - m_MovingAt) / Mathf.Max(Time.fixedDeltaTime, 1e-5f);
            m_MovingAt = now;

            // Sideways only. Speeding up and slowing down are not what tips a load off a flat deck;
            // being swung is.
            LateralAcceleration = Vector3.ProjectOnPlane(change, transform.up).magnitude
                                  - Mathf.Abs(Vector3.Dot(change, transform.forward));

            LateralAcceleration = Mathf.Max(0f, LateralAcceleration);
        }
    }
}
