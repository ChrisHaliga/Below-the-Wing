using BelowTheWing.Crew;
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
    /// What is particular to people is how tall they are. A character drawn standing while they
    /// are crouched inside a cart has their head through its roof on every screen but their own.
    /// </summary>
    [RequireComponent(typeof(CrewCharacter))]
    [DisallowMultipleComponent]
    public sealed class CrewMotion : MotionReplication
    {
        /// <summary>
        /// Whether this character is crouched, as the machine they are played on reports it.
        ///
        /// Separate from their motion because it changes far less often and is worth sending on its
        /// own.
        /// </summary>
        readonly NetworkVariable<bool> m_Crouched =
            new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Owner);

        CrewCharacter m_Crew;

        void Awake() => m_Crew = GetComponent<CrewCharacter>();

        protected override Rigidbody Body => m_Crew.Body;

        void FixedUpdate()
        {
            if (Body == null)
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
                return;
            }

            // Asked for rather than applied outright, so that a copy standing under something low
            // stays down for the same reason the original does. Both machines then agree about a
            // player who is crouched because they have to be.
            m_Crew.Stance.Want(m_Crouched.Value);

            KeepUp();
        }
    }
}
