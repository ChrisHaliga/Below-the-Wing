using UnityEngine;

namespace BelowTheWing.Crew
{
    public struct Footing
    {
        public const float BackOnTheirFeetBelow = 0.5f;

        Vector3 m_SurfaceWasMovingAt;
        Object m_Surface;
        bool m_Started;

        public bool Lost { get; private set; }

        public void Settle(
            Object surface,
            Vector3 surfaceVelocity,
            Vector3 ownVelocity,
            float gripMetresPerSecondSquared,
            float deltaTime)
        {
            if (!m_Started || surface != m_Surface)
            {
                m_Started = true;
                m_Surface = surface;
                m_SurfaceWasMovingAt = surfaceVelocity;
            }

            var pulledAt = (surfaceVelocity - m_SurfaceWasMovingAt).magnitude / Mathf.Max(deltaTime, 1e-5f);
            m_SurfaceWasMovingAt = surfaceVelocity;

            if (pulledAt > gripMetresPerSecondSquared)
            {
                Lost = true;
                return;
            }

            if (Lost && (ownVelocity - surfaceVelocity).magnitude <= BackOnTheirFeetBelow)
            {
                Lost = false;
            }
        }

        public void Reset()
        {
            Lost = false;
            m_Started = false;
        }
    }
}
