using BelowTheWing.Vehicles;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Letting a crash finish before correcting it.
    ///
    /// Correction running through a collision fights the impulse: the vehicle bounces off the way
    /// physics says it should, and then correction -- holding a report taken before the crash and
    /// knowing nothing about it -- drags it back through the impact it just had. That reads as the
    /// game refusing what the player just did.
    /// </summary>
    public sealed class ContactBlackoutTests
    {
        static readonly BlackoutSettings Settings = BlackoutSettings.Default;

        static ContactBlackout Crashed()
        {
            var blackout = new ContactBlackout(Settings);
            blackout.Touched();
            return blackout;
        }

        static void WaitOut(ContactBlackout blackout, float seconds)
        {
            const float step = 0.02f;
            for (var spent = 0f; spent < seconds; spent += step)
            {
                blackout.Tick(step);
            }
        }

        [Test]
        public void AVehicleNothingHasTouchedIsFullyCorrected()
        {
            var blackout = new ContactBlackout(Settings);

            Assert.That(blackout.InProgress, Is.False);
            Assert.That(blackout.Authority, Is.EqualTo(1f),
                "an ordinary vehicle that has hit nothing must be kept in step as normal, or every " +
                "vehicle in the session is permanently half-corrected");
        }

        [Test]
        public void NothingInterferesWhileTheCrashIsHappening()
        {
            var blackout = Crashed();

            Assert.That(blackout.InProgress, Is.True);
            Assert.That(blackout.Authority, Is.Zero,
                "this is the moment the impulse is being resolved. Correcting through it drags the " +
                "vehicle back through the collision it just had");
        }

        [Test]
        public void CorrectionStaysOutOfItForTheWholeWindow()
        {
            var blackout = Crashed();
            WaitOut(blackout, Settings.silenceSeconds * 0.9f);

            Assert.That(blackout.Authority, Is.Zero, "the crash is not over yet");
        }

        [Test]
        public void CorrectionComesBackGraduallyRatherThanAllAtOnce()
        {
            var blackout = Crashed();
            WaitOut(blackout, Settings.silenceSeconds + (Settings.easeBackSeconds * 0.5f));

            var partWayBack = blackout.Authority;

            Assert.That(partWayBack, Is.GreaterThan(0f), "the silence is over, so it has to be coming back");
            Assert.That(partWayBack, Is.LessThan(1f),
                "switching correction back on at full strength is itself a lurch, and it arrives while " +
                "the player is still looking at the wreck");
        }

        [Test]
        public void OnceTheVehicleHasSettledItIsCorrectedNormallyAgain()
        {
            var blackout = Crashed();
            WaitOut(blackout, Settings.silenceSeconds + Settings.easeBackSeconds + 0.1f);

            Assert.That(blackout.Authority, Is.EqualTo(1f).Within(1e-3f),
                "a vehicle permanently under-corrected after its first bump drifts further out of " +
                "step with every knock it takes");
        }

        [Test]
        public void BeingHitAgainMidRecoveryStartsTheWholeWindowOver()
        {
            var blackout = Crashed();
            WaitOut(blackout, Settings.silenceSeconds + (Settings.easeBackSeconds * 0.5f));
            Assert.That(blackout.Authority, Is.GreaterThan(0f), "precondition: it had started recovering");

            blackout.Touched();

            Assert.That(blackout.Authority, Is.Zero,
                "a second impact is a second crash. Correction resuming part way through a pile-up " +
                "hauls a vehicle out of an impact that is still happening");
        }
    }
}
