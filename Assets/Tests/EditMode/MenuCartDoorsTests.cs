using BelowTheWing.Menu;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class MenuCartDoorsTests
    {
        [Test]
        public void TheDoorsOnTheCameraSideOfTheCartGoFirst()
        {
            Assert.That(MenuCartDoors.WaitFor(nearTheCamera: true, secondSetWaits: 0.35f),
                Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void TheDoorsOnTheFarSideWaitOutTheStagger()
        {
            Assert.That(MenuCartDoors.WaitFor(nearTheCamera: false, secondSetWaits: 0.35f),
                Is.EqualTo(0.35f).Within(1e-4f));
        }

        [Test]
        public void AStaggerSetBelowZeroFiresBothSetsTogetherRatherThanFiringTheFarOnesFirst()
        {
            Assert.That(MenuCartDoors.WaitFor(nearTheCamera: false, secondSetWaits: -2f),
                Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void OpeningShovesAlongTheRailAndShuttingShovesBackTheOtherWay()
        {
            Assert.That(MenuCartDoors.ShoveFor(open: true, newtonSeconds: 55f),
                Is.EqualTo(55f).Within(1e-4f));
            Assert.That(MenuCartDoors.ShoveFor(open: false, newtonSeconds: 55f),
                Is.EqualTo(-55f).Within(1e-4f));
        }

        [Test]
        public void AShoveSetBelowZeroDoesNotOpenTheDoorsByShuttingThem()
        {
            Assert.That(MenuCartDoors.ShoveFor(open: true, newtonSeconds: -12f),
                Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void ADoorOnThePositiveSideOfTheCartIsTheOneTheCameraLooksThrough()
        {
            Assert.That(MenuCartDoors.NearTheCamera(0.83f), Is.True);
            Assert.That(MenuCartDoors.NearTheCamera(-0.83f), Is.False);
        }
    }
}
