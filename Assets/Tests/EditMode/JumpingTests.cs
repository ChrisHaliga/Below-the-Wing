using BelowTheWing.Crew;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Getting off the ground.
    ///
    /// Jump height is tuned to one thing -- reaching a cart deck -- and then quietly governs every
    /// vertical decision in the game afterwards: how high a pit lip can be, whether a belt loader
    /// can be climbed, whether a wing is something you can get under. Worth being a number somebody
    /// chose rather than an impulse that happened to feel right.
    /// </summary>
    public sealed class JumpingTests
    {
        const float Gravity = 9.81f;

        [Test]
        public void JumpingHigherNeedsMoreSpeed()
        {
            Assert.That(Jumping.TakeOffSpeed(2f, Gravity), Is.GreaterThan(Jumping.TakeOffSpeed(1f, Gravity)));
        }

        [Test]
        public void TheSpeedAskedForIsTheSpeedThatReachesThatHeight()
        {
            const float wanted = 1.4f;

            var speed = Jumping.TakeOffSpeed(wanted, Gravity);

            // Where the upward speed runs out, which is the top of the arc.
            var reached = speed * speed / (2f * Gravity);

            Assert.That(reached, Is.EqualTo(wanted).Within(0.01f),
                "asking for a height and getting a different one makes every clearance in the game " +
                "a guess -- the deck, the pit lip, and whatever comes after");
        }

        [Test]
        public void AskingForNoHeightAsksForNoJump()
        {
            Assert.That(Jumping.TakeOffSpeed(0f, Gravity), Is.Zero);
        }

        [Test]
        public void ANonsenseHeightDoesNotProduceANonsenseJump()
        {
            Assert.That(Jumping.TakeOffSpeed(-3f, Gravity), Is.Zero,
                "a negative height is somebody's typo, and the answer to it is not a downward launch");
        }

        [Test]
        public void ADefaultJumpClearsACartDeck()
        {
            var crew = ScriptableObject.CreateInstance<CrewProfile>();

            // The deck sits on top of a cart, which is what jumping exists to get onto.
            const float deckHeight = 0.94f;

            Assert.That(crew.jumpHeightMetres, Is.GreaterThan(deckHeight),
                "if a standing jump cannot reach a cart deck then riding is unreachable, and riding " +
                "is half of what this slice is for");

            Object.DestroyImmediate(crew);
        }
    }
}
