using UnityEngine;

namespace BelowTheWing.Cargo
{
    public sealed class Hands
    {
        public const string LeftAnchorName = "Left Hand";

        public const string RightAnchorName = "Right Hand";

        public Hands(Transform leftAnchor, Transform rightAnchor, Rigidbody body, HandSettings settings)
        {
            Left = new Hand(leftAnchor, body, settings);
            Right = new Hand(rightAnchor, body, settings);
        }

        public Hand Left { get; }

        public Hand Right { get; }

        public void Tick()
        {
            Left.Tick();
            Right.Tick();
        }

        public void LetGoOfEverything()
        {
            Left.LetGo();
            Right.LetGo();
        }
    }
}
