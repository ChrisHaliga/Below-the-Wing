using BelowTheWing.Menu;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class ReadyIntentTests
    {
        [Test]
        public void NobodyStartsReady()
        {
            Assert.That(new ReadyIntent().Want, Is.False);
        }

        [Test]
        public void ReadyingBeforeTheRosterExistsIsRememberedRatherThanDropped()
        {
            var intent = new ReadyIntent();

            intent.Toggle();

            Assert.That(intent.Want, Is.True, "the lobby opens before the service answers, and a " +
                                              "player who readied in that gap stays readied");
            Assert.That(intent.NeedsTelling(rosterIsUp: false), Is.False,
                "there is nothing to tell yet");
        }

        [Test]
        public void TheRosterIsToldOnceItArrives()
        {
            var intent = new ReadyIntent();

            intent.Toggle();

            Assert.That(intent.NeedsTelling(rosterIsUp: true), Is.True);

            intent.Told();

            Assert.That(intent.NeedsTelling(rosterIsUp: true), Is.False,
                "telling it again every frame would send one remote call per frame");
        }

        [Test]
        public void ChangingYourMindTellsTheRosterAgain()
        {
            var intent = new ReadyIntent();

            intent.Toggle();
            intent.Told();
            intent.Toggle();

            Assert.That(intent.Want, Is.False);
            Assert.That(intent.NeedsTelling(rosterIsUp: true), Is.True);
        }

        [Test]
        public void ARosterThatNeverHeardTheFirstAnswerHearsItAgainAfterALeave()
        {
            var intent = new ReadyIntent();

            intent.Toggle();
            intent.Told();
            intent.Forget();

            Assert.That(intent.Want, Is.False);

            intent.Toggle();

            Assert.That(intent.NeedsTelling(rosterIsUp: true), Is.True);
        }
    }
}
