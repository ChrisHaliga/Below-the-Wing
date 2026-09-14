namespace BelowTheWing.Crew
{
    /// <summary>
    /// A key press that survives long enough for the physics to see it.
    ///
    /// Keys are read once per frame; what a character does about them happens once per physics
    /// step. The two do not line up. Physics runs at a fixed rate -- fifty steps a second here --
    /// while frames come as fast as the machine can draw them, so on a quick machine most frames
    /// contain no physics step at all. A press noticed during one of those frames and stored as
    /// "pressed this frame" is overwritten by the next frame's reading before anything could act on
    /// it, and the player is left pressing a key that works about one time in four.
    ///
    /// So the ask is held until a step has actually run, and dropped once one has. Not until it has
    /// been <em>acted on</em>: a jump asked for in mid-air is a jump that was heard and refused, and
    /// holding it until the feet touch down would be a jump the player did not ask for.
    /// </summary>
    public struct PressLatch
    {
        double m_AskedAt;

        /// <summary>Whether a press is waiting to be seen.</summary>
        public bool Asked { get; private set; }

        /// <summary>Notes a press, made at this reading of the physics clock.</summary>
        public void Ask(double physicsTime)
        {
            Asked = true;
            m_AskedAt = physicsTime;
        }

        /// <summary>
        /// Drops the press once the physics clock has moved past it, which is to say once a step has
        /// run and had the chance to act on it. Called every frame.
        /// </summary>
        public void ForgetOnceAStepHasSeenIt(double physicsTime)
        {
            if (Asked && physicsTime > m_AskedAt)
            {
                Asked = false;
            }
        }
    }
}
