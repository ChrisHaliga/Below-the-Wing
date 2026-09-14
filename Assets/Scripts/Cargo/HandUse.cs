using UnityEngine;

namespace BelowTheWing.Cargo
{
    [DisallowMultipleComponent]
    public sealed class HandUse : MonoBehaviour
    {
        public enum Category
        {
            Carry,

            HoldOnto
        }

        [SerializeField, Tooltip("What hands may do with this")]
        Category m_As;

        public Category As
        {
            get => m_As;
            set => m_As = value;
        }

        public static bool TryFind(Collider collider, out HandUse use)
        {
            use = collider != null ? collider.GetComponentInParent<HandUse>() : null;
            return use != null;
        }
    }
}
