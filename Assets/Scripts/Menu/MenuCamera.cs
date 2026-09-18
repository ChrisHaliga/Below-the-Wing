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
        float m_TravelledFor = TravelSeconds;

        public Pose Showing => Between(m_From, m_To, m_TravelledFor / TravelSeconds);

        public bool Travelling => m_TravelledFor < TravelSeconds;

        public void StartOn(Transform shot)
        {
            m_From = At(shot);
            m_To = m_From;
            m_TravelledFor = TravelSeconds;

            Place();
        }

        public void TravelTo(Transform shot)
        {
            m_From = Showing;
            m_To = At(shot);
            m_TravelledFor = 0f;
        }

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
            if (m_TravelledFor < TravelSeconds)
            {
                m_TravelledFor = Mathf.Min(m_TravelledFor + Time.unscaledDeltaTime, TravelSeconds);
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
