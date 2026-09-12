using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// What a ramp worker is physically, and how fast they get about.
    ///
    /// Crew are rigidbodies rather than character controllers because being shoved is part of the
    /// game: a tractor that runs into somebody is supposed to send them flying, and an object that
    /// cannot be pushed cannot be sent anywhere.
    /// </summary>
    [CreateAssetMenu(menuName = "Below the Wing/Crew Profile", fileName = "CrewProfile")]
    public sealed class CrewProfile : ScriptableObject
    {
        [Header("Mass and scale (real-world)")]
        [Tooltip("Mass of a person in kilograms, including boots and hi-vis.")]
        public float massKg = 80f;

        [Tooltip("Standing height in metres.")]
        public float heightMetres = 1.8f;

        [Tooltip("Radius of the capsule in metres, roughly shoulder width halved.")]
        public float radiusMetres = 0.3f;

        [Tooltip("How tall they are crouched, in metres. Has to clear the inside of a baggage cart, " +
                 "which is the lowest thing anybody is expected to get into.")]
        public float crouchedHeightMetres = 1.2f;

        [Tooltip("How much of their walking speed they keep while crouched. Crouching has to cost " +
                 "something or nobody ever stands up.")]
        public float crouchSpeedMultiplier = 0.45f;

        [Header("Movement")]
        [Tooltip("Metres per second at a walk.")]
        public float walkSpeedMetresPerSecond = 4f;

        [Tooltip("Metres per second while sprinting.")]
        public float sprintSpeedMetresPerSecond = 7f;

        [Tooltip("How hard the legs push to reach the requested speed, in metres per second squared.")]
        public float accelerationMetresPerSecondSquared = 30f;

        [Tooltip("How fast the body comes round to face where it is going, in degrees per second.")]
        public float turnRateDegreesPerSecond = 720f;

        [Header("Jumping")]
        [Tooltip("How high a standing jump clears, in metres. Tuned to reach a cart deck with room " +
                 "to spare, because that is the one thing jumping is for -- and once set, every " +
                 "vertical decision in the game is measured against it.")]
        public float jumpHeightMetres = 1.4f;
    }
}
