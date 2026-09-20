using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class ShippedCartTests
    {
        VehicleShape Shape() => ShippedContent.Shape(ShippedContent.CartPrefabPath);

        [Test]
        public void NothingSolidStandsBetweenTheDeckAndAnOpenDoor()
        {
            var shape = Shape();
            var deckTop = shape.InteriorLocal.min.y;
            var side = shape.InteriorLocal.max.x;

            foreach (var part in shape.SolidParts)
            {
                var room = VehicleShape.TheRoomAPartTakesUp(part);

                if (room.max.y <= deckTop + 0.01f || room.min.y >= shape.InteriorLocal.max.y)
                {
                    continue;
                }

                Assert.That(Mathf.Abs(room.center.x), Is.LessThan(side - 0.01f),
                    $"'{part.Name}' stands {Mathf.Abs(room.center.x):0.00} m out at the cart's own " +
                    $"side of {side:0.00} m, above the deck. A wall there holds bags in whatever " +
                    "the doors are doing, so an open door opens onto nothing");
            }
        }

        [Test]
        public void TheCartCarriesNoFabricatedRetainingLip()
        {
            foreach (var part in Shape().SolidParts)
            {
                Assert.That(part.Name, Does.Not.Contain("Lip"),
                    $"'{part.Name}' is a box invented by the measuring rather than a piece of the " +
                    "model, and it runs the full length of a side the doors are supposed to open");
            }
        }

        [Test]
        public void TheCartsDoorsAreTunedOnItsOwnAssetRatherThanInCode()
        {
            var rail = ShippedContent.Load<VehicleProfile>(ShippedContent.CartProfilePath).doorRail;

            Assert.That(rail.poleKg, Is.GreaterThan(0f),
                "the cart's profile carries a door rail of all zeroes, so its doors are weightless " +
                "and nothing holds them anywhere. A field added to a profile defaults to zero on an " +
                "asset that already existed, whatever the field's initialiser says");

            Assert.That(rail.latchHoldsAtNewtons, Is.InRange(400f, 3000f));
            Assert.That(rail.seatsAtNewtons, Is.GreaterThan(0f));
            Assert.That(rail.seatedWithinFraction, Is.GreaterThan(0f).And.LessThan(0.5f));
        }
    }
}
