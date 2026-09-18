using UnityEngine;

namespace BelowTheWing.Crew
{
    public sealed class Crouching
    {
        const float RoomToSpareMetres = 0.05f;

        readonly CrewProfile m_Profile;
        readonly CapsuleCollider m_Body;
        readonly LayerMask m_Blocks;

        bool m_WantsToCrouch;

        public Crouching(CrewProfile profile, CapsuleCollider body, LayerMask blocks)
        {
            m_Profile = profile;
            m_Body = body;
            m_Blocks = blocks;

            Crouched = false;
            ApplyHeight();
        }

        public bool Crouched { get; private set; }

        public float HeightMetres => Crouched ? m_Profile.crouchedHeightMetres : m_Profile.heightMetres;

        public float SpeedMultiplier => Crouched ? Mathf.Clamp01(m_Profile.crouchSpeedMultiplier) : 1f;

        bool RoomToStand
        {
            get
            {
                if (m_Body == null || !Crouched)
                {
                    return true;
                }

                var radius = m_Profile.radiusMetres;

                var feet = m_Body.transform.position + m_Body.center - (Vector3.up * (m_Body.height * 0.5f));
                var standing = m_Profile.heightMetres + RoomToSpareMetres;

                var crouchedCrown = feet.y + HeightMetres;
                var standingCrown = feet.y + standing;
                var middle = (crouchedCrown + standingCrown) * 0.5f;
                var half = Mathf.Max((standingCrown - crouchedCrown) * 0.5f - radius, 0f);

                var lower = new Vector3(feet.x, middle - half, feet.z);
                var upper = new Vector3(feet.x, middle + half, feet.z);

                var count = Physics.OverlapCapsuleNonAlloc(
                    lower, upper, radius * 0.95f, s_Overhead, m_Blocks, QueryTriggerInteraction.Ignore);

                for (var i = 0; i < count; i++)
                {
                    if (s_Overhead[i] != m_Body)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        const int MostOverhead = 8;

        static readonly Collider[] s_Overhead = new Collider[MostOverhead];

        public void Want(bool crouched) => m_WantsToCrouch = crouched;

        public void Settle()
        {
            var shouldBe = m_WantsToCrouch || !RoomToStand;

            if (shouldBe == Crouched)
            {
                return;
            }

            Crouched = shouldBe;
            ApplyHeight();
        }

        void ApplyHeight()
        {
            if (m_Body == null)
            {
                return;
            }

            var wanted = HeightMetres;
            var lost = m_Profile.heightMetres - wanted;

            m_Body.height = wanted;
            m_Body.center = new Vector3(0f, -lost * 0.5f, 0f);
        }
    }
}
