using BelowTheWing.Crew;
using Unity.Netcode;
using UnityEngine;

namespace BelowTheWing.Session
{
    [RequireComponent(typeof(CrewCharacter))]
    [DisallowMultipleComponent]
    public sealed class CrewMotion : MotionReplication
    {
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

            m_Crew.Stance.Want(m_Crouched.Value);

            KeepUp();
        }
    }
}
