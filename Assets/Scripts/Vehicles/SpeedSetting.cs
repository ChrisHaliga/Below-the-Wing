using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Sets a speed on something that drives itself, from a number typed into the inspector.
    ///
    /// Deliberately the dullest component in the game. It holds a speed, and when asked, it applies
    /// it. Somebody wires a button to one of these and types a number in; what that number means,
    /// whether it is fast or slow, safe or reckless, is a matter for whoever set the scene up.
    ///
    /// The rule it exists to keep: no runtime assembly contains a value chosen for a particular
    /// scene, or a word describing one. A component called "safe speed" would be the first crack in
    /// that, and the next thing to learn about the sandbox would be harder to prise out.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpeedSetting : MonoBehaviour
    {
        [SerializeField, Tooltip("What drives itself when this is applied.")]
        ScriptedDriver m_Driver;

        [SerializeField, Tooltip("The speed to ask for, in metres per second.")]
        float m_SpeedMetresPerSecond;

        [SerializeField, Tooltip("How quickly to get there, in metres per second per second. " +
                                 "Below zero leaves whatever the driver was already using.")]
        float m_RampMetresPerSecondSquared = -1f;

        /// <summary>The speed this would ask for.</summary>
        public float SpeedMetresPerSecond
        {
            get => m_SpeedMetresPerSecond;
            set => m_SpeedMetresPerSecond = value;
        }

        /// <summary>What this sets the speed on.</summary>
        public ScriptedDriver Driver
        {
            get => m_Driver;
            set => m_Driver = value;
        }

        /// <summary>How quickly to approach it. Below zero leaves the driver's own ramp alone.</summary>
        public float RampMetresPerSecondSquared
        {
            get => m_RampMetresPerSecondSquared;
            set => m_RampMetresPerSecondSquared = value;
        }

        /// <summary>Asks for that speed. Wired to whatever wants to trigger it.</summary>
        public void Apply()
        {
            if (m_Driver == null)
            {
                return;
            }

            if (m_RampMetresPerSecondSquared >= 0f)
            {
                m_Driver.RampMetresPerSecondSquared = m_RampMetresPerSecondSquared;
            }

            m_Driver.TargetSpeed = m_SpeedMetresPerSecond;
        }
    }
}
