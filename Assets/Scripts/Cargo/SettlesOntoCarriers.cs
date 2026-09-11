using UnityEngine;

namespace BelowTheWing.Cargo
{
    /// <summary>
    /// Notices when a loose object has come to rest on something that carries things, and puts it
    /// aboard.
    ///
    /// This is what makes a bag authored sitting on a cart deck, a bag thrown into one, and a bag
    /// that bounced off and landed in the next cart back all arrive by the same route. There is no
    /// separate path for cargo that was already there when the scene loaded.
    ///
    /// At rest first, for objects. A bag skidding across a deck is still in the middle of doing
    /// something and grabbing it mid-slide freezes it in a pose nobody would expect. People are the
    /// exception and are handled where riding is.
    /// </summary>
    [RequireComponent(typeof(Carried))]
    [DisallowMultipleComponent]
    public sealed class SettlesOntoCarriers : MonoBehaviour
    {
        [SerializeField, Tooltip("Below this speed the object counts as having come to rest, in " +
                                 "metres per second.")]
        float m_AtRestBelow = 0.15f;

        [SerializeField, Tooltip("How far below itself to look for something to settle onto, in metres.")]
        float m_LooksDownMetres = 0.4f;

        [SerializeField, Tooltip("What might be a carrier. Everything, by default.")]
        LayerMask m_CouldCarryIt = ~0;

        Carried m_Carried;

        /// <summary>
        /// Whether this machine decides that the object has come to rest on something. Set from
        /// whoever owns it.
        ///
        /// Every machine judging separately is how the same bag ends up aboard a different cart on
        /// each screen, and there is no force that would ever pull those two answers back together.
        ///
        /// Defaults to true so that an object with no networking above it -- a test, or a single
        /// player -- simply works.
        /// </summary>
        public bool OursToDecide { get; set; } = true;

        void Awake() => m_Carried = GetComponent<Carried>();

        void FixedUpdate()
        {
            if (!OursToDecide || m_Carried.Attached || !m_Carried.WouldSettle(Time.time))
            {
                return;
            }

            if (m_Carried.Body.linearVelocity.magnitude > m_AtRestBelow)
            {
                return;
            }

            var carrier = WhatItIsSittingOn();
            if (carrier != null && carrier.Reaches(transform.position))
            {
                m_Carried.AttachTo(carrier);
            }
        }

        /// <summary>
        /// Whatever carrier is directly underneath, or null.
        ///
        /// Found by looking down rather than by asking what it is touching, because an object can
        /// be leaning against the side of a cart without being on it, and a bag wedged against a
        /// wheel is not cargo.
        /// </summary>
        Carrier WhatItIsSittingOn()
        {
            if (!Physics.Raycast(transform.position, Vector3.down, out var hit, m_LooksDownMetres,
                    m_CouldCarryIt, QueryTriggerInteraction.Ignore))
            {
                return null;
            }

            return hit.collider.GetComponentInParent<Carrier>();
        }
    }
}
