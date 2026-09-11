using System;
using System.Collections.Generic;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Taking back trains that a departing player left behind, and keeping at it until it works.
    ///
    /// Asking once is not enough, and the way it fails is silent. Ownership of five vehicles does
    /// not move in one instant: while one of them is still mid-transfer the request for the set is
    /// refused, and nothing about the apron looks wrong at that moment. The train simply belongs to
    /// a machine that has gone, so nobody simulates it, and it stands there for the rest of the
    /// session -- or worse, keeps whatever speed it had when its owner vanished and rolls away with
    /// no one able to stop it.
    ///
    /// Kept apart from the session for the usual reason: this is a rule about retrying, it needs to
    /// be tested without a network under it, and it was previously a discarded callback that no test
    /// could see.
    /// </summary>
    public sealed class Reclaiming
    {
        /// <summary>A train still to be taken back, and whether an answer is currently awaited.</summary>
        sealed class Outstanding
        {
            public CartChain Train;
            public bool Asked;
            public float SinceAsked;
        }

        readonly List<Outstanding> m_Wanted = new List<Outstanding>();
        readonly float m_RetryAfterSeconds;

        public Reclaiming(float retryAfterSeconds = 1f) => m_RetryAfterSeconds = retryAfterSeconds;

        /// <summary>How many trains are still not back. Zero when everything has been recovered.</summary>
        public int StillMissing => m_Wanted.Count;

        /// <summary>Whether this train is one of the ones still being chased.</summary>
        public bool Chasing(CartChain train) => Find(train) != null;

        /// <summary>
        /// Adds a train to the list of ones to take back. Asking for one already being chased does
        /// nothing, so a second disconnect naming the same train cannot start two chases for it.
        /// </summary>
        public void TakeBack(CartChain train)
        {
            if (train == null || Chasing(train))
            {
                return;
            }

            m_Wanted.Add(new Outstanding { Train = train });
        }

        /// <summary>
        /// Asks for anything outstanding that is not already waiting on an answer, and asks again
        /// for anything whose answer never came.
        ///
        /// A request that is refused, and one that is simply never answered because the machine it
        /// was sent to has left, look the same from here. Both are handled by asking again.
        /// </summary>
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
                        // Let the timer run out and ask again. Removing it here would be the bug
                        // this class exists to fix.
                        chasing.Asked = true;
                    }
                });
            }
        }

        /// <summary>Stops chasing a train, for when it has gone off the apron entirely.</summary>
        public void Forget(CartChain train)
        {
            var found = Find(train);
            if (found != null)
            {
                m_Wanted.Remove(found);
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
