using BelowTheWing.Crew;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class MouseCaptureTests
    {
        static bool Next(bool held, bool focus, bool escape, bool click)
            => MouseCaptureRules.HeldAfterThisFrame(held, focus, escape, click);

        [Test]
        public void PressingEscapeHandsThePointerBack()
        {
            Assert.That(Next(held: true, focus: true, escape: true, click: false), Is.False,
                "Escape is the only way out a player finds without being told about it");
        }

        [Test]
        public void TheWindowLosingFocusHandsThePointerBack()
        {
            Assert.That(Next(held: true, focus: false, escape: false, click: false), Is.False,
                "alt-tabbing away must not leave the desktop with no pointer on it");
        }

        [Test]
        public void HoldingThePointerSurvivesAFrameWithNothingPressed()
        {
            Assert.That(Next(held: true, focus: true, escape: false, click: false), Is.True,
                "the ordinary frame: nothing happened, so nothing changes");
        }

        [Test]
        public void PressingEscapeWhenThePointerIsAlreadyFreeChangesNothing()
        {
            Assert.That(Next(held: false, focus: true, escape: true, click: false), Is.False);
        }

        [Test]
        public void ClickingInTheWindowTakesThePointerBack()
        {
            Assert.That(Next(held: false, focus: true, escape: false, click: true), Is.True,
                "a click is how a player asks for the pointer back after Escape");
        }

        [Test]
        public void FocusReturningWithoutAClickLeavesThePointerAlone()
        {
            Assert.That(Next(held: false, focus: true, escape: false, click: false), Is.False,
                "moving the pointer back over the window must not swallow it; it has to be asked for");
        }

        [Test]
        public void ClickingWhileTheWindowIsInTheBackgroundTakesNothing()
        {
            Assert.That(Next(held: false, focus: false, escape: false, click: true), Is.False,
                "a click that landed elsewhere on the desktop is not a click on the game");
        }

        [Test]
        public void EscapeBeatsAClickArrivingInTheSameFrame()
        {
            Assert.That(Next(held: true, focus: true, escape: true, click: true), Is.False,
                "the way out has to work on the first press, whatever else the mouse is doing");
        }
    }
}
