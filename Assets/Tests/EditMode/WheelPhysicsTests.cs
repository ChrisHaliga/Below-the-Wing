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
        /// <summary>The tractor's two wheel sizes, as measured off its model.</summary>
        const float FrontWheelRadius = 0.2203f;

        const float RearWheelRadius = 0.2647f;

        VehicleProfile m_Tractor;

        [SetUp]
        public void SetUp() => m_Tractor = TestProfiles.Tractor();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Tractor);

        [Test]
        public void SuspensionAtFullExtensionCarriesNothing()
        {
            var justTouching = m_Tractor.suspensionRestLengthMetres + FrontWheelRadius;

            Assert.That(WheelPhysics.Compression(justTouching, FrontWheelRadius, m_Tractor),
                Is.EqualTo(0f).Within(1e-4f),
                "a wheel only just reaching the ground has not compressed its spring");
            Assert.That(WheelPhysics.SuspensionForce(0f, 0f, m_Tractor), Is.EqualTo(0f).Within(1e-4f),
                "an uncompressed spring pushes with nothing");
        }

        [Test]
        public void SuspensionAtHalfTravelPushesBackWithHalfItsSpring()
        {
            var halfway = FrontWheelRadius + (m_Tractor.suspensionRestLengthMetres * 0.5f);

            Assert.That(WheelPhysics.Compression(halfway, FrontWheelRadius, m_Tractor),
                Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(WheelPhysics.SuspensionForce(0.5f, 0f, m_Tractor),
                Is.EqualTo(m_Tractor.springStrengthNewtons * 0.5f).Within(0.01f));
        }

        [Test]
        public void DamperTakesForceOutOfSuspensionThatIsExtending()
        {
            // Slow enough that the spring is still carrying something afterwards. Faster than the
            // spring can answer and the wheel simply comes unloaded, which is the case below.
            const float risingAt = 0.5f;

            var still = WheelPhysics.SuspensionForce(0.5f, 0f, m_Tractor);
            var rising = WheelPhysics.SuspensionForce(0.5f, risingAt, m_Tractor);

            Assert.That(rising, Is.GreaterThan(0f), "this case is only about a wheel still carrying weight");
            Assert.That(rising, Is.LessThan(still), "a damper must resist the suspension's own movement");
            Assert.That(still - rising,
                Is.EqualTo(risingAt * m_Tractor.damperNewtonsPerMetrePerSecond).Within(0.01f),
                "and it must resist it in proportion to how fast it is moving");
        }

        [Test]
        public void AWheelExtendingFasterThanItsSpringCanAnswerCarriesNothing()
        {
            var halfTheSpring = m_Tractor.springStrengthNewtons * 0.5f;
            var fasterThanTheSpring = (halfTheSpring / m_Tractor.damperNewtonsPerMetrePerSecond) + 0.1f;

            Assert.That(WheelPhysics.SuspensionForce(0.5f, fasterThanTheSpring, m_Tractor),
                Is.EqualTo(0f).Within(1e-4f),
                $"dropping away at {fasterThanTheSpring:F2} m/s, this wheel's damper wants more " +
                "force than its spring has. A suspension that answered with the difference would " +
                "be pulling the body down onto a wheel that is no longer touching anything");
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
                wheelRadiusMetres: FrontWheelRadius,
                deltaTime: 0.02f,
                m_Tractor);

            Assert.That(slidingAndFlooredIt.Grounded, Is.False);
            Assert.That(slidingAndFlooredIt.AlongSuspension, Is.EqualTo(0f), "nothing to push against");
            Assert.That(slidingAndFlooredIt.Lateral, Is.EqualTo(0f), "a wheel off the ground cannot grip");
            Assert.That(slidingAndFlooredIt.Forward, Is.EqualTo(0f), "and it cannot drive the vehicle either");
        }

        [Test]
        public void CompressionIsMeasuredFromTheWheelsOwnRadius()
        {
            var travel = m_Tractor.suspensionRestLengthMetres;

            Assert.That(WheelPhysics.Compression(RearWheelRadius + travel, RearWheelRadius, m_Tractor),
                Is.EqualTo(0f).Within(1e-4f),
                "the bigger wheel reaches further before its spring starts to take any load");
            Assert.That(WheelPhysics.Compression(RearWheelRadius, RearWheelRadius, m_Tractor),
                Is.EqualTo(1f).Within(1e-4f),
                "and is bottomed out when the ground is exactly its own radius away");

            var measuredAsTheSmallOne =
                WheelPhysics.Compression(RearWheelRadius + travel, FrontWheelRadius, m_Tractor);

            Assert.That(measuredAsTheSmallOne, Is.LessThan(0f),
                $"a 0.2647 m wheel just touching the ground reads as {measuredAsTheSmallOne:F2} " +
                "compressed when measured with the front wheel's radius -- past full extension, " +
                "which is to say hanging in the air, so that corner of the tractor carries nothing");
        }

        [Test]
        public void TheBodyHangsHigherOverABiggerWheel()
        {
            var overTheFront = VehicleController.SuspensionMountHeightMetres(m_Tractor, FrontWheelRadius);
            var overTheRear = VehicleController.SuspensionMountHeightMetres(m_Tractor, RearWheelRadius);

            Assert.That(overTheRear - overTheFront,
                Is.EqualTo(RearWheelRadius - FrontWheelRadius).Within(1e-4f),
                "the difference between the two axles' mounts is exactly the difference between " +
                "their wheels. Hung at one height, the small wheels are left in the air and the big " +
                "ones are pushed into the tarmac by 2.2 cm each");
        }

        [Test]
        public void TireRollingTrueIsPulledNowhereSideways()
        {
            Assert.That(WheelPhysics.LateralForce(0f, 750f, Step, m_Tractor), Is.EqualTo(0f).Within(1e-4f));
        }

        /// <summary>One physics step at this project's fixed rate.</summary>
        const float Step = 0.02f;

        [Test]
        public void ATireBarelySlidingHoldsUntilTheCrawlIsGone()
        {
            const float load = 100f;
            var slip = WheelPhysics.HoldsBelowMetresPerSecond * 0.9f;

            // The wheel, on its own, held against its own crawl for a third of a second.
            var creepingAt = slip;
            var steps = 0;
            while (steps < 16)
            {
                var force = WheelPhysics.LateralForce(slip, load, Step, m_Tractor);

                Assert.That(force, Is.LessThan(0f), "the force has to oppose the slide");
                Assert.That(-force, Is.LessThanOrEqualTo((slip * load / Step) + 0.01f),
                    $"a tyre pulling harder than {slip:F3} m/s of slip is worth does not stop the " +
                    "crawl, it reverses it, and a parked cart answers a whisker of drift by " +
                    "drifting back the other way for ever");

                slip += force / load * Step;
                steps++;
            }

            Assert.That(slip, Is.LessThan(creepingAt * 0.01f),
                $"a third of a second of holding took a {creepingAt:F3} m/s crawl down to only " +
                $"{slip:F4} m/s. Read off a curve through the origin there is almost nothing there " +
                "to stop it, so a nudged cart drifts on until something else ends it");
        }

        [Test]
        public void HoldingNeverPullsHarderThanTheTireCanGrip()
        {
            const float load = 100f;
            var sliding = WheelPhysics.HoldsBelowMetresPerSecond * 0.95f;

            // A tyre with almost no grip in it -- something on ice rather than on tarmac -- because
            // a tyre that could grip its way out of any crawl would never show this bound at all.
            m_Tractor.lateralGripCurve = AnimationCurve.Linear(0f, 0f, 12f, 2f);

            var enoughToStopIt = sliding * load / Step;
            var whatItCanGrip = WheelPhysics.MostGripPerKilogram(m_Tractor) * load;

            Assert.That(whatItCanGrip, Is.LessThan(enoughToStopIt),
                "this case is only worth anything while the tyre is the limit");
            Assert.That(WheelPhysics.LateralForce(sliding, load, Step, m_Tractor),
                Is.EqualTo(-whatItCanGrip).Within(0.01f),
                "a tyre asked to stop a slide it has not the grip for must give way. Held to it, " +
                "anything that could not be gripped would be stopped dead anyway, which is a hand " +
                "reaching in and taking the speed off it");
        }

        [Test]
        public void ATireSlidingProperlyStillFollowsTheGripCurve()
        {
            const float load = 750f;
            const float sliding = 2f;
            var fromCurve = m_Tractor.lateralGripCurve.Evaluate(sliding) * load;

            Assert.That(WheelPhysics.LateralForce(sliding, load, Step, m_Tractor),
                Is.EqualTo(-fromCurve).Within(0.01f),
                "above a crawl the curve is the whole model, including the falling half that lets a " +
                "vehicle break traction. Holding at speed would put the apron on rails");
        }

        [Test]
        public void SidewaysForceOpposesTheSlideAndFollowsTheGripCurve()
        {
            const float load = 750f;
            const float slidingAt = 2f;
            var fromCurve = m_Tractor.lateralGripCurve.Evaluate(slidingAt) * load;

            Assert.That(WheelPhysics.LateralForce(slidingAt, load, Step, m_Tractor),
                Is.EqualTo(-fromCurve).Within(0.01f), "sliding right must push left");
            Assert.That(WheelPhysics.LateralForce(-slidingAt, load, Step, m_Tractor),
                Is.EqualTo(fromCurve).Within(0.01f), "and sliding left must push right");
        }

        [Test]
        public void GripFallsAwayOnceTheTireIsSlidingHardEnough()
        {
            const float load = 750f;

            var nearThePeak = Mathf.Abs(WheelPhysics.LateralForce(3f, load, Step, m_Tractor));
            var wellPastIt = Mathf.Abs(WheelPhysics.LateralForce(10f, load, Step, m_Tractor));

            Assert.That(wellPastIt, Is.LessThan(nearThePeak),
                "a tire that grips harder the faster it slides can never let go, and nothing can ever slide");
        }

        [Test]
        public void HeavierWheelsGripHarder()
        {
            var lightlyLoaded = Mathf.Abs(WheelPhysics.LateralForce(2f, 300f, Step, m_Tractor));
            var heavilyLoaded = Mathf.Abs(WheelPhysics.LateralForce(2f, 900f, Step, m_Tractor));

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
