using System;
using BelowTheWing.Cargo;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Crew
{
    public sealed class VehicleOccupancy : IOfferSomething
    {
        const float DismountClearanceMetres = 1f;

        const float StandingClearanceMetres = 0.05f;

        readonly Transform m_Crew;
        readonly IOwnershipBroker m_Broker;
        readonly Func<Ray> m_Aim;
        readonly float m_ReachMetres;
        readonly float m_ConeDegrees;

        VehicleController m_Driving;

        VehicleController m_Refused;

        bool m_Asking;

        VehicleController m_AskedFor;

        public VehicleOccupancy(
            Transform crew, IOwnershipBroker broker, Func<Ray> aim, float reachMetres, float coneDegrees)
        {
            m_Crew = crew != null ? crew : throw new ArgumentNullException(nameof(crew));
            m_Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            m_Aim = aim ?? throw new ArgumentNullException(nameof(aim));
            m_ReachMetres = reachMetres;
            m_ConeDegrees = coneDegrees;
        }

        public VehicleController Offer { get; private set; }

        public VehicleController Driving => m_Driving;

        public Transform Focus => m_Driving != null ? m_Driving.transform : m_Crew;

        public CrewPrompt Prompt { get; private set; } = CrewPrompt.None;

        public string Subject { get; private set; } = "";

        public bool IsDriving => m_Driving != null;

        public void Refresh()
        {
            if (IsDriving)
            {
                if (!m_Broker.OwnedByUs(m_Driving))
                {
                    StepOut();
                    return;
                }

                Offer = null;
                Say(CrewPrompt.None, "");
                return;
            }

            if (m_Asking && m_AskedFor != null && !WithinReachOf(m_AskedFor))
            {
                m_Asking = false;
                m_AskedFor = null;
            }

            Offer = WhatTheyAreLookingAt();

            if (Offer == null)
            {
                m_Refused = null;
                Say(CrewPrompt.None, "");
                return;
            }

            if (ReferenceEquals(Offer, m_Refused))
            {
                Say(WhyItWasRefused(Offer), Offer.DisplayName);
                return;
            }

            Say(CrewPrompt.Offer, Offer.DisplayName);
        }

        VehicleController WhatTheyAreLookingAt()
        {
            var looked = Aiming.At<VehicleController>(m_Aim(), m_ReachMetres, m_ConeDegrees);

            return looked != null && looked.AcceptsDriver ? looked : null;
        }

        public void Toggle(IDriveIntentSource intentSource)
        {
            if (IsDriving)
            {
                StepOut();
                return;
            }

            var wanted = Offer;
            if (m_Asking || wanted == null)
            {
                return;
            }

            m_Asking = true;
            m_AskedFor = wanted;
            var train = wanted.Chain;

            train.RequestOwnership(m_Broker, granted => Answered(wanted, train, intentSource, granted));
        }

        void Answered(VehicleController wanted, CartChain train, IDriveIntentSource intentSource, bool granted)
        {
            var stillWanted = ReferenceEquals(m_AskedFor, wanted) && !IsDriving;

            if (ReferenceEquals(m_AskedFor, wanted))
            {
                m_Asking = false;
                m_AskedFor = null;
            }

            if (!granted)
            {
                if (stillWanted)
                {
                    m_Refused = wanted;
                    Say(WhyItWasRefused(wanted), wanted.DisplayName);
                }

                return;
            }

            if (!stillWanted || !WithinReachOf(wanted))
            {
                m_Broker.HandBack(train.Members);
                return;
            }

            m_Driving = wanted;
            m_Refused = null;
            wanted.IntentSource = intentSource;
            wanted.Occupied = true;
            Offer = null;
            Say(CrewPrompt.None, "");
        }

        public Vector3 DismountPosition(VehicleController vehicle, float standingHeightMetres)
        {
            var clearOfTheBodywork = (vehicle.Shape.EnvelopeSizeMetres.x * 0.5f) + DismountClearanceMetres;
            var standing = (standingHeightMetres * 0.5f) + StandingClearanceMetres;

            return vehicle.transform.position
                   + (vehicle.transform.right * clearOfTheBodywork)
                   + (Vector3.up * standing);
        }

        bool WithinReachOf(VehicleController vehicle)
            => Vector3.Distance(m_Crew.position, vehicle.transform.position) <= m_ReachMetres;

        void StepOut()
        {
            m_Driving.IntentSource = null;
            m_Driving.Occupied = false;
            m_Driving = null;

            Say(CrewPrompt.None, "");
        }

        static CrewPrompt WhyItWasRefused(VehicleController vehicle)
            => vehicle.AcceptsDriver ? CrewPrompt.NoAnswer : CrewPrompt.BeingDriven;

        void Say(CrewPrompt prompt, string subject)
        {
            Prompt = prompt;
            Subject = subject;
        }
    }
}
