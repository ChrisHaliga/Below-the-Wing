using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Apron
{
    [DisallowMultipleComponent]
    public sealed class SmoothedLook : MonoBehaviour
    {
        public const float CatchUpSeconds = 0.1f;

        Transform m_Body;
        IMovedFromHere m_Mover;

        Vector3 m_ShownAt;
        Quaternion m_ShownFacing = Quaternion.identity;

        Vector3 m_PlacedAt;
        Quaternion m_PlacedFacing = Quaternion.identity;

        public float TrailingByMetres => m_Body == null ? 0f : Vector3.Distance(m_ShownAt, m_Body.position);

        public bool WorthSmoothing => m_Mover != null && !m_Mover.OursToMove;

        void Awake()
        {
            m_Body = transform.parent;
            m_Mover = m_Body != null ? m_Body.GetComponent<IMovedFromHere>() : null;
            RememberHowItWasPlaced();
            CatchUpNow();
        }

        void LateUpdate() => Follow(Time.deltaTime);

        public void Follow(float deltaTime)
        {
            if (m_Body == null)
            {
                return;
            }

            if (!WorthSmoothing)
            {
                CatchUpNow();
                return;
            }

            var caughtUp = 1f - Mathf.Exp(-deltaTime / Mathf.Max(CatchUpSeconds, 1e-4f));

            m_ShownAt = Vector3.Lerp(m_ShownAt, m_Body.position, caughtUp);
            m_ShownFacing = Quaternion.Slerp(m_ShownFacing, m_Body.rotation, caughtUp);

            ShowIt();
        }

        public void RememberHowItWasPlaced()
        {
            m_PlacedAt = transform.localPosition;
            m_PlacedFacing = transform.localRotation;
        }

        public void CatchUpNow()
        {
            if (m_Body == null)
            {
                return;
            }

            m_ShownAt = m_Body.position;
            m_ShownFacing = m_Body.rotation;
            ShowIt();
        }

        void ShowIt()
            => transform.SetPositionAndRotation(
                m_ShownAt + (m_ShownFacing * m_PlacedAt),
                m_ShownFacing * m_PlacedFacing);
    }
}
