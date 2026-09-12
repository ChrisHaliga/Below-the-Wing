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
        /// <summary>What this is drawn as.</summary>
        public enum Shape
        {
            /// <summary>Anything on wheels.</summary>
            Box,

            /// <summary>Anything with legs.</summary>
            UprightCapsule,

            /// <summary>Anything longer than it is tall, such as a fuselage.</summary>
            LyingCapsule,

            /// <summary>
            /// Nothing: there is already a model on this object to look at.
            ///
            /// Everything else this component does still applies to a modelled thing. It is named,
            /// it carries a floating label, and what you see of it trails its body so that a
            /// correction reads as a fast slide rather than a teleport.
            /// </summary>
            AlreadyModelled
        }

        /// <summary>
        /// What the thing you look at is called, whether it is a grey box or a real model.
        ///
        /// One name for both, so that everything hung off appearance -- the smoothing, the tests --
        /// finds it without asking which kind of thing this is.
        /// </summary>
        public const string LookName = "Look";

        [SerializeField, Tooltip("Which stand-in shape to draw.")]
        Shape m_Shape = Shape.Box;

        [SerializeField, Tooltip("Size in metres. For a capsule: x is the diameter, y the length.")]
        Vector3 m_SizeMetres = Vector3.one;

        [SerializeField, Tooltip("Colour of the stand-in shape.")]
        Color m_Colour = Color.grey;

        [SerializeField, Tooltip("How far above this object's origin its name floats, in metres.")]
        float m_LabelHeightMetres = 1.4f;

        [SerializeField, Tooltip("Where on this object the thing you look at sits. A vehicle's " +
                                 "origin is on the ground between its wheels, so its bodywork is " +
                                 "entirely above that origin rather than centred on it.")]
        Vector3 m_DrawnAtLocal;

        [SerializeField, Tooltip("The name written above it. Set when the object is placed.")]
        string m_DisplayName = "";

        /// <summary>Size in metres, as the profile this was built from describes it.</summary>
        public Vector3 SizeMetres => m_SizeMetres;

        /// <summary>Which stand-in shape this is drawn as.</summary>
        public Shape DrawnAs => m_Shape;

        /// <summary>Where on this object the thing you look at sits.</summary>
        public Vector3 DrawnAtLocal => m_DrawnAtLocal;

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

            var look = transform.Find(LookName);
            if (look == null)
            {
                Draw();
                look = transform.Find(LookName);
            }

            // What you see trails the body slightly, so that a vehicle moved outright rather than
            // eased into place reads as a fast slide instead of ceasing to exist in one spot and
            // starting in another. Colliders stay on the body, so nothing is ever hit where it is
            // not drawn.
            if (look != null && look.GetComponent<SmoothedLook>() == null)
            {
                look.localPosition += m_DrawnAtLocal;
                look.gameObject.AddComponent<SmoothedLook>();
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

                case Shape.AlreadyModelled:
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
        public void DescribeAs(
            Shape shape,
            Vector3 sizeMetres,
            Color colour,
            float labelHeightMetres,
            Vector3 drawnAtLocal = default)
        {
            m_Shape = shape;
            m_SizeMetres = sizeMetres;
            m_Colour = colour;
            m_LabelHeightMetres = labelHeightMetres;
            m_DrawnAtLocal = drawnAtLocal;
        }
    }
}
