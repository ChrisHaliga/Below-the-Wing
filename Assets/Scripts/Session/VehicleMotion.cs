using BelowTheWing.Vehicles;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// Where a vehicle is and how it is moving, as its owner reports it.
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
    /// Keeping this machine's copy of a vehicle in step with the machine that owns it.
    ///
    /// The owner reports where its vehicle is and how it is moving, several times a second. Every
    /// other machine is already simulating that vehicle properly -- it has weight, springs and
    /// momentum of its own -- so the report is not applied to it. It is blended toward, by changing
    /// what the vehicle is doing rather than where it is.
    ///
    /// That distinction is the whole point. A vehicle whose position is assigned cannot take part in
    /// a collision: it arrives somewhere without having travelled, so the impulse that should have
    /// been exchanged never happens, and the crash comes out differently on each screen. Steering it
    /// instead leaves every contact, coupling and spring intact, and the disagreement is settled
    /// over a few frames by pushing rather than by overwriting.
    ///
    /// Velocity is reported alongside position for two reasons. It is what lets a copy carry on
    /// sensibly between updates instead of lurching from one report to the next. And it is what a
    /// collision modifies -- without it there is nothing for an impact to write into that survives
    /// the next update arriving.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    [DisallowMultipleComponent]
    public sealed class VehicleMotion : NetworkBehaviour
    {
        [SerializeField, Tooltip("How hard a copy is steered back towards what its owner reports.")]
        CorrectionSettings m_Correction = CorrectionSettings.Default;

        [SerializeField, Tooltip("How many times a second the owner reports where its vehicle is.")]
        float m_ReportsPerSecond = 20f;

        readonly NetworkVariable<ReportedMotion> m_Reported =
            new NetworkVariable<ReportedMotion>(default, NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);

        VehicleController m_Vehicle;
        float m_SinceLastReport;

        void Awake() => m_Vehicle = GetComponent<VehicleController>();

        /// <summary>
        /// Authority over a vehicle is netcode ownership, and this is where the two are tied together.
        ///
        /// Nothing else gets to have an opinion. Left to be set from somewhere else -- by whatever
        /// decides which trains this machine is holding, say -- the game's idea of who is in charge
        /// and netcode's can disagree, and every machine that wrongly believes a vehicle is its own
        /// drives it locally and tries to broadcast where it went. Netcode refuses the write, so the
        /// only sign of it is an error in a log nobody is reading while the vehicle quietly diverges.
        /// </summary>
        public override void OnNetworkSpawn() => m_Vehicle.OursToMove = IsOwner;

        protected override void OnOwnershipChanged(ulong previous, ulong now)
            => m_Vehicle.OursToMove = now == NetworkManager.LocalClientId;

        /// <summary>Whether anything has been heard from the machine that owns this vehicle yet.</summary>
        public bool HeardFromTheOwner => m_Reported.Value.TakenAt > 0d;

        /// <summary>
        /// Where the owner last said this vehicle was. Useful for a readout, and for telling a copy
        /// that has never heard anything from one that is being kept in step.
        /// </summary>
        public Vector3 WhereTheOwnerSaysItIs => m_Reported.Value.Position;

        /// <summary>
        /// How far this copy is from where its owner says it should be by now, in metres.
        ///
        /// This is the number that says whether a disagreement between two machines is a tuning
        /// problem or an architectural one, and it cannot be seen any other way -- both screens look
        /// perfectly reasonable on their own.
        /// </summary>
        public float MetresOutOfPlace
        {
            get
            {
                if (IsOwner || !HeardFromTheOwner)
                {
                    return 0f;
                }

                var shouldBe = Correction.WhereItShouldBeNow(
                    m_Reported.Value.AsState(), SecondsSinceReading(), m_Correction);

                return Vector3.Distance(m_Vehicle.Body.position, shouldBe);
            }
        }

        float SecondsSinceReading()
            => Mathf.Max(0f, (float)(NetworkManager.ServerTime.Time - m_Reported.Value.TakenAt));

        /// <summary>
        /// Whether this vehicle is the one in its train that gets corrected.
        ///
        /// Only the vehicle at the front is. Everything behind it is towed by the local copy of that
        /// vehicle through the hinges that already hold the train together, exactly as it is towed on
        /// the machine that owns it.
        ///
        /// Correcting each cart separately is what tears these trains apart. A chain is a set of
        /// constraints and a correction is a force applied to one body: told that cart three is
        /// thirty centimetres back and cart four twenty centimetres left, correction pushes each
        /// toward a place the hinge between them forbids, and the solver and the network take turns
        /// losing. Towing removes the argument -- there is one corrected body and the hinges
        /// distribute its motion the way they already know how to.
        /// </summary>
        bool TheOneWorthCorrecting => m_Vehicle.Chain == null || m_Vehicle.Chain.Leader == m_Vehicle;

        void FixedUpdate()
        {
            if (m_Vehicle.Body == null)
            {
                return;
            }

            if (IsOwner)
            {
                Report();
            }
            else if (TheOneWorthCorrecting)
            {
                KeepUp();
            }
        }

        /// <summary>
        /// Tells everybody else where this vehicle is, at a steady rate rather than every step.
        ///
        /// Twenty times a second against a fifty hertz step. Reporting every step would spend two
        /// and a half times the bandwidth to say what the receiving end cannot act on any better,
        /// because what it does with a report is blend toward it over the following frames anyway.
        /// </summary>
        void Report()
        {
            m_SinceLastReport += Time.fixedDeltaTime;
            if (m_SinceLastReport < 1f / Mathf.Max(m_ReportsPerSecond, 1f))
            {
                return;
            }

            m_SinceLastReport = 0f;
            m_Reported.Value = ReportedMotion.Taken(m_Vehicle.Body, NetworkManager.ServerTime.Time);
        }

        void KeepUp()
        {
            if (!HeardFromTheOwner)
            {
                // Better to let it sit where it was created than to drag it toward the origin,
                // which is where an empty report points.
                return;
            }

            Correction.Apply(m_Vehicle.Body, m_Reported.Value.AsState(), SecondsSinceReading(), m_Correction);
        }
    }
}
