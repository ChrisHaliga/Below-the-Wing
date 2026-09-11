using System;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>How long a crash is left alone before correction starts arguing with it again.</summary>
    [Serializable]
    public struct BlackoutSettings
    {
        [Tooltip("Seconds after a collision during which the owner's word is ignored entirely.")]
        public float silenceSeconds;

        [Tooltip("Seconds over which correction is brought back in, once the silence is over.")]
        public float easeBackSeconds;

        /// <summary>Long enough for an impact to play out, short enough not to be noticed.</summary>
        public static BlackoutSettings Default => new BlackoutSettings
        {
            silenceSeconds = 0.6f,
            easeBackSeconds = 0.4f
        };
    }

    /// <summary>
    /// Letting a crash happen before correcting it.
    ///
    /// Correction running through a collision fights the impulse. The vehicle bounces off the way
    /// physics says it should, and then correction -- still holding a report taken before the crash
    /// and knowing nothing about it -- drags it back through the impact it just had. That reads as
    /// the game refusing to accept what the player just did, and it is the single most
    /// immersion-breaking artefact in networked physics.
    ///
    /// So contact buys silence. For a moment afterwards nothing outside this machine gets a say,
    /// local physics resolves the collision without interference, and the two machines are simply
    /// allowed to disagree. Then correction is brought back gradually rather than switched on: a
    /// hard re-enable at full strength is itself a lurch, and it lands exactly when the player is
    /// still watching the crash.
    ///
    /// It is a whole train that goes quiet, not one cart. A collision that displaces one cart
    /// displaces everything hitched to it, so blending any member back mid-crash reintroduces the
    /// constraint fight that towing exists to remove.
    /// </summary>
    public sealed class ContactBlackout
    {
        readonly BlackoutSettings m_Settings;
        float m_SinceContact = float.MaxValue;

        public ContactBlackout(BlackoutSettings settings) => m_Settings = settings;

        /// <summary>Something hit it. Start the clock again, whatever the clock was doing.</summary>
        public void Touched() => m_SinceContact = 0f;

        public void Tick(float deltaTime)
        {
            if (m_SinceContact < float.MaxValue)
            {
                m_SinceContact += deltaTime;
            }
        }

        /// <summary>Whether a crash is still playing out and nothing should interfere with it.</summary>
        public bool InProgress => m_SinceContact < m_Settings.silenceSeconds;

        /// <summary>
        /// How much of the usual correction applies right now, from nothing during the crash to all
        /// of it once the vehicle has settled.
        ///
        /// The ramp is the point. Going straight from nothing to everything puts a step in the force
        /// on the frame the silence ends, which is a pop -- and it arrives while the player is still
        /// looking at the wreck.
        /// </summary>
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
