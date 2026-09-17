using UnityEngine;

namespace BelowTheWing.Cargo
{
    [DisallowMultipleComponent]
    public sealed class HandUse : MonoBehaviour
    {
        public enum Category
        {
            Carry,

            HoldOnto,

            Nothing
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

            if (use != null && use.As == Category.Nothing)
            {
                use = null;
            }

            return use != null;
        }
    }
}
