using System.Collections.Generic;
using BelowTheWing.Apron;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class GreyboxShapeTests
    {
        readonly List<GameObject> m_Built = new List<GameObject>();

        [TearDown]
        public void ClearUp()
        {
            foreach (var thing in m_Built)
            {
                Object.DestroyImmediate(thing);
            }

            m_Built.Clear();
        }

        [Test]
        public void ASphereIsScaledToItsDiameterAndCarriesNoCollider()
        {
            var sphere = GreyboxShape.AttachSphere(Holder(), 0.12f, Color.red);

            Assert.That(sphere.localScale, Is.EqualTo(Vector3.one * 0.12f));
            Assert.That(sphere.GetComponent<Collider>(), Is.Null, "a stand-in shape is drawn, never collided with");
            Assert.That(sphere.GetComponent<MeshFilter>().sharedMesh.name, Does.Contain("Sphere"));
        }

        [Test]
        public void EveryShapeIsPaintedWithoutInstancingThroughRendererMaterial()
        {
            foreach (var shape in new[]
                     {
                         GreyboxShape.AttachBox(Holder(), Vector3.one, Color.blue),
                         GreyboxShape.AttachCapsule(Holder(), 1.8f, 0.6f, Color.blue),
                         GreyboxShape.AttachSphere(Holder(), 0.1f, Color.blue)
                     })
            {
                var renderer = shape.GetComponent<MeshRenderer>();

                Assert.That(renderer.sharedMaterial.color, Is.EqualTo(Color.blue),
                    "the colour has to be on the material the renderer actually holds; in edit mode, " +
                    "renderer.material instantiates one behind a warning and leaks it into the scene");
                Assert.That(renderer.sharedMaterial.name, Does.Not.Contain("(Instance)"));
            }
        }

        Transform Holder()
        {
            var holder = new GameObject("Holder");
            m_Built.Add(holder);
            return holder.transform;
        }
    }
}
