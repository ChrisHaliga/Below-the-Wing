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

        SlidingDoorPole APoleOnACart(out VehicleController cart)
        {
            cart = m_Apron.AddVehicle(
                m_CartProfile, "Cart 1", Vector3.zero, Quaternion.identity, TestShapes.Cart());

            var panel = new GameObject("Door1").AddComponent<SkinnedMeshRenderer>();
            panel.transform.SetParent(cart.transform, worldPositionStays: false);
            panel.transform.localPosition = new Vector3(-0.83f, 1.21f, 0.79f);

            SlidingDoors.Build(cart.gameObject, cart.Shape, null);

            var pole = cart.GetComponentInChildren<SlidingDoorPole>(true);
            Assert.That(pole, Is.Not.Null, "no pole was raised, so there is nothing to slide");
            return pole;
        }

        [UnityTest]
        public IEnumerator ADoorPoleSlidesAlongTheCartWhenItIsPushedAlongIt()
        {
            var pole = APoleOnACart(out var cart);

            yield return Steps.Seconds(0.5f);

            Assert.That(pole.Openness, Is.EqualTo(0f).Within(0.05f),
                $"a cart put down should have its doors shut, not sitting at " +
                $"{pole.Openness:P0} open");

            pole.GetComponent<Rigidbody>().AddForce(
                cart.transform.forward * -12f, ForceMode.Impulse);

            var widest = 0f;

            for (var step = 0; step < 120; step++)
            {
                yield return new WaitForFixedUpdate();
                widest = Mathf.Max(widest, pole.Openness);
            }

            Assert.That(widest, Is.GreaterThan(0.8f),
                $"pushed along the cart at 2 m/s the pole only ever reached {widest:P0} open. " +
                "A door that cannot be slid open by pushing it along its own track is not a door");
        }

        [UnityTest]
        public IEnumerator ADoorSlammedIntoItsStopComesBackOffIt()
        {
            var pole = APoleOnACart(out var cart);
            var body = pole.GetComponent<Rigidbody>();

            yield return Steps.Seconds(0.5f);

            body.AddForce(cart.transform.forward * -60f, ForceMode.Impulse);

            var slammed = false;
            var cameBack = 0f;

            for (var step = 0; step < 200; step++)
            {
                yield return new WaitForFixedUpdate();

                if (pole.Openness > 0.99f)
                {
                    slammed = true;
                }
                else if (slammed)
                {
                    cameBack = Mathf.Max(cameBack, 1f - pole.Openness);
                }
            }

            Assert.That(slammed, Is.True, "a 10 m/s shove has to reach the end of a 1.57 m track");
            Assert.That(cameBack, Is.GreaterThan(0.02f),
                "a door slammed into its stop has to bounce back off it rather than stick there");
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
