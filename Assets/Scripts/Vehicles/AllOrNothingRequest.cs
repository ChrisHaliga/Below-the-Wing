using System;
using System.Collections.Generic;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// A request for several things at once that succeeds only if every one of them is granted.
    ///
    /// Answers to such a request arrive one at a time and in no particular order, so the outcome is
    /// not known until the last one lands. Anything granted before a refusal turns up has to be
    /// handed back, or the caller is left holding part of what it asked for -- which for a cart
    /// train means a tractor and two carts on one machine and two carts on another, with couplings
    /// spanning the gap.
    ///
    /// The policy lives here, apart from whatever actually does the asking, because getting it
    /// wrong is invisible until a second player turns up and by then it is very hard to see.
    /// </summary>
    /// <typeparam name="T">Whatever is being asked for.</typeparam>
    public sealed class AllOrNothingRequest<T>
    {
        readonly HashSet<T> m_Awaited;
        readonly List<T> m_Granted = new List<T>();
        readonly Action<T> m_GiveBack;
        readonly Action<bool> m_OnResult;

        bool m_AnyRefused;
        bool m_Settled;

        /// <summary>
        /// Starts tracking a request.
        /// </summary>
        /// <param name="wanted">
        /// Everything being asked for. Tracked by identity rather than counted, so that a repeated
        /// answer cannot settle the request while something else is still outstanding.
        /// </param>
        /// <param name="giveBack">
        /// Hands one granted item back. Called for everything that was granted, if anything at all
        /// was refused.
        /// </param>
        /// <param name="onResult">Told true only when every answer was a grant.</param>
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

        /// <summary>Whether every answer is in and the caller has been told the outcome.</summary>
        public bool Settled => m_Settled;

        /// <summary>
        /// Records one answer. Once the last one arrives, anything granted is handed back if any
        /// answer was a refusal, and only then is the caller told.
        ///
        /// Anything that was not asked for, and anything answering twice, is ignored: a late,
        /// repeated or stray response cannot report a second outcome, give back something twice, or
        /// stand in for an answer that has not arrived.
        /// </summary>
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
