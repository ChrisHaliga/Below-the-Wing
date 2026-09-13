using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Something that is keeping a vehicle in step with the machine that owns it, and knows how
    /// well that is going.
    ///
    /// An interface so that a readout can ask without the diagnostics needing to know anything
    /// about netcode, sessions, or how a report reaches a vehicle in the first place.
    /// </summary>
    public interface IKeepsInStep
    {
        /// <summary>
        /// How far this copy is from where its owner says it should be by now, in metres. Zero on
        /// the machine that owns the vehicle, which cannot be out of step with itself.
        /// </summary>
        float MetresOutOfPlace { get; }
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
        /// Steers a whole train one step closer to what its owner reported.
        ///
        /// A train is one object as far as this is concerned. The correction is worked out from the
        /// vehicle at the front, because that is the one whose position is reported, and then the
        /// same change is applied to every vehicle in the train.
        ///
        /// Applying it to the front alone is what makes a train wander. The carts behind do not get
        /// the push, so the couplings have to drag them along, and the solver spends every step
        /// undoing what the correction just did. Moving all of them by the same amount leaves the
        /// couplings with nothing to resist -- the shape of the train never changes, so no hinge is
        /// asked to do anything.
        ///
        /// Here rather than in the component that receives the report, so that what correction does
        /// to real rigidbodies can be watched without a network underneath it. The alternative is
        /// testing it through three machines in one process, and those share a single physics world
        /// -- every copy of a vehicle is a solid body in the same space as every other copy, so they
        /// collide with themselves and the measurement is meaningless.
        /// </summary>
        public static void Apply(
            IReadOnlyList<Rigidbody> train, Rigidbody leader, VehicleState said, float secondsSince,
            CorrectionSettings settings, float say = 1f)
        {
            say = Mathf.Clamp01(say);
            if (say <= 0f || leader == null)
            {
                // A crash is playing out. Local physics resolves it without interference, and the
                // two machines are allowed to disagree until it has finished.
                return;
            }

            // Nothing to steer. A kinematic body is not being moved by the solver at all -- it is
            // being carried by whatever it is attached to, and that thing is reporting its own
            // position. Pushing it would do nothing except log an error every step.
            if (leader.isKinematic)
            {
                return;
            }

            var shouldBe = WhereItShouldBeNow(said, secondsSince, settings);

            if (TooFarToBlend(leader.position, shouldBe, settings))
            {
                PutBack(train, leader, said, shouldBe);
                return;
            }

            var nudge = Nudge(leader.position, leader.linearVelocity, said, secondsSince, settings) * say;
            var spin = SpinNudge(leader.rotation, leader.angularVelocity, said, settings) * say;

            // A correction too small to see is not worth making, and making it has a cost that has
            // nothing to do with its size: any force at all wakes a rigidbody. Applied every step
            // it means a vehicle standing still, in the right place, with nobody driving it, is
            // kept awake for the rest of the session -- and so is every other vehicle on the apron,
            // on every machine that does not own them.
            if (nudge.magnitude < settings.leaveAloneBelow && spin.magnitude < settings.leaveAloneBelow)
            {
                return;
            }

            foreach (var body in train)
            {
                if (body == null || body.isKinematic)
                {
                    continue;
                }

                // A turn about the front of the train, not five vehicles each twisting where they
                // stand. Turning a body also carries it sideways by however far it sits from the
                // point being turned about, and leaving that out is what makes the carts fight the
                // couplings holding them: each one is spun in place, the hinge refuses, and the
                // pair of them shuffle for ever without ever settling.
                var carriedRound = Vector3.Cross(spin, body.position - leader.position);

                body.AddForce(nudge + carriedRound, ForceMode.VelocityChange);
                body.AddTorque(spin, ForceMode.VelocityChange);
            }
        }

        /// <summary>
        /// Moves a train bodily to where its owner says the front of it is, keeping its own shape.
        ///
        /// Every vehicle moves and turns by the same amount, so the train arrives still hitched up
        /// in the order it was in. Moving only the front of it leaves every coupling violated by the
        /// distance travelled, and the solver answers that by throwing the carts apart -- which
        /// reads as a train that twists itself inside out rather than arriving.
        /// </summary>
        static void PutBack(
            IReadOnlyList<Rigidbody> train, Rigidbody leader, VehicleState said, Vector3 shouldBe)
        {
            var wasAt = leader.position;
            var turn = said.Rotation * Quaternion.Inverse(leader.rotation);

            foreach (var body in train)
            {
                if (body == null)
                {
                    continue;
                }

                var offset = body.position - wasAt;

                body.position = shouldBe + (turn * offset);
                body.rotation = turn * body.rotation;
                body.linearVelocity = said.Velocity;
                body.angularVelocity = said.Spin;
            }
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
