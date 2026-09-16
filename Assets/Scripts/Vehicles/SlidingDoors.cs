using BelowTheWing.Cargo;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class SlidingDoors
    {
        public const string PolesName = "Doors";

        const float PoleKg = 6f;
        const float BouncesBackBy = 0.35f;
        const float PoleThickness = 0.06f;

        public static void Build(GameObject vehicle, VehicleShape shape, PhysicsMaterial bodywork)
        {
            var already = vehicle.transform.Find(PolesName);

            if (already != null || shape == null)
            {
                return;
            }

            var poles = new GameObject(PolesName);
            poles.transform.SetParent(vehicle.transform, worldPositionStays: false);

            for (var i = 1; i <= 4; i++)
            {
                var panel = Find(vehicle.transform, $"Door{i}");

                if (panel == null)
                {
                    continue;
                }

                Raise(vehicle, poles.transform, panel, shape, bodywork);
            }
        }

        static SkinnedMeshRenderer Find(Transform under, string called)
        {
            foreach (var candidate in under.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (candidate.name == called)
                {
                    return candidate;
                }
            }

            return null;
        }

        static void Raise(
            GameObject vehicle, Transform poles, SkinnedMeshRenderer panel,
            VehicleShape shape, PhysicsMaterial bodywork)
        {
            var onTheCart = vehicle.transform.InverseTransformPoint(panel.bounds.center);
            var inside = shape.InteriorLocal;

            var outerEdge = Mathf.Sign(onTheCart.z) * inside.extents.z;
            var track = Mathf.Abs(outerEdge);

            var pole = new GameObject($"{panel.name} pole");
            pole.transform.SetParent(poles, worldPositionStays: false);
            pole.transform.localPosition = new Vector3(onTheCart.x, inside.center.y, outerEdge);

            var bar = pole.AddComponent<BoxCollider>();
            bar.size = new Vector3(PoleThickness, inside.size.y * 0.9f, PoleThickness);
            bar.sharedMaterial = bodywork;
            bar.excludeLayers = 1 << vehicle.layer;

            var body = pole.AddComponent<Rigidbody>();
            body.mass = PoleKg;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            pole.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            OnItsTrack(pole, vehicle.GetComponent<Rigidbody>(), track);

            var cover = new GameObject($"{panel.name} cover").transform;
            cover.SetParent(poles, worldPositionStays: false);
            cover.localScale = new Vector3(PoleThickness, inside.size.y, 0.001f);

            var sheet = cover.gameObject.AddComponent<BoxCollider>();
            sheet.size = Vector3.one;
            sheet.sharedMaterial = bodywork;

            pole.AddComponent<SlidingDoorPole>().Runs(
                panel, cover,
                pole.transform.localPosition, new Vector3(0f, 0f, -Mathf.Sign(outerEdge)),
                track, track);
        }

        static void OnItsTrack(GameObject pole, Rigidbody cart, float trackMetres)
        {
            var rail = pole.AddComponent<ConfigurableJoint>();
            rail.connectedBody = cart;
            rail.autoConfigureConnectedAnchor = false;
            rail.anchor = Vector3.zero;
            rail.connectedAnchor = cart.transform.InverseTransformPoint(pole.transform.position)
                                   + new Vector3(0f, 0f, -Mathf.Sign(pole.transform.localPosition.z) * trackMetres * 0.5f);

            rail.axis = Vector3.forward;
            rail.secondaryAxis = Vector3.up;

            rail.xMotion = ConfigurableJointMotion.Locked;
            rail.yMotion = ConfigurableJointMotion.Locked;
            rail.zMotion = ConfigurableJointMotion.Limited;

            rail.angularXMotion = ConfigurableJointMotion.Locked;
            rail.angularYMotion = ConfigurableJointMotion.Locked;
            rail.angularZMotion = ConfigurableJointMotion.Locked;

            rail.linearLimit = new SoftJointLimit
            {
                limit = trackMetres * 0.5f,
                bounciness = BouncesBackBy
            };

            rail.enablePreprocessing = false;
        }
    }
}
