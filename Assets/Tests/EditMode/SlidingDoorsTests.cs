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
                "the rig, the menu and the generator's measuring all bake the door at this one weight");
        }

        [Test]
        public void EveryDoorTheModelHasGetsARailNotJustTheFirstFour()
        {
            var cart = ACartWithDoors(6);

            SlidingDoors.Build(cart, cart.GetComponent<VehicleShape>(), null, DoorRailSettings.Default);

            Assert.That(cart.GetComponentsInChildren<SlidingDoorPole>(true).Length, Is.EqualTo(6),
                "every panel named Door followed by a number gets a rail");
        }

        [Test]
        public void AGapInTheDoorNumberingDoesNotLoseTheDoorsAfterIt()
        {
            var cart = ACartWithDoors(1, 2, 4);

            SlidingDoors.Build(cart, cart.GetComponent<VehicleShape>(), null, DoorRailSettings.Default);

            Assert.That(cart.GetComponentsInChildren<SlidingDoorPole>(true).Length, Is.EqualTo(3),
                "Door4 is a door whether or not a Door3 exists");
        }

        [Test]
        public void OnlyNamesOfTheFormDoorNumberAreDoors()
        {
            Assert.That(SlidingDoors.IsAPanel("Door3", out var door), Is.True);
            Assert.That(door, Is.EqualTo(3));
            Assert.That(SlidingDoors.IsAPanel("Door_Fabric3", out _), Is.False);
            Assert.That(SlidingDoors.IsAPanel("Doorframe", out _), Is.False);
        }

        [Test]
        public void TheRailIsBuiltToTheSettingsItIsGiven()
        {
            var cart = ACartWithDoors(2);
            var rail = DoorRailSettings.Default;
            rail.dragNewtonsPerMetrePerSecond = 20f;
            rail.bounceOffTheEnd = 0.25f;

            SlidingDoors.Build(cart, cart.GetComponent<VehicleShape>(), null, rail);

            var joints = cart.GetComponentsInChildren<ConfigurableJoint>(true);
            Assert.That(joints.Length, Is.EqualTo(2), "two doors, two rails, or the loop below checks nothing");

            foreach (var joint in joints)
            {
                Assert.That(joint.zDrive.positionDamper, Is.EqualTo(20f).Within(1e-4f));
                Assert.That(joint.linearLimit.bounciness, Is.EqualTo(0.25f).Within(1e-4f));
                Assert.That(joint.zDrive.positionSpring, Is.EqualTo(0f).Within(1e-4f),
                    "a spring on this drive pulls toward the joint's anchor, which is the middle of " +
                    "the travel. What carries a door to an end is a force the pole applies, so that " +
                    "it can be capped without capping the drag along with it");

                Assert.That(joint.zDrive.maximumForce, Is.EqualTo(rail.holdsAtNewtons).Within(1e-4f),
                    "the drive damps, and a cap below the rail's hold takes the damping away: a " +
                    "door then bounces off its end stop and drifts back");

                Assert.That(joint.zMotion, Is.EqualTo(ConfigurableJointMotion.Limited),
                    "locking this axis pins the pole at the joint's connected anchor, which is the " +
                    "middle of the travel, so every door reads exactly half open");
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

            Assert.That(rail.seatsAtNewtons, Is.GreaterThan(0f),
                "nothing carries a door left halfway to either end, so it stays halfway");

            Assert.That(rail.latchHoldsAtNewtons, Is.InRange(400f, 3000f),
                $"a latch of {rail.latchHoldsAtNewtons:0} N has to beat a 20 kg bag leaning on a " +
                "door, around 200 N, and lose to a hand, which makes 1000 N at 0.1 m of arm stretch");
        }

        [Test]
        public void TheDefaultRailUnhooksOnAShakeNoBagCanCause()
        {
            var rail = DoorRailSettings.Default;

            Assert.That(rail.unhooksAboveMetresPerSecondSquared, Is.GreaterThan(10f),
                $"a hook thrown by {rail.unhooksAboveMetresPerSecondSquared:0} m/s^2 comes off " +
                "every time a bag lands in the cart or a tractor pulls away");

            Assert.That(rail.unhooksAboveMetresPerSecondSquared, Is.LessThan(100f),
                "a hook nothing can shake off is a door that never opens when the cart is hit");
        }

        [Test]
        public void ARailWithNoLatchHooksNothing()
        {
            var cart = ACartWithDoors(1);
            var rail = DoorRailSettings.Default;
            rail.latchHoldsAtNewtons = 0f;

            SlidingDoors.Build(cart, cart.GetComponent<VehicleShape>(), null, rail);

            var pole = cart.GetComponentInChildren<SlidingDoorPole>(true);

            Assert.That(pole.HooksShut, Is.False,
                "the menu shoves its doors with a rail that holds them nowhere, and a hook it never " +
                "asked for pins them shut against a 38 newton-second shove");
        }

        [Test]
        public void TheDefaultRailLetsAHandMoveADoorFromWhereAPlayerStands()
        {
            var rail = DoorRailSettings.Default;

            Assert.That(rail.followsAHandNewtonsPerMetre, Is.GreaterThan(0f),
                "a held door that follows nothing only moves when the arm tether hits its own " +
                "reach limit, which is 1.2 m away, so the player has to sprint or flick to open it");

            Assert.That(rail.followsAHandNewtonsPerMetre * 0.1f, Is.GreaterThan(rail.seatsAtNewtons),
                $"a hand 0.1 m along the rail pulls {rail.followsAHandNewtonsPerMetre * 0.1f:0} N " +
                $"against the {rail.seatsAtNewtons:0} N carrying the door back to its end, so a " +
                "small movement has to win");
        }

        [Test]
        public void ADoorLetGoBelowHalfwayShutsAndAboveHalfwayOpens()
        {
            Assert.That(SlidingDoor.EndItSettlesTo(0f), Is.EqualTo(SlidingDoor.Shut));
            Assert.That(SlidingDoor.EndItSettlesTo(0.4f), Is.EqualTo(SlidingDoor.Shut));
            Assert.That(SlidingDoor.EndItSettlesTo(0.49f), Is.EqualTo(SlidingDoor.Shut));

            Assert.That(SlidingDoor.EndItSettlesTo(0.5f), Is.EqualTo(SlidingDoor.Open));
            Assert.That(SlidingDoor.EndItSettlesTo(0.6f), Is.EqualTo(SlidingDoor.Open),
                "a door shoved most of the way open finishes the job rather than sliding back");
            Assert.That(SlidingDoor.EndItSettlesTo(1f), Is.EqualTo(SlidingDoor.Open));
        }

        [Test]
        public void OnlyADoorNearAnEndCountsAsSeated()
        {
            Assert.That(SlidingDoor.Seated(0f, 0.04f), Is.True);
            Assert.That(SlidingDoor.Seated(1f, 0.04f), Is.True);
            Assert.That(SlidingDoor.Seated(0.04f, 0.04f), Is.True);

            Assert.That(SlidingDoor.Seated(0.2f, 0.04f), Is.False,
                "a door a fifth of the way open is travelling, and latching it there is what " +
                "leaves a door stuck in an intermediate spot");
            Assert.That(SlidingDoor.Seated(0.5f, 0.04f), Is.False);
        }

        GameObject ACartWithDoors(int count)
        {
            var doors = new int[count];

            for (var i = 0; i < count; i++)
            {
                doors[i] = i + 1;
            }

            return ACartWithDoors(doors);
        }

        GameObject ACartWithDoors(params int[] doors)
        {
            var cart = new GameObject("Cart");
            m_Built.Add(cart);

            cart.AddComponent<Rigidbody>().isKinematic = true;
            TestShapes.On(cart, TestShapes.Cart());

            foreach (var i in doors)
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
