using BelowTheWing.Crew;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class PressLatchTests
    {
        [Test]
        public void APressSurvivesFramesThatCarryNoPhysicsStep()
        {
            var latch = new PressLatch();
            latch.Ask(physicsTime: 4d);

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
