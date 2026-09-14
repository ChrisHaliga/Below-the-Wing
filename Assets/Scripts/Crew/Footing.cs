using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// Whether somebody still has their feet under them.
    ///
    /// A person on a moving deck is held there by friction and nothing else, and friction has a
    /// limit. Below it they stand as though the deck were the ground; past it their feet skid, the
    /// deck goes on without them, and they keep the speed they had. That limit is the whole of what
    /// makes riding a cart a thing a player can lose.
    ///
    /// It matters because walking and standing are not limited by the same thing. How hard a deck
    /// can drag somebody is friction, a real figure of about ten metres per second squared. How
    /// quickly a player's own legs change their speed is a decision about how the controls feel,
    /// and it is set far higher -- a player is somebody holding a key, and a walk that winds up
    /// over half a second is felt at once as the controls going soft. Kept as one number, the
    /// higher of the two wins and nothing can ever shake a rider off.
    ///
    /// So they are two numbers, and this is what keeps them from contradicting each other: legs
    /// only work while the feet are still gripping. Once the surface has out-pulled them the
    /// gait does nothing, which is true of a real person skidding -- there is nothing to push
    /// against until the sliding stops.
    /// </summary>
    public struct Footing
    {
        /// <summary>
        /// How slowly somebody has to be sliding across a surface to get their feet back, in metres
        /// per second.
        ///
        /// Not zero: friction closes the gap asymptotically, so a figure of zero is a player who
        /// never stands up again. A walking pace of slip is still sliding; a fifth of that is
        /// somebody who has caught up.
        /// </summary>
        public const float BackOnTheirFeetBelow = 0.5f;

        Vector3 m_SurfaceWasMovingAt;
        Object m_Surface;
        bool m_Started;

        /// <summary>Whether their feet have been taken out from under them.</summary>
        public bool Lost { get; private set; }

        /// <summary>
        /// Works out whether they still have their feet, from what they are standing on and how it
        /// is moving. Called every step they are on something.
        ///
        /// Judged on how hard the surface itself changed velocity, rather than on how far they are
        /// from where they would like to be. The difference matters: a player who has just asked to
        /// walk is also a long way from the speed they want, and their own legs asking for something
        /// must never read as the ground being pulled out from under them.
        /// </summary>
        public void Settle(
            Object surface,
            Vector3 surfaceVelocity,
            Vector3 ownVelocity,
            float gripMetresPerSecondSquared,
            float deltaTime)
        {
            // Stepping from a cart onto the apron swaps one velocity for another in a single step.
            // That is a different surface, not this one yanking, and reading it as a yank would take
            // a player's feet away every time they walked off a moving deck.
            if (!m_Started || surface != m_Surface)
            {
                m_Started = true;
                m_Surface = surface;
                m_SurfaceWasMovingAt = surfaceVelocity;
            }

            var pulledAt = (surfaceVelocity - m_SurfaceWasMovingAt).magnitude / Mathf.Max(deltaTime, 1e-5f);
            m_SurfaceWasMovingAt = surfaceVelocity;

            if (pulledAt > gripMetresPerSecondSquared)
            {
                Lost = true;
                return;
            }

            if (Lost && (ownVelocity - surfaceVelocity).magnitude <= BackOnTheirFeetBelow)
            {
                Lost = false;
            }
        }

        /// <summary>
        /// Gives them their feet back, for somebody who has stopped standing on anything at all --
        /// in the air, or put somewhere by hand. What they land on decides it again from scratch.
        /// </summary>
        public void Reset()
        {
            Lost = false;
            m_Started = false;
        }
    }
}
