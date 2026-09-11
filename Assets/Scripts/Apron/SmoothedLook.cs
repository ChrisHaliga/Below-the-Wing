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
        VehicleController m_Vehicle;

        /// <summary>
        /// Where the shape is being drawn, kept here rather than read back off its own transform.
        ///
        /// It stays a child of the body, so the body moving carries it along before anything gets a
        /// chance to look. Read back, the shape would always report itself as exactly where the body
        /// is and there would be nothing to lag behind.
        /// </summary>
        Vector3 m_ShownAt;
        Quaternion m_ShownFacing = Quaternion.identity;

        /// <summary>How far behind the body the shape currently is, in metres.</summary>
        public float TrailingByMetres => m_Body == null ? 0f : Vector3.Distance(m_ShownAt, m_Body.position);

        /// <summary>
        /// Whether the shape should trail at all.
        ///
        /// Only for things this machine does not decide the position of. A vehicle you are driving
        /// never gets snapped -- there is nothing to hide -- and putting a tenth of a second between
        /// the wheel and what you see would be felt immediately as the controls going soft.
        /// </summary>
        public bool WorthSmoothing => m_Vehicle == null || !m_Vehicle.OursToMove;

        void Awake()
        {
            // Stays a child. Its world pose is written every frame, which overrides whatever it
            // inherited, so there is nothing to detach and nothing left behind when the body goes.
            m_Body = transform.parent;
            m_Vehicle = m_Body != null ? m_Body.GetComponent<VehicleController>() : null;
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

            transform.SetPositionAndRotation(m_ShownAt, m_ShownFacing);
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
            transform.SetPositionAndRotation(m_ShownAt, m_ShownFacing);
        }
    }
}
