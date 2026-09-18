using BelowTheWing.Session;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class ArrivalPointTests
    {
        [Test]
        public void TheOnlyPlayerTakesTheFirstPoint()
        {
            Assert.That(RampSession.ArrivalFor(0, new ulong[] { 0 }, 5), Is.EqualTo(0));
        }

        [Test]
        public void EveryPlayerTakesADifferentPoint()
        {
            var connected = new ulong[] { 0, 1, 2, 3, 4 };
            var taken = new int[5];

            foreach (var player in connected)
            {
                taken[RampSession.ArrivalFor(player, connected, 5)]++;
            }

            foreach (var point in taken)
            {
                Assert.That(point, Is.EqualTo(1),
                    "two players on one arrival point spawn inside each other and fire apart, " +
                    "which is what probing the world for an empty point used to fail to prevent");
            }
        }

        [Test]
        public void APlayerWorksOutTheSamePointOnEveryMachine()
        {
            var connected = new ulong[] { 7, 2, 9 };

            Assert.That(RampSession.ArrivalFor(9, connected, 5), Is.EqualTo(2),
                "the order is by client id, not by the order a machine happens to list them, or " +
                "two machines put the same player in two places");
            Assert.That(RampSession.ArrivalFor(2, connected, 5), Is.EqualTo(0));
            Assert.That(RampSession.ArrivalFor(7, connected, 5), Is.EqualTo(1));
        }

        [Test]
        public void ASixthPlayerWrapsRatherThanFallingOffTheEnd()
        {
            var connected = new ulong[] { 0, 1, 2, 3, 4, 5 };

            Assert.That(RampSession.ArrivalFor(5, connected, 5), Is.EqualTo(0),
                "standing on somebody is survivable; an exception thrown out of OnNetworkSpawn " +
                "leaves the session half built with only a log line to say so");
        }
    }
}
