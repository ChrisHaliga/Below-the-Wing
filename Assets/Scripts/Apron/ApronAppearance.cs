using UnityEngine;

namespace BelowTheWing.Apron
{
    [DisallowMultipleComponent]
    public sealed class ApronAppearance : MonoBehaviour
    {
        public enum Shape
        {
            Box,

            UprightCapsule,

            LyingCapsule,

            AlreadyModelled
        }

        public const string LookName = "Look";

        [SerializeField, Tooltip("Stand-in shape to draw")]
        Shape m_Shape = Shape.Box;

        [SerializeField, Tooltip("Size, m. Capsule: x diameter, y length")]
        Vector3 m_SizeMetres = Vector3.one;

        [SerializeField, Tooltip("Colour of the stand-in shape")]
        Color m_Colour = Color.grey;

        [SerializeField, Tooltip("Height above the subject, m")]
        float m_LabelHeightMetres = 1.4f;

        [SerializeField, Tooltip("Where the shape sits on the body, m, local")]
        Vector3 m_DrawnAtLocal;

        [SerializeField, Tooltip("Name shown above it")]
        string m_DisplayName = "";

        public Vector3 SizeMetres => m_SizeMetres;

        public Shape DrawnAs => m_Shape;

        public Vector3 DrawnAtLocal => m_DrawnAtLocal;

        public string DisplayName => m_DisplayName;

        public void Show(string displayName)
        {
            m_DisplayName = displayName;
            name = displayName;

            var look = transform.Find(LookName);
            if (look == null)
            {
                Draw();
                look = transform.Find(LookName);

                if (look != null)
                {
                    look.localPosition += m_DrawnAtLocal;
                }
            }

            if (look != null && look.GetComponent<SmoothedLook>() == null)
            {
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

        public void DescribeAsModelled(float labelHeightMetres)
        {
            m_Shape = Shape.AlreadyModelled;
            m_LabelHeightMetres = labelHeightMetres;
            m_DrawnAtLocal = Vector3.zero;
        }

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
