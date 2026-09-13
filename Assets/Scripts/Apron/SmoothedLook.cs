using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Apron
{
    /// <summary>
    /// Keeps what you see a little behind where the physics actually is.
    ///
    /// Hard corrections are going to happen. A vehicle that has drifted too far from where its
    /// owner says it is gets moved outright rather than eased across, because easing across twenty
    /// metres is worse than arriving. That jump is correct, and watching it is horrible: the box
    /// simply ceases to be in one place and starts being in another.
    ///
    /// So the shape is not the body. The body is moved wherever it has to go, and the shape catches
    /// up over about a tenth of a second -- long enough to turn a teleport into a fast slide, short
    /// enough that nobody can see the lag while driving. Colliders stay on the body, unsmoothed, so
    /// nothing is ever hit where it is not.
    ///
    /// This is a transform concern rather than an art one. It costs nothing at all now, and
    /// retrofitting it once real meshes hang off attachment points is materially harder.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SmoothedLook : MonoBehaviour
    {
        /// <summary>Roughly how long the shape takes to catch up, in seconds.</summary>
        public const float CatchUpSeconds = 0.1f;

        /// <summary>
        /// Past this the shape stops trailing and is simply put where the body is.
        ///
        /// A vehicle that has been moved right across the apron would otherwise be drawn sliding
        /// there under its own power, which is a worse lie than the jump it was hiding. Smoothing
        /// is for the gap between where a body is and where it nearly is.
        /// </summary>
        public const float TooFarToFollowMetres = 8f;

        Transform m_Body;
        IMovedFromHere m_Mover;

        /// <summary>
        /// Where the shape is being drawn, kept here rather than read back off its own transform.
        ///
        /// It stays a child of the body, so the body moving carries it along before anything gets a
        /// chance to look. Read back, the shape would always report itself as exactly where the body
        /// is and there would be nothing to lag behind.
        /// </summary>
        Vector3 m_ShownAt;
        Quaternion m_ShownFacing = Quaternion.identity;

        /// <summary>
        /// Where and how the shape was placed on its body, in the body's own space.
        ///
        /// Trailing means writing the shape's world pose every frame, and a world pose written
        /// outright throws away whatever the shape was authored with. That is not a corner case: an
        /// aircraft's stand-in is a capsule turned on its side, and a vehicle's real model sits
        /// above an origin that is down on the tarmac between its wheels. Both are a local offset
        /// or rotation, and both are destroyed by catching up unless the catching up puts them back.
        /// </summary>
        Vector3 m_PlacedAt;
        Quaternion m_PlacedFacing = Quaternion.identity;

        /// <summary>How far behind the body the shape currently is, in metres.</summary>
        public float TrailingByMetres => m_Body == null ? 0f : Vector3.Distance(m_ShownAt, m_Body.position);

        /// <summary>
        /// Whether the shape should trail at all.
        ///
        /// Only for things this machine does not decide the position of. A vehicle you are driving
        /// or the character you are walking never gets snapped -- there is nothing to hide -- and
        /// putting a tenth of a second between the controls and what you see would be felt at once
        /// as them going soft.
        /// </summary>
        public bool WorthSmoothing => m_Mover == null || !m_Mover.OursToMove;

        void Awake()
        {
            // Stays a child. Its world pose is written every frame, which overrides whatever it
            // inherited, so there is nothing to detach and nothing left behind when the body goes.
            m_Body = transform.parent;
            m_Mover = m_Body != null ? m_Body.GetComponent<IMovedFromHere>() : null;
            RememberHowItWasPlaced();
            CatchUpNow();
        }

        void LateUpdate() => Follow(Time.deltaTime);

        /// <summary>
        /// Moves the shape a step closer to the body. Separated from the frame loop so the catching
        /// up can be watched a step at a time.
        /// </summary>
        public void Follow(float deltaTime)
        {
            if (m_Body == null)
            {
                return;
            }

            if (!WorthSmoothing || TrailingByMetres > TooFarToFollowMetres)
            {
                CatchUpNow();
                return;
            }

            // Framerate independent: the fraction of the remaining gap closed per second is what is
            // fixed, not the fraction closed per frame. Otherwise the shape trails further behind on
            // a fast machine than a slow one, which is the opposite of what anybody would want.
            var caughtUp = 1f - Mathf.Exp(-deltaTime / Mathf.Max(CatchUpSeconds, 1e-4f));

            m_ShownAt = Vector3.Lerp(m_ShownAt, m_Body.position, caughtUp);
            m_ShownFacing = Quaternion.Slerp(m_ShownFacing, m_Body.rotation, caughtUp);

            ShowIt();
        }

        /// <summary>
        /// Takes note of where and how the shape has been placed on its body, so that trailing the
        /// body does not throw it away.
        ///
        /// Called when the shape is built. Called again by anything that moves a shape afterwards,
        /// which in practice means a test.
        /// </summary>
        public void RememberHowItWasPlaced()
        {
            m_PlacedAt = transform.localPosition;
            m_PlacedFacing = transform.localRotation;
        }

        /// <summary>Puts the shape exactly on the body, for when it is first placed.</summary>
        public void CatchUpNow()
        {
            if (m_Body == null)
            {
                return;
            }

            m_ShownAt = m_Body.position;
            m_ShownFacing = m_Body.rotation;
            ShowIt();
        }

        /// <summary>
        /// Puts the shape where it is currently being drawn, keeping how it was placed on its body.
        ///
        /// The pose that gets written is the trailed body pose with the shape's own offset and
        /// rotation applied on top, which is exactly what being a child of the body would have given
        /// -- only a tenth of a second late.
        /// </summary>
        void ShowIt()
            => transform.SetPositionAndRotation(
                m_ShownAt + (m_ShownFacing * m_PlacedAt),
                m_ShownFacing * m_PlacedFacing);
    }
}
