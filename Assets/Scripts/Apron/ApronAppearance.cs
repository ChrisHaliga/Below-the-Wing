using UnityEngine;

namespace BelowTheWing.Apron
{
    /// <summary>
    /// What a thing on the apron looks like while everything is still grey primitives.
    ///
    /// Carried as data on the prefab rather than worked out at runtime from whatever components an
    /// object happens to have. Asking "what kind of thing is this?" and switching on the answer is
    /// what produced a cart wearing a tractor's profile, and it needed the same question answered in
    /// four separate places that all had to agree.
    ///
    /// The figures come from the same profile the physics uses, filled in when the prefab is built,
    /// so a cart is drawn at the size of the cart it collides as.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ApronAppearance : MonoBehaviour
    {
        /// <summary>The stand-in shapes anything on an apron can be drawn as.</summary>
        public enum Shape
        {
            /// <summary>Anything on wheels.</summary>
            Box,

            /// <summary>Anything with legs.</summary>
            UprightCapsule,

            /// <summary>Anything longer than it is tall, such as a fuselage.</summary>
            LyingCapsule
        }

        [SerializeField, Tooltip("Which stand-in shape to draw.")]
        Shape m_Shape = Shape.Box;

        [SerializeField, Tooltip("Size in metres. For a capsule: x is the diameter, y the length.")]
        Vector3 m_SizeMetres = Vector3.one;

        [SerializeField, Tooltip("Colour of the stand-in shape.")]
        Color m_Colour = Color.grey;

        [SerializeField, Tooltip("How far above this object's origin its name floats, in metres.")]
        float m_LabelHeightMetres = 1.4f;

        [SerializeField, Tooltip("The name written above it. Set when the object is placed.")]
        string m_DisplayName = "";

        /// <summary>Size in metres, as the profile this was built from describes it.</summary>
        public Vector3 SizeMetres => m_SizeMetres;

        /// <summary>Which stand-in shape this is drawn as.</summary>
        public Shape DrawnAs => m_Shape;

        /// <summary>The name written above it.</summary>
        public string DisplayName => m_DisplayName;

        /// <summary>
        /// Names this object and dresses it. Called once, when the object is placed, on every
        /// machine that receives it.
        /// </summary>
        public void Show(string displayName)
        {
            m_DisplayName = displayName;
            name = displayName;

            if (transform.Find(GreyboxShape.ShapeName) == null)
            {
                Draw();
            }

            if (GetComponentInChildren<WorldLabel>() == null)
            {
                WorldLabel.Attach(transform, displayName, m_LabelHeightMetres);
            }
        }

        void Draw()
        {
            switch (m_Shape)
            {
                case Shape.UprightCapsule:
                    GreyboxShape.AttachCapsule(transform, m_SizeMetres.y, m_SizeMetres.x, m_Colour);
                    break;

                case Shape.LyingCapsule:
                    GreyboxShape.AttachLyingCapsule(transform, m_SizeMetres.y, m_SizeMetres.x, m_Colour);
                    break;

                default:
                    GreyboxShape.AttachBox(transform, m_SizeMetres, m_Colour);
                    break;
            }
        }

        /// <summary>
        /// Fills this in from a profile's real dimensions. Used when a prefab is built, so that what
        /// a thing looks like and what it collides as come from one source and cannot drift apart.
        /// </summary>
        public void DescribeAs(Shape shape, Vector3 sizeMetres, Color colour, float labelHeightMetres)
        {
            m_Shape = shape;
            m_SizeMetres = sizeMetres;
            m_Colour = colour;
            m_LabelHeightMetres = labelHeightMetres;
        }
    }
}
