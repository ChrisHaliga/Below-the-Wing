using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Cargo
{
    /// <summary>
    /// Something things can ride on: a cart deck, a player's hands, later a pit or a belt.
    ///
    /// A carrier is not a floor. An object resting on it is attached to it rather than simulated
    /// against it -- it rides as part of the carrier until something shakes it loose. The naive
    /// version, a loose rigidbody sitting on a moving mesh, jitters and drifts and eventually
    /// launches for no reason a player can read, because the floor is moving into the object every
    /// step and contact resolution is pushing it back out.
    ///
    /// Carriers nest. A player holding a bag while riding a cart is a bag attached to a player
    /// attached to a cart, and shaking the player loose has to shake the bag loose with them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Carrier : MonoBehaviour
    {
        [SerializeField, Tooltip("The space an object has to come to rest in to be carried. " +
                                 "Centre is local to this carrier.")]
        Vector3 m_VolumeCentreLocal = Vector3.zero;

        [SerializeField, Tooltip("Width, height and length of that space in metres.")]
        Vector3 m_VolumeSizeMetres = new Vector3(1.4f, 0.6f, 2.8f);

        readonly List<Carried> m_Riding = new List<Carried>();

        Rigidbody m_Body;

        /// <summary>The body this carrier moves as, if it moves at all.</summary>
        public Rigidbody Body
        {
            get
            {
                if (m_Body == null)
                {
                    m_Body = GetComponentInParent<Rigidbody>();
                }

                return m_Body;
            }
        }

        /// <summary>Everything currently riding on this carrier.</summary>
        public IReadOnlyList<Carried> Riding => m_Riding;

        /// <summary>Sets the space an object has to be in to be picked up, for a hand or a deck.</summary>
        public void Covers(Vector3 centreLocal, Vector3 sizeMetres)
        {
            m_VolumeCentreLocal = centreLocal;
            m_VolumeSizeMetres = sizeMetres;
        }

        /// <summary>Whether this point is inside the space this carrier will take things in.</summary>
        public bool Reaches(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint) - m_VolumeCentreLocal;
            var half = m_VolumeSizeMetres * 0.5f;

            return Mathf.Abs(local.x) <= half.x
                   && Mathf.Abs(local.y) <= half.y
                   && Mathf.Abs(local.z) <= half.z;
        }

        /// <summary>
        /// How fast this carrier is moving at a particular point on it, in metres per second.
        ///
        /// At a point rather than overall, because the far end of a turning cart is travelling
        /// faster than the near end and a bag thrown off it should go where that corner of the deck
        /// was going. Unity's point velocity already includes the spin, which is the whole reason
        /// to ask it rather than work it out again.
        /// </summary>
        public Vector3 VelocityAt(Vector3 worldPoint)
            => Body != null ? Body.GetPointVelocity(worldPoint) : Vector3.zero;

        internal void NowCarrying(Carried carried)
        {
            if (!m_Riding.Contains(carried))
            {
                m_Riding.Add(carried);
            }
        }

        internal void NoLongerCarrying(Carried carried) => m_Riding.Remove(carried);

        /// <summary>
        /// Lets go of everything, handing each object the speed this carrier had where it was
        /// standing.
        ///
        /// Called when the carrier itself is going away. The velocity has to be read here, before
        /// it goes, because afterwards there is nothing left to ask -- and an object unparented
        /// without it hangs in the air exactly where the carrier used to be, which reads as cargo
        /// freezing in mid-flight rather than as a cart leaving.
        /// </summary>
        public void LetEverythingGo()
        {
            for (var i = m_Riding.Count - 1; i >= 0; i--)
            {
                // Anything already destroyed is nothing to hand on: the scene is being torn down
                // and its cargo has gone with it.
                if (m_Riding[i] != null)
                {
                    m_Riding[i].Orphan();
                }
            }

            m_Riding.Clear();
        }

        void OnDestroy() => LetEverythingGo();
    }
}
