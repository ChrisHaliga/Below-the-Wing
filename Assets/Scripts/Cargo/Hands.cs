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

        public Vector3? BothHoldingAt
        {
            get
            {
                if (Left.HoldingOnto == null || Right.HoldingOnto == null)
                {
                    return null;
                }

                var left = Left.HoldingAt;
                var right = Right.HoldingAt;

                return left.HasValue && right.HasValue
                    ? (left.Value + right.Value) * 0.5f
                    : (Vector3?)null;
            }
        }

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
