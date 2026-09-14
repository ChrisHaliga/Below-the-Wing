using System;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public readonly struct GroundProbe
    {
        public readonly bool HitGround;

        public readonly float DistanceToGround;

        public GroundProbe(bool hitGround, float distanceToGround)
        {
            HitGround = hitGround;
            DistanceToGround = distanceToGround;
        }

        public static GroundProbe Airborne => new GroundProbe(false, float.PositiveInfinity);
    }

    public readonly struct ContactVelocity
    {
        public readonly float AlongSuspension;

        public readonly float Lateral;

        public readonly float Forward;

        public ContactVelocity(float alongSuspension, float lateral, float forward)
        {
            AlongSuspension = alongSuspension;
            Lateral = lateral;
            Forward = forward;
        }
    }

    public readonly struct WheelLoad
    {
        public readonly float SupportedMassKg;

        public readonly float DriveShare;

        public readonly float BrakeShare;

        public WheelLoad(float supportedMassKg, float driveShare, float brakeShare)
        {
            SupportedMassKg = supportedMassKg;
            DriveShare = driveShare;
            BrakeShare = brakeShare;
        }
    }

    public readonly struct WheelForce
    {
        public readonly bool Grounded;

        public readonly float AlongSuspension;

        public readonly float Lateral;

        public readonly float Forward;

        public WheelForce(bool grounded, float alongSuspension, float lateral, float forward)
        {
            Grounded = grounded;
            AlongSuspension = alongSuspension;
            Lateral = lateral;
            Forward = forward;
        }

        public static WheelForce None => new WheelForce(false, 0f, 0f, 0f);
    }

    public static class WheelPhysics
    {
        public static float Compression(float distanceToGround, float wheelRadiusMetres, VehicleProfile profile)
        {
            var fullyExtended = wheelRadiusMetres + profile.suspensionRestLengthMetres;
            return (fullyExtended - distanceToGround) / profile.suspensionRestLengthMetres;
        }

        public static float SuspensionForce(float compression, float alongSuspensionVelocity, VehicleProfile profile)
        {
            var spring = Mathf.Clamp01(compression) * profile.springStrengthNewtons;
            var damper = alongSuspensionVelocity * profile.damperNewtonsPerMetrePerSecond;

            return Mathf.Max(0f, spring - damper);
        }

        public const float HoldsBelowMetresPerSecond = 0.15f;

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

        const float StepsToTakeUpASlip = 3f;

        public static float MostGripPerKilogram(VehicleProfile profile)
            => profile.MostLateralGripPerKilogram;

        public static float RollingResistance(
            float forwardVelocity, float supportedMassKg, float deltaTime, bool drivingWithTheMotion, VehicleProfile profile)
        {
            if (Mathf.Approximately(forwardVelocity, 0f))
            {
                return 0f;
            }

            var speed = Mathf.Abs(forwardVelocity);
            var fromTheTire = profile.rollingResistanceCoefficient * supportedMassKg * Physics.gravity.magnitude;

            var fromTheDriveline = drivingWithTheMotion ? 0f : profile.coastingDragPerSecond * supportedMassKg * speed;
            var enoughToStopItThisStep = speed * supportedMassKg / Mathf.Max(deltaTime, 1e-5f);

            return -Mathf.Sign(forwardVelocity)
                   * Mathf.Min(fromTheTire + fromTheDriveline, enoughToStopItThisStep);
        }

        public static float DriveForce(float throttle, bool sprinting, float forwardVelocity, VehicleProfile profile)
        {
            var asked = Mathf.Clamp(throttle, -1f, 1f) * profile.maxDriveForceNewtons;
            var top = profile.topSpeedMetresPerSecond;
            if (sprinting)
            {
                asked *= profile.sprintDriveMultiplier;
                top *= profile.sprintDriveMultiplier;
            }

            if (!PushingWithTheMotion(throttle, forwardVelocity))
            {
                return asked;
            }

            var headroom = top > 0f ? Mathf.Clamp01((top - Mathf.Abs(forwardVelocity)) / (top * TopSpeedFadeFraction)) : 0f;
            return asked * headroom;
        }

        public static bool PushingWithTheMotion(float throttle, float forwardVelocity)
            => !Mathf.Approximately(throttle, 0f)
               && !Mathf.Approximately(forwardVelocity, 0f)
               && Mathf.Sign(throttle) == Mathf.Sign(forwardVelocity);

        const float TopSpeedFadeFraction = 0.25f;

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
