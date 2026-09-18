using System;
using System.Collections.Generic;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Session
{
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

            m_Character.TakeTheSeat(broker);
            m_Character.Camera = camera;

            m_Character.Hitching = new CouplingHand(
                broker,
                nearbyVehicles,
                hitched: train => session.Reshaped(train, session.TrainIndexOf(train)),
                split: (front, back) =>
                {
                    session.Reshaped(front, session.TrainIndexOf(front));
                    session.Reshaped(back, session.ATrainNumberNobodyIsUsing());
                });

            m_Character.Hitching.LookingAt = () => Aiming.At<VehicleController>(
                m_Character.LookingAlong(), m_Character.ReachMetres, m_Character.ConeDegrees);

            var left = m_Character.transform.Find(Hands.LeftAnchorName);
            var right = m_Character.transform.Find(Hands.RightAnchorName);

            if (left == null || right == null)
            {
                throw MisbuiltException.For(
                    m_Character,
                    $"has no '{Hands.LeftAnchorName}' and '{Hands.RightAnchorName}' under it, so this player has no hands");
            }

            m_Character.Handling = new Hands(left, right, m_Character.Body, m_Character.Profile.hands);

            m_Character.gameObject.AddComponent<HandLook>()
                .Watch(m_Character.Handling, left, right);

            m_Character.gameObject.AddComponent<MouseCapture>();
            m_Character.gameObject.AddComponent<LocalCrewInput>();
            m_Character.gameObject.AddComponent<CrewPromptView>()
                .Watch(m_Character.Seat);

            if (camera != null)
            {
                camera.Subject = m_Character.transform;
            }
        }

        public void FollowWhateverTheyAreControlling()
        {
            if (m_Camera != null && m_Character != null && m_Character.Seat != null)
            {
                m_Camera.Subject = m_Character.Seat.Focus;
                m_Camera.Frame(CameraFraming.For(m_Character.Seat.IsDriving, m_Character.EyeMetresAboveOrigin));
            }
        }
    }
}
