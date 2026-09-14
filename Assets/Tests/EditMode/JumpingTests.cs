using BelowTheWing.Crew;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
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

            const float deckHeight = 0.94f;

            Assert.That(crew.jumpHeightMetres, Is.GreaterThan(deckHeight),
                "if a standing jump cannot reach a cart deck then riding is unreachable, and riding " +
                "is the whole reason to be able to jump");

            Object.DestroyImmediate(crew);
        }
    }
}
