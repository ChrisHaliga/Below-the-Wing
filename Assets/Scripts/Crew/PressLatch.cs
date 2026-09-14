namespace BelowTheWing.Crew
{
    public struct PressLatch
    {
        double m_AskedAt;

        public bool Asked { get; private set; }

        public void Ask(double physicsTime)
        {
            Asked = true;
            m_AskedAt = physicsTime;
        }

        public void ForgetOnceAStepHasSeenIt(double physicsTime)
        {
            if (Asked && physicsTime > m_AskedAt)
            {
                Asked = false;
            }
        }
    }
}
