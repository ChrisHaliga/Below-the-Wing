namespace BelowTheWing.Menu
{
    public sealed class ReadyIntent
    {
        bool? m_Told;

        public bool Want { get; private set; }

        public void Toggle() => Want = !Want;

        public bool NeedsTelling(bool rosterIsUp) => rosterIsUp && m_Told != Want;

        public void Told() => m_Told = Want;

        public void Forget()
        {
            Want = false;
            m_Told = null;
        }
    }
}
