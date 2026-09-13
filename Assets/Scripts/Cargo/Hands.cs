using UnityEngine;

namespace BelowTheWing.Cargo
{
    /// <summary>
    /// A player's two hands.
    ///
    /// Two independent <see cref="Hand"/>s and nothing more: there is no rule here about what the
    /// pair may do together, because there is none. A bag in each, a rail in one and a bag in the
    /// other, both on a rail through a bad corner -- whatever two buttons ask for.
    /// </summary>
    public sealed class Hands
    {
        /// <summary>The name of the transform on a character that marks where the left hand is.</summary>
        public const string LeftAnchorName = "Left Hand";

        /// <summary>The name of the transform on a character that marks where the right hand is.</summary>
        public const string RightAnchorName = "Right Hand";

        public Hands(Transform leftAnchor, Transform rightAnchor, Rigidbody body, HandSettings settings)
        {
            Left = new Hand(leftAnchor, body, settings);
            Right = new Hand(rightAnchor, body, settings);
        }

        public Hand Left { get; }

        public Hand Right { get; }

        /// <summary>Lets both hands notice anything that broke. Called every fixed step.</summary>
        public void Tick()
        {
            Left.Tick();
            Right.Tick();
        }

        /// <summary>Both hands let go of whatever they hold, without throwing anything.</summary>
        public void LetGoOfEverything()
        {
            Left.LetGo();
            Right.LetGo();
        }
    }
}
