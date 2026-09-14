using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class VehicleBody
    {
        public const string PartsName = "Solid";

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

            var bodywork = new PhysicsMaterial($"{vehicle.name} bodywork")
            {
                bounciness = bounciness,
                bounceCombine = PhysicsMaterialCombine.Average
            };

            var parts = new GameObject(PartsName);
            parts.transform.SetParent(vehicle.transform, worldPositionStays: false);

            foreach (var part in shape.SolidParts)
            {
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
