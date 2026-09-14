using System;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [Serializable]
    public struct BlackoutSettings
    {
        [Tooltip("Silence after a knock, s")]
        public float silenceSeconds;

        [Tooltip("Time to ease authority back, s")]
        public float easeBackSeconds;

        public static BlackoutSettings Default => new BlackoutSettings
        {
            silenceSeconds = 0.6f,
            easeBackSeconds = 0.4f
        };
    }

    public sealed class ContactBlackout
    {
        readonly BlackoutSettings m_Settings;
        float m_SinceContact = float.MaxValue;

        public ContactBlackout(BlackoutSettings settings) => m_Settings = settings;

        public void Touched() => m_SinceContact = 0f;

        public void Tick(float deltaTime)
        {
            if (m_SinceContact < float.MaxValue)
            {
                m_SinceContact += deltaTime;
            }
        }

        public bool InProgress => m_SinceContact < m_Settings.silenceSeconds;

        public float Authority
        {
            get
            {
                if (InProgress)
                {
                    return 0f;
                }

                if (m_Settings.easeBackSeconds <= 0f)
                {
                    return 1f;
                }

                var into = (m_SinceContact - m_Settings.silenceSeconds) / m_Settings.easeBackSeconds;
                return Mathf.Clamp01(into);
            }
        }
    }
}
