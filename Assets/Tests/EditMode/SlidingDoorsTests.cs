using System.Collections.Generic;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class SlidingDoorsTests
    {
        readonly List<Object> m_Built = new List<Object>();

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
        public void ADoorAndItsFabricAreNamedFromOneRule()
        {
            Assert.That(SlidingDoors.PanelName(3), Is.EqualTo("Door3"));
            Assert.That(SlidingDoors.FabricName(3), Is.EqualTo("Door_Fabric3"));
        }

        [Test]
        public void FullyOpenIsOneWeightEverywhere()
        {
            Assert.That(SlidingDoor.ShapeWeight(1f), Is.EqualTo(SlidingDoor.FullyOpenWeight).Within(1e-4f),
                "the rig, the menu and the generator's measuring all bake the door at this weight, " +
                "and each used to write 100 for itself");
        }

        [Test]
        public void EveryDoorTheModelHasGetsARailNotJustTheFirstFour()
        {
            var cart = ACartWithDoors(6);

            SlidingDoors.Build(cart, cart.GetComponent<VehicleShape>(), null, DoorRailSettings.Default);

            Assert.That(cart.GetComponentsInChildren<SlidingDoorPole>(true).Length, Is.EqualTo(6),
                "discovery stopped at Door4 because a loop was written to four");
        }

        [Test]
        public void TheRailIsBuiltToTheSettingsItIsGiven()
        {
            var cart = ACartWithDoors(2);
            var rail = DoorRailSettings.Default;
            rail.dragNewtonsPerMetrePerSecond = 20f;
            rail.bounceOffTheEnd = 0.25f;
            rail.settlesAtNewtonsPerMetre = 30f;

            SlidingDoors.Build(cart, cart.GetComponent<VehicleShape>(), null, rail);

            var joints = cart.GetComponentsInChildren<ConfigurableJoint>(true);
            Assert.That(joints.Length, Is.EqualTo(2), "two doors, two rails, or the loop below checks nothing");

            foreach (var joint in joints)
            {
                Assert.That(joint.zDrive.positionDamper, Is.EqualTo(20f).Within(1e-4f));
                Assert.That(joint.linearLimit.bounciness, Is.EqualTo(0.25f).Within(1e-4f));
                Assert.That(joint.zDrive.positionSpring, Is.EqualTo(30f).Within(1e-4f),
                    "the menu used to reach into each joint after the rig was raised and rewrite these");
            }
        }

        [Test]
        public void TheDefaultRailIsTheOneTheGameDrivesWith()
        {
            var rail = DoorRailSettings.Default;

            Assert.That(rail.poleKg, Is.EqualTo(6f).Within(1e-4f));
            Assert.That(rail.dragNewtonsPerMetrePerSecond, Is.EqualTo(40f).Within(1e-4f));
            Assert.That(rail.holdsAtNewtons, Is.EqualTo(1500f).Within(1e-4f));
            Assert.That(rail.bounceOffTheEnd, Is.EqualTo(0f).Within(1e-4f), "a hand-pushed door stops dead at its end");
            Assert.That(rail.settlesAtNewtonsPerMetre, Is.EqualTo(0f).Within(1e-4f), "and nothing springs it back at the player");
        }

        GameObject ACartWithDoors(int count)
        {
            var cart = new GameObject("Cart");
            m_Built.Add(cart);

            cart.AddComponent<Rigidbody>().isKinematic = true;
            TestShapes.On(cart, TestShapes.Cart());

            for (var i = 1; i <= count; i++)
            {
                var side = i % 2 == 0 ? -0.83f : 0.83f;
                var along = ((i - 1) / 2) * 0.4f;

                var panel = new GameObject(SlidingDoors.PanelName(i)).AddComponent<SkinnedMeshRenderer>();
                panel.transform.SetParent(cart.transform, worldPositionStays: false);
                panel.transform.localPosition = new Vector3(side, 1.2f, along + 0.2f);
                panel.sharedMesh = ADoorMesh();
            }

            return cart;
        }

        Mesh ADoorMesh()
        {
            var mesh = new Mesh();
            m_Built.Add(mesh);

            mesh.vertices = new[]
            {
                new Vector3(-0.02f, -0.7f, -0.7f), new Vector3(0.02f, -0.7f, -0.7f),
                new Vector3(-0.02f, 0.7f, 0.7f), new Vector3(0.02f, 0.7f, 0.7f)
            };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };

            var slide = new Vector3[4];
            for (var i = 0; i < 4; i++)
            {
                slide[i] = new Vector3(0f, 0f, 1f);
            }

            mesh.AddBlendShapeFrame("Open", SlidingDoor.FullyOpenWeight, slide, null, null);

            return mesh;
        }
    }
}
