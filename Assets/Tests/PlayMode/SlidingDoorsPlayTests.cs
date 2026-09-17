using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class SlidingDoorsPlayTests
    {
        const float ShutEdgeZ = 0.00748f;
        const float FarEdgeZ = 1.57189f;
        const float TravelMetres = 1.01163f;
        const float PoleRadius = 0.02171f;

        TestApron m_Apron;
        VehicleProfile m_CartProfile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_CartProfile = TestProfiles.Cart();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_CartProfile);
        }

        static Mesh ADoorMesh()
        {
            var near = ShutEdgeZ;
            var far = FarEdgeZ;
            var acrossHalf = PoleRadius;
            var tall = 0.74f;

            var vertices = new[]
            {
                new Vector3(-acrossHalf, -tall, near), new Vector3(acrossHalf, -tall, near),
                new Vector3(-acrossHalf, tall, near), new Vector3(acrossHalf, tall, near),
                new Vector3(-acrossHalf, -tall, far), new Vector3(acrossHalf, -tall, far),
                new Vector3(-acrossHalf, tall, far), new Vector3(acrossHalf, tall, far)
            };

            var mesh = new Mesh { vertices = vertices };
            mesh.SetTriangles(new[] { 0, 1, 2, 2, 1, 3, 4, 6, 5, 5, 6, 7 }, 0);
            mesh.RecalculateNormals();

            var slide = new Vector3[vertices.Length];

            for (var i = 0; i < 4; i++)
            {
                slide[i] = new Vector3(0f, 0f, TravelMetres);
            }

            mesh.AddBlendShapeFrame("Open", 100f, slide, null, null);

            return mesh;
        }

        SlidingDoorPole APoleOnACart(out VehicleController cart)
        {
            cart = m_Apron.AddVehicle(
                m_CartProfile, "Cart 1", Vector3.zero, Quaternion.identity, TestShapes.Cart());

            var panel = new GameObject("Door1").AddComponent<SkinnedMeshRenderer>();
            panel.transform.SetParent(cart.transform, worldPositionStays: false);
            panel.transform.localPosition = new Vector3(-0.83464f, 1.20934f, 0f);
            panel.sharedMesh = ADoorMesh();

            SlidingDoors.Build(cart.gameObject, cart.Shape, null);

            var pole = cart.GetComponentInChildren<SlidingDoorPole>(true);
            Assert.That(pole, Is.Not.Null, "no pole was raised, so there is nothing to slide");
            return pole;
        }

        [UnityTest]
        public IEnumerator ADoorPoleStartsShutWhereTheModelPutsIt()
        {
            var pole = APoleOnACart(out _);

            yield return Steps.Seconds(0.5f);

            Assert.That(pole.Openness, Is.EqualTo(0f).Within(0.02f),
                $"a cart put down should have its doors shut, not sitting at {pole.Openness:P0} open");
            Assert.That(pole.transform.localPosition.z, Is.EqualTo(0.02919f).Within(5e-3f));
        }

        [UnityTest]
        public IEnumerator ADoorPoleSlidesAllTheWayOpenWhenItIsPushedAlongTheCart()
        {
            var pole = APoleOnACart(out var cart);

            yield return Steps.Seconds(0.5f);

            pole.GetComponent<Rigidbody>().AddForce(
                cart.transform.forward * 12f, ForceMode.Impulse);

            var widest = 0f;

            for (var step = 0; step < 120; step++)
            {
                yield return new WaitForFixedUpdate();
                widest = Mathf.Max(widest, pole.Openness);
            }

            Assert.That(widest, Is.GreaterThan(0.95f),
                $"pushed along the cart at 2 m/s the pole only ever reached {widest:P0} open");
        }

        [UnityTest]
        public IEnumerator ADoorSlammedIntoItsStopStaysThereRatherThanBouncingBack()
        {
            var pole = APoleOnACart(out var cart);

            yield return Steps.Seconds(0.5f);

            pole.GetComponent<Rigidbody>().AddForce(
                cart.transform.forward * 60f, ForceMode.Impulse);

            yield return Steps.Seconds(2f);

            Assert.That(pole.Openness, Is.EqualTo(1f).Within(0.02f),
                $"slammed open at 10 m/s the door settled at {pole.Openness:P0}. A door with no " +
                "spring on it stops where it is shoved and stays there");
        }

        [UnityTest]
        public IEnumerator ADoorPoleDoesNotWanderSidewaysOffItsTrack()
        {
            var pole = APoleOnACart(out var cart);
            var startedAt = pole.transform.localPosition.x;

            pole.GetComponent<Rigidbody>().AddForce(
                cart.transform.right * 60f, ForceMode.Impulse);

            yield return Steps.Seconds(1f);

            Assert.That(pole.transform.localPosition.x, Is.EqualTo(startedAt).Within(0.02f),
                $"the pole moved from x {startedAt:F3} to {pole.transform.localPosition.x:F3}. " +
                "A door pole shoved across the cart has to stay on its rail");
        }
    }
}
