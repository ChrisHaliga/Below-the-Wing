using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    public struct ReportedMotion : INetworkSerializable
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 Spin;

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

    public abstract class MotionReplication : NetworkBehaviour, IKeepsInStep
    {
        [SerializeField, Tooltip("How hard a copy is steered toward its owner's report")]
        CorrectionSettings m_Correction = CorrectionSettings.Default;

        [SerializeField, Tooltip("Reports per second")]
        float m_ReportsPerSecond = NetworkClock.ReportsPerSecond;

        readonly NetworkVariable<ReportedMotion> m_Reported =
            new NetworkVariable<ReportedMotion>(default, NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);

        float m_SinceLastReport;

        protected abstract Rigidbody Body { get; }

        public bool HeardFromTheOwner => m_Reported.Value.TakenAt > 0d;

        public Vector3 WhereTheOwnerSaysItIs => m_Reported.Value.Position;

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

        protected void Report()
        {
            m_SinceLastReport += Time.fixedDeltaTime;
            if (m_SinceLastReport < 1f / Mathf.Max(m_ReportsPerSecond, 1f))
            {
                return;
            }

            ReportNow();
        }

        protected void ReportNow()
        {
            m_SinceLastReport = 0f;
            m_Reported.Value = ReportedMotion.Taken(Body, NetworkManager.ServerTime.Time);
        }

        protected void KeepUp(float say = 1f)
        {
            if (!HeardFromTheOwner)
            {
                return;
            }

            Correction.Apply(Body, m_Reported.Value.AsState(), SecondsSinceReading(), m_Correction, say);
        }

        float SecondsSinceReading()
            => Mathf.Max(0f, (float)(NetworkManager.ServerTime.Time - m_Reported.Value.TakenAt));
    }
}
