using BelowTheWing.Menu;
using BelowTheWing.Vehicles;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class MenuCartDoorsTests
    {
        [Test]
        public void HalfTheSwingOpensHalfTheDoor()
        {
            Assert.That(
                MenuCartDoors.Toward(0f, 1f, seconds: 1.1f, step: 0.55f),
                Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void AShutDoorStopsShutRatherThanCarryingOnPastIt()
        {
            Assert.That(
                MenuCartDoors.Toward(1f, 0f, seconds: 1.1f, step: 3f),
                Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void AnOpenDoorStopsOpenRatherThanCarryingOnPastIt()
        {
            Assert.That(
                MenuCartDoors.Toward(0f, 1f, seconds: 1.1f, step: 3f),
                Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void ADoorWithNoSwingTimeIsWhereItWasAskedToBeImmediately()
        {
            Assert.That(
                MenuCartDoors.Toward(0f, 1f, seconds: 0f, step: 0.001f),
                Is.EqualTo(1f).Within(1e-4f),
                "a zero swing must not divide by zero and must not leave the door stuck shut");
        }

        [Test]
        public void ADoorHalfOpenCarriesTheBlendWeightItsPhysicsDriverWouldHaveGiven()
        {
            Assert.That(
                MenuCartDoors.WeightFor(0.25f),
                Is.EqualTo(SlidingDoor.ShapeWeight(0.25f)).Within(1e-4f),
                "the menu cart and the played cart use one blend shape between them, so a menu " +
                "that invents its own scale drifts from the game the moment either changes");
        }
    }
}
