using System.Collections.Generic;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    public sealed class BuiltMeshes : MonoBehaviour
    {
        readonly List<Mesh> m_Meshes = new List<Mesh>();

        public void Keep(Mesh mesh) => m_Meshes.Add(mesh);

        void OnDestroy()
        {
            foreach (var mesh in m_Meshes)
            {
                if (mesh != null)
                {
                    Destroy(mesh);
                }
            }
        }
    }

    public static class TestDoorCart
    {
        public const int Doors = 4;

        public static GameObject Build(VehicleProfile profile)
        {
            var cart = new GameObject("Cart");
            var meshes = cart.AddComponent<BuiltMeshes>();

            var body = cart.AddComponent<Rigidbody>();
            body.mass = profile.massKg;

            TestShapes.On(cart, TestShapes.Cart());

            var shape = cart.GetComponent<VehicleShape>();
            var standing = cart.AddComponent<BoxCollider>();
            standing.center = shape.EnvelopeCentreLocal;
            standing.size = shape.EnvelopeSizeMetres;

            cart.transform.position = new Vector3(
                0f, (shape.EnvelopeSizeMetres.y * 0.5f) - shape.EnvelopeCentreLocal.y, 0f);

            for (var door = 1; door <= Doors; door++)
            {
                var side = door % 2 == 0 ? -0.83f : 0.83f;
                var towardsTheEnd = door <= 2 ? 1f : -1f;

                var panel = new GameObject(SlidingDoors.PanelName(door)).AddComponent<SkinnedMeshRenderer>();
                panel.transform.SetParent(cart.transform, worldPositionStays: false);
                panel.transform.localPosition = new Vector3(side, 1.2f, towardsTheEnd * 0.79f);

                var mesh = ADoorMesh(towardsTheEnd);
                meshes.Keep(mesh);
                panel.sharedMesh = mesh;
            }

            SlidingDoors.Build(cart, cart.GetComponent<VehicleShape>(), null, profile.doorRail);

            return cart;
        }

        static Mesh ADoorMesh(float towardsTheEnd)
        {
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-0.02f, -0.74f, -0.78f), new Vector3(0.02f, -0.74f, -0.78f),
                    new Vector3(-0.02f, 0.74f, 0.78f), new Vector3(0.02f, 0.74f, 0.78f)
                },
                triangles = new[] { 0, 2, 1, 1, 2, 3 }
            };

            var slide = new Vector3[4];

            for (var corner = 0; corner < slide.Length; corner++)
            {
                slide[corner] = new Vector3(0f, 0f, towardsTheEnd * 0.5f);
            }

            mesh.AddBlendShapeFrame("Open", SlidingDoor.FullyOpenWeight, slide, null, null);

            return mesh;
        }
    }
}
