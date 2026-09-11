using System;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>Where a vehicle was, and how it was moving, at the moment its owner said so.</summary>
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

    /// <summary>How hard a vehicle is steered back toward where its owner says it is.</summary>
    [Serializable]
    public struct CorrectionSettings
    {
        [Tooltip("How quickly a position error is turned into a velocity that closes it, per second.")]
        public float closingRatePerSecond;

        [Tooltip("How much of the difference between wanted and actual velocity is applied per step, " +
                 "from 0 to 1. Below 1 the correction eases in rather than arriving as a kick.")]
        public float authority;

        [Tooltip("How far out of position a vehicle may be before blending is given up and it is " +
                 "moved outright, in metres.")]
        public float snapMetres;

        [Tooltip("Seconds of extrapolation to trust before treating the last update as stale. Past " +
                 "this, carrying a velocity forward invents motion rather than predicting it.")]
        public float extrapolationCeilingSeconds;

        [Tooltip("How small a correction is not worth making, in metres per second. Below this the " +
                 "vehicle is left alone entirely so that it can fall asleep.")]
        public float leaveAloneBelow;

        /// <summary>Settings that close an ordinary error without visible pull.</summary>
        public static CorrectionSettings Default => new CorrectionSettings
        {
            closingRatePerSecond = 6f,
            authority = 0.35f,
            snapMetres = 2f,
            extrapolationCeilingSeconds = 0.5f,
            leaveAloneBelow = 0.05f
        };
    }

    /// <summary>
    /// Bringing a vehicle this machine does not own back to where its owner says it is.
    ///
    /// The difficulty is that both things are true at once: the owner decides where the vehicle is,
    /// and the vehicle is a physical body here with its own momentum that other bodies are pushing
    /// against. Setting the position outright satisfies the first and destroys the second -- a
    /// vehicle that teleports mid-collision takes the impulse out of the crash, and PhysX is left
    /// resolving contacts against a body that moved without moving.
    ///
    /// So the owner's word is applied as force. The vehicle is steered toward where it should be by
    /// changing what it is doing rather than where it is, which leaves every collision, every
    /// coupling and every spring intact and simply loses the argument gradually. Only when the gap
    /// grows too large to close that way is the vehicle moved outright, and that is a failure being
    /// cut short rather than the normal case.
    ///
    /// Netcode-agnostic on purpose: it knows nothing about who sent what or when, only about a
    /// state, its age, and a body.
    /// </summary>
    public static class Correction
    {
        /// <summary>
        /// Where a vehicle would be now, given where it was and how it was moving when its owner
        /// last said so.
        ///
        /// Comparing against the raw reported position instead would measure the age of the message
        /// rather than the error: a vehicle driven in a straight line at ten metres a second is a
        /// metre and a half "wrong" at 150 milliseconds while being exactly where both machines
        /// expect. Correcting that gap fights the driver the whole way down the apron.
        ///
        /// Extrapolation is capped because a carried velocity stops being a prediction once it is
        /// old. Beyond the ceiling the vehicle has more likely braked, turned or hit something, and
        /// running the last known velocity onward invents motion nobody performed.
        /// </summary>
        public static Vector3 WhereItShouldBeNow(VehicleState said, float secondsSince, CorrectionSettings settings)
        {
            var carried = Mathf.Clamp(secondsSince, 0f, settings.extrapolationCeilingSeconds);
            return said.Position + (said.Velocity * carried);
        }

        /// <summary>
        /// Whether the gap is too wide to be worth closing gently.
        ///
        /// Past this the vehicle is not slightly out of step, it is somewhere else -- a stall, a
        /// burst of lost packets, a collision that happened on one machine and not the other. Easing
        /// across that distance takes long enough to be watched, and what gets watched is a vehicle
        /// sliding sideways through the apron under its own power.
        /// </summary>
        public static bool TooFarToBlend(Vector3 here, Vector3 shouldBe, CorrectionSettings settings)
            => (shouldBe - here).sqrMagnitude > settings.snapMetres * settings.snapMetres;

        /// <summary>
        /// The change in velocity that closes the gap, in metres per second.
        ///
        /// Two parts. The owner's own velocity, so the vehicle travels the way its owner is
        /// travelling rather than repeatedly falling behind and being hauled back. And a closing
        /// term proportional to how far out of position it is, so a standing error is walked in
        /// instead of persisting.
        ///
        /// Returned as a velocity change rather than a force so that the same correction moves a
        /// lone tractor and a tractor towing eight loaded carts at the same rate. Expressed in
        /// newtons it would not: the same push moves nine bodies a ninth as far, and a gain tuned
        /// against one vehicle would quietly under-correct every train in the game.
        /// </summary>
        public static Vector3 Nudge(
            Vector3 here, Vector3 movingAt, VehicleState said, float secondsSince, CorrectionSettings settings)
        {
            var shouldBe = WhereItShouldBeNow(said, secondsSince, settings);
            var wanted = said.Velocity + ((shouldBe - here) * settings.closingRatePerSecond);

            return (wanted - movingAt) * Mathf.Clamp01(settings.authority);
        }

        /// <summary>
        /// Steers a body one step closer to what its owner reported.
        ///
        /// Here rather than in the component that receives the report, so that what correction does
        /// to a real rigidbody can be watched without a network underneath it. The alternative is
        /// testing it through three machines in one process, and those share a single physics world
        /// -- every copy of a vehicle is a solid body in the same space as every other copy, so they
        /// collide with themselves and the measurement is meaningless.
        /// </summary>
        public static void Apply(Rigidbody body, VehicleState said, float secondsSince, CorrectionSettings settings)
        {
            var shouldBe = WhereItShouldBeNow(said, secondsSince, settings);

            if (TooFarToBlend(body.position, shouldBe, settings))
            {
                // Blending has been given up on, so the body is put where it belongs and given the
                // motion that goes with it. Left with its old velocity it would immediately set off
                // away from the place it was just moved to.
                body.position = shouldBe;
                body.rotation = said.Rotation;
                body.linearVelocity = said.Velocity;
                body.angularVelocity = said.Spin;
                return;
            }

            var nudge = Nudge(body.position, body.linearVelocity, said, secondsSince, settings);
            var spin = SpinNudge(body.rotation, body.angularVelocity, said, settings);

            // A correction too small to see is not worth making, and making it has a cost that has
            // nothing to do with its size: any force at all wakes a rigidbody. Applied every step
            // it means a vehicle standing still, in the right place, with nobody driving it, is
            // kept awake for the rest of the session -- and so is every other vehicle on the apron,
            // on every machine that does not own them.
            if (nudge.magnitude < settings.leaveAloneBelow && spin.magnitude < settings.leaveAloneBelow)
            {
                return;
            }

            body.AddForce(nudge, ForceMode.VelocityChange);
            body.AddTorque(spin, ForceMode.VelocityChange);
        }

        /// <summary>
        /// The change in angular velocity that turns the vehicle to face the way its owner says it
        /// faces, in radians per second.
        ///
        /// The same argument as <see cref="Nudge"/>, for the same reason. Slerping the rotation into
        /// place would be an assignment wearing a blend's clothing: it overrides whatever the solver
        /// just worked out, so a cart being jackknifed by a collision would be quietly straightened
        /// mid-impact and the crash would come out different on the two machines.
        ///
        /// Rotation is deliberately not extrapolated. Angular velocity carried forward through a
        /// turn diverges far faster than a position does, and a vehicle spun past where it should be
        /// reads worse than one a few degrees behind and then has to be turned back.
        /// </summary>
        public static Vector3 SpinNudge(
            Quaternion facing, Vector3 spinningAt, VehicleState said, CorrectionSettings settings)
        {
            (said.Rotation * Quaternion.Inverse(facing)).ToAngleAxis(out var degrees, out var axis);

            // ToAngleAxis always reports the positive turn, so a small turn the other way arrives as
            // a nearly-full one. Taken at face value the vehicle would be spun the long way round.
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
