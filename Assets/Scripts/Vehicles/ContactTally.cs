using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// How many things are touching this body right now.
    ///
    /// Counted here rather than asked for, because there is no way to ask: physics reports contacts
    /// as they begin and end and keeps no running total anybody can read. The number matters because
    /// contacts are what the solver spends its time on -- a step time that has climbed with no more
    /// bodies awake than before is a pile-up somewhere, and without this there is nothing to look at
    /// that would say so.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ContactTally : MonoBehaviour
    {
        /// <summary>How many other bodies are in contact with this one.</summary>
        public int Touching { get; private set; }

        void OnCollisionEnter() => Touching++;

        void OnCollisionExit() => Touching = Mathf.Max(0, Touching - 1);

        void OnDisable() => Touching = 0;
    }
}
