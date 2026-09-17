using UnityEngine;

namespace BelowTheWing.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class MenuCamera : MonoBehaviour
    {
        public const float TravelSeconds = 1.6f;

        Camera m_Eye;
        CameraShot m_From;
        CameraShot m_To;
        float m_TravelledFor = TravelSeconds;
        float m_Angle;

        public CameraShot Showing => CameraShot.Between(m_From, m_To, m_TravelledFor / TravelSeconds);

        public bool Travelling => m_TravelledFor < TravelSeconds;

        public void StartOn(CameraShot shot)
        {
            m_From = shot;
            m_To = shot;
            m_TravelledFor = TravelSeconds;

            Place();
        }

        public void TravelTo(CameraShot shot)
        {
            m_From = Showing;
            m_To = shot;
            m_TravelledFor = 0f;
        }

        void Awake() => m_Eye = GetComponent<Camera>();

        void LateUpdate()
        {
            if (m_TravelledFor < TravelSeconds)
            {
                m_TravelledFor = Mathf.Min(m_TravelledFor + Time.unscaledDeltaTime, TravelSeconds);
            }

            var shot = Showing;
            m_Angle = shot.AngleAfter(m_Angle, Time.unscaledDeltaTime);

            Place();
        }

        void Place()
        {
            var shot = Showing;

            transform.SetPositionAndRotation(shot.PlacedAt(m_Angle), shot.FacingFrom(m_Angle));

            if (m_Eye != null)
            {
                m_Eye.fieldOfView = Settings.CrewSettings.FieldOfViewDegrees;
            }
        }
    }
}
