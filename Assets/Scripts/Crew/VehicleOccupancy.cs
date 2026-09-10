using System;
using System.Collections.Generic;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>What a player standing on the apron is currently being told.</summary>
    public enum OccupancyPrompt
    {
        /// <summary>Nothing worth saying: no vehicle within reach, or already driving one.</summary>
        None,

        /// <summary>A vehicle is within reach and would take a driver.</summary>
        OfferToDrive,

        /// <summary>A vehicle was asked for and somebody else has it.</summary>
        VehicleTaken
    }

    /// <summary>
    /// Getting in and out of vehicles.
    ///
    /// Walking near something driveable offers it; accepting the offer asks to take over
    /// simulating that vehicle -- and its whole train, if it has one -- and only once that is
    /// granted does the player actually get the wheel. Asking and being told no is a normal
    /// outcome, not an error: somebody else may be driving it.
    ///
    /// This is a plain object rather than a component so that the sequence can be exercised
    /// directly, including the refusal, which is otherwise difficult to arrange on purpose.
    /// </summary>
    public sealed class VehicleOccupancy
    {
        /// <summary>Metres of clear ground left between a player and the vehicle they step out of.</summary>
        const float DismountClearanceMetres = 1f;

        readonly Transform m_Crew;
        readonly IOwnershipBroker m_Broker;
        readonly Func<IReadOnlyList<IDriveable>> m_NearbyVehicles;
        readonly float m_ReachMetres;

        VehicleController m_Driving;

        /// <summary>
        /// The vehicle this player last asked for and was refused.
        ///
        /// Kept so the refusal stays on screen while they are still standing at it. Clearing it on
        /// the next refresh would put the message up for a single physics step -- twenty
        /// milliseconds -- and then quietly offer them the vehicle again as though nothing had
        /// happened.
        /// </summary>
        IDriveable m_Refused;

        /// <summary>Set while a request is in flight, so one press asks once.</summary>
        bool m_Asking;

        /// <summary>What that request was for, so it can be given up on.</summary>
        IDriveable m_AskedFor;

        public VehicleOccupancy(Transform crew, IOwnershipBroker broker, Func<IReadOnlyList<IDriveable>> nearbyVehicles, float reachMetres)
        {
            m_Crew = crew != null ? crew : throw new ArgumentNullException(nameof(crew));
            m_Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            m_NearbyVehicles = nearbyVehicles ?? throw new ArgumentNullException(nameof(nearbyVehicles));
            m_ReachMetres = reachMetres;
        }

        /// <summary>The vehicle currently being offered, or null if none is.</summary>
        public IDriveable Offer { get; private set; }

        /// <summary>The vehicle being driven, or null while the player is on foot.</summary>
        public VehicleController Driving => m_Driving;

        /// <summary>
        /// What the player is attached to right now: their own body while on foot, and the vehicle
        /// itself once they are in one. This is what the camera follows, and it is the reason the
        /// camera does not need to know which of the two it is looking at.
        /// </summary>
        public Transform Subject => m_Driving != null ? m_Driving.transform : m_Crew;

        /// <summary>Which of the things a player can be told is currently being said.</summary>
        public OccupancyPrompt Prompt { get; private set; } = OccupancyPrompt.None;

        /// <summary>The words on screen, or empty if there is nothing to say.</summary>
        public string Message { get; private set; } = "";

        /// <summary>Whether the player is currently in a vehicle rather than on foot.</summary>
        public bool IsDriving => m_Driving != null;

        /// <summary>Works out what, if anything, to offer the player where they are standing.</summary>
        public void Refresh()
        {
            if (IsDriving)
            {
                // A vehicle can be taken from underneath somebody: a session owner reclaiming an
                // orphaned train, or ownership moving for any other reason. Sitting in a body this
                // machine no longer simulates means pressing controls that reach nothing.
                if (!m_Broker.OwnedByUs(m_Driving))
                {
                    StepOut();
                    return;
                }

                Offer = null;
                Say(OccupancyPrompt.None, "");
                return;
            }

            // A request whose owner has left the session is never answered, and waiting on it for
            // ever would leave this player unable to ask for anything again. Walking away gives up.
            if (m_Asking && m_AskedFor != null && !WithinReachOf(m_AskedFor))
            {
                m_Asking = false;
                m_AskedFor = null;
            }

            Offer = DriverPrompt.Nearest(m_Crew.position, m_ReachMetres, m_NearbyVehicles());

            if (Offer == null)
            {
                m_Refused = null;
                Say(OccupancyPrompt.None, "");
                return;
            }

            if (ReferenceEquals(Offer, m_Refused))
            {
                // A refusal has two causes and they need different words. Somebody is in it, or
                // nobody answered -- and telling a player that an empty tractor is being driven is
                // false about the only thing the message says.
                Say(OccupancyPrompt.VehicleTaken, Offer.AcceptsDriver
                    ? $"{Offer.DisplayName} did not answer. Try again."
                    : $"{Offer.DisplayName} is being driven");
                return;
            }

            Say(OccupancyPrompt.OfferToDrive, $"Press E to drive {Offer.DisplayName}");
        }

        /// <summary>
        /// The player asked to get in or out.
        ///
        /// On foot with an offer standing, this asks for the vehicle and takes it only if the ask
        /// is granted. Already driving, this gets out. On foot with nothing offered, nothing happens.
        /// </summary>
        public void Toggle(IDriveIntentSource intentSource)
        {
            if (IsDriving)
            {
                StepOut();
                return;
            }

            // A second press while the first is still in flight would send a second request for the
            // same train, and the answers would race each other.
            if (m_Asking || Offer is not VehicleController wanted)
            {
                return;
            }

            m_Asking = true;
            m_AskedFor = wanted;
            var train = wanted.Chain;

            train.RequestOwnership(m_Broker, granted =>
            {
                m_Asking = false;
                m_AskedFor = null;

                if (!granted)
                {
                    m_Refused = wanted;
                    Say(OccupancyPrompt.VehicleTaken, wanted.AcceptsDriver
                        ? $"{wanted.DisplayName} did not answer. Try again."
                        : $"{wanted.DisplayName} is being driven");
                    return;
                }

                // An answer can arrive after the player has walked off. Seating them then would
                // teleport them back into a vehicle they had given up on.
                if (!WithinReachOf(wanted))
                {
                    m_Broker.HandBack(train.Members);
                    return;
                }

                m_Driving = wanted;
                m_Refused = null;
                wanted.IntentSource = intentSource;
                wanted.Occupied = true;
                Offer = null;
                Say(OccupancyPrompt.None, "");
            });
        }

        /// <summary>
        /// Where a player who has just got out should be put: clear of the vehicle they were in,
        /// rather than inside it.
        /// </summary>
        public Vector3 DismountPosition(VehicleController vehicle)
        {
            var clearOfTheBodywork = (vehicle.Profile.bodySizeMetres.x * 0.5f) + DismountClearanceMetres;
            return vehicle.transform.position + (vehicle.transform.right * clearOfTheBodywork);
        }

        bool WithinReachOf(IDriveable vehicle)
            => Vector3.Distance(m_Crew.position, vehicle.Position) <= m_ReachMetres;

        void StepOut()
        {
            // Where the body physically goes is the character's business, not the seat's. This
            // records only that nobody is driving any more; the character notices and climbs out.
            m_Driving.IntentSource = null;
            m_Driving.Occupied = false;
            m_Driving = null;

            Say(OccupancyPrompt.None, "");
        }

        void Say(OccupancyPrompt prompt, string message)
        {
            Prompt = prompt;
            Message = message;
        }
    }
}
