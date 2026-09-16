using BelowTheWing.Cargo;
using UnityEngine;

namespace BelowTheWing.Crew
{
    [DisallowMultipleComponent]
    public sealed class HandLook : MonoBehaviour
    {
        public const string LeftLookName = "Left Hand Look";

        public const string RightLookName = "Right Hand Look";

        public const float RadiusMetres = 0.06f;

        static readonly Color Left = new Color(0.85f, 0.15f, 0.15f);
        static readonly Color Right = new Color(0.15f, 0.8f, 0.25f);

        Hands m_Hands;
        Transform m_LeftAnchor;
        Transform m_RightAnchor;
        Transform m_LeftLook;
        Transform m_RightLook;

        public void Watch(Hands hands, Transform leftAnchor, Transform rightAnchor)
        {
            m_Hands = hands;
            m_LeftAnchor = leftAnchor;
            m_RightAnchor = rightAnchor;

            m_LeftLook = ASphere(LeftLookName, Left);
            m_RightLook = ASphere(RightLookName, Right);
        }

        Transform ASphere(string called, Color colour)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = called;
            sphere.transform.localScale = Vector3.one * (RadiusMetres * 2f);

            Destroy(sphere.GetComponent<Collider>());

            var paint = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            paint.color = colour;
            sphere.GetComponent<MeshRenderer>().sharedMaterial = paint;

            return sphere.transform;
        }

        void LateUpdate()
        {
            Place(m_LeftLook, m_LeftAnchor, m_Hands?.Left);
            Place(m_RightLook, m_RightAnchor, m_Hands?.Right);
        }

        static void Place(Transform look, Transform anchor, Hand hand)
        {
            if (look == null)
            {
                return;
            }

            var holding = hand?.HoldingAt;

            if (holding.HasValue)
            {
                look.position = holding.Value;
                return;
            }

            if (anchor != null)
            {
                look.position = anchor.position;
            }
        }

        void OnDestroy()
        {
            Tidy(m_LeftLook);
            Tidy(m_RightLook);
        }

        static void Tidy(Transform look)
        {
            if (look == null)
            {
                return;
            }

            var renderer = look.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Destroy(renderer.sharedMaterial);
            }

            Destroy(look.gameObject);
        }
    }
}
