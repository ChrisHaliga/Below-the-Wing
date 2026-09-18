using BelowTheWing.Apron;
using BelowTheWing.Cargo;
using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Crew
{
    [DisallowMultipleComponent]
    public sealed class HandLook : MonoBehaviour
    {
        public const string LeftLookName = "Left Hand Look";

        public const string RightLookName = "Right Hand Look";

        public const float RadiusMetres = 0.06f;

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

            m_LeftLook = ASphere(LeftLookName, Palette.LeftHand);
            m_RightLook = ASphere(RightLookName, Palette.RightHand);
        }

        Transform ASphere(string called, Color colour)
        {
            var sphere = GreyboxShape.AttachSphere(transform, RadiusMetres * 2f, colour);
            sphere.name = called;
            return sphere;
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
