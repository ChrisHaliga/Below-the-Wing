using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    /// <summary>
    /// Keeping this machine's copy of somebody else's character in step with the machine they are
    /// playing on.
    ///
    /// The same arrangement vehicles use, and for the same reason: players run each other over on
    /// purpose. A character whose position is written straight onto them arrives somewhere without
    /// having travelled, so the impulse a collision should have exchanged never happens -- you
    /// drive a three tonne tractor through a colleague and nothing registers on either screen.
    ///
    /// So the owner reports where their character is and how fast, and every other machine steers
    /// its copy toward that with force, leaving the body free to be shoved in between.
    /// </summary>
    [RequireComponent(typeof(CrewCharacter))]
    [DisallowMultipleComponent]
    public sealed class CrewMotion : NetworkBehaviour, IKeepsInStep
    {
        [SerializeField, Tooltip("How hard a copy is steered back towards what its owner reports.")]
        CorrectionSettings m_Correction = CorrectionSettings.Default;

        [SerializeField, Tooltip("How many times a second the owner reports where their character is.")]
        float m_ReportsPerSecond = 20f;

        readonly NetworkVariable<ReportedMotion> m_Reported =
            new NetworkVariable<ReportedMotion>(default, NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Whether this character is crouched, as the machine they are played on reports it.
        ///
        /// Separate from their motion because it changes far less often and is worth sending on its
        /// own. It has to be sent at all: how tall somebody is decides where their head is, and a
        /// character drawn standing while they are crouched inside a cart has their head through
        /// its roof on every screen but their own.
        /// </summary>
        readonly NetworkVariable<bool> m_Crouched =
            new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);

        CrewCharacter m_Crew;
        Rigidbody[] m_JustThisOne;
        float m_SinceLastReport;

        void Awake()
        {
            m_Crew = GetComponent<CrewCharacter>();
            m_JustThisOne = new[] { m_Crew.Body };
        }

        /// <summary>Whether anything has been heard from the machine this character belongs to.</summary>
        public bool HeardFromTheOwner => m_Reported.Value.TakenAt > 0d;

        /// <summary>How far this copy is from where its owner says it should be by now, in metres.</summary>
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

                return Vector3.Distance(m_Crew.Body.position, shouldBe);
            }
        }

        float SecondsSinceReading()
            => Mathf.Max(0f, (float)(NetworkManager.ServerTime.Time - m_Reported.Value.TakenAt));

        void FixedUpdate()
        {
            if (m_Crew.Body == null)
            {
                return;
            }

            // Read afresh rather than caught when it changes, for the same reason vehicles do: an
            // object spawned elsewhere runs its spawn callback before ownership has been applied.
            m_Crew.OursToMove = IsOwner;

            if (IsOwner)
            {
                if (m_Crouched.Value != m_Crew.Stance.Crouched)
                {
                    m_Crouched.Value = m_Crew.Stance.Crouched;
                }

                Report();
            }
            else
            {
                // Asked for rather than applied outright, so that a copy standing under something
                // low stays down for the same reason the original does. Both machines then agree
                // about a player who is crouched because they have to be.
                m_Crew.Stance.Want(m_Crouched.Value);
            }

            if (!IsOwner && HeardFromTheOwner)
            {
                Correction.Apply(
                    m_JustThisOne, m_Crew.Body, m_Reported.Value.AsState(), SecondsSinceReading(),
                    m_Correction);
            }
        }

        void Report()
        {
            m_SinceLastReport += Time.fixedDeltaTime;
            if (m_SinceLastReport < 1f / Mathf.Max(m_ReportsPerSecond, 1f))
            {
                return;
            }

            m_SinceLastReport = 0f;
            m_Reported.Value = ReportedMotion.Taken(m_Crew.Body, NetworkManager.ServerTime.Time);
        }
    }
}
