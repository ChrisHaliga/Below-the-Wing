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
        public static float Compression(float distanceToGround, VehicleProfile profile)
        {
            var fullyExtended = profile.wheelRadiusMetres + profile.suspensionRestLengthMetres;
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
        /// Newtons across the tire, opposing a sideways slide. Magnitude comes from the profile's
        /// grip curve read at this sliding speed, scaled by the weight the wheel carries.
        /// </summary>
        public static float LateralForce(float lateralVelocity, float supportedMassKg, VehicleProfile profile)
        {
            if (Mathf.Approximately(lateralVelocity, 0f))
            {
                return 0f;
            }

            var gripPerKilogram = profile.lateralGripCurve.Evaluate(Mathf.Abs(lateralVelocity));
            return -Mathf.Sign(lateralVelocity) * gripPerKilogram * supportedMassKg;
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
        public static float RollingResistance(float forwardVelocity, float supportedMassKg, float deltaTime, VehicleProfile profile)
        {
            if (Mathf.Approximately(forwardVelocity, 0f))
            {
                return 0f;
            }

            var speed = Mathf.Abs(forwardVelocity);

            var fromTheTire = profile.rollingResistanceCoefficient * supportedMassKg * Physics.gravity.magnitude;
            var fromTheDriveline = profile.coastingDragPerSecond * supportedMassKg * speed;
            var enoughToStopItThisStep = speed * supportedMassKg / Mathf.Max(deltaTime, 1e-5f);

            return -Mathf.Sign(forwardVelocity)
                   * Mathf.Min(fromTheTire + fromTheDriveline, enoughToStopItThisStep);
        }

        /// <summary>Newtons of forward drive the whole vehicle produces at this throttle.</summary>
        public static float DriveForce(float throttle, VehicleProfile profile)
            => Mathf.Clamp(throttle, -1f, 1f) * profile.maxDriveForceNewtons;

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
            float deltaTime,
            VehicleProfile profile)
        {
            if (!probe.HitGround)
            {
                return WheelForce.None;
            }

            var compression = Compression(probe.DistanceToGround, profile);
            if (compression < 0f)
            {
                return WheelForce.None;
            }

            var suspension = SuspensionForce(compression, velocity.AlongSuspension, profile);
            var lateral = LateralForce(velocity.Lateral, load.SupportedMassKg, profile);
            var drive = DriveForce(intent.Throttle, profile) * load.DriveShare;
            var braking = BrakeForce(intent.Brake, velocity.Forward, vehicleMassKg, deltaTime, profile) * load.BrakeShare;
            var resistance = RollingResistance(velocity.Forward, load.SupportedMassKg, deltaTime, profile);

            return new WheelForce(true, suspension, lateral, drive + braking + resistance);
        }
    }
}
