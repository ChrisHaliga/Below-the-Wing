using System;
using System.Collections.Generic;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// The one character on this machine that belongs to the person sitting in front of it.
    ///
    /// Every other character in a session is a copy of somebody else's, driven by what arrives over
    /// the network. This is the only one that reads the keyboard, holds the mouse pointer, moves
    /// the camera, gets in and out of vehicles, and is told things on screen.
    ///
    /// Keeping that distinction in one named object is what stops "is this ours?" being asked in
    /// several places and answered differently.
    /// </summary>
    public sealed class LocalPlayerRig
    {
        readonly CrewCharacter m_Character;
        readonly FollowCamera m_Camera;

        public LocalPlayerRig(
            CrewCharacter character,
            FollowCamera camera,
            IOwnershipBroker broker,
            Func<IReadOnlyList<VehicleController>> nearbyVehicles,
            RampSession session)
        {
            m_Character = character != null ? character : throw new ArgumentNullException(nameof(character));
            m_Camera = camera;

            m_Character.TakeTheSeat(broker, nearbyVehicles);
            m_Character.Camera = camera;

            // Hooking carts on and dropping them off. Writing the new shape down is what finishes
            // the job: which train a vehicle belongs to is replicated rather than worked out from
            // where things are parked, so until it is said out loud only this machine knows, and a
            // player elsewhere is still offered a cart that is physically coupled to a train.
            m_Character.Hitching = new CouplingHand(
                broker,
                nearbyVehicles,
                hitched: train => session.Reshaped(train, session.TrainIndexOf(train)),
                split: (front, back) =>
                {
                    session.Reshaped(front, session.TrainIndexOf(front));
                    session.Reshaped(back, session.ATrainNumberNobodyIsUsing());
                });

            // Before the input, so that the first frame of looking around already has the pointer.
            // Arriving on the apron is the moment this player starts playing, and playing is when
            // the pointer belongs to the game rather than to their desktop.
            m_Character.gameObject.AddComponent<MouseCapture>();
            m_Character.gameObject.AddComponent<LocalCrewInput>();
            m_Character.gameObject.AddComponent<OccupancyPromptView>().Watch(m_Character.Seat);

            if (camera != null)
            {
                camera.Subject = m_Character.transform;
            }
        }

        /// <summary>
        /// Points the camera at whatever the player is currently in charge of, which changes the
        /// moment they climb into a tractor and again when they step out.
        /// </summary>
        public void FollowWhateverTheyAreControlling()
        {
            if (m_Camera != null && m_Character != null && m_Character.Seat != null)
            {
                m_Camera.Subject = m_Character.Seat.Subject;
            }
        }
    }
}
