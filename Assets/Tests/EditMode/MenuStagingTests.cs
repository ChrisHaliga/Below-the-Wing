using BelowTheWing.Menu;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class MenuStagingTests
    {
        [Test]
        public void TheTitleCardStandsWellBackWithTheCartShut()
        {
            var staging = MenuStaging.For(MenuScreen.Title);

            Assert.That(staging.Station, Is.EqualTo(MenuStation.AsPlaced));
            Assert.That(staging.DoorsOpen, Is.False);
        }

        [Test]
        public void EveryPanelScreenStandsAtTheShutCart()
        {
            foreach (var screen in new[] { MenuScreen.Main, MenuScreen.Join, MenuScreen.Settings })
            {
                var staging = MenuStaging.For(screen);

                Assert.That(staging.Station, Is.EqualTo(MenuStation.Cart), $"{screen}");
                Assert.That(staging.DoorsOpen, Is.False, $"{screen}");
            }
        }

        [Test]
        public void TheLobbyOpensTheCartAndPushesThroughIt()
        {
            var staging = MenuStaging.For(MenuScreen.Lobby);

            Assert.That(staging.Station, Is.EqualTo(MenuStation.Inside));
            Assert.That(staging.DoorsOpen, Is.True);
        }

        [Test]
        public void ThePushThroughTheDoorsTakesLongerThanAFlickBetweenPanels()
        {
            Assert.That(
                MenuStaging.For(MenuScreen.Lobby).TravelSeconds,
                Is.GreaterThan(MenuStaging.For(MenuScreen.Join).TravelSeconds),
                "the doors have to be visibly sliding while the camera moves, and a panel to panel " +
                "move has nothing to wait for");
        }

        [Test]
        public void AClosedMenuIsNotStagedAtAll()
        {
            Assert.That(MenuStaging.For(MenuScreen.None).Staged, Is.False);
        }
    }
}
