namespace BelowTheWing.Menu
{
    public sealed class LobbySlots
    {
        public const int Capacity = 5;

        readonly ulong?[] m_Sitting = new ulong?[Capacity];
        readonly bool[] m_Ready = new bool[Capacity];
        readonly ulong m_Host;

        public LobbySlots(ulong host) => m_Host = host;

        public int Filled
        {
            get
            {
                var taken = 0;

                foreach (var seat in m_Sitting)
                {
                    if (seat.HasValue)
                    {
                        taken++;
                    }
                }

                return taken;
            }
        }

        public bool EveryoneReady
        {
            get
            {
                if (Filled == 0)
                {
                    return false;
                }

                for (var slot = 0; slot < Capacity; slot++)
                {
                    if (m_Sitting[slot].HasValue && !m_Ready[slot])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public ulong? Who(int slot) => slot >= 0 && slot < Capacity ? m_Sitting[slot] : null;

        public bool Arrived(ulong player)
        {
            if (SlotOf(player) >= 0)
            {
                return true;
            }

            var empty = FirstEmpty();

            if (empty < 0)
            {
                return false;
            }

            m_Sitting[empty] = player;
            m_Ready[empty] = false;

            return true;
        }

        public void Left(ulong player)
        {
            var slot = SlotOf(player);

            if (slot < 0)
            {
                return;
            }

            m_Sitting[slot] = null;
            m_Ready[slot] = false;
        }

        public void Ready(ulong player, bool ready)
        {
            var slot = SlotOf(player);

            if (slot < 0)
            {
                return;
            }

            m_Ready[slot] = ready;
        }

        public bool IsReady(ulong player)
        {
            var slot = SlotOf(player);

            return slot >= 0 && m_Ready[slot];
        }

        public bool CanStart(ulong asking) => asking == m_Host && EveryoneReady;

        int SlotOf(ulong player)
        {
            for (var slot = 0; slot < Capacity; slot++)
            {
                if (m_Sitting[slot] == player)
                {
                    return slot;
                }
            }

            return -1;
        }

        int FirstEmpty()
        {
            for (var slot = 0; slot < Capacity; slot++)
            {
                if (!m_Sitting[slot].HasValue)
                {
                    return slot;
                }
            }

            return -1;
        }
    }
}
