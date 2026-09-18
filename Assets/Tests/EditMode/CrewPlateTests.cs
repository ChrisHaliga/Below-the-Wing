using BelowTheWing.Menu;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CrewPlateTests
    {
        [Test]
        public void SomebodyWhoHasNotReadiedSaysSoInGrey()
        {
            Assert.That(CrewPlate.Says(ready: false), Is.EqualTo("UNREADY"));
            Assert.That(CrewPlate.Colour(ready: false), Is.EqualTo(MenuLook.InkSoft));
        }

        [Test]
        public void SomebodyWhoHasReadiedSaysSoInGreen()
        {
            Assert.That(CrewPlate.Says(ready: true), Is.EqualTo("READY"));
            Assert.That(CrewPlate.Colour(ready: true), Is.EqualTo(MenuLook.Good));
        }

        [Test]
        public void APlateSitsWhereItsHeadIsWithTheVerticalFlipped()
        {
            var at = CrewPlate.OnPanel(new Vector3(0.25f, 0.75f, 4f), new Vector2(1600f, 900f));

            Assert.That(at.x, Is.EqualTo(400f).Within(1e-3f));
            Assert.That(at.y, Is.EqualTo(225f).Within(1e-3f),
                "a viewport counts up from the bottom and a panel counts down from the top");
        }

        [Test]
        public void AHeadBehindTheCameraGetsNoPlate()
        {
            Assert.That(CrewPlate.Visible(new Vector3(0.5f, 0.5f, -3f)), Is.False);
        }

        [Test]
        public void AHeadOffTheSideOfTheFrameGetsNoPlate()
        {
            Assert.That(CrewPlate.Visible(new Vector3(1.4f, 0.5f, 6f)), Is.False);
            Assert.That(CrewPlate.Visible(new Vector3(0.5f, -0.2f, 6f)), Is.False);
        }

        [Test]
        public void AHeadInFrameGetsAPlate()
        {
            Assert.That(CrewPlate.Visible(new Vector3(0.5f, 0.6f, 6f)), Is.True);
        }
    }
}
