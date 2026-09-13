using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// What a player is asking their character to do, independent of what they pressed to ask it.
    ///
    /// The same separation vehicles use. A character on the apron never reads a keyboard directly,
    /// which is what lets one belong to a player, another belong to somebody on a different
    /// machine, and a third be walked about by a test, without any of them being a special case.
    /// </summary>
    public readonly struct CrewIntent
    {
        /// <summary>
        /// Which way to walk, relative to the camera. Length up to 1, so a half-pushed stick
        /// asks for a half-speed walk. Doubles as steering and throttle while driving.
        /// </summary>
        public readonly Vector2 Move;

        /// <summary>Whether to run rather than walk.</summary>
        public readonly bool Sprint;

        /// <summary>How hard the brake is being asked for, from 0 to 1. Ignored on foot.</summary>
        public readonly float Brake;

        /// <summary>Whether a jump was asked for this step.</summary>
        public readonly bool Jump;

        /// <summary>
        /// Whether they are holding on to whatever they are riding.
        ///
        /// Occupies the hands, which is the point of it: holding on is a trade a rider makes rather
        /// than a rule the game enforces, and it gives the driver and the passenger something to
        /// shout at each other about.
        /// </summary>
        public readonly bool HoldingOn;

        /// <summary>
        /// Whether they are asking to be crouched.
        ///
        /// Held rather than toggled, so that letting go is asking to stand. The asking is what gets
        /// remembered: a request to stand under a cart roof is granted the moment they walk out,
        /// rather than being dropped because it could not be granted the step it was made.
        /// </summary>
        public readonly bool Crouch;

        public CrewIntent(
            Vector2 move,
            bool sprint = false,
            float brake = 0f,
            bool jump = false,
            bool holdingOn = false,
            bool crouch = false)
        {
            Move = move;
            Sprint = sprint;
            Brake = brake;
            Jump = jump;
            HoldingOn = holdingOn;
            Crouch = crouch;
        }

        /// <summary>A character being asked to do nothing.</summary>
        public static CrewIntent Idle => new CrewIntent(Vector2.zero);
    }

    /// <summary>Where a character gets its <see cref="CrewIntent"/> from.</summary>
    public interface ICrewIntentSource
    {
        CrewIntent Current { get; }
    }
}
