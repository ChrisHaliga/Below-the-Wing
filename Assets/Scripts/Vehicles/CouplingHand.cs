using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>What a player is currently being offered at the back of a train.</summary>
    public enum CouplingPrompt
    {
        /// <summary>Nothing in reach worth hooking on or unhooking.</summary>
        None,

        /// <summary>A free cart is close enough to be hitched to the back of the train.</summary>
        OfferToHitch,

        /// <summary>The train being driven has something on the back that could be dropped.</summary>
        OfferToUnhitch,

        /// <summary>A cart was asked for and could not be had.</summary>
        Refused
    }

    /// <summary>
    /// Hooking a cart on and dropping one off, from the point of view of the player doing it.
    ///
    /// Both are ownership events before they are physical ones. Hitching makes one longer train out
    /// of two things that were owned separately, so the whole resulting chain has to be taken at
    /// once -- a train with a cart on the back that some other machine is still simulating has a
    /// coupling with one end on each side of an authority boundary, which is the thing this game
    /// goes to some trouble to prevent. Dropping carts off leaves a train behind that this player is
    /// no longer driving, so it is handed back rather than kept.
    ///
    /// A plain object rather than a component, so the whole sequence -- including being refused, the
    /// case that is hardest to arrange on purpose and most likely to be wrong -- can be driven
    /// directly by a test.
    /// </summary>
    public sealed class CouplingHand
    {
        readonly IOwnershipBroker m_Broker;
        readonly Func<IReadOnlyList<VehicleController>> m_Nearby;
        readonly Action<IReadOnlyList<VehicleController>> m_Hitched;
        readonly Action<IReadOnlyList<VehicleController>, IReadOnlyList<VehicleController>> m_Split;

        VehicleController m_Offered;
        bool m_Asking;

        public CouplingHand(
            IOwnershipBroker broker,
            Func<IReadOnlyList<VehicleController>> nearby,
            Action<IReadOnlyList<VehicleController>> hitched = null,
            Action<IReadOnlyList<VehicleController>, IReadOnlyList<VehicleController>> split = null)
        {
            m_Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            m_Nearby = nearby ?? throw new ArgumentNullException(nameof(nearby));
            m_Hitched = hitched;
            m_Split = split;
        }

        /// <summary>The train the player is currently driving, or null if they are on foot.</summary>
        public CartChain Driving { get; set; }

        /// <summary>What to tell the player right now.</summary>
        public CouplingPrompt Prompt { get; private set; } = CouplingPrompt.None;

        /// <summary>Acts on whatever is currently being offered, which is what one key press means.</summary>
        public void Act()
        {
            switch (Prompt)
            {
                case CouplingPrompt.OfferToHitch:
                    Hitch();
                    break;
                case CouplingPrompt.OfferToUnhitch:
                    UnhitchTheBack();
                    break;
            }
        }

        /// <summary>The cart that would be hitched if they accepted. Null when nothing is offered.</summary>
        public VehicleController Offered => m_Offered;

        /// <summary>Works out what is on offer. Called every step while the player is driving.</summary>
        public void Refresh()
        {
            if (Driving == null || m_Asking)
            {
                return;
            }

            m_Offered = Coupling.WorthHitching(Driving, m_Nearby());

            if (m_Offered != null)
            {
                Prompt = CouplingPrompt.OfferToHitch;
            }
            else if (Driving.Members.Count > 1)
            {
                Prompt = CouplingPrompt.OfferToUnhitch;
            }
            else
            {
                Prompt = CouplingPrompt.None;
            }
        }

        /// <summary>
        /// Hooks the offered cart onto the back of the train being driven.
        ///
        /// The cart is asked for first and moved into place second. Moving it before the answer
        /// arrives would shove a vehicle another machine is still simulating, and that machine would
        /// simply put it back.
        /// </summary>
        public void Hitch()
        {
            if (Driving == null || m_Offered == null || m_Asking)
            {
                return;
            }

            var wanted = m_Offered;
            var joining = new List<VehicleController>(Driving.Members) { wanted };
            m_Asking = true;

            m_Broker.RequestAll(joining, granted =>
            {
                m_Asking = false;

                if (!granted)
                {
                    Prompt = CouplingPrompt.Refused;
                    return;
                }

                // Put into place before anything hooks it up, so the coupling is made with its two
                // ends already touching and has nothing to pull against.
                var (position, rotation) = Coupling.WhereToStand(
                    Driving.Members[Driving.Members.Count - 1], wanted);

                wanted.transform.SetPositionAndRotation(position, rotation);
                wanted.Body.position = position;
                wanted.Body.rotation = rotation;

                // Given the train's speed so it is not left standing while the train drives off,
                // which would be a coupling asked to accelerate half a tonne in one step.
                wanted.Body.linearVelocity = Driving.Leader.Body.linearVelocity;
                wanted.Body.angularVelocity = Vector3.zero;

                // Saying who belongs to the train is the whole job. Building the chain and hanging
                // the hinges is the register's, on every machine, from the membership it is told --
                // doing it here as well would leave two chains and two sets of hinges on the same
                // vehicles, with nothing holding a reference to the first.
                Prompt = CouplingPrompt.None;
                m_Hitched?.Invoke(joining);
            });
        }

        /// <summary>
        /// Drops everything behind the given member, leaving the player driving the front half.
        ///
        /// What is dropped is handed back rather than kept. A player who uncouples four carts and
        /// drives away is not driving those carts any more, and a machine that goes on simulating
        /// them is holding equipment nobody can take from it.
        /// </summary>
        public void UnhitchAfter(int memberIndex)
        {
            if (!Coupling.CanBeSplitAfter(Driving, memberIndex))
            {
                return;
            }

            var members = Driving.Members;
            var staying = new List<VehicleController>(members.Count);
            var dropped = new List<VehicleController>(members.Count);

            for (var i = 0; i < members.Count; i++)
            {
                (i <= memberIndex ? staying : dropped).Add(members[i]);
            }

            // Again, said rather than done. The register rebuilds both trains from the new
            // membership and takes the coupling at the break off with the chain it belonged to.
            m_Broker.HandBack(dropped);

            Prompt = CouplingPrompt.None;
            m_Split?.Invoke(staying, dropped);
        }

        /// <summary>Drops the last vehicle off the back, which is what a single key press means.</summary>
        public void UnhitchTheBack()
            => UnhitchAfter(Driving == null ? -1 : Driving.Members.Count - 2);
    }
}
