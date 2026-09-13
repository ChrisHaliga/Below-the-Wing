using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// The camera, sitting on whatever the local player is currently in charge of: in their own
    /// head while they are on foot, a little behind the vehicle once they climb into one.
    ///
    /// The subject and the framing change during play. The camera is told both, and does not work
    /// out or care which kind of thing it is looking at.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField, Tooltip("How the camera sits on its subject until something frames it otherwise.")]
        CameraFraming m_Framing = CameraFraming.Driving;

        [SerializeField, Tooltip("Degrees of rotation per unit of mouse movement.")]
        float m_LookSensitivity = 0.15f;

        float m_YawDegrees;
        float m_PitchDegrees = 15f;

        /// <summary>What the camera is orbiting, or null if the player is in charge of nothing.</summary>
        public Transform Subject { get; set; }

        /// <summary>Puts the camera into a framing: on foot or driving.</summary>
        public void Frame(CameraFraming framing)
        {
            m_Framing = framing;
            m_PitchDegrees = Mathf.Clamp(m_PitchDegrees, framing.MinPitchDegrees, framing.MaxPitchDegrees);
        }

        /// <summary>
        /// Which way round the subject the camera is sitting, in degrees. Deliberately unlimited:
        /// a player can walk all the way round their own tractor and keep looking at it.
        /// </summary>
        public float YawDegrees => m_YawDegrees;

        /// <summary>
        /// How far above or below level the camera is looking, in degrees. Held between limits so
        /// the view can never roll over the top of the subject or come up underneath it.
        /// </summary>
        public float PitchDegrees => m_PitchDegrees;

        /// <summary>Swings the camera by a mouse movement, in the input system's raw units.</summary>
        public void Look(Vector2 delta)
        {
            m_YawDegrees += delta.x * m_LookSensitivity;
            m_PitchDegrees = Mathf.Clamp(
                m_PitchDegrees - (delta.y * m_LookSensitivity),
                m_Framing.MinPitchDegrees,
                m_Framing.MaxPitchDegrees);
        }

        void LateUpdate() => Place();

        /// <summary>Puts the camera where the framing says, around the subject.</summary>
        public void Place()
        {
            if (Subject == null)
            {
                return;
            }

            var orbit = Quaternion.Euler(m_PitchDegrees, m_YawDegrees, 0f);
            var lookingAt = Subject.position + (Vector3.up * m_Framing.HeightMetres);

            transform.SetPositionAndRotation(
                lookingAt - (orbit * Vector3.forward * m_Framing.DistanceMetres),
                orbit);
        }
    }
}
