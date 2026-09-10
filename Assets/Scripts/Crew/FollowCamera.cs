using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// The third-person camera, orbiting whatever the local player is currently in charge of.
    ///
    /// That subject changes during play: it is the player's character while they are on foot and
    /// the vehicle itself once they climb into one. The camera is told which, and does not work out
    /// or care which kind of thing it is looking at.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField, Tooltip("Metres back from the subject.")]
        float m_DistanceMetres = 8f;

        [SerializeField, Tooltip("Metres above the subject's origin that the camera aims at.")]
        float m_HeightMetres = 2f;

        [SerializeField, Tooltip("Degrees of rotation per unit of mouse movement.")]
        float m_LookSensitivity = 0.15f;

        [SerializeField, Tooltip("How far the camera may look down, in degrees below level.")]
        float m_MinPitchDegrees = -30f;

        [SerializeField, Tooltip("How far the camera may look up, in degrees above level.")]
        float m_MaxPitchDegrees = 70f;

        float m_YawDegrees;
        float m_PitchDegrees = 15f;

        /// <summary>What the camera is orbiting, or null if the player is in charge of nothing.</summary>
        public Transform Subject { get; set; }

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
                m_MinPitchDegrees,
                m_MaxPitchDegrees);
        }

        void LateUpdate()
        {
            if (Subject == null)
            {
                return;
            }

            var orbit = Quaternion.Euler(m_PitchDegrees, m_YawDegrees, 0f);
            var lookingAt = Subject.position + (Vector3.up * m_HeightMetres);

            transform.SetPositionAndRotation(
                lookingAt - (orbit * Vector3.forward * m_DistanceMetres),
                orbit);
        }
    }
}
