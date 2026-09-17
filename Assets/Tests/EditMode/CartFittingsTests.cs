using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CartFittingsTests
    {
        [Test]
        public void APoleAtTheStartOfItsTravelLeavesTheDoorShut()
        {
            Assert.That(SlidingDoor.OpennessAt(0f, 1.01162f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(SlidingDoor.ShapeWeight(0f), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void APoleAtTheEndOfItsTravelLeavesTheDoorFullyOpen()
        {
            Assert.That(SlidingDoor.OpennessAt(1.01162f, 1.01162f), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(SlidingDoor.ShapeWeight(1f), Is.EqualTo(100f).Within(1e-3f));
        }

        [Test]
        public void APoleDraggedPastItsTravelIsStillReadAsFullyOpenOrFullyShut()
        {
            Assert.That(SlidingDoor.OpennessAt(2.5f, 1.01162f), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(SlidingDoor.OpennessAt(-0.4f, 1.01162f), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void TheCoverRunsFromTheMovingPoleToTheFixedOne()
        {
            Assert.That(SlidingDoor.CoveredMetres(0.02919f, 1.55018f), Is.EqualTo(1.52099f).Within(1e-4f),
                "a shut door covers everything between the two poles, or a bag walks through it");
            Assert.That(SlidingDoor.CoveredMetres(1.04081f, 1.55018f), Is.EqualTo(0.50937f).Within(1e-4f),
                "an open door still has its vinyl bunched at the end, which is still solid");

            Assert.That(SlidingDoor.CoverSitsAt(0.02919f, 1.55018f), Is.EqualTo(0.789685f).Within(1e-4f));
            Assert.That(SlidingDoor.CoverSitsAt(1.04081f, 1.55018f), Is.EqualTo(1.295495f).Within(1e-4f),
                "the cover stays centred between the poles, so it shrinks toward the end as it opens");
        }

        [Test]
        public void ADoorOnTheFarSideOfTheCartCoversTheSameSpanMirrored()
        {
            Assert.That(
                SlidingDoor.CoveredMetres(-0.02919f, -1.55018f), Is.EqualTo(1.52099f).Within(1e-4f));
            Assert.That(
                SlidingDoor.CoverSitsAt(-0.02919f, -1.55018f), Is.EqualTo(-0.789685f).Within(1e-4f));
        }

        [Test]
        public void AnAxleFollowsTheDrawbarItIsPulledBy()
        {
            Assert.That(FrontAxle.PointsAlongDegrees(new Vector3(0f, 0f, 1f), 60f),
                Is.EqualTo(0f).Within(0.5f), "pulled straight ahead, the wheels point straight ahead");

            var pulledLeft = FrontAxle.PointsAlongDegrees(new Vector3(-1f, 0f, 1f), 60f);
            Assert.That(pulledLeft, Is.EqualTo(-45f).Within(0.5f),
                $"pulled forty five degrees off, the axle came round {pulledLeft:F1}. The whole point " +
                "is that the cart takes the angle rather than shoving the tractor about");
        }

        [Test]
        public void AnAxleCannotSwingFurtherThanItsPin()
        {
            Assert.That(FrontAxle.PointsAlongDegrees(new Vector3(-8f, 0f, 0.2f), 60f),
                Is.EqualTo(-60f).Within(0.5f), "a drawbar folded right round still stops at the pin");
        }

        [Test]
        public void ParkingIsRefusedWhileItIsStillRolling()
        {
            var brake = new CartParking();

            Assert.That(brake.Toggle(4f), Is.False,
                "the brake went on at four metres a second, which is a cart being dragged to a halt " +
                "by its handbrake rather than parked");
            Assert.That(brake.Parked, Is.False);
        }

        [Test]
        public void ParkingAndUnparkingAtAStandstillWorks()
        {
            var brake = new CartParking();

            Assert.That(brake.Toggle(0f), Is.True);
            Assert.That(brake.Parked, Is.True);

            Assert.That(brake.Toggle(0f), Is.True);
            Assert.That(brake.Parked, Is.False, "the same key has to let it go again");
        }

        [Test]
        public void AParkedCartCanAlwaysBeReleasedHoweverFastItIsBeingDragged()
        {
            var brake = new CartParking();
            brake.Toggle(0f);

            Assert.That(brake.Toggle(9f), Is.True,
                "a cart being dragged with its brake on is exactly when a player needs to release it");
        }

        [Test]
        public void TheHitchSwingsUpWhenParkedAndBackDownWhenNot()
        {
            var brake = new CartParking();
            brake.Toggle(0f);

            var up = brake.HitchDegrees(0.5f, 0f, swingsAtDegreesPerSecond: 180f);
            Assert.That(up, Is.GreaterThan(0f), "the hitch has to lift, that is what sets the brake");

            brake.Toggle(0f);
            Assert.That(brake.HitchDegrees(0.5f, up, 180f), Is.LessThan(up), "and drop again");
        }

        [Test]
        public void AParkedCartResistsButIsNotFrozen()
        {
            var brake = new CartParking();
            brake.Toggle(0f);

            var holding = brake.BrakingForce(2f, 550f, holdsAt: 6f, deltaTime: 0.02f);

            Assert.That(holding, Is.GreaterThan(0f), "a parked cart has to fight being dragged");
            Assert.That(holding, Is.LessThan(float.PositiveInfinity),
                "and has to be draggable, so a tractor can still shift it and a crash can move it");
        }
    
        [Test]
        public void ADrawbarSpansFromTheCartsNoseOutToItsCouplingPoint()
        {
            var bar = Drawbar.Spans(new Vector3(0f, 0.4f, 2.6f), noseAtZ: 1.8f);

            Assert.That(bar.LengthMetres, Is.EqualTo(0.8f).Within(1e-4f));
            Assert.That(bar.PivotLocal.z, Is.EqualTo(1.8f).Within(1e-4f),
                "it has to hinge at the cart, or lifting it swings the whole bar into the ground");
            Assert.That(bar.ReachesMetres, Is.EqualTo(0.8f).Within(1e-4f));
        }

        [Test]
        public void ADrawbarBehindTheCartSpansBackwardsJustTheSame()
        {
            var bar = Drawbar.Spans(new Vector3(0f, 0.4f, -2.6f), noseAtZ: -1.8f);

            Assert.That(bar.LengthMetres, Is.EqualTo(0.8f).Within(1e-4f));
            Assert.That(bar.ReachesMetres, Is.EqualTo(-0.8f).Within(1e-4f));
        }

        [Test]
        public void OnlyALooseUnparkedCartCanBePulledAround()
        {
            Assert.That(Drawbar.CanBePulled(parked: false, hitched: false), Is.True);
            Assert.That(Drawbar.CanBePulled(parked: true, hitched: false), Is.False,
                "a parked cart is braked, so hauling it by hand would defeat the brake");
            Assert.That(Drawbar.CanBePulled(parked: false, hitched: true), Is.False,
                "a hitched cart belongs to the tractor, and grabbing its bar fights the joint");
        }
    
        [Test]
        public void ThePanelAndTheVinylTakeTheSameWeightAtEveryPosition()
        {
            Assert.That(SlidingDoor.ShapeWeight(0.25f), Is.EqualTo(25f).Within(1e-3f),
                "Door1 carries one shape, Open, and Door_Fabric1 one shape, Closed, and measuring " +
                "the baked meshes shows the two line up only when both take the same weight: at 0 " +
                "the panel spans 0.00748 to 1.57189 and the vinyl 0.04329 to 1.53521, at 100 the " +
                "panel spans 1.01911 to 1.57189 and the vinyl 1.03353 to 1.54316");
            Assert.That(SlidingDoor.ShapeWeight(1.4f), Is.EqualTo(100f).Within(1e-3f));
            Assert.That(SlidingDoor.ShapeWeight(-0.2f), Is.EqualTo(0f).Within(1e-3f));
        }
    
        [Test]
        public void ParkingAtExactlyTheSpeedLimitIsAllowed()
        {
            var brake = new CartParking();

            Assert.That(brake.Toggle(CartParking.TooFastToParkMetresPerSecond), Is.True,
                "the limit is a ceiling, and a cart rolling at it has just about stopped");
        }

        [Test]
        public void ABrakeNeverPushesAParkedCartBackwards()
        {
            var brake = new CartParking();
            brake.Toggle(0f);

            foreach (var step in new[] { 0.02f, 0.01f, 0.005f })
            {
                var atMost = 0.1f * 550f / step;

                Assert.That(brake.BrakingForce(0.1f, 550f, holdsAt: 6f, deltaTime: step),
                    Is.LessThanOrEqualTo(atMost + 1e-3f),
                    $"at a {step} s step a cart creeping at 0.1 m/s got more force than stops it in " +
                    "one step, which reverses it instead");
            }
        }
    }
}
