using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// Keeping this machine's copy of a vehicle in step with the machine that owns it.
    ///
    /// What is particular to vehicles is that they come in trains. Only the vehicle at the front of
    /// a train is corrected; everything behind it is towed by the local copy of that vehicle through
    /// the hinges that already hold the train together, exactly as it is towed on the machine that
    /// owns it.
    ///
    /// Correcting each cart separately is what tears these trains apart. A chain is a set of
    /// constraints and a correction is a force applied to one body: told that cart three is thirty
    /// centimetres back and cart four twenty centimetres left, correction pushes each toward a place
    /// the hinge between them forbids, and the solver and the network take turns losing. Towing
    /// removes the argument -- there is one corrected body and the hinges distribute its motion the
    /// way they already know how to.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    [DisallowMultipleComponent]
    public sealed class VehicleMotion : MotionReplication
    {
        VehicleController m_Vehicle;

        void Awake() => m_Vehicle = GetComponent<VehicleController>();

        protected override Rigidbody Body => m_Vehicle.Body;

        /// <summary>
        /// Anything solid touching this vehicle leaves its whole train alone for a moment.
        ///
        /// Reported on the machine where the contact happened rather than replicated as an event.
        /// A collision between two players happens on both machines at slightly different moments
        /// and slightly differently, and each is entitled to resolve its own.
        /// </summary>
        void OnCollisionEnter(Collision other)
        {
            if (other.rigidbody != null && !other.rigidbody.isKinematic)
            {
                m_Vehicle.Chain.Struck();
            }
        }

        /// <summary>Whether this vehicle is the one in its train that gets corrected: the front one.</summary>
        bool TheOneWorthCorrecting => m_Vehicle.Chain != null && m_Vehicle.Chain.Leader == m_Vehicle;

        void FixedUpdate()
        {
            if (Body == null)
            {
                return;
            }

            // Read afresh every step rather than caught when it changes.
            //
            // Ownership is a question with a live answer, and catching it as an event gets it wrong
            // at the one moment it matters most: an object spawned by another machine runs its spawn
            // callback before ownership has been applied, reads itself as owned, and no later change
            // event arrives to correct it -- because from netcode's side nothing changed. That
            // machine then drives a vehicle it does not own and broadcasts where it went, which
            // netcode silently refuses, leaving an error in a log nobody reads.
            m_Vehicle.OursToMove = IsOwner;

            if (TheOneWorthCorrecting)
            {
                m_Vehicle.Chain.TickBlackout(Time.fixedDeltaTime);
            }

            if (IsOwner)
            {
                Report();
            }
            else if (TheOneWorthCorrecting)
            {
                KeepUp(m_Vehicle.Chain.Bodies, m_Vehicle.Chain.OwnersSay);
            }
        }
    }
}
