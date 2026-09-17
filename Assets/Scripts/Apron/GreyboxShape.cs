using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Apron
{
    public static class GreyboxShape
    {
        const float CapsuleHeightUnits = 2f;

        public static Transform AttachBox(Transform target, Vector3 sizeMetres, Color colour)
        {
            var shape = Build(target, PrimitiveType.Cube, colour);
            shape.localScale = sizeMetres;
            return shape;
        }

        public static Transform AttachCapsule(Transform target, float heightMetres, float diameterMetres, Color colour)
        {
            var shape = Build(target, PrimitiveType.Capsule, colour);
            shape.localScale = new Vector3(diameterMetres, heightMetres / CapsuleHeightUnits, diameterMetres);
            return shape;
        }

        public static Transform AttachLyingCapsule(Transform target, float lengthMetres, float diameterMetres, Color colour)
        {
            var shape = AttachCapsule(target, lengthMetres, diameterMetres, colour);
            shape.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return shape;
        }

        static Transform Build(Transform target, PrimitiveType type, Color colour)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = ApronAppearance.LookName;

            Discard.Now(primitive.GetComponent<Collider>());

            primitive.GetComponent<MeshRenderer>().material.color = colour;
            primitive.transform.SetParent(target, worldPositionStays: false);

            return primitive.transform;
        }
    }
}
