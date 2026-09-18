using System.Collections.Generic;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class WheelSharesTests
    {
        static List<VehicleShape.WheelPlacement> Wheels(params float[] zs)
        {
            var wheels = new List<VehicleShape.WheelPlacement>(zs.Length);

            foreach (var z in zs)
            {
                wheels.Add(new VehicleShape.WheelPlacement(new Vector3(0f, 0.2f, z), 0.2f));
            }

            return wheels;
        }

        [Test]
        public void AFourWheelerDrivesThroughItsTwoRearWheelsAndSteersWithTheFront()
        {
            var roles = WheelShares.Of(Wheels(1f, 1f, -1f, -1f));

            Assert.That(roles[0].Steers, Is.True);
            Assert.That(roles[2].Steers, Is.False);
            Assert.That(roles[0].DriveShare, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(roles[2].DriveShare, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(roles[3].DriveShare, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void ASixWheelerStillPutsTheWholeDriveForceThroughItsRearWheels()
        {
            var roles = WheelShares.Of(Wheels(1f, 1f, -0.5f, -0.5f, -1f, -1f));

            var drive = 0f;
            foreach (var role in roles)
            {
                drive += role.DriveShare;
            }

            Assert.That(drive, Is.EqualTo(1f).Within(1e-4f),
                "the old formula was 2 over the wheel count per rear wheel, which hands a six " +
                "wheeler four sixths of its engine and nobody notices until one is built");
            Assert.That(roles[2].DriveShare, Is.EqualTo(0.25f).Within(1e-4f));
        }

        [Test]
        public void EveryWheelBrakesAnEqualShareWhateverTheCount()
        {
            foreach (var count in new[] { 3, 4, 6 })
            {
                var zs = new float[count];
                for (var i = 0; i < count; i++)
                {
                    zs[i] = i - (count * 0.5f);
                }

                var roles = WheelShares.Of(Wheels(zs));
                var brake = 0f;

                foreach (var role in roles)
                {
                    Assert.That(role.BrakeShare, Is.EqualTo(1f / count).Within(1e-4f), $"{count} wheels");
                    brake += role.BrakeShare;
                }

                Assert.That(brake, Is.EqualTo(1f).Within(1e-4f), $"{count} wheels");
            }
        }

        [Test]
        public void WheelsAllOnOneAxleAreAllRearWheels()
        {
            var roles = WheelShares.Of(Wheels(0f, 0f));

            Assert.That(roles[0].Steers, Is.False, "nothing is ahead of the middle, so nothing steers");
            Assert.That(roles[0].DriveShare, Is.EqualTo(0.5f).Within(1e-4f), "and the drive is still shared out fully");
        }

        [Test]
        public void RestCompressionDividesTheWeightOverTheWheelsItActuallyHas()
        {
            var tractor = TestProfiles.Tractor();

            try
            {
                var overFour = VehicleController.SuspensionCompressionAtRest(tractor, wheelCount: 4);
                var overSix = VehicleController.SuspensionCompressionAtRest(tractor, wheelCount: 6);

                Assert.That(overSix, Is.EqualTo(overFour * 4f / 6f).Within(1e-4f),
                    "a literal four corners hangs a six wheeler too low: each spring is asked to carry " +
                    "a quarter of the weight when it carries a sixth");
            }
            finally
            {
                Object.DestroyImmediate(tractor);
            }
        }
    }
}
