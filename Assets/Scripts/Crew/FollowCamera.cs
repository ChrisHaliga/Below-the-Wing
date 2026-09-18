using BelowTheWing.Settings;
using UnityEngine;

namespace BelowTheWing.Crew
{
    [DisallowMultipleComponent]
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField, Tooltip("How the camera sits on its subject")]
        CameraFraming m_Framing = CameraFraming.Driving;

        [SerializeField, Tooltip("Degrees turned per unit of mouse movement at a sensitivity of 1")]
        float m_DegreesPerCount = 0.15f;

        const float StartingPitchDegrees = 15f;

        Camera m_Lens;
        float m_YawDegrees;
        float m_PitchDegrees = StartingPitchDegrees;

        public Transform Subject { get; set; }

        public void Frame(CameraFraming framing)
        {
            m_Framing = framing;
            m_PitchDegrees = Mathf.Clamp(m_PitchDegrees, framing.MinPitchDegrees, framing.MaxPitchDegrees);
        }

        public float YawDegrees => m_YawDegrees;

        public float PitchDegrees => m_PitchDegrees;

        public void Look(Vector2 delta)
        {
            m_YawDegrees += delta.x * m_DegreesPerCount * CrewSettings.LookSensitivity;
            m_PitchDegrees = Mathf.Clamp(
                m_PitchDegrees - (delta.y * m_DegreesPerCount * CrewSettings.LookYMultiplier),
                m_Framing.MinPitchDegrees,
                m_Framing.MaxPitchDegrees);
        }

        void LateUpdate() => Place();

        public void Place()
        {
            if (m_Lens == null)
            {
                m_Lens = GetComponent<Camera>();
            }

            if (m_Lens != null)
            {
                m_Lens.fieldOfView = CrewSettings.FieldOfViewDegrees;
            }

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
