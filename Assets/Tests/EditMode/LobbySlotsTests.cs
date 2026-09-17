using BelowTheWing.Menu;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class LobbySlotsTests
    {
        const ulong Host = 1;
        const ulong Second = 2;
        const ulong Third = 3;

        LobbySlots m_Lobby;

        [SetUp]
        public void SetUp() => m_Lobby = new LobbySlots(Host);

        [Test]
        public void AFreshLobbyHasFiveEmptySlots()
        {
            Assert.That(LobbySlots.Capacity, Is.EqualTo(5));
            Assert.That(m_Lobby.Filled, Is.EqualTo(0));

            for (var slot = 0; slot < LobbySlots.Capacity; slot++)
            {
                Assert.That(m_Lobby.Who(slot).HasValue, Is.False, $"slot {slot} starts empty");
            }
        }

        [Test]
        public void TheFirstPlayerToArriveTakesSlotOneAndLeavesFourEmpty()
        {
            m_Lobby.Arrived(Host);

            Assert.That(m_Lobby.Filled, Is.EqualTo(1));
            Assert.That(m_Lobby.Who(0), Is.EqualTo(Host));
            Assert.That(m_Lobby.Who(1).HasValue, Is.False);
        }

        [Test]
        public void EachPlayerToArriveTakesTheNextSlotDown()
        {
            m_Lobby.Arrived(Host);
            m_Lobby.Arrived(Second);

            Assert.That(m_Lobby.Who(0), Is.EqualTo(Host));
            Assert.That(m_Lobby.Who(1), Is.EqualTo(Second));
            Assert.That(m_Lobby.Filled, Is.EqualTo(2));
        }

        [Test]
        public void APlayerWhoIsAlreadyInTheLobbyDoesNotTakeASecondSlot()
        {
            m_Lobby.Arrived(Host);
            m_Lobby.Arrived(Host);

            Assert.That(m_Lobby.Filled, Is.EqualTo(1),
                "a reconnect or a duplicate message would otherwise eat a seat nobody is in");
        }

        [Test]
        public void ASixthPlayerIsTurnedAway()
        {
            for (ulong player = 1; player <= 5; player++)
            {
                Assert.That(m_Lobby.Arrived(player), Is.True);
            }

            Assert.That(m_Lobby.Arrived(6), Is.False, "there are five slots and no more");
            Assert.That(m_Lobby.Filled, Is.EqualTo(5));
        }

        [Test]
        public void APlayerLeavingFreesTheirSlotForTheNextArrival()
        {
            m_Lobby.Arrived(Host);
            m_Lobby.Arrived(Second);
            m_Lobby.Left(Host);

            Assert.That(m_Lobby.Filled, Is.EqualTo(1));
            Assert.That(m_Lobby.Who(0).HasValue, Is.False);
            Assert.That(m_Lobby.Who(1), Is.EqualTo(Second),
                "the player who stayed keeps the slot they were in, so the lobby does not " +
                "reshuffle under everyone every time somebody drops");
        }

        [Test]
        public void APlayerArrivesNotReady()
        {
            m_Lobby.Arrived(Host);

            Assert.That(m_Lobby.IsReady(Host), Is.False);
        }

        [Test]
        public void ReadyingUpAndTakingItBackBothStick()
        {
            m_Lobby.Arrived(Host);

            m_Lobby.Ready(Host, true);
            Assert.That(m_Lobby.IsReady(Host), Is.True);

            m_Lobby.Ready(Host, false);
            Assert.That(m_Lobby.IsReady(Host), Is.False);
        }

        [Test]
        public void APlayerWhoLeavesAndComesBackIsNotStillReady()
        {
            m_Lobby.Arrived(Host);
            m_Lobby.Ready(Host, true);
            m_Lobby.Left(Host);
            m_Lobby.Arrived(Host);

            Assert.That(m_Lobby.IsReady(Host), Is.False,
                "a stale ready flag would let a shift start on somebody who is still loading in");
        }

        [Test]
        public void AHostAloneAndReadyCanStartTheShift()
        {
            m_Lobby.Arrived(Host);
            m_Lobby.Ready(Host, true);

            Assert.That(m_Lobby.CanStart(Host), Is.True,
                "one player has to be able to run a shift on their own");
        }

        [Test]
        public void AHostWhoHasNotReadiedUpCannotStartTheShift()
        {
            m_Lobby.Arrived(Host);

            Assert.That(m_Lobby.CanStart(Host), Is.False,
                "the host is a player like any other, and readying up is what says they are set");
        }

        [Test]
        public void TheShiftWaitsOnThePlayerWhoIsNotReadyYet()
        {
            m_Lobby.Arrived(Host);
            m_Lobby.Arrived(Second);
            m_Lobby.Ready(Host, true);

            Assert.That(m_Lobby.EveryoneReady, Is.False);
            Assert.That(m_Lobby.CanStart(Host), Is.False);

            m_Lobby.Ready(Second, true);

            Assert.That(m_Lobby.EveryoneReady, Is.True);
            Assert.That(m_Lobby.CanStart(Host), Is.True);
        }

        [Test]
        public void NobodyButTheHostCanStartTheShift()
        {
            m_Lobby.Arrived(Host);
            m_Lobby.Arrived(Second);
            m_Lobby.Ready(Host, true);
            m_Lobby.Ready(Second, true);

            Assert.That(m_Lobby.CanStart(Second), Is.False);
            Assert.That(m_Lobby.CanStart(Third), Is.False);
            Assert.That(m_Lobby.CanStart(Host), Is.True);
        }

        [Test]
        public void AnEmptyLobbyCannotStartAShift()
        {
            Assert.That(m_Lobby.EveryoneReady, Is.False,
                "nobody being ready is not everybody being ready");
            Assert.That(m_Lobby.CanStart(Host), Is.False);
        }

        [Test]
        public void ReadyingUpAPlayerWhoIsNotInTheLobbyChangesNothing()
        {
            m_Lobby.Arrived(Host);
            m_Lobby.Ready(Host, true);

            m_Lobby.Ready(Third, true);

            Assert.That(m_Lobby.IsReady(Third), Is.False);
            Assert.That(m_Lobby.CanStart(Host), Is.True);
        }
    }
}
