using System;
using System.Collections.Generic;
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
        readonly Func<IReadOnlyList<VehicleController>> m_NearbyVehicles;
        readonly float m_ReachMetres;

        VehicleController m_Driving;

        VehicleController m_Refused;

        bool m_Asking;

        VehicleController m_AskedFor;

        public Func<Ray> Aim { get; set; }

        public float LooksIntoConeDegrees { get; set; } = 40f;

        public VehicleOccupancy(Transform crew, IOwnershipBroker broker, Func<IReadOnlyList<VehicleController>> nearbyVehicles, float reachMetres)
        {
            m_Crew = crew != null ? crew : throw new ArgumentNullException(nameof(crew));
            m_Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            m_NearbyVehicles = nearbyVehicles ?? throw new ArgumentNullException(nameof(nearbyVehicles));
            m_ReachMetres = reachMetres;
        }

        public VehicleController Offer { get; private set; }

        public VehicleController Driving => m_Driving;

        public Transform Subject => m_Driving != null ? m_Driving.transform : m_Crew;

        public CrewPrompt Prompt { get; private set; } = CrewPrompt.None;

        public string Message { get; private set; } = "";

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
                Say(CrewPrompt.Refused, Offer.AcceptsDriver
                    ? $"{Offer.DisplayName} did not answer. Try again."
                    : $"{Offer.DisplayName} is being driven");
                return;
            }

            Say(CrewPrompt.Offer, $"Press E to drive {Offer.DisplayName}");
        }

        VehicleController WhatTheyAreLookingAt()
        {
            if (Aim == null)
            {
                return DriverPrompt.Nearest(m_Crew.position, m_ReachMetres, m_NearbyVehicles());
            }

            var looked = Aiming.At<VehicleController>(Aim(), m_ReachMetres, LooksIntoConeDegrees);

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
                    Say(CrewPrompt.Refused, wanted.AcceptsDriver
                        ? $"{wanted.DisplayName} did not answer. Try again."
                        : $"{wanted.DisplayName} is being driven");
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

        void Say(CrewPrompt prompt, string message)
        {
            Prompt = prompt;
            Message = message;
        }
    }
}
