using System;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>What the ground looks like beneath one wheel this step.</summary>
    public readonly struct GroundProbe
    {
        /// <summary>False when the wheel is in the air and the suspension has nothing to push against.</summary>
        public readonly bool HitGround;

        /// <summary>
        /// Metres from the wheel's mounting point down to the ground. At rest length plus wheel
        /// radius the suspension is fully extended and carrying nothing; smaller values compress it.
        /// </summary>
        public readonly float DistanceToGround;

        public GroundProbe(bool hitGround, float distanceToGround)
        {
            HitGround = hitGround;
            DistanceToGround = distanceToGround;
        }

        /// <summary>A wheel with no ground under it.</summary>
        public static GroundProbe Airborne => new GroundProbe(false, float.PositiveInfinity);
    }

    /// <summary>
    /// How fast one wheel's contact patch is moving, split into the three directions that matter.
    /// Splitting velocity this way is what separates rolling from sliding: motion along
    /// <see cref="Forward"/> is the wheel doing its job, motion along <see cref="Lateral"/> is the
    /// tire being dragged sideways and is what the grip curve resists.
    /// </summary>
    public readonly struct ContactVelocity
    {
        /// <summary>Metres per second along the suspension axis. Positive means the wheel is rising.</summary>
        public readonly float AlongSuspension;

        /// <summary>Metres per second sideways across the tire. Positive is to the wheel's right.</summary>
        public readonly float Lateral;

        /// <summary>Metres per second along the direction the wheel is pointing.</summary>
        public readonly float Forward;

        public ContactVelocity(float alongSuspension, float lateral, float forward)
        {
            AlongSuspension = alongSuspension;
            Lateral = lateral;
            Forward = forward;
        }
    }

    /// <summary>
    /// How much of the vehicle one wheel is responsible for.
    ///
    /// Grip depends on the weight a tire carries, and drive and braking are split between wheels,
    /// so each wheel needs to know its share before it can work out what force to produce.
    /// </summary>
    public readonly struct WheelLoad
    {
        /// <summary>Kilograms of the vehicle resting on this wheel.</summary>
        public readonly float SupportedMassKg;

        /// <summary>Fraction of the vehicle's drive force delivered here. Zero on an undriven wheel.</summary>
        public readonly float DriveShare;

        /// <summary>Fraction of the vehicle's braking force delivered here.</summary>
        public readonly float BrakeShare;

        public WheelLoad(float supportedMassKg, float driveShare, float brakeShare)
        {
            SupportedMassKg = supportedMassKg;
            DriveShare = driveShare;
            BrakeShare = brakeShare;
        }
    }

    /// <summary>Forces one wheel produces this step, in newtons, in the wheel's own directions.</summary>
    public readonly struct WheelForce
    {
        /// <summary>False when the wheel was in the air, in which case every component is zero.</summary>
        public readonly bool Grounded;

        /// <summary>Up the suspension axis: spring less damper.</summary>
        public readonly float AlongSuspension;

        /// <summary>Across the tire, opposing sideways sliding.</summary>
        public readonly float Lateral;

        /// <summary>Along the wheel's heading: drive less braking.</summary>
        public readonly float Forward;

        public WheelForce(bool grounded, float alongSuspension, float lateral, float forward)
        {
            Grounded = grounded;
            AlongSuspension = alongSuspension;
            Lateral = lateral;
            Forward = forward;
        }

        /// <summary>A wheel producing nothing, because it is not touching the ground.</summary>
        public static WheelForce None => new WheelForce(false, 0f, 0f, 0f);
    }

    /// <summary>
    /// The force a single wheel produces, worked out from the ground beneath it, how it is moving,
    /// what it is carrying, and what the driver is asking for.
    ///
    /// This is deliberately free of Unity's scene graph: no rigidbody, no transform, no raycast.
    /// The controller performs the raycast and applies the result; everything in between is here,
    /// where it can be checked a value at a time.
    /// </summary>
    public static class WheelPhysics
    {
        /// <summary>
        /// How far the suspension is squashed, from 0 (fully extended, carrying nothing) to
        /// 1 (fully compressed). Values outside that range mean the wheel is beyond its travel.
        /// </summary>
        public static float Compression(float distanceToGround, float wheelRadiusMetres, VehicleProfile profile)
        {
            var fullyExtended = wheelRadiusMetres + profile.suspensionRestLengthMetres;
            return (fullyExtended - distanceToGround) / profile.suspensionRestLengthMetres;
        }

        /// <summary>
        /// Newtons up the suspension axis: the spring pushing back against compression, less the
        /// damper resisting however fast the suspension is currently moving.
        /// </summary>
        public static float SuspensionForce(float compression, float alongSuspensionVelocity, VehicleProfile profile)
        {
            var spring = Mathf.Clamp01(compression) * profile.springStrengthNewtons;
            var damper = alongSuspensionVelocity * profile.damperNewtonsPerMetrePerSecond;

            // A wheel can push the body up and never pull it down, so a damper that would more
            // than cancel the spring simply leaves the wheel unloaded.
            return Mathf.Max(0f, spring - damper);
        }

        /// <summary>
        /// The slip below which a tire holds rather than being read off the grip curve, in metres
        /// per second.
        ///
        /// Small on purpose, and smaller than it looks like it could be. Below this is a crawl --
        /// something drifting rather than sliding -- and everything above it is the curve's
        /// business. Set high enough to catch a wheel mid-corner and the hold starts resisting the
        /// turn itself: at 0.3 m/s a train flat out on full lock came round six degrees less in six
        /// seconds, because the wheels that were barely slipping held the vehicle straight.
        /// </summary>
        public const float HoldsBelowMetresPerSecond = 0.15f;

        /// <summary>
        /// Newtons across the tire, opposing a sideways slide. Magnitude comes from the profile's
        /// grip curve read at this sliding speed, scaled by the weight the wheel carries.
        ///
        /// Except at a crawl, where the curve has nothing useful to say. A curve that passes through
        /// the origin gives almost no force to a slide that is almost over, so the slower something
        /// drifts the less there is to stop it and it never quite arrives: a nudged cart wanders a
        /// metre across the apron over six seconds. A real tire does the opposite -- below some
        /// small slip it simply holds, which is why a trolley left standing stays where it was put.
        ///
        /// So below that slip the tire holds with everything it has, bounded by what would take the
        /// crawl out of it -- the same shape as the rolling resistance below: a real force, with the
        /// bound there only so that stopping something can never turn into pushing it the other way.
        ///
        /// The crawl is taken out over a few steps rather than one. Each wheel only knows its own
        /// contact patch, and four of them cancelling their own slip in the same step overshoot a
        /// slow turn between them -- the four forces meet at lever arms the wheels know nothing
        /// about, and a parked cart would answer a whisker of yaw by rotating back slightly harder
        /// than it was rotating. Spreading it over a few steps is also what a tire really does: the
        /// carcass gives a little before the rubber holds.
        /// </summary>
        public static float LateralForce(
            float lateralVelocity, float supportedMassKg, float deltaTime, VehicleProfile profile)
        {
            if (Mathf.Approximately(lateralVelocity, 0f))
            {
                return 0f;
            }

            var slip = Mathf.Abs(lateralVelocity);
            var against = -Mathf.Sign(lateralVelocity);

            if (slip >= HoldsBelowMetresPerSecond)
            {
                return against * profile.lateralGripCurve.Evaluate(slip) * supportedMassKg;
            }

            var whatItCanHoldWith = MostGripPerKilogram(profile) * supportedMassKg;
            var takingTheCrawlOutOfIt =
                slip * supportedMassKg / (Mathf.Max(deltaTime, 1e-5f) * StepsToTakeUpASlip);

            return against * Mathf.Min(whatItCanHoldWith, takingTheCrawlOutOfIt);
        }

        /// <summary>
        /// How many steps a tire takes to absorb a crawl it is holding against.
        ///
        /// More than one, so that four wheels holding at once cannot answer a slow turn with more
        /// than it had in it. Few enough that a nudged cart stops in a fraction of a second.
        /// </summary>
        const float StepsToTakeUpASlip = 3f;

        /// <summary>
        /// The most sideways force this tire can ever make, in newtons per kilogram it carries.
        ///
        /// The peak of its own grip curve, which is what a tire holding rather than sliding is good
        /// for. Read off the curve's corners rather than sampled: they are where a curve of this
        /// shape peaks, and reading them costs nothing on a step that does this once per wheel.
        /// </summary>
        public static float MostGripPerKilogram(VehicleProfile profile)
        {
            var curve = profile.lateralGripCurve;
            var most = 0f;

            for (var i = 0; i < curve.length; i++)
            {
                most = Mathf.Max(most, curve[i].value);
            }

            return most;
        }

        /// <summary>
        /// Newtons opposing a wheel's rolling, from the tire deforming under load and from the
        /// driveline it is turning.
        ///
        /// Without this a vehicle released from the throttle keeps whatever speed it had for ever,
        /// because nothing else in the model resists moving forwards. Braking is a thing a driver
        /// does; this is what happens when they do nothing at all.
        ///
        /// Bounded by what it would take to stop the wheel this step, so a vehicle that has come to
        /// rest is not dragged backwards by its own tires.
        /// </summary>
        public static float RollingResistance(
            float forwardVelocity, float supportedMassKg, float deltaTime, bool drivingWithTheMotion, VehicleProfile profile)
        {
            if (Mathf.Approximately(forwardVelocity, 0f))
            {
                return 0f;
            }

            var speed = Mathf.Abs(forwardVelocity);
            var fromTheTire = profile.rollingResistanceCoefficient * supportedMassKg * Physics.gravity.magnitude;

            // The driveline drags only when it is not driving the way the vehicle is going. Charged
            // for both at once, a vehicle's top speed is wherever the engine and its own gearbox
            // happen to meet, and it pulls away as if it were towing its own handbrake. Throttle
            // against the motion is braking with the engine, and the driveline drags then too.
            var fromTheDriveline = drivingWithTheMotion ? 0f : profile.coastingDragPerSecond * supportedMassKg * speed;
            var enoughToStopItThisStep = speed * supportedMassKg / Mathf.Max(deltaTime, 1e-5f);

            return -Mathf.Sign(forwardVelocity)
                   * Mathf.Min(fromTheTire + fromTheDriveline, enoughToStopItThisStep);
        }

        /// <summary>
        /// Newtons of forward drive the whole vehicle produces at this throttle.
        ///
        /// Sprinting multiplies what the driven wheels get, which raises both how hard a vehicle
        /// accelerates and the speed at which drive force and drag finally balance. It is not a
        /// throttle of its own: sprinting with the throttle shut still produces nothing.
        /// </summary>
        public static float DriveForce(float throttle, bool sprinting, float forwardVelocity, VehicleProfile profile)
        {
            var asked = Mathf.Clamp(throttle, -1f, 1f) * profile.maxDriveForceNewtons;
            var top = profile.topSpeedMetresPerSecond;
            if (sprinting)
            {
                asked *= profile.sprintDriveMultiplier;
                top *= profile.sprintDriveMultiplier;
            }

            // Full pull through most of the range, then fading to nothing at the top speed, so the
            // number on the profile is the speed you get rather than a number drag happens to allow.
            // Only when pushing the way the vehicle is already going: reverse against forward
            // motion is braking with the engine, and gets everything the engine has.
            if (!PushingWithTheMotion(throttle, forwardVelocity))
            {
                return asked;
            }

            var headroom = top > 0f ? Mathf.Clamp01((top - Mathf.Abs(forwardVelocity)) / (top * TopSpeedFadeFraction)) : 0f;
            return asked * headroom;
        }

        /// <summary>Whether the throttle is pushing the way the vehicle is already moving.</summary>
        public static bool PushingWithTheMotion(float throttle, float forwardVelocity)
            => !Mathf.Approximately(throttle, 0f)
               && !Mathf.Approximately(forwardVelocity, 0f)
               && Mathf.Sign(throttle) == Mathf.Sign(forwardVelocity);

        /// <summary>The last fraction of the top speed over which the drive fades away.</summary>
        const float TopSpeedFadeFraction = 0.25f;

        /// <summary>
        /// Newtons of braking the whole vehicle produces, opposing its current motion.
        ///
        /// Bounded by what it would take to bring the vehicle to a standstill this step, so that
        /// braking can stop a vehicle but never drag it backwards.
        /// </summary>
        public static float BrakeForce(float brake, float forwardVelocity, float vehicleMassKg, float deltaTime, VehicleProfile profile)
        {
            if (Mathf.Approximately(forwardVelocity, 0f))
            {
                return 0f;
            }

            var asked = Mathf.Clamp01(brake) * profile.maxBrakeForceNewtons;
            var enoughToStopItThisStep = Mathf.Abs(forwardVelocity) * vehicleMassKg / Mathf.Max(deltaTime, 1e-5f);

            return -Mathf.Sign(forwardVelocity) * Mathf.Min(asked, enoughToStopItThisStep);
        }

        /// <summary>
        /// Everything one wheel contributes this step. A wheel with no ground under it contributes
        /// nothing at all -- no spring, no grip, and no drive, because a spinning wheel in the air
        /// cannot push the vehicle anywhere.
        /// </summary>
        public static WheelForce Evaluate(
            in GroundProbe probe,
            in ContactVelocity velocity,
            in WheelLoad load,
            in DriveIntent intent,
            float vehicleMassKg,
            float wheelRadiusMetres,
            float deltaTime,
            VehicleProfile profile)
        {
            if (!probe.HitGround)
            {
                return WheelForce.None;
            }

            var compression = Compression(probe.DistanceToGround, wheelRadiusMetres, profile);
            if (compression < 0f)
            {
                return WheelForce.None;
            }

            var suspension = SuspensionForce(compression, velocity.AlongSuspension, profile);

            // Against the weight this wheel is actually carrying, not the share of the vehicle it
            // would carry standing level. A wheel barely touching the ground -- the two on the light
            // side of a vehicle up on the edge of tipping -- grips in proportion to that, which is
            // to say hardly at all. Given the vehicle's quarter regardless, those two would hold it
            // from sliding out while carrying nothing.
            var carrying = suspension / Mathf.Max(Physics.gravity.magnitude, 1e-5f);
            var lateral = LateralForce(velocity.Lateral, carrying, deltaTime, profile);
            var drive = DriveForce(intent.Throttle, intent.Sprint, velocity.Forward, profile) * load.DriveShare;
            var braking = BrakeForce(intent.Brake, velocity.Forward, vehicleMassKg, deltaTime, profile) * load.BrakeShare;
            var resistance = RollingResistance(
                velocity.Forward, load.SupportedMassKg, deltaTime,
                PushingWithTheMotion(intent.Throttle, velocity.Forward), profile);

            return new WheelForce(true, suspension, lateral, drive + braking + resistance);
        }
    }
}
