using BelowTheWing.Cargo;
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

        [Tooltip("The most whatever is underfoot can drag them with, in metres per second squared. " +
                 "This is the whole of their grip -- the body has no friction of its own -- and it " +
                 "is what a corner has to beat to throw a rider off a cart deck. A real figure: 10 " +
                 "sits above the 8 a tractor pulls away at, so a launch keeps its riders, and below " +
                 "the 12 to 15 a cart makes cornering hard at speed, so a corner takes them.")]
        public float footGripMetresPerSecondSquared = 10f;

        [Tooltip("How quickly their own walking changes their speed relative to what they are " +
                 "standing on, in metres per second squared. A feel figure rather than a physical " +
                 "one: real legs manage about the grip figure, which is half a second to walking " +
                 "pace and is felt as the controls going soft. It only applies while they still " +
                 "have their feet -- see Footing.")]
        public float gaitResponseMetresPerSecondSquared = 30f;

        [Header("Jumping")]
        [Tooltip("How high a standing jump clears, in metres. Tuned to reach a cart deck with room " +
                 "to spare, because that is the one thing jumping is for -- and once set, every " +
                 "vertical decision in the game is measured against it.")]
        public float jumpHeightMetres = 1.4f;

        [Header("Hands")]
        [Tooltip("How far they reach, how hard they grip, and how hard they throw.")]
        public HandSettings hands = HandSettings.Default;
    }
}
