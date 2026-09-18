using BelowTheWing.Menu;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CrewPlateTests
    {
        [Test]
        public void SomebodyWhoHasNotReadiedShowsAnEmptyRing()
        {
            Assert.That(CrewPlate.Fill(ready: false).a, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(CrewPlate.Ring(ready: false), Is.EqualTo(MenuLook.InkSoft));
            Assert.That(CrewPlate.TickShows(ready: false), Is.False);
        }

        [Test]
        public void SomebodyWhoHasReadiedShowsAFilledGreenRingWithATickInIt()
        {
            Assert.That(CrewPlate.Fill(ready: true), Is.EqualTo(MenuLook.Good));
            Assert.That(CrewPlate.Ring(ready: true), Is.EqualTo(MenuLook.Good));
            Assert.That(CrewPlate.TickShows(ready: true), Is.True);
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
