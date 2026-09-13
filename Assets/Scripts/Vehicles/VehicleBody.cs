using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Building the solid parts of a vehicle from its shape.
    ///
    /// One box the size of the whole vehicle is what a greybox is. It is also what makes a
    /// container impossible: the space bags are supposed to occupy is filled with collider, so
    /// nothing can ever be inside a cart.
    ///
    /// Here rather than in the editor script that builds prefabs, because what a vehicle is solid
    /// where is something tests have to be able to reach. Put behind an editor-only wall, the one
    /// description of a cart's floor and walls would exist somewhere no test can see it, and every
    /// test about loading one would be written against colliders the shipped cart does not have.
    /// </summary>
    public static class VehicleBody
    {
        /// <summary>What the collider objects this builds are called, so they can be found again.</summary>
        public const string PartsName = "Solid";

        /// <summary>
        /// Gives a vehicle the colliders its shape describes, replacing any it already had, and
        /// returns the material they are made of, which the caller owns and destroys.
        ///
        /// Built when the vehicle is configured rather than baked into the prefab, so that there is
        /// exactly one description of what a cart is solid where. Baked in, the prefab's colliders
        /// and the shape they came from are two records of the same measurement, and this project
        /// has already spent a day on what happens when two such records disagree.
        /// </summary>
        public static PhysicsMaterial Build(GameObject vehicle, VehicleShape shape, float bounciness)
        {
            var existing = vehicle.transform.Find(PartsName);
            if (existing != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(existing.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(existing.gameObject);
                }
            }

            if (shape == null || shape.SolidParts.Count == 0)
            {
                return null;
            }

            // Averaged with whatever it hits. Vehicle on vehicle gives the full figure; a bag or a
            // person, which carry no bounce and say so with a mode that wins over averaging, gets
            // none -- a soft bag must not spring off a cart's lip and out the far side.
            var bodywork = new PhysicsMaterial($"{vehicle.name} bodywork")
            {
                bounciness = bounciness,
                bounceCombine = PhysicsMaterialCombine.Average
            };

            var parts = new GameObject(PartsName);
            parts.transform.SetParent(vehicle.transform, worldPositionStays: false);

            foreach (var part in shape.SolidParts)
            {
                // One object per box rather than several colliders on one, so that each is named in
                // the hierarchy. A cart with six unlabelled colliders on its root is unreadable the
                // first time somebody has to work out why a bag will not go in.
                var piece = new GameObject(part.Name);
                piece.transform.SetParent(parts.transform, worldPositionStays: false);

                var box = piece.AddComponent<BoxCollider>();
                box.size = part.SizeMetres;
                box.center = Vector3.zero;
                box.sharedMaterial = bodywork;
                piece.transform.localPosition = part.CentreLocal;
            }

            return bodywork;
        }
    }
}
