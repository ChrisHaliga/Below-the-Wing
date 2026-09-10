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
    /// outcome, not an error: somebody else may have reached the tractor first.
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
        /// Builds a seat for one crew member.
        /// </summary>
        /// <param name="crew">The character this seat belongs to.</param>
        /// <param name="broker">How ownership of a vehicle is asked for and granted.</param>
        /// <param name="nearbyVehicles">
        /// Everything that might be offered. Called each time the offer is refreshed rather than
        /// captured once, because vehicles move.
        /// </param>
        /// <param name="reachMetres">How close a player must be to be offered a vehicle.</param>
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

        /// <summary>
        /// The words on screen: an offer to drive a named vehicle, a note that one has been taken
        /// by somebody else, or nothing at all.
        /// </summary>
        public string Message { get; private set; } = "";

        /// <summary>Whether the player is currently in a vehicle rather than on foot.</summary>
        public bool IsDriving => m_Driving != null;

        /// <summary>Works out what, if anything, to offer the player where they are standing.</summary>
        public void Refresh()
        {
            if (IsDriving)
            {
                Offer = null;
                Say(OccupancyPrompt.None, "");
                return;
            }

            Offer = DriverPrompt.Nearest(m_Crew.position, m_ReachMetres, m_NearbyVehicles());

            if (Offer == null)
            {
                Say(OccupancyPrompt.None, "");
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
        /// <param name="intentSource">What will drive the vehicle once the player is in it.</param>
        public void Toggle(IDriveIntentSource intentSource)
        {
            if (IsDriving)
            {
                StepOut();
                return;
            }

            if (Offer is not VehicleController wanted)
            {
                return;
            }

            // The whole train is asked for, not just the tractor. A refusal is an ordinary answer
            // and leaves the player exactly where they were standing.
            wanted.Chain.RequestOwnership(m_Broker, granted =>
            {
                if (!granted)
                {
                    Say(OccupancyPrompt.VehicleTaken, $"{wanted.DisplayName} is taken");
                    return;
                }

                m_Driving = wanted;
                wanted.IntentSource = intentSource;
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

        void StepOut()
        {
            // Where the body physically goes is the character's business, not the seat's. This
            // records only that nobody is driving any more; the character notices and climbs out.
            m_Driving.IntentSource = null;
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
