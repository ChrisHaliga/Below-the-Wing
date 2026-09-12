using BelowTheWing.Crew;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    /// <summary>
    /// What a player is holding down right now, changeable mid-test.
    ///
    /// A character never reads a keyboard; it asks something what is being asked of it. This is
    /// that something, for a test: set the fields to what the player would be pressing and the
    /// character does what it would do for a player.
    /// </summary>
    public sealed class HeldKeys : ICrewIntentSource
    {
        public Vector2 Move;
        public bool Sprint;
        public bool Jump;
        public bool HoldingOn;
        public bool Crouch;

        public HeldKeys(Vector2 move = default, bool sprint = false)
        {
            Move = move;
            Sprint = sprint;
        }

        public CrewIntent Current
            => new CrewIntent(Move, Sprint, jump: Jump, holdingOn: HoldingOn, crouch: Crouch);
    }
}
