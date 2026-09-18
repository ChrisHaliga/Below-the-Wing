using BelowTheWing.Cargo;
using BelowTheWing.Wiring;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CargoOwnershipTests
    {
        const ulong Server = 0;
        const ulong Us = 1;
        const ulong Them = 2;

        [Test]
        public void WhoeverTakesHoldOfABagOwnsIt()
        {
            Assert.That(CargoOwnership.WhoShouldOwn(owner: Server, thisMachine: Us, heldHere: true,
                    restingOnVehicleOwnedBy: Shift.Nobody),
                Is.EqualTo(Us));
        }

        [Test]
        public void ABagKeepsItsOwnerThroughAThrowAndWhereverItLands()
        {
            Assert.That(CargoOwnership.WhoShouldOwn(owner: Us, thisMachine: Us, heldHere: false,
                    restingOnVehicleOwnedBy: Shift.Nobody),
                Is.EqualTo(Us),
                "lying on the tarmac, or on another player, is nobody else's business");
        }

        [Test]
        public void ABagAtRestOnSomebodyElsesVehicleBecomesTheirs()
        {
            Assert.That(CargoOwnership.WhoShouldOwn(owner: Us, thisMachine: Us, heldHere: false,
                    restingOnVehicleOwnedBy: Them),
                Is.EqualTo(Them),
                "bag and cart have to be one machine's physics, or a corner throws the bag on one " +
                "screen and not the other");
        }

        [Test]
        public void ABagAtRestOnYourOwnVehicleStaysYours()
        {
            Assert.That(CargoOwnership.WhoShouldOwn(owner: Us, thisMachine: Us, heldHere: false,
                    restingOnVehicleOwnedBy: Us),
                Is.EqualTo(Us));
        }

        [Test]
        public void TakingHoldWinsOverResting()
        {
            Assert.That(CargoOwnership.WhoShouldOwn(owner: Them, thisMachine: Us, heldHere: true,
                    restingOnVehicleOwnedBy: Them),
                Is.EqualTo(Us),
                "a hand on it beats a deck under it: whoever is about to throw it is the one whose " +
                "physics the throw has to come from");
        }
    }
}
