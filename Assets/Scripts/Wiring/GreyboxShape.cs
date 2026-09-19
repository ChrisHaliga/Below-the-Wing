using UnityEngine;

namespace BelowTheWing.Wiring
{
    public static class GreyboxShape
    {
        public const string LookName = "Look";

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

        public static Transform AttachSphere(Transform target, float diameterMetres, Color colour)
        {
            var shape = Build(target, PrimitiveType.Sphere, colour);
            shape.localScale = Vector3.one * diameterMetres;
            return shape;
        }

        public static void DiscardThePaintOn(Transform shape)
        {
            var renderer = shape != null ? shape.GetComponent<MeshRenderer>() : null;

            if (renderer != null && renderer.sharedMaterial != null)
            {
                Discard.Now(renderer.sharedMaterial);
            }
        }

        static Transform Build(Transform target, PrimitiveType type, Color colour)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = LookName;

            Discard.Now(primitive.GetComponent<Collider>());

            var renderer = primitive.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(renderer.sharedMaterial) { color = colour, name = $"{type} {colour}" };
            primitive.transform.SetParent(target, worldPositionStays: false);

            return primitive.transform;
        }
    }
}
