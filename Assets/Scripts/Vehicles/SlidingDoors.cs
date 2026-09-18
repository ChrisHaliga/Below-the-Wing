using System.Collections.Generic;
using System.Text.RegularExpressions;
using BelowTheWing.Cargo;
using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class SlidingDoors
    {
        public const string PolesName = "Doors";

        static readonly Regex PanelPattern = new Regex(@"^Door(\d+)$", RegexOptions.Compiled);

        public static string PanelName(int door) => $"Door{door}";

        public static string FabricName(int door) => $"Door_Fabric{door}";

        public static bool IsAPanel(string name, out int door)
        {
            var match = PanelPattern.Match(name);

            door = match.Success ? int.Parse(match.Groups[1].Value) : 0;

            return match.Success;
        }

        public static void Build(GameObject vehicle, VehicleShape shape, PhysicsMaterial bodywork, DoorRailSettings rail)
        {
            if (vehicle.transform.Find(PolesName) != null || shape == null)
            {
                return;
            }

            Transform poles = null;

            foreach (var (door, panel) in Panels(vehicle.transform))
            {
                if (panel.sharedMesh == null || panel.sharedMesh.blendShapeCount == 0)
                {
                    continue;
                }

                poles ??= Container(vehicle);
                Raise(vehicle, poles, panel, Find(vehicle.transform, FabricName(door)), bodywork, rail);
            }
        }

        public static IReadOnlyList<(int Door, SkinnedMeshRenderer Panel)> Panels(Transform under)
        {
            var panels = new List<(int, SkinnedMeshRenderer)>();

            foreach (var candidate in under.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (IsAPanel(candidate.name, out var door))
                {
                    panels.Add((door, candidate));
                }
            }

            panels.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            return panels;
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
            SkinnedMeshRenderer fabric, PhysicsMaterial bodywork, DoorRailSettings rail)
        {
            var shut = InCartSpace(panel, vehicle.transform, 0f);
            var open = InCartSpace(panel, vehicle.transform, SlidingDoor.FullyOpenWeight);

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
            body.mass = rail.poleKg;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            pole.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            OnItsTrack(pole, vehicle.GetComponent<Rigidbody>(), shutAt, openAt, rail);

            var cover = new GameObject($"{panel.name} cover").transform;
            cover.SetParent(poles, worldPositionStays: false);
            cover.localScale = new Vector3(radius * 2f, shut.size.y, CoverThicknessMetres);

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

        const float CoverThicknessMetres = 0.001f;

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

            Discard.Now(baked);
            panel.SetBlendShapeWeight(0, was);

            return box;
        }

        static Vector3 At(Transform cart, Transform panel, Vector3 vertex)
            => cart.InverseTransformPoint(panel.TransformPoint(vertex));

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

        static void OnItsTrack(GameObject pole, Rigidbody cart, float shutAt, float openAt, DoorRailSettings rail)
        {
            var track = pole.AddComponent<ConfigurableJoint>();
            track.connectedBody = cart;
            track.autoConfigureConnectedAnchor = false;
            track.anchor = Vector3.zero;
            track.connectedAnchor = new Vector3(
                pole.transform.localPosition.x,
                pole.transform.localPosition.y,
                (shutAt + openAt) * 0.5f);

            track.axis = Vector3.right;
            track.secondaryAxis = Vector3.up;

            track.xMotion = ConfigurableJointMotion.Locked;
            track.yMotion = ConfigurableJointMotion.Locked;
            track.zMotion = ConfigurableJointMotion.Limited;

            track.angularXMotion = ConfigurableJointMotion.Locked;
            track.angularYMotion = ConfigurableJointMotion.Locked;
            track.angularZMotion = ConfigurableJointMotion.Locked;

            track.linearLimit = new SoftJointLimit
            {
                limit = Mathf.Abs(openAt - shutAt) * 0.5f,
                bounciness = Mathf.Clamp01(rail.bounceOffTheEnd)
            };

            track.zDrive = new JointDrive
            {
                positionSpring = Mathf.Max(rail.settlesAtNewtonsPerMetre, 0f),
                positionDamper = Mathf.Max(rail.dragNewtonsPerMetrePerSecond, 0f),
                maximumForce = rail.holdsAtNewtons
            };

            track.projectionMode = JointProjectionMode.PositionAndRotation;
            track.projectionDistance = rail.projectionDistanceMetres;
            track.projectionAngle = rail.projectionAngleDegrees;

            track.enablePreprocessing = false;
        }
    }
}
