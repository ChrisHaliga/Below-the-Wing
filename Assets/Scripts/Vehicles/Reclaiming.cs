using System;
using System.Collections.Generic;

namespace BelowTheWing.Vehicles
{
    public sealed class Reclaiming
    {
        sealed class Outstanding
        {
            public CartChain Train;
            public bool Asked;
            public float SinceAsked;
        }

        readonly List<Outstanding> m_Wanted = new List<Outstanding>();
        readonly float m_RetryAfterSeconds;

        public Reclaiming(float retryAfterSeconds = 1f) => m_RetryAfterSeconds = retryAfterSeconds;

        public int StillMissing => m_Wanted.Count;

        public bool Chasing(CartChain train) => Find(train) != null;

        public void TakeBack(CartChain train)
        {
            if (train == null || Chasing(train))
            {
                return;
            }

            m_Wanted.Add(new Outstanding { Train = train });
        }

        public void Chase(IOwnershipBroker broker, float deltaTime)
        {
            for (var i = m_Wanted.Count - 1; i >= 0; i--)
            {
                var wanted = m_Wanted[i];

                if (Ours(wanted.Train, broker))
                {
                    m_Wanted.RemoveAt(i);
                    continue;
                }

                if (wanted.Asked)
                {
                    wanted.SinceAsked += deltaTime;
                    if (wanted.SinceAsked < m_RetryAfterSeconds)
                    {
                        continue;
                    }
                }

                wanted.Asked = true;
                wanted.SinceAsked = 0f;

                var chasing = wanted;
                chasing.Train.RequestOwnership(broker, granted =>
                {
                    if (!granted)
                    {
                        chasing.Asked = true;
                    }
                });
            }
        }

        static bool Ours(CartChain train, IOwnershipBroker broker)
        {
            foreach (var member in train.Members)
            {
                if (!broker.OwnedByUs(member))
                {
                    return false;
                }
            }

            return true;
        }

        Outstanding Find(CartChain train)
        {
            foreach (var wanted in m_Wanted)
            {
                if (ReferenceEquals(wanted.Train, train))
                {
                    return wanted;
                }
            }

            return null;
        }
    }
}
