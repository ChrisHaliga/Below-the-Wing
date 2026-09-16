using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CartFittingsTests
    {
        [Test]
        public void APoleAtTheStartOfItsTrackLeavesTheDoorShut()
        {
            Assert.That(SlidingDoor.OpennessAt(0f, 1.5f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(SlidingDoor.StillCoveredMetres(0f, 1.5f), Is.EqualTo(1.5f).Within(1e-4f),
                "a shut door has to cover the whole opening, or a bag walks straight through it");
        }

        [Test]
        public void APoleAtTheEndOfItsTrackLeavesNothingCovered()
        {
            Assert.That(SlidingDoor.OpennessAt(1.5f, 1.5f), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(SlidingDoor.StillCoveredMetres(1f, 1.5f), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void TheCoverStaysAnchoredToTheShutEndAsItOpens()
        {
            var shut = SlidingDoor.CoverSitsAt(0f, 2f);
            var half = SlidingDoor.CoverSitsAt(0.5f, 2f);
            var open = SlidingDoor.CoverSitsAt(1f, 2f);

            Assert.That(shut, Is.EqualTo(0f).Within(1e-4f), "shut, it is centred on the opening");
            Assert.That(half, Is.EqualTo(-0.5f).Within(1e-4f),
                "half open, the metre that is left sits against the shut end rather than floating");
            Assert.That(open, Is.EqualTo(-1f).Within(1e-4f));
        }

        [Test]
        public void APoleDraggedPastItsTrackIsStillReadAsFullyOpenOrFullyShut()
        {
            Assert.That(SlidingDoor.OpennessAt(-0.4f, 1.5f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(SlidingDoor.OpennessAt(2.9f, 1.5f), Is.EqualTo(1f).Within(1e-4f),
                "a pole bouncing past the end of its spring must not drive the shape key past one");
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

            var holding = brake.BrakingForce(2f, 550f, holdsAt: 6f);

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
    }
}
