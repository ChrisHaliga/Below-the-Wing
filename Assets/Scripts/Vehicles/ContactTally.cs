using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [DisallowMultipleComponent]
    public sealed class ContactTally : MonoBehaviour
    {
        public int Touching { get; private set; }

        void OnCollisionEnter() => Touching++;

        void OnCollisionExit() => Touching = Mathf.Max(0, Touching - 1);

        void OnDisable() => Touching = 0;
    }
}
