using UnityEngine;

namespace BelowTheWing.Crew
{
    public readonly struct CrewIntent
    {
        public readonly Vector2 Move;

        public readonly bool Sprint;

        public readonly float Brake;

        public readonly bool Jump;

        public readonly bool Crouch;

        public CrewIntent(
            Vector2 move,
            bool sprint = false,
            float brake = 0f,
            bool jump = false,
            bool crouch = false)
        {
            Move = move;
            Sprint = sprint;
            Brake = brake;
            Jump = jump;
            Crouch = crouch;
        }

        public static CrewIntent Idle => new CrewIntent(Vector2.zero);
    }

    public interface ICrewIntentSource
    {
        CrewIntent Current { get; }
    }
}
