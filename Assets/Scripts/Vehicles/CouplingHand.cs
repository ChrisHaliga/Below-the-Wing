using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public enum CouplingPrompt
    {
        None,

        OfferToHitch,

        OfferToUnhitch,

        Refused
    }

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

        public CartChain Driving { get; set; }

        public CouplingPrompt Prompt { get; private set; } = CouplingPrompt.None;

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

        public VehicleController Offered => m_Offered;

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

                var standing = Coupling.WhereToStand(Driving.Members[Driving.Members.Count - 1], wanted);
                if (standing == null)
                {
                    Prompt = CouplingPrompt.Refused;
                    return;
                }

                var (position, rotation) = standing.Value;

                wanted.transform.SetPositionAndRotation(position, rotation);
                wanted.Body.position = position;
                wanted.Body.rotation = rotation;

                wanted.Body.linearVelocity = Driving.Leader.Body.linearVelocity;
                wanted.Body.angularVelocity = Vector3.zero;

                Prompt = CouplingPrompt.None;
                m_Hitched?.Invoke(joining);
            });
        }

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

            m_Broker.HandBack(dropped);

            Prompt = CouplingPrompt.None;
            m_Split?.Invoke(staying, dropped);
        }

        public void UnhitchTheBack()
            => UnhitchAfter(Driving == null ? -1 : Driving.Members.Count - 2);
    }
}
