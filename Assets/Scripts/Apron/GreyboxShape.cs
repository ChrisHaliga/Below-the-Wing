using UnityEngine;

namespace BelowTheWing.Apron
{
    /// <summary>
    /// The plain shapes that stand in for equipment before there is any art.
    ///
    /// A box for anything with wheels, a capsule for anything with legs, and a capsule on its side
    /// for the aircraft. They are sized in metres from the same profiles the physics uses, so what
    /// you see is the size of the thing you are driving into rather than a placeholder that happens
    /// to be a unit cube.
    ///
    /// Purely something to look at. The shape carries no collider of its own -- the collider lives
    /// on the object itself -- so replacing all of this with real models later changes nothing
    /// about how anything behaves.
    /// </summary>
    public static class GreyboxShape
    {
        /// <summary>
        /// How tall Unity's capsule primitive is at a scale of one, in units. Dividing a wanted
        /// height in metres by this gives the scale to ask for.
        /// </summary>
        const float CapsuleHeightUnits = 2f;

        /// <summary>Puts a box of the given size in metres on an object.</summary>
        public static Transform AttachBox(Transform target, Vector3 sizeMetres, Color colour)
        {
            var shape = Build(target, PrimitiveType.Cube, colour);
            shape.localScale = sizeMetres;
            return shape;
        }

        /// <summary>Puts an upright capsule of the given height and diameter in metres on an object.</summary>
        public static Transform AttachCapsule(Transform target, float heightMetres, float diameterMetres, Color colour)
        {
            var shape = Build(target, PrimitiveType.Capsule, colour);
            shape.localScale = new Vector3(diameterMetres, heightMetres / CapsuleHeightUnits, diameterMetres);
            return shape;
        }

        /// <summary>
        /// Puts a capsule lying along the object's forward axis on it, for something longer than it
        /// is tall.
        /// </summary>
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

            // The object it hangs on already has whatever collider it is supposed to have, and a
            // second one inside the first would fight it.
            var ownCollider = primitive.GetComponent<Collider>();
            if (ownCollider != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(ownCollider);
                }
                else
                {
                    Object.DestroyImmediate(ownCollider);
                }
            }

            primitive.GetComponent<MeshRenderer>().material.color = colour;
            primitive.transform.SetParent(target, worldPositionStays: false);

            return primitive.transform;
        }
    }
}
