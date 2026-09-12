using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// Getting down low enough to fit somewhere.
    ///
    /// A person who is shorter, and nothing more specific than that. It exists because a baggage
    /// cart is a container with a roof and the clear space above its deck is shorter than somebody
    /// standing, but nothing here knows what a cart is: the same crouch fits under a wing, into a
    /// hold, or through any other low gap.
    ///
    /// Standing back up is the half that needs care. A character who stands into a ceiling is a
    /// capsule overlapping a collider by half a metre, and the solver answers that by firing them
    /// through it -- so wanting to stand and being able to are kept apart. The want is remembered,
    /// and granted on the first step there is room, which is what lets somebody crouch into a cart,
    /// ask to stand, and come up as they walk out rather than staying bent double for ever.
    /// </summary>
    public sealed class Crouching
    {
        /// <summary>
        /// How much clear air a character needs above their head to stand up, in metres.
        ///
        /// Small on purpose. It is there so that a ceiling exactly as tall as somebody standing does
        /// not leave them flickering between the two heights, not to stop them standing in tight
        /// spaces they would really fit in.
        /// </summary>
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

        /// <summary>Whether they are crouched right now.</summary>
        public bool Crouched { get; private set; }

        /// <summary>How tall they are right now, in metres.</summary>
        public float HeightMetres => Crouched ? m_Profile.crouchedHeightMetres : m_Profile.heightMetres;

        /// <summary>
        /// How much of their walking speed they keep.
        ///
        /// Crouching has to cost something. Free, it is strictly better than standing -- smaller
        /// target, fits everywhere -- so everybody would spend the whole session down there and it
        /// would stop being a choice anybody makes.
        /// </summary>
        public float SpeedMultiplier => Crouched ? Mathf.Clamp01(m_Profile.crouchSpeedMultiplier) : 1f;

        /// <summary>
        /// Whether there is room above them to stand up.
        ///
        /// Asked as a sweep of the body they would have rather than a ray from their head, because a
        /// ray up the middle misses a shelf they are standing half under and lets them stand into
        /// its edge.
        /// </summary>
        public bool RoomToStand
        {
            get
            {
                if (m_Body == null)
                {
                    return true;
                }

                var standing = m_Profile.heightMetres;
                var radius = m_Profile.radiusMetres;

                // From the top of the crouched capsule to the top of the standing one: the space
                // that has to be empty for standing up to be possible.
                var feet = m_Body.transform.position - (Vector3.up * (HeightMetres * 0.5f));
                var from = feet + (Vector3.up * (HeightMetres - radius));
                var reach = standing + RoomToSpareMetres - HeightMetres;

                if (reach <= 0f)
                {
                    return true;
                }

                return !Physics.SphereCast(
                    from, radius * 0.95f, Vector3.up, out _, reach, m_Blocks, QueryTriggerInteraction.Ignore);
            }
        }

        /// <summary>
        /// Asks to be crouched or standing.
        ///
        /// Remembered rather than acted on. A request to stand that cannot be granted this step is
        /// not thrown away: it is what gets granted the moment they walk out from under whatever is
        /// over them.
        /// </summary>
        public void Want(bool crouched) => m_WantsToCrouch = crouched;

        /// <summary>Grants whatever was last asked for, if it can be. Called every fixed step.</summary>
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

        /// <summary>
        /// Resizes the body to match.
        ///
        /// The capsule keeps its centre on the character's origin, so shrinking it lifts the feet as
        /// much as it lowers the head. That is wrong for a person, who crouches from the floor up,
        /// so the centre moves down by half of what was lost and the feet stay where they were.
        /// </summary>
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
