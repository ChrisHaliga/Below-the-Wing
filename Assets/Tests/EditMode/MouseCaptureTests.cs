using BelowTheWing.Crew;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// When the game holds the mouse pointer, and when it hands it back to the desktop.
    ///
    /// Holding it means the pointer is pinned to the middle of the window and invisible, so that
    /// swinging the camera cannot walk the pointer onto a second monitor and leave the player
    /// clicking on something else. The cost of holding it is that the player cannot reach anything
    /// outside the game, so the ways out matter at least as much as the way in, and most of what
    /// is below is about those.
    ///
    /// These are the rules by themselves. What acts on them is a component with a lifecycle, and
    /// an editor test run never starts one, so that half lives in the play mode tests.
    /// </summary>
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
