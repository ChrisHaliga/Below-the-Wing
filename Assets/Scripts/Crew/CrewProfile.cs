using BelowTheWing.Cargo;
using UnityEngine;

namespace BelowTheWing.Crew
{
    [CreateAssetMenu(menuName = "Below the Wing/Crew Profile", fileName = "CrewProfile")]
    public sealed class CrewProfile : ScriptableObject
    {
        [Header("Mass and scale (real-world)")]
        [Tooltip("Mass, kg")]
        public float massKg = 80f;

        [Tooltip("Standing height, m")]
        public float heightMetres = 1.8f;

        [Tooltip("Capsule radius, m")]
        public float radiusMetres = 0.3f;

        [Tooltip("Height above the subject, m")]
        public float crouchedHeightMetres = 1.2f;

        [Tooltip("Walk speed kept while crouched, 0 to 1")]
        public float crouchSpeedMultiplier = 0.45f;

        [Header("Movement")]
        [Tooltip("Walking speed, m/s")]
        public float walkSpeedMetresPerSecond = 4f;

        [Tooltip("Sprinting speed, m/s")]
        public float sprintSpeedMetresPerSecond = 7f;

        [Tooltip("Most the surface underfoot can drag them with, m/s^2")]
        public float footGripMetresPerSecondSquared = 10f;

        [Tooltip("How fast walking changes their speed, m/s^2")]
        public float gaitResponseMetresPerSecondSquared = 30f;

        [Header("Jumping")]
        [Tooltip("Height above the subject, m")]
        public float jumpHeightMetres = 1.4f;

        [Header("Hands")]
        [Tooltip("Hand settings")]
        public HandSettings hands = HandSettings.Default;
    }
}
