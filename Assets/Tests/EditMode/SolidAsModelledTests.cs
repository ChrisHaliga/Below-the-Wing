using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class SolidAsModelledTests
    {
        [Test]
        public void ATractorIsSolidInTheShapeOfItsModelRatherThanOneBox()
        {
            var parts = ShippedContent.Shape(ShippedContent.TractorPrefabPath).SolidParts;

            Assert.That(parts.Count, Is.GreaterThan(1),
                "a tractor described as a single box is solid from the tarmac to above head height " +
                "across its whole length: there is nothing to stand on, no gap between its axles to " +
                "walk through, and the open sides of it are a wall");

            foreach (var part in parts)
            {
                Assert.That(part.IsAPieceOfTheModel, Is.True,
                    $"'{part.Name}' is a box standing in for a piece of a machine that was modelled. " +
                    "What a tractor is solid in is what it looks like, or the two drift apart the " +
                    "first time somebody moves anything");
            }
        }

        [Test]
        public void ATractorIsSolidInTheMudguardSomebodyStepsOnToGetUp()
        {
            var shape = ShippedContent.Shape(ShippedContent.TractorPrefabPath);

            var stepped = default(VehicleShape.SolidPart);
            var found = false;

            foreach (var part in shape.SolidParts)
            {
                if (part.Name != "Tire_Cover")
                {
                    continue;
                }

                stepped = part;
                found = true;
            }

            Assert.That(found, Is.True,
                "the mudguard over the rear wheel is what somebody puts a foot on to get into one " +
                "of these. Named rather than found by a height band, because a band wide enough to " +
                "catch it also catches the seat cushions, and a test that settles for whichever " +
                "part it happens to reach last passes with nothing to stand on at all");

            Assert.That(stepped.IsAPieceOfTheModel, Is.True,
                "a box around a mudguard fills the arch underneath it, so the wheel it covers is " +
                "solid to the ground and the tractor cannot be walked past");

            var rearWheel = default(VehicleShape.WheelPlacement);
            var haveOne = false;

            foreach (var wheel in shape.Wheels)
            {
                if (wheel.CentreLocal.z >= 0f || (haveOne && wheel.CentreLocal.x * stepped.CentreLocal.x <= 0f))
                {
                    continue;
                }

                rearWheel = wheel;
                haveOne = true;
            }

            Assert.That(haveOne, Is.True, "a tractor with no rear wheel has nothing to cover");
            Assert.That(stepped.CentreLocal.y, Is.GreaterThan(rearWheel.CentreLocal.y),
                $"'{stepped.Name}' sits at {stepped.CentreLocal.y:F3} m against a rear wheel centred " +
                $"at {rearWheel.CentreLocal.y:F3} m. Below the axle it is not a step, it is a skirt");
        }

        [Test]
        public void ACartIsStillSolidInBoxesBecauseADeckAndALipAreBoxes()
        {
            var parts = ShippedContent.Shape(ShippedContent.CartPrefabPath).SolidParts;

            Assert.That(parts, Is.Not.Empty);

            foreach (var part in parts)
            {
                Assert.That(part.IsAPieceOfTheModel, Is.False,
                    $"'{part.Name}' is a slab of a cart: a deck, a lip, an end or a roof. Taking " +
                    "these off the model instead would hand the load space over to whatever shape " +
                    "the bodywork mesh happens to be, and a cart's interior is the one thing about " +
                    "it that is described rather than drawn");
            }
        }
    }
}
