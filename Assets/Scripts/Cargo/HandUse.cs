using UnityEngine;

namespace BelowTheWing.Cargo
{
    /// <summary>
    /// What a pair of hands may do with a physics object.
    ///
    /// Said by the object, on the object, so that hands never work it out from what kind of thing
    /// they are looking at. A bag can be carried; a cart can be held onto; the tarmac and other
    /// people can be neither, and say so by having no such component. Adding a carryable crate or a
    /// rail worth grabbing is authoring, not a change to how hands work.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HandUse : MonoBehaviour
    {
        /// <summary>The ways a hand can use a thing.</summary>
        public enum Category
        {
            /// <summary>Picked up into a hand and thrown.</summary>
            Carry,

            /// <summary>Held onto, so that you go where it goes.</summary>
            HoldOnto
        }

        [SerializeField, Tooltip("What a hand does with this.")]
        Category m_As;

        /// <summary>What a hand does with this.</summary>
        public Category As
        {
            get => m_As;
            set => m_As = value;
        }

        /// <summary>
        /// How hands may use whatever this collider belongs to, if at all.
        ///
        /// Looked up through the parents, because the collider a hand reaches is a part -- a cart's
        /// floor, a bag's box -- and what may be done with it is a fact about the whole thing.
        /// </summary>
        public static bool TryFind(Collider collider, out HandUse use)
        {
            use = collider != null ? collider.GetComponentInParent<HandUse>() : null;
            return use != null;
        }
    }
}
