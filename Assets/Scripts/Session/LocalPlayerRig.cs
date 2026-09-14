using System;
using System.Collections.Generic;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
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

            m_Character.TakeTheSeat(broker, nearbyVehicles);
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

            var left = m_Character.transform.Find(Hands.LeftAnchorName);
            var right = m_Character.transform.Find(Hands.RightAnchorName);
            if (left != null && right != null)
            {
                m_Character.Handling = new Hands(left, right, m_Character.Body, m_Character.Profile.hands);
            }
            else
            {
                Debug.LogError(
                    $"'{m_Character.name}' has no '{Hands.LeftAnchorName}' and '{Hands.RightAnchorName}' " +
                    "under it, so this player has no hands: nothing can be picked up or held onto.",
                    m_Character);
            }

            m_Character.gameObject.AddComponent<MouseCapture>();
            m_Character.gameObject.AddComponent<LocalCrewInput>();
            m_Character.gameObject.AddComponent<CrewPromptView>()
                .Watch(m_Character.Climb, m_Character.Seat);

            if (camera != null)
            {
                camera.Subject = m_Character.transform;
            }
        }

        public void FollowWhateverTheyAreControlling()
        {
            if (m_Camera != null && m_Character != null && m_Character.Seat != null)
            {
                m_Camera.Subject = m_Character.Seat.Subject;
                m_Camera.Frame(CameraFraming.For(m_Character.Seat.IsDriving, m_Character.EyeMetresAboveOrigin));
            }
        }
    }
}
