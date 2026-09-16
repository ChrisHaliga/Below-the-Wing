using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class VehicleBody
    {
        public const string PartsName = "Solid";

        public static PhysicsMaterial Build(GameObject vehicle, VehicleShape shape, float bounciness)
        {
            DiscardAnyAlreadyThere(vehicle);

            if (shape == null || shape.SolidParts.Count == 0)
            {
                return null;
            }

            var bodywork = new PhysicsMaterial($"{vehicle.name} bodywork")
            {
                bounciness = bounciness,
                bounceCombine = PhysicsMaterialCombine.Average
            };

            var parts = new GameObject(PartsName);
            parts.transform.SetParent(vehicle.transform, worldPositionStays: false);

            foreach (var part in shape.SolidParts)
            {
                AddPiece(parts.transform, part, bodywork);
            }

            return bodywork;
        }

        static void DiscardAnyAlreadyThere(GameObject vehicle)
        {
            var existing = vehicle.transform.Find(PartsName);

            if (existing == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(existing.gameObject);
            }
            else
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        static void AddPiece(Transform parts, VehicleShape.SolidPart part, PhysicsMaterial bodywork)
        {
            var piece = new GameObject(part.Name);
            piece.transform.SetParent(parts, worldPositionStays: false);

            var solid = part.IsAPieceOfTheModel ? AsModelled(piece, part) : AsABox(piece, part);

            solid.sharedMaterial = bodywork;
            piece.transform.localPosition = part.CentreLocal;
        }

        static Collider AsModelled(GameObject piece, VehicleShape.SolidPart part)
        {
            var shaped = piece.AddComponent<MeshCollider>();
            shaped.sharedMesh = part.Piece;
            shaped.convex = true;

            piece.transform.localRotation = part.PieceTurn;
            piece.transform.localScale = part.PieceScale;

            return shaped;
        }

        static Collider AsABox(GameObject piece, VehicleShape.SolidPart part)
        {
            var box = piece.AddComponent<BoxCollider>();
            box.size = part.SizeMetres;
            box.center = Vector3.zero;

            return box;
        }
    }
}
