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

        [SerializeField, Tooltip("Whether things taken aboard are moved to the middle of this " +
                                 "carrier rather than left where they were.")]
        bool m_HoldsAtItsCentre;

        readonly List<Carried> m_Riding = new List<Carried>();

        Rigidbody m_Body;
        Carried m_RidingOnSomethingElse;

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

        /// <summary>
        /// Whether this carrier puts what it takes in one particular spot.
        ///
        /// True of hands: a bag picked up goes into them, not to wherever the player was standing
        /// when they reached for it. False of a deck, where a bag stays exactly where it came to
        /// rest, because a deck full of bags all stacked in the middle is not a loaded cart.
        ///
        /// It matters most where nobody is watching. A machine told over the network that a bag is
        /// now in somebody's hands has to put it in their hands; leaving it where its own copy
        /// happened to be would strand it a few metres from the player holding it, for good, since
        /// nothing steers something that is being carried.
        /// </summary>
        public bool HoldsAtItsCentre => m_HoldsAtItsCentre;

        /// <summary>Sets the space an object has to be in to be picked up, for a hand or a deck.</summary>
        public void Covers(Vector3 centreLocal, Vector3 sizeMetres, bool holdsAtItsCentre = false)
        {
            m_VolumeCentreLocal = centreLocal;
            m_VolumeSizeMetres = sizeMetres;
            m_HoldsAtItsCentre = holdsAtItsCentre;
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
        {
            if (m_RidingOnSomethingElse == null)
            {
                m_RidingOnSomethingElse = GetComponentInParent<Carried>();
            }

            // A carrier that is itself riding on something has a kinematic body, and a kinematic
            // body reports no velocity at all -- it is being moved by being parented, not by
            // physics. So it passes the question up: a player standing on a cart is moving at the
            // speed of the cart under them, and a bag thrown from their hands has to know that.
            //
            // This is the nested case, and it is the one worth getting right first. A bag held by
            // somebody riding a cart is the most ordinary thing a player will do, and it is where
            // two chains of attachment have to come apart in the right order.
            if (m_RidingOnSomethingElse != null && m_RidingOnSomethingElse.Attached)
            {
                return m_RidingOnSomethingElse.On.VelocityAt(worldPoint);
            }

            return Body != null ? Body.GetPointVelocity(worldPoint) : Vector3.zero;
        }

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
