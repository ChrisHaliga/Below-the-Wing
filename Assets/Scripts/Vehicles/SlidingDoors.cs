using BelowTheWing.Cargo;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class SlidingDoors
    {
        public const string PolesName = "Doors";

        const float PoleKg = 6f;
        const float RailDragNewtonsPerMetrePerSecond = 40f;
        const float RailHoldsAtNewtons = 1500f;

        public static void Build(GameObject vehicle, VehicleShape shape, PhysicsMaterial bodywork)
        {
            if (vehicle.transform.Find(PolesName) != null || shape == null)
            {
                return;
            }

            Transform poles = null;

            for (var i = 1; i <= 4; i++)
            {
                var panel = Find(vehicle.transform, $"Door{i}");

                if (panel == null || panel.sharedMesh == null
                    || panel.sharedMesh.blendShapeCount == 0)
                {
                    continue;
                }

                poles ??= Container(vehicle);
                Raise(vehicle, poles, panel, Find(vehicle.transform, $"Door_Fabric{i}"), bodywork);
            }
        }

        static Transform Container(GameObject vehicle)
        {
            var poles = new GameObject(PolesName).transform;
            poles.SetParent(vehicle.transform, worldPositionStays: false);

            return poles;
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
            SkinnedMeshRenderer fabric, PhysicsMaterial bodywork)
        {
            var shut = InCartSpace(panel, vehicle.transform, 0f);
            var open = InCartSpace(panel, vehicle.transform, 100f);

            var towardsTheEnd = Mathf.Sign(shut.center.z);
            var radius = shut.size.x * 0.5f;

            var shutAt = NearestTheMiddle(shut, towardsTheEnd) + (towardsTheEnd * radius);
            var openAt = NearestTheMiddle(open, towardsTheEnd) + (towardsTheEnd * radius);
            var fixedPoleAt = FurthestFromTheMiddle(shut, towardsTheEnd) - (towardsTheEnd * radius);

            var pole = new GameObject($"{panel.name} pole");
            pole.transform.SetParent(poles, worldPositionStays: false);
            pole.transform.localPosition = new Vector3(shut.center.x, shut.center.y, shutAt);

            var grab = pole.AddComponent<CapsuleCollider>();
            grab.direction = 1;
            grab.radius = radius;
            grab.height = shut.size.y;
            grab.sharedMaterial = bodywork;

            var body = pole.AddComponent<Rigidbody>();
            body.mass = PoleKg;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            pole.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            OnItsTrack(pole, vehicle.GetComponent<Rigidbody>(), shutAt, openAt);

            var cover = new GameObject($"{panel.name} cover").transform;
            cover.SetParent(poles, worldPositionStays: false);
            cover.localScale = new Vector3(radius * 2f, shut.size.y, 0.001f);

            var sheet = cover.gameObject.AddComponent<BoxCollider>();
            sheet.size = Vector3.one;
            sheet.sharedMaterial = bodywork;

            cover.gameObject.AddComponent<HandUse>().As = HandUse.Category.Nothing;

            pole.AddComponent<SlidingDoorPole>().Runs(
                panel, fabric, cover,
                pole.transform.localPosition, new Vector3(0f, 0f, towardsTheEnd),
                Mathf.Abs(openAt - shutAt), fixedPoleAt);

            LeaveTheCartAlone(grab, vehicle);
        }

        static float NearestTheMiddle(Bounds box, float towardsTheEnd)
            => towardsTheEnd > 0f ? box.min.z : box.max.z;

        static float FurthestFromTheMiddle(Bounds box, float towardsTheEnd)
            => towardsTheEnd > 0f ? box.max.z : box.min.z;

        static Bounds InCartSpace(SkinnedMeshRenderer panel, Transform cart, float weight)
        {
            var was = panel.GetBlendShapeWeight(0);
            panel.SetBlendShapeWeight(0, weight);

            var baked = new Mesh();
            panel.BakeMesh(baked, useScale: true);

            var vertices = baked.vertices;
            var box = new Bounds(At(cart, panel.transform, vertices[0]), Vector3.zero);

            foreach (var vertex in vertices)
            {
                box.Encapsulate(At(cart, panel.transform, vertex));
            }

            Discard(baked);
            panel.SetBlendShapeWeight(0, was);

            return box;
        }

        static Vector3 At(Transform cart, Transform panel, Vector3 vertex)
            => cart.InverseTransformPoint(panel.TransformPoint(vertex));

        static void Discard(Object baked)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(baked);
            }
            else
            {
                Object.DestroyImmediate(baked);
            }
        }

        static void LeaveTheCartAlone(Collider bar, GameObject vehicle)
        {
            var cart = vehicle.GetComponent<Rigidbody>();

            foreach (var part in vehicle.GetComponentsInChildren<Collider>(true))
            {
                if (part.attachedRigidbody == cart)
                {
                    Physics.IgnoreCollision(bar, part, true);
                }
            }
        }

        static void OnItsTrack(GameObject pole, Rigidbody cart, float shutAt, float openAt)
        {
            var rail = pole.AddComponent<ConfigurableJoint>();
            rail.connectedBody = cart;
            rail.autoConfigureConnectedAnchor = false;
            rail.anchor = Vector3.zero;
            rail.connectedAnchor = new Vector3(
                pole.transform.localPosition.x,
                pole.transform.localPosition.y,
                (shutAt + openAt) * 0.5f);

            rail.axis = Vector3.right;
            rail.secondaryAxis = Vector3.up;

            rail.xMotion = ConfigurableJointMotion.Locked;
            rail.yMotion = ConfigurableJointMotion.Locked;
            rail.zMotion = ConfigurableJointMotion.Limited;

            rail.angularXMotion = ConfigurableJointMotion.Locked;
            rail.angularYMotion = ConfigurableJointMotion.Locked;
            rail.angularZMotion = ConfigurableJointMotion.Locked;

            rail.linearLimit = new SoftJointLimit
            {
                limit = Mathf.Abs(openAt - shutAt) * 0.5f,
                bounciness = 0f
            };

            rail.zDrive = new JointDrive
            {
                positionSpring = 0f,
                positionDamper = RailDragNewtonsPerMetrePerSecond,
                maximumForce = RailHoldsAtNewtons
            };

            rail.projectionMode = JointProjectionMode.PositionAndRotation;
            rail.projectionDistance = 0.005f;
            rail.projectionAngle = 0.5f;

            rail.enablePreprocessing = false;
        }
    }
}
