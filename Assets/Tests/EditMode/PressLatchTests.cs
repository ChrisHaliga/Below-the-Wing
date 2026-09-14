using BelowTheWing.Crew;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// A press that survives long enough for the physics to see it.
    ///
    /// Keys are read once per frame and acted on once per physics step, and the two do not line up:
    /// physics runs at a fixed fifty steps a second while frames come as fast as the machine can
    /// draw them. At two hundred frames a second, three frames out of four contain no physics step
    /// at all, so a press noticed during one of them is overwritten by the next frame's reading
    /// before anything could act on it. Jumping then works about one press in four, which reads as
    /// a jump key that does not work.
    /// </summary>
    public sealed class PressLatchTests
    {
        [Test]
        public void APressSurvivesFramesThatCarryNoPhysicsStep()
        {
            var latch = new PressLatch();
            latch.Ask(physicsTime: 4d);

            // Three frames drawn, no step run in any of them: physics time has not moved.
            for (var frame = 0; frame < 3; frame++)
            {
                latch.ForgetOnceAStepHasSeenIt(physicsTime: 4d);
            }

            Assert.That(latch.Asked, Is.True,
                "the press was thrown away by a frame that drew without stepping. That is most of " +
                "them on any machine quick enough to run this game well");
        }

        [Test]
        public void APressIsForgottenOnceAStepHasRun()
        {
            var latch = new PressLatch();
            latch.Ask(physicsTime: 4d);

            latch.ForgetOnceAStepHasSeenIt(physicsTime: 4.02d);

            Assert.That(latch.Asked, Is.False,
                "a press held past the step that saw it is a second jump nobody asked for");
        }

        [Test]
        public void HoldingTheKeyDownIsStillOnePress()
        {
            var latch = new PressLatch();
            latch.Ask(physicsTime: 4d);
            latch.ForgetOnceAStepHasSeenIt(physicsTime: 4.02d);

            // Held, not pressed again: nothing asks a second time.
            latch.ForgetOnceAStepHasSeenIt(physicsTime: 4.04d);

            Assert.That(latch.Asked, Is.False, "holding the key is one jump, not a jump every step");
        }

        [Test]
        public void AskingAgainBeforeTheFirstWasSeenIsStillOnePress()
        {
            var latch = new PressLatch();
            latch.Ask(physicsTime: 4d);
            latch.Ask(physicsTime: 4d);

            latch.ForgetOnceAStepHasSeenIt(physicsTime: 4.02d);

            Assert.That(latch.Asked, Is.False,
                "two presses inside one physics step are one jump: there is only one step for them " +
                "to be taken in");
        }
    }
}
