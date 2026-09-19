using BelowTheWing.Crew;
using BelowTheWing.Session;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class ShippedBeltLoaderTests
    {
        const string LoaderPrefabPath = ShippedContent.PrefabFolder + "/BeltLoader.prefab";
        const string LoaderProfilePath = "Assets/Content/Vehicles/BeltLoader.asset";

        VehicleShape Shape() => ShippedContent.Shape(LoaderPrefabPath);

        [Test]
        public void TheBeltLoaderRunsOnFourWheelsOfItsOwn()
        {
            var wheels = Shape().Wheels;

            Assert.That(wheels.Count, Is.EqualTo(4), "a belt loader carries four wheels");

            foreach (var wheel in wheels)
            {
                Assert.That(wheel.RadiusMetres, Is.GreaterThan(0.1f).And.LessThan(0.5f),
                    $"a wheel of {wheel.RadiusMetres:0.00} m radius is not one this model has");
            }
        }

        [Test]
        public void TheFrontWheelsSteerAndTheBackWheelsDrive()
        {
            var roles = WheelShares.Of(Shape().Wheels);

            var steering = 0;
            var driving = 0f;

            foreach (var role in roles)
            {
                steering += role.Steers ? 1 : 0;
                driving += role.DriveShare;
            }

            Assert.That(steering, Is.EqualTo(2), "a four wheeler steers on its front pair");
            Assert.That(driving, Is.EqualTo(1f).Within(1e-4f),
                "the drive is shared out fully across whichever wheels are not steering");
        }

        [Test]
        public void TheBeltLoaderTowsNothing()
        {
            var shape = Shape();

            Assert.That(shape.HasFrontCoupling, Is.False,
                "a belt loader has no drawbar, so nothing should be able to tow it into a train");
            Assert.That(shape.HasRearCoupling, Is.False,
                "a belt loader has no hitch, so no cart should be able to follow it to the aircraft");
        }

        [Test]
        public void ADriverSitsOnTheDeckBesideTheBeltRatherThanInsideTheChassis()
        {
            var shape = Shape();
            var seat = shape.SeatLocal;

            Assert.That(seat.HasValue, Is.True,
                "nothing says where a driver sits, so a crew member climbing in is dropped at the " +
                "loader's origin, which is on the ground between its wheels");

            var envelope = new Bounds(shape.EnvelopeCentreLocal, shape.EnvelopeSizeMetres);
            var radius = ShippedContent.Load<CrewProfile>("Assets/Content/Crew/RampWorker.asset").radiusMetres;

            Assert.That(seat.Value.y, Is.GreaterThan(envelope.min.y + 0.4f),
                $"the seat sits {seat.Value.y:0.00} m up, which is below the deck");

            Assert.That(seat.Value.z, Is.LessThan(0f),
                $"the seat sits at z {seat.Value.z:0.00}, ahead of the chassis centre. A loader is " +
                "driven from the back, looking along the belt");

            Assert.That(seat.Value.x, Is.GreaterThan(envelope.min.x).And.LessThan(0f),
                $"the seat at x {seat.Value.x:0.00} is outside the loader's own left edge at " +
                $"{envelope.min.x:0.00}, so the driver is not standing on the machine at all");

            Assert.That(seat.Value.x + radius, Is.LessThan(0.2f),
                $"a driver of {radius:0.00} m radius seated at x {seat.Value.x:0.00} reaches to " +
                $"{seat.Value.x + radius:0.00}, which is across the middle of the belt. " +
                "belt_loader.fbx carries no SEAT marker and its clear deck either side of the belt " +
                "is 0.265 m wide against a 0.6 m body, so the seat is derived and the driver " +
                "overlaps the rails. A SEAT marker in the model is what fixes it properly");
        }

        [Test]
        public void TheBeltLoaderIsSolidInItsChassisAndItsBelt()
        {
            var named = "";

            foreach (var part in Shape().SolidParts)
            {
                named += part.Name + " ";

                Assert.That(part.IsAPieceOfTheModel, Is.True,
                    $"'{part.Name}' is solid as a box rather than in the shape it is drawn");
            }

            Assert.That(named, Does.Contain("Body").And.Contain("Belt"),
                $"the loader is solid in [{named.Trim()}]. A bag dropped on the belt falls through " +
                "anything the belt is not solid in");
        }

        [Test]
        public void AnyoneMayDriveTheBeltLoader()
        {
            var profile = ShippedContent.Load<VehicleProfile>(LoaderProfilePath);

            Assert.That(profile.driveable, Is.True);
            Assert.That(profile.topSpeedMetresPerSecond, Is.GreaterThan(0f).And.LessThan(10f),
                "a loader is an apron machine, not a road one");
            Assert.That(profile.massKg, Is.InRange(2500f, 5000f), "around three and a half tonnes");

            Assert.That(ShippedContent.Prefab(LoaderPrefabPath).GetComponent<VehicleOccupant>(), Is.Not.Null,
                "nothing on the loader tells other machines who is driving it, so two players could " +
                "both take the seat");
        }
    }
}
