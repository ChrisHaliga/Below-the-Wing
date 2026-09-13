using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// The force one wheel produces, checked a quantity at a time.
    ///
    /// These are the numbers every vehicle in the game is built out of, so they are checked
    /// directly rather than inferred from watching something drive.
    /// </summary>
    public sealed class WheelPhysicsTests
    {
        VehicleProfile m_Tractor;

        [SetUp]
        public void SetUp() => m_Tractor = TestProfiles.Tractor();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Tractor);

        [Test]
        public void SuspensionAtFullExtensionCarriesNothing()
        {
            var justTouching = m_Tractor.suspensionRestLengthMetres + m_Tractor.wheelRadiusMetres;

            Assert.That(WheelPhysics.Compression(justTouching, m_Tractor), Is.EqualTo(0f).Within(1e-4f),
                "a wheel only just reaching the ground has not compressed its spring");
            Assert.That(WheelPhysics.SuspensionForce(0f, 0f, m_Tractor), Is.EqualTo(0f).Within(1e-4f),
                "an uncompressed spring pushes with nothing");
        }

        [Test]
        public void SuspensionAtHalfTravelPushesBackWithHalfItsSpring()
        {
            var halfway = m_Tractor.wheelRadiusMetres + (m_Tractor.suspensionRestLengthMetres * 0.5f);

            Assert.That(WheelPhysics.Compression(halfway, m_Tractor), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(WheelPhysics.SuspensionForce(0.5f, 0f, m_Tractor),
                Is.EqualTo(m_Tractor.springStrengthNewtons * 0.5f).Within(0.01f));
        }

        [Test]
        public void DamperTakesForceOutOfSuspensionThatIsExtending()
        {
            const float risingAt = 1.5f;

            var still = WheelPhysics.SuspensionForce(0.5f, 0f, m_Tractor);
            var rising = WheelPhysics.SuspensionForce(0.5f, risingAt, m_Tractor);

            Assert.That(rising, Is.LessThan(still), "a damper must resist the suspension's own movement");
            Assert.That(still - rising,
                Is.EqualTo(risingAt * m_Tractor.damperNewtonsPerMetrePerSecond).Within(0.01f),
                "and it must resist it in proportion to how fast it is moving");
        }

        [Test]
        public void WheelInTheAirProducesNoForceOfAnyKind()
        {
            var slidingAndFlooredIt = WheelPhysics.Evaluate(
                GroundProbe.Airborne,
                new ContactVelocity(alongSuspension: 0f, lateral: 4f, forward: 6f),
                new WheelLoad(supportedMassKg: 750f, driveShare: 0.5f, brakeShare: 0.25f),
                new DriveIntent(steer: 1f, throttle: 1f, brake: 0f),
                vehicleMassKg: m_Tractor.massKg,
                deltaTime: 0.02f,
                m_Tractor);

            Assert.That(slidingAndFlooredIt.Grounded, Is.False);
            Assert.That(slidingAndFlooredIt.AlongSuspension, Is.EqualTo(0f), "nothing to push against");
            Assert.That(slidingAndFlooredIt.Lateral, Is.EqualTo(0f), "a wheel off the ground cannot grip");
            Assert.That(slidingAndFlooredIt.Forward, Is.EqualTo(0f), "and it cannot drive the vehicle either");
        }

        [Test]
        public void TireRollingTrueIsPulledNowhereSideways()
        {
            Assert.That(WheelPhysics.LateralForce(0f, 750f, m_Tractor), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void SidewaysForceOpposesTheSlideAndFollowsTheGripCurve()
        {
            const float load = 750f;
            const float slidingAt = 2f;
            var fromCurve = m_Tractor.lateralGripCurve.Evaluate(slidingAt) * load;

            Assert.That(WheelPhysics.LateralForce(slidingAt, load, m_Tractor),
                Is.EqualTo(-fromCurve).Within(0.01f), "sliding right must push left");
            Assert.That(WheelPhysics.LateralForce(-slidingAt, load, m_Tractor),
                Is.EqualTo(fromCurve).Within(0.01f), "and sliding left must push right");
        }

        [Test]
        public void GripFallsAwayOnceTheTireIsSlidingHardEnough()
        {
            const float load = 750f;

            var nearThePeak = Mathf.Abs(WheelPhysics.LateralForce(3f, load, m_Tractor));
            var wellPastIt = Mathf.Abs(WheelPhysics.LateralForce(10f, load, m_Tractor));

            Assert.That(wellPastIt, Is.LessThan(nearThePeak),
                "a tire that grips harder the faster it slides can never let go, and nothing can ever slide");
        }

        [Test]
        public void HeavierWheelsGripHarder()
        {
            var lightlyLoaded = Mathf.Abs(WheelPhysics.LateralForce(2f, 300f, m_Tractor));
            var heavilyLoaded = Mathf.Abs(WheelPhysics.LateralForce(2f, 900f, m_Tractor));

            Assert.That(heavilyLoaded, Is.GreaterThan(lightlyLoaded));
        }

        [Test]
        public void ThrottleProducesTheProfilesDriveForce()
        {
            Assert.That(WheelPhysics.DriveForce(1f, sprinting: false, forwardVelocity: 0f, m_Tractor),
                Is.EqualTo(m_Tractor.maxDriveForceNewtons).Within(0.01f));
            Assert.That(WheelPhysics.DriveForce(0f, sprinting: false, forwardVelocity: 0f, m_Tractor), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(WheelPhysics.DriveForce(0.5f, sprinting: false, forwardVelocity: 0f, m_Tractor),
                Is.EqualTo(m_Tractor.maxDriveForceNewtons * 0.5f).Within(0.01f));
        }

        [Test]
        public void SprintingPutsMoreForceThroughTheDrivenWheels()
        {
            var cruising = WheelPhysics.DriveForce(1f, sprinting: false, forwardVelocity: 0f, m_Tractor);
            var flatOut = WheelPhysics.DriveForce(1f, sprinting: true, forwardVelocity: 0f, m_Tractor);

            Assert.That(cruising, Is.EqualTo(m_Tractor.maxDriveForceNewtons).Within(0.01f));
            Assert.That(flatOut,
                Is.EqualTo(m_Tractor.maxDriveForceNewtons * m_Tractor.sprintDriveMultiplier).Within(0.01f));
            Assert.That(flatOut, Is.GreaterThan(cruising));
        }

        [Test]
        public void SprintingWithTheThrottleShutStillProducesNothing()
        {
            Assert.That(WheelPhysics.DriveForce(0f, sprinting: true, forwardVelocity: 0f, m_Tractor), Is.EqualTo(0f).Within(1e-4f),
                "holding sprint is not a throttle of its own");
        }

        [Test]
        public void ATractorCanOutrunItsOwnDrag()
        {
            // Top speed is where drive force and resistance meet. It is worth pinning, because the
            // figure that sets it is a real one and the honest real answer -- about 23 km/h -- is
            // slower than this game wants to be.
            const int corners = 4;
            var atSpeed = 10f;

            var resistance = Mathf.Abs(
                WheelPhysics.RollingResistance(atSpeed, m_Tractor.massKg / corners, 0.02f, drivingWithTheMotion: false, m_Tractor)) * corners;

            Assert.That(WheelPhysics.DriveForce(1f, sprinting: false, forwardVelocity: 0f, m_Tractor), Is.GreaterThan(resistance),
                $"a tractor cannot reach {atSpeed} m/s -- about 36 km/h -- because drag has already " +
                "beaten the engine before it gets there. Everything on this apron then feels heavy " +
                "and slow to drive, which is the complaint this figure exists to prevent returning");
        }

        [Test]
        public void ARollingWheelIsSlowedEvenWhenNobodyIsBraking()
        {
            const float load = 750f;

            var coasting = WheelPhysics.RollingResistance(6f, load, 0.02f, drivingWithTheMotion: false, m_Tractor);

            Assert.That(coasting, Is.LessThan(0f),
                "a vehicle released from the throttle must lose speed; nothing else in the model " +
                "resists moving forwards, so without this it coasts at the same speed for ever");
        }

        [Test]
        public void AFasterWheelIsSlowedHarderThanASlowOne()
        {
            const float load = 750f;

            var walkingPace = Mathf.Abs(WheelPhysics.RollingResistance(1f, load, 0.02f, drivingWithTheMotion: false, m_Tractor));
            var flatOut = Mathf.Abs(WheelPhysics.RollingResistance(9f, load, 0.02f, drivingWithTheMotion: false, m_Tractor));

            Assert.That(flatOut, Is.GreaterThan(walkingPace));
        }

        [Test]
        public void RollingResistanceOpposesWhicheverWayTheWheelIsTurning()
        {
            const float load = 750f;

            Assert.That(WheelPhysics.RollingResistance(4f, load, 0.02f, drivingWithTheMotion: false, m_Tractor), Is.LessThan(0f));
            Assert.That(WheelPhysics.RollingResistance(-4f, load, 0.02f, drivingWithTheMotion: false, m_Tractor), Is.GreaterThan(0f));
        }

        [Test]
        public void AStandingVehicleIsNotRolledBackwardsByItsOwnTires()
        {
            const float load = 750f;
            const float step = 0.02f;

            Assert.That(WheelPhysics.RollingResistance(0f, load, step, drivingWithTheMotion: false, m_Tractor), Is.EqualTo(0f).Within(1e-4f));

            var crawling = WheelPhysics.RollingResistance(0.01f, load, step, drivingWithTheMotion: false, m_Tractor);
            Assert.That(Mathf.Abs(crawling), Is.LessThanOrEqualTo((0.01f * load / step) + 0.01f),
                "resistance may bring a wheel to a stop and must never push it back the other way");
        }

        [Test]
        public void BrakingHardAtSpeedIsCappedByTheProfile()
        {
            var braking = WheelPhysics.BrakeForce(1f, 20f, m_Tractor.massKg, 0.02f, m_Tractor);

            Assert.That(braking, Is.LessThan(0f), "braking must oppose forward motion");
            Assert.That(Mathf.Abs(braking), Is.EqualTo(m_Tractor.maxBrakeForceNewtons).Within(0.01f));
        }

        [Test]
        public void BrakingNeverDragsAVehicleBackwards()
        {
            const float crawling = 0.1f;
            const float step = 0.02f;
            var enoughToStopIt = crawling * m_Tractor.massKg / step;

            var braking = WheelPhysics.BrakeForce(1f, crawling, m_Tractor.massKg, step, m_Tractor);

            Assert.That(Mathf.Abs(braking), Is.LessThanOrEqualTo(enoughToStopIt + 0.01f),
                "braking may bring a vehicle to a stop but must not push it back the other way");
            Assert.That(WheelPhysics.BrakeForce(1f, 0f, m_Tractor.massKg, step, m_Tractor),
                Is.EqualTo(0f).Within(1e-4f),
                "a vehicle already standing still has nothing to brake against");
        }

        [Test]
        public void DrivelineDragIsOffWhileTheThrottleIsOpen()
        {
            const float load = 750f;

            var coasting = Mathf.Abs(WheelPhysics.RollingResistance(6f, load, 0.02f, drivingWithTheMotion: false, m_Tractor));
            var driven = Mathf.Abs(WheelPhysics.RollingResistance(6f, load, 0.02f, drivingWithTheMotion: true, m_Tractor));
            var tyreOnly = m_Tractor.rollingResistanceCoefficient * load * Physics.gravity.magnitude;

            Assert.That(driven, Is.EqualTo(tyreOnly).Within(0.01f),
                "under throttle the driveline is doing the pushing, not the dragging. Charged for " +
                "both, the top speed becomes wherever engine and drag happen to meet -- about 40 km/h " +
                "-- and the tractor pulls like it is towing its own handbrake");
            Assert.That(coasting, Is.GreaterThan(driven), "off the throttle the driveline drag is what stops it");
        }

        [Test]
        public void DriveForceFadesToNothingAtTopSpeed()
        {
            var standing = WheelPhysics.DriveForce(1f, sprinting: false, forwardVelocity: 0f, m_Tractor);
            var halfway = WheelPhysics.DriveForce(1f, sprinting: false, m_Tractor.topSpeedMetresPerSecond * 0.5f, m_Tractor);
            var flatOut = WheelPhysics.DriveForce(1f, sprinting: false, m_Tractor.topSpeedMetresPerSecond, m_Tractor);
            var beyond = WheelPhysics.DriveForce(1f, sprinting: false, m_Tractor.topSpeedMetresPerSecond * 1.2f, m_Tractor);

            Assert.That(standing, Is.EqualTo(m_Tractor.maxDriveForceNewtons).Within(0.01f));
            Assert.That(halfway, Is.EqualTo(m_Tractor.maxDriveForceNewtons).Within(0.01f), "full pull through most of the range");
            Assert.That(flatOut, Is.EqualTo(0f).Within(0.01f), "and nothing left at the top");
            Assert.That(beyond, Is.EqualTo(0f).Within(0.01f), "never a push past it");
        }

        [Test]
        public void SprintingRaisesTheTopSpeedAsWellAsThePull()
        {
            var top = m_Tractor.topSpeedMetresPerSecond;

            Assert.That(WheelPhysics.DriveForce(1f, sprinting: false, top, m_Tractor), Is.EqualTo(0f).Within(0.01f));
            Assert.That(WheelPhysics.DriveForce(1f, sprinting: true, top, m_Tractor), Is.GreaterThan(0f),
                "sprinting with nothing left to give at the old top speed is no sprint");
            Assert.That(WheelPhysics.DriveForce(1f, sprinting: true, top * m_Tractor.sprintDriveMultiplier, m_Tractor),
                Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void ReverseAgainstForwardMotionIsFullEngineBrakingAndTheDrivelineStillDrags()
        {
            const float load = 750f;
            var top = m_Tractor.topSpeedMetresPerSecond;

            Assert.That(WheelPhysics.PushingWithTheMotion(1f, 19f), Is.True);
            Assert.That(WheelPhysics.PushingWithTheMotion(-1f, 19f), Is.False);
            Assert.That(WheelPhysics.PushingWithTheMotion(-1f, -3f), Is.True);
            Assert.That(WheelPhysics.PushingWithTheMotion(0f, 19f), Is.False);

            Assert.That(WheelPhysics.DriveForce(-1f, sprinting: false, top * 0.95f, m_Tractor),
                Is.EqualTo(-m_Tractor.maxDriveForceNewtons).Within(0.01f),
                "holding reverse at near top speed is braking with the engine and gets all of it; " +
                "faded like a forward push, pressing S at speed would do less than pressing nothing");

            var coasting = Mathf.Abs(WheelPhysics.RollingResistance(19f, load, 0.02f, drivingWithTheMotion: false, m_Tractor));
            var braking = Mathf.Abs(WheelPhysics.RollingResistance(19f, load, 0.02f,
                WheelPhysics.PushingWithTheMotion(-1f, 19f), m_Tractor));
            Assert.That(braking, Is.EqualTo(coasting).Within(0.01f), "and the driveline drags as when coasting");
        }
    }
}
