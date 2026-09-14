using BelowTheWing.Crew;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    public sealed class HeldKeys : ICrewIntentSource
    {
        public Vector2 Move;
        public bool Sprint;
        public bool Jump;
        public bool Crouch;

        public HeldKeys(Vector2 move = default, bool sprint = false)
        {
            Move = move;
            Sprint = sprint;
        }

        public CrewIntent Current
            => new CrewIntent(Move, Sprint, jump: Jump, crouch: Crouch);
    }
}
