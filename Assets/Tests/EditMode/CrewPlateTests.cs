using BelowTheWing.Menu;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CrewPlateTests
    {
        [Test]
        public void EachReadinessShowsItsOwnIcon()
        {
            var was = MenuLook.Icons;

            MenuLook.Icons = new MenuIcons
            {
                Ready = new Texture2D(2, 2),
                Unready = new Texture2D(2, 2)
            };

            Assert.That(CrewPlate.Badge(ready: true), Is.SameAs(MenuLook.Icons.Ready));
            Assert.That(CrewPlate.Badge(ready: false), Is.SameAs(MenuLook.Icons.Unready));

            Object.DestroyImmediate(MenuLook.Icons.Ready);
            Object.DestroyImmediate(MenuLook.Icons.Unready);

            MenuLook.Icons = was;
        }

        [Test]
        public void TheShippedIconsKeepTheShapeTheyWereDrawnIn()
        {
            foreach (var path in new[] { "Assets/UI/Icons/Ready.png", "Assets/UI/Icons/Unready.png" })
            {
                var icon = ShippedContent.Load<Texture2D>(path);
                var shape = icon.width / (float)icon.height;

                Assert.That(shape, Is.EqualTo(1f).Within(0.12f),
                    $"{path} imports at {icon.width} x {icon.height}. Both badges are drawn very " +
                    "nearly square, so an aspect far from 1 means the importer resized it. " +
                    "nPOTScale rounds each side of a non power of two texture separately, and a " +
                    "94 by 97 image rounds to 64 by 128");
            }
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
