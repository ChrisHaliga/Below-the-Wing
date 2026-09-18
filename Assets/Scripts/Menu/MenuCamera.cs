using UnityEngine;

namespace BelowTheWing.Menu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class MenuCamera : MonoBehaviour
    {
        public const float TravelSeconds = 1.4f;

        Camera m_Eye;
        Pose m_From;
        Pose m_To;
        float m_Seconds = TravelSeconds;
        float m_TravelledFor = TravelSeconds;

        public Camera Eye => m_Eye;

        public Pose Showing => Between(m_From, m_To, HowFar(m_TravelledFor, m_Seconds));

        public bool Travelling => m_TravelledFor < m_Seconds;

        public void StartWhereItIs()
        {
            m_From = new Pose(transform.position, transform.rotation);
            m_To = m_From;
            m_Seconds = TravelSeconds;
            m_TravelledFor = m_Seconds;
        }

        public void TravelTo(Transform shot) => TravelTo(shot, TravelSeconds);

        public void TravelTo(Transform shot, float seconds)
        {
            m_From = Showing;
            m_To = At(shot);
            m_Seconds = Mathf.Max(seconds, 0f);
            m_TravelledFor = 0f;
        }

        public static float HowFar(float travelledFor, float seconds)
            => seconds <= 0f ? 1f : Mathf.Clamp01(travelledFor / seconds);

        public static Pose Between(Pose from, Pose to, float howFar)
        {
            var eased = Eased(Mathf.Clamp01(howFar));

            return new Pose(
                Vector3.Lerp(from.position, to.position, eased),
                Quaternion.Slerp(from.rotation, to.rotation, eased));
        }

        static float Eased(float howFar)
            => howFar < 0.5f
                ? 2f * howFar * howFar
                : 1f - (Mathf.Pow((-2f * howFar) + 2f, 2f) * 0.5f);

        static Pose At(Transform shot)
            => shot != null
                ? new Pose(shot.position, shot.rotation)
                : new Pose(Vector3.up * 2f, Quaternion.identity);

        void Awake() => m_Eye = GetComponent<Camera>();

        void LateUpdate()
        {
            if (m_TravelledFor < m_Seconds)
            {
                m_TravelledFor = Mathf.Min(m_TravelledFor + Time.unscaledDeltaTime, m_Seconds);
            }

            Place();
        }

        void Place()
        {
            var pose = Showing;

            transform.SetPositionAndRotation(pose.position, pose.rotation);

            if (m_Eye != null)
            {
                m_Eye.fieldOfView = Settings.CrewSettings.FieldOfViewDegrees;
            }
        }
    }
}
