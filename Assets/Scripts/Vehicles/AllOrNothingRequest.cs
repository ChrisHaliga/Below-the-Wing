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
        readonly List<T> m_Granted = new List<T>();
        readonly Action<T> m_GiveBack;
        readonly Action<bool> m_OnResult;

        int m_Outstanding;
        bool m_AnyRefused;
        bool m_Settled;

        /// <summary>
        /// Starts tracking a request.
        /// </summary>
        /// <param name="wanted">How many answers to expect. Must be at least one.</param>
        /// <param name="giveBack">
        /// Hands one granted item back. Called for everything that was granted, if anything at all
        /// was refused.
        /// </param>
        /// <param name="onResult">Told true only when every answer was a grant.</param>
        public AllOrNothingRequest(int wanted, Action<T> giveBack, Action<bool> onResult)
        {
            if (wanted < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(wanted), "A request has to be for something.");
            }

            m_Outstanding = wanted;
            m_GiveBack = giveBack ?? throw new ArgumentNullException(nameof(giveBack));
            m_OnResult = onResult;
        }

        /// <summary>Whether every answer is in and the caller has been told the outcome.</summary>
        public bool Settled => m_Settled;

        /// <summary>
        /// Records one answer. Once the last one arrives, anything granted is handed back if any
        /// answer was a refusal, and only then is the caller told.
        ///
        /// Answers arriving after the request has settled are ignored, so a late or duplicated
        /// response cannot report a second outcome or give back something twice.
        /// </summary>
        public void Answer(T item, bool granted)
        {
            if (m_Settled)
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

            m_Outstanding--;
            if (m_Outstanding > 0)
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
