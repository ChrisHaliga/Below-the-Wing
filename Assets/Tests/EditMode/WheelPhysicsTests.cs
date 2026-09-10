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
            Assert.That(WheelPhysics.DriveForce(1f, m_Tractor),
                Is.EqualTo(m_Tractor.maxDriveForceNewtons).Within(0.01f));
            Assert.That(WheelPhysics.DriveForce(0f, m_Tractor), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(WheelPhysics.DriveForce(0.5f, m_Tractor),
                Is.EqualTo(m_Tractor.maxDriveForceNewtons * 0.5f).Within(0.01f));
        }

        [Test]
        public void ARollingWheelIsSlowedEvenWhenNobodyIsBraking()
        {
            const float load = 750f;

            var coasting = WheelPhysics.RollingResistance(6f, load, 0.02f, m_Tractor);

            Assert.That(coasting, Is.LessThan(0f),
                "a vehicle released from the throttle must lose speed; nothing else in the model " +
                "resists moving forwards, so without this it coasts at the same speed for ever");
        }

        [Test]
        public void AFasterWheelIsSlowedHarderThanASlowOne()
        {
            const float load = 750f;

            var walkingPace = Mathf.Abs(WheelPhysics.RollingResistance(1f, load, 0.02f, m_Tractor));
            var flatOut = Mathf.Abs(WheelPhysics.RollingResistance(9f, load, 0.02f, m_Tractor));

            Assert.That(flatOut, Is.GreaterThan(walkingPace));
        }

        [Test]
        public void RollingResistanceOpposesWhicheverWayTheWheelIsTurning()
        {
            const float load = 750f;

            Assert.That(WheelPhysics.RollingResistance(4f, load, 0.02f, m_Tractor), Is.LessThan(0f));
            Assert.That(WheelPhysics.RollingResistance(-4f, load, 0.02f, m_Tractor), Is.GreaterThan(0f));
        }

        [Test]
        public void AStandingVehicleIsNotRolledBackwardsByItsOwnTires()
        {
            const float load = 750f;
            const float step = 0.02f;

            Assert.That(WheelPhysics.RollingResistance(0f, load, step, m_Tractor), Is.EqualTo(0f).Within(1e-4f));

            var crawling = WheelPhysics.RollingResistance(0.01f, load, step, m_Tractor);
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
    }
}
