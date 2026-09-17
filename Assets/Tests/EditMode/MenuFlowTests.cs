using BelowTheWing.Menu;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class MenuFlowTests
    {
        MenuFlow m_Flow;

        [SetUp]
        public void SetUp() => m_Flow = new MenuFlow();

        [Test]
        public void TheGameOpensOnTheTitleCard()
        {
            Assert.That(m_Flow.Showing, Is.EqualTo(MenuScreen.Title));
        }

        [Test]
        public void AnyButtonOnTheTitleCardMovesOnToTheMainMenu()
        {
            m_Flow.AnyButtonPressed();

            Assert.That(m_Flow.Showing, Is.EqualTo(MenuScreen.Main));
        }

        [Test]
        public void AButtonPressAnywhereElseIsNotAWayForward()
        {
            m_Flow.AnyButtonPressed();
            m_Flow.AnyButtonPressed();

            Assert.That(m_Flow.Showing, Is.EqualTo(MenuScreen.Main),
                "press any key belongs to the title card alone, or a player mashing a key walks " +
                "through every screen in the menu");
        }

        [Test]
        public void EveryScreenReachableFromTheMainMenuComesBackToIt()
        {
            foreach (var screen in new[] { MenuScreen.Join, MenuScreen.Settings })
            {
                m_Flow.Show(screen);
                m_Flow.Back();

                Assert.That(m_Flow.Showing, Is.EqualTo(MenuScreen.Main), $"back out of {screen}");
            }
        }

        [Test]
        public void BackingOutOfTheLobbyLeavesTheSessionBehindOnce()
        {
            var left = 0;
            m_Flow.LeftTheSession += () => left++;
            m_Flow.Show(MenuScreen.Lobby);

            m_Flow.Back();

            Assert.That(m_Flow.Showing, Is.EqualTo(MenuScreen.Main));
            Assert.That(left, Is.EqualTo(1),
                "walking out of a lobby without telling the service leaves a session record and a " +
                "join code that still points at a game nobody is in");
        }

        [Test]
        public void BackingOutOfAnythingElseLeavesTheSessionAlone()
        {
            var left = 0;
            m_Flow.LeftTheSession += () => left++;

            m_Flow.Show(MenuScreen.Settings);
            m_Flow.Back();

            Assert.That(left, Is.EqualTo(0));
        }

        [Test]
        public void ALeaveDoesNotLingerIntoTheNextSession()
        {
            var left = 0;
            m_Flow.LeftTheSession += () => left++;

            m_Flow.Show(MenuScreen.Lobby);
            m_Flow.Back();
            m_Flow.Show(MenuScreen.Join);
            m_Flow.Show(MenuScreen.Lobby);
            m_Flow.ShiftStarted();

            Assert.That(left, Is.EqualTo(1),
                "a leave that is stored as state rather than raised as an event stays true through " +
                "the next lobby, and a consumer reading it on the next change leaves a session the " +
                "player did not back out of");
        }

        [Test]
        public void ThereIsNowhereToGoBackToFromTheTitleCard()
        {
            m_Flow.Back();

            Assert.That(m_Flow.Showing, Is.EqualTo(MenuScreen.Title));
        }

        [Test]
        public void TheMainMenuIsTheBottomOfTheStack()
        {
            m_Flow.AnyButtonPressed();
            m_Flow.Back();

            Assert.That(m_Flow.Showing, Is.EqualTo(MenuScreen.Main),
                "back on the main menu does not drop the player onto the title card again");
        }

        [Test]
        public void StartingTheShiftClosesTheMenuAltogether()
        {
            m_Flow.Show(MenuScreen.Lobby);
            m_Flow.ShiftStarted();

            Assert.That(m_Flow.Showing, Is.EqualTo(MenuScreen.None));
            Assert.That(m_Flow.Open, Is.False);
        }

        [Test]
        public void EveryScreenButNoneCountsAsTheMenuBeingOpen()
        {
            Assert.That(m_Flow.Open, Is.True);

            m_Flow.Show(MenuScreen.Settings);
            Assert.That(m_Flow.Open, Is.True);
        }

        [Test]
        public void AScreenChangeIsAnnouncedOnceSoTheCameraCanFollow()
        {
            var moved = 0;
            var landedOn = MenuScreen.None;

            m_Flow.Changed += screen =>
            {
                moved++;
                landedOn = screen;
            };

            m_Flow.Show(MenuScreen.Settings);
            m_Flow.Show(MenuScreen.Settings);

            Assert.That(moved, Is.EqualTo(1), "showing the screen already up is not a change");
            Assert.That(landedOn, Is.EqualTo(MenuScreen.Settings));
        }
    }
}
