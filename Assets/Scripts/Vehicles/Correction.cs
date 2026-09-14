using System;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [Serializable]
    public struct VehicleState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 Spin;

        public static VehicleState Of(Rigidbody body)
            => new VehicleState
            {
                Position = body.position,
                Rotation = body.rotation,
                Velocity = body.linearVelocity,
                Spin = body.angularVelocity
            };
    }

    public interface IKeepsInStep
    {
        float MetresOutOfPlace { get; }
    }

    [Serializable]
    public struct CorrectionSettings
    {
        [Tooltip("Position error turned into closing speed, per second")]
        public float closingRatePerSecond;

        [Tooltip("Fraction of the wanted change applied per step, 0 to 1")]
        public float authority;

        [Tooltip("Fastest a correction may close a gap, m/s")]
        public float closingCeilingMetresPerSecond;

        [Tooltip("Extrapolation trusted, s")]
        public float extrapolationCeilingSeconds;

        [Tooltip("Corrections smaller than this are skipped, m/s")]
        public float leaveAloneBelow;

        public static CorrectionSettings Default => new CorrectionSettings
        {
            closingRatePerSecond = 6f,
            authority = 0.35f,
            closingCeilingMetresPerSecond = 12f,
            extrapolationCeilingSeconds = 0.5f,
            leaveAloneBelow = 0.05f
        };
    }

    public static class Correction
    {
        public static Vector3 WhereItShouldBeNow(VehicleState said, float secondsSince, CorrectionSettings settings)
        {
            var carried = Mathf.Clamp(secondsSince, 0f, settings.extrapolationCeilingSeconds);
            return said.Position + (said.Velocity * carried);
        }

        public static Vector3 Nudge(
            Vector3 here, Vector3 movingAt, VehicleState said, float secondsSince, CorrectionSettings settings)
        {
            var shouldBe = WhereItShouldBeNow(said, secondsSince, settings);
            var closing = Vector3.ClampMagnitude(
                (shouldBe - here) * settings.closingRatePerSecond, settings.closingCeilingMetresPerSecond);

            return (said.Velocity + closing - movingAt) * Mathf.Clamp01(settings.authority);
        }

        public static void Apply(
            Rigidbody body, VehicleState said, float secondsSince,
            CorrectionSettings settings, float say = 1f)
        {
            say = Mathf.Clamp01(say);
            if (say <= 0f || body == null)
            {
                return;
            }

            if (body.isKinematic)
            {
                return;
            }

            var nudge = Nudge(body.position, body.linearVelocity, said, secondsSince, settings) * say;
            var spin = SpinNudge(body.rotation, body.angularVelocity, said, settings) * say;

            if (nudge.magnitude < settings.leaveAloneBelow && spin.magnitude < settings.leaveAloneBelow)
            {
                return;
            }

            body.AddForce(nudge, ForceMode.VelocityChange);
            body.AddTorque(spin, ForceMode.VelocityChange);
        }

        public static Vector3 SpinNudge(
            Quaternion facing, Vector3 spinningAt, VehicleState said, CorrectionSettings settings)
        {
            (said.Rotation * Quaternion.Inverse(facing)).ToAngleAxis(out var degrees, out var axis);

            if (degrees > 180f)
            {
                degrees -= 360f;
            }

            if (float.IsInfinity(axis.sqrMagnitude) || axis.sqrMagnitude < 1e-6f)
            {
                return Vector3.zero;
            }

            var wanted = said.Spin + (axis.normalized * (degrees * Mathf.Deg2Rad * settings.closingRatePerSecond));
            return (wanted - spinningAt) * Mathf.Clamp01(settings.authority);
        }
    }
}
