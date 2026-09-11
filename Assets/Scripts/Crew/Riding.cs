using BelowTheWing.Cargo;
using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// A player standing on something that moves.
    ///
    /// Landing on a cart deck attaches them to it, so they ride rather than being bounced about by
    /// a floor that is moving into them every step. While attached they can still walk, within the
    /// footprint of what they are standing on, and look wherever they like.
    ///
    /// Holding on raises what it takes to throw them off. It occupies the hands, which is the whole
    /// point of it: riding becomes a trade the player makes -- carry a bag or keep your grip -- and
    /// something for the driver and the passenger to shout at each other about, rather than a rule
    /// the game quietly enforces.
    /// </summary>
    public sealed class Riding
    {
        readonly CrewCharacter m_Crew;
        readonly Carried m_Rider;

        public Riding(CrewCharacter crew, Carried rider)
        {
            m_Crew = crew;
            m_Rider = rider;
        }

        /// <summary>How much harder it is to shake off a rider who is holding on.</summary>
        public float GripMultiplier { get; set; } = 3f;

        /// <summary>Whether they are currently riding something.</summary>
        public bool Attached => m_Rider != null && m_Rider.Attached;

        /// <summary>
        /// Starts riding whatever they have landed on, if it will take them.
        ///
        /// No requirement to stand still first. A bag has to settle before it counts as cargo; a
        /// person standing on a deck is riding it immediately, because they are not going to oblige
        /// by keeping still and being flung off for moving is not a rule anybody would accept.
        /// </summary>
        public void LandedOn(Carrier carrier, float now)
        {
            if (carrier == null || Attached || m_Rider == null || !m_Rider.WouldSettle(now))
            {
                return;
            }

            if (carrier.Reaches(m_Crew.transform.position))
            {
                m_Rider.AttachTo(carrier);
            }
        }

        /// <summary>
        /// What it takes to shake this rider off, given whether they are holding on.
        ///
        /// Being hit is not made survivable by a better grip. Holding a rail through a head-on
        /// collision and staying put would read as the game ignoring the crash, which is the one
        /// thing slice 2 spent its length making sure could not happen.
        /// </summary>
        public WakeThresholds Thresholds(in WakeThresholds standing, bool holdingOn)
            => holdingOn ? standing.HoldingOn(GripMultiplier) : standing;

        /// <summary>
        /// Steps off, keeping whatever the carrier was doing.
        ///
        /// Walking off an edge is not being thrown off. Nothing was crossed, so there is no wake
        /// event and nothing downstream of one should fire -- but the velocity still comes along,
        /// because somebody who walks off the back of a moving cart is already moving.
        /// </summary>
        public void SteppedOff(float now)
        {
            if (Attached)
            {
                m_Rider.Wake(now);
            }
        }

        /// <summary>
        /// Whether a rider has wandered off the edge of what they are standing on.
        ///
        /// Asked of the carrier rather than worked out here, so a deck, a pit floor and a belt all
        /// answer the same question about their own shape.
        /// </summary>
        public bool WalkedOffTheEdge()
            => Attached && !m_Rider.On.Reaches(m_Crew.transform.position);
    }
}
