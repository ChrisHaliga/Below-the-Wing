using BelowTheWing.Menu;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class SteppingTests
    {
        [Test]
        public void SteppingForwardMovesToTheNextChoice()
        {
            Assert.That(Stepping.Next(1, 4, 1), Is.EqualTo(2));
        }

        [Test]
        public void SteppingPastTheLastChoiceComesBackToTheFirst()
        {
            Assert.That(Stepping.Next(3, 4, 1), Is.EqualTo(0));
        }

        [Test]
        public void SteppingBackFromTheFirstChoiceGoesToTheLast()
        {
            Assert.That(Stepping.Next(0, 4, -1), Is.EqualTo(3));
        }

        [Test]
        public void SteppingAnEmptyListStaysAtNothingRatherThanDividingByZero()
        {
            Assert.That(Stepping.Next(0, 0, 1), Is.EqualTo(0));
        }

        [Test]
        public void SteppingFromOutsideTheListLandsInsideIt()
        {
            Assert.That(Stepping.Next(9, 4, 1), Is.InRange(0, 3));
            Assert.That(Stepping.Next(-7, 4, 1), Is.InRange(0, 3));
        }

        [Test]
        public void NudgingADialMovesItOneStepOfItsRange()
        {
            Assert.That(Stepping.Nudge(0.5f, 0f, 1f, by: 1, steps: 10),
                Is.EqualTo(0.6f).Within(1e-4f));
        }

        [Test]
        public void ADialStopsAtItsEndsRatherThanRunningPastThem()
        {
            Assert.That(Stepping.Nudge(1f, 0f, 1f, by: 1, steps: 10), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(Stepping.Nudge(0f, 0f, 1f, by: -1, steps: 10), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void ADialWithNoStepsDoesNotDivideByZero()
        {
            Assert.That(Stepping.Nudge(0.5f, 0f, 1f, by: 1, steps: 0), Is.EqualTo(1f).Within(1e-4f));
        }
    }
}
