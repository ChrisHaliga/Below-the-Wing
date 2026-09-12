using System.Collections.Generic;
using BelowTheWing.Vehicles;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// Where something is and how it is moving, as its owner reports it.
    ///
    /// A separate type from <see cref="VehicleState"/> because that one belongs to the physics and
    /// must stay ignorant of netcode. This is the same facts in the shape the wire wants them.
    /// </summary>
    public struct ReportedMotion : INetworkSerializable
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 Spin;

        /// <summary>The session clock when the owner took this reading.</summary>
        public double TakenAt;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref Spin);
            serializer.SerializeValue(ref TakenAt);
        }

        public VehicleState AsState()
            => new VehicleState
            {
                Position = Position,
                Rotation = Rotation,
                Velocity = Velocity,
                Spin = Spin
            };

        public static ReportedMotion Taken(Rigidbody body, double at)
            => new ReportedMotion
            {
                Position = body.position,
                Rotation = body.rotation,
                Velocity = body.linearVelocity,
                Spin = body.angularVelocity,
                TakenAt = at
            };
    }

    /// <summary>
    /// Keeping this machine's copy of a moving body in step with the machine that owns it.
    ///
    /// The owner reports where its body is and how it is moving, several times a second. Every
    /// other machine is already simulating that body properly -- it has weight, momentum, and
    /// whatever holds it up -- so the report is not applied to it. It is blended toward, by changing
    /// what the body is doing rather than where it is.
    ///
    /// That distinction is the whole point. A body whose position is assigned cannot take part in a
    /// collision: it arrives somewhere without having travelled, so the impulse that should have
    /// been exchanged never happens, and the crash comes out differently on each screen. Steering it
    /// instead leaves every contact, coupling and spring intact, and the disagreement is settled
    /// over a few frames by pushing rather than by overwriting.
    ///
    /// Velocity is reported alongside position for two reasons. It is what lets a copy carry on
    /// sensibly between updates instead of lurching from one report to the next. And it is what a
    /// collision modifies -- without it there is nothing for an impact to write into that survives
    /// the next update arriving.
    ///
    /// Vehicles, people and cargo all do this, and differ only in what else they carry and in which
    /// bodies a correction is spread across. What they share lives here so that it is one thing.
    /// </summary>
    public abstract class MotionReplication : NetworkBehaviour, IKeepsInStep
    {
        [SerializeField, Tooltip("How hard a copy is steered back towards what its owner reports.")]
        CorrectionSettings m_Correction = CorrectionSettings.Default;

        [SerializeField, Tooltip("How many times a second the owner reports where this is.")]
        float m_ReportsPerSecond = 20f;

        readonly NetworkVariable<ReportedMotion> m_Reported =
            new NetworkVariable<ReportedMotion>(default, NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);

        Rigidbody[] m_JustThisOne;
        float m_SinceLastReport;

        /// <summary>The body whose motion is reported and steered.</summary>
        protected abstract Rigidbody Body { get; }

        /// <summary>Whether anything has been heard from the machine that owns this yet.</summary>
        public bool HeardFromTheOwner => m_Reported.Value.TakenAt > 0d;

        /// <summary>
        /// Where the owner last said this was. What tells a copy that has never heard anything from
        /// one that is being kept in step.
        /// </summary>
        public Vector3 WhereTheOwnerSaysItIs => m_Reported.Value.Position;

        /// <summary>
        /// How far this copy is from where its owner says it should be by now, in metres.
        ///
        /// This is the number that says whether a disagreement between two machines is a tuning
        /// problem or an architectural one, and it cannot be seen any other way -- both screens look
        /// perfectly reasonable on their own.
        /// </summary>
        public virtual float MetresOutOfPlace
        {
            get
            {
                if (IsOwner || !HeardFromTheOwner || Body == null)
                {
                    return 0f;
                }

                var shouldBe = Correction.WhereItShouldBeNow(
                    m_Reported.Value.AsState(), SecondsSinceReading(), m_Correction);

                return Vector3.Distance(Body.position, shouldBe);
            }
        }

        /// <summary>
        /// Tells everybody else where this is, at a steady rate rather than every step.
        ///
        /// Twenty times a second against a fifty hertz step. Reporting every step would spend two
        /// and a half times the bandwidth to say what the receiving end cannot act on any better,
        /// because what it does with a report is blend toward it over the following frames anyway.
        /// </summary>
        protected void Report()
        {
            m_SinceLastReport += Time.fixedDeltaTime;
            if (m_SinceLastReport < 1f / Mathf.Max(m_ReportsPerSecond, 1f))
            {
                return;
            }

            m_SinceLastReport = 0f;
            m_Reported.Value = ReportedMotion.Taken(Body, NetworkManager.ServerTime.Time);
        }

        /// <summary>
        /// Steers this body one step closer to what its owner reported. Nothing happens until the
        /// owner has said anything at all: better to leave a copy where it was created than to drag
        /// it toward the origin, which is where an empty report points.
        /// </summary>
        protected void KeepUp() => KeepUp(m_JustThisOne ??= new[] { Body }, say: 1f);

        /// <summary>
        /// Steers a set of bodies one step closer to what their owner reported about this one, with
        /// however much say the owner currently has.
        /// </summary>
        protected void KeepUp(IReadOnlyList<Rigidbody> bodies, float say)
        {
            if (!HeardFromTheOwner)
            {
                return;
            }

            Correction.Apply(bodies, Body, m_Reported.Value.AsState(), SecondsSinceReading(), m_Correction, say);
        }

        float SecondsSinceReading()
            => Mathf.Max(0f, (float)(NetworkManager.ServerTime.Time - m_Reported.Value.TakenAt));
    }
}
