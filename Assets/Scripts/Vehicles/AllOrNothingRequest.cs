using System;
using System.Collections.Generic;

namespace BelowTheWing.Vehicles
{
    public sealed class AllOrNothingRequest<T>
    {
        readonly HashSet<T> m_Awaited;
        readonly List<T> m_Granted = new List<T>();
        readonly Action<T> m_GiveBack;
        readonly Action<bool> m_OnResult;

        bool m_AnyRefused;
        bool m_Settled;

        public AllOrNothingRequest(IReadOnlyList<T> wanted, Action<T> giveBack, Action<bool> onResult)
        {
            if (wanted == null || wanted.Count < 1)
            {
                throw new ArgumentException("A request has to be for something.", nameof(wanted));
            }

            m_Awaited = new HashSet<T>(wanted);
            m_GiveBack = giveBack ?? throw new ArgumentNullException(nameof(giveBack));
            m_OnResult = onResult;
        }

        public bool Settled => m_Settled;

        public void Answer(T item, bool granted)
        {
            if (m_Settled || !m_Awaited.Remove(item))
            {
                return;
            }

            if (granted)
            {
                m_Granted.Add(item);
            }
            else
            {
                m_AnyRefused = true;
            }

            if (m_Awaited.Count > 0)
            {
                return;
            }

            m_Settled = true;

            if (m_AnyRefused)
            {
                foreach (var alreadyGranted in m_Granted)
                {
                    m_GiveBack(alreadyGranted);
                }
            }

            m_OnResult?.Invoke(!m_AnyRefused);
        }
    }
}
