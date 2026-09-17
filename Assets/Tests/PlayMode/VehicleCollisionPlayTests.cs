using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class VehicleCollisionPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;
        VehicleProfile m_CartProfile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
            m_CartProfile = TestProfiles.Cart();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
            Object.DestroyImmediate(m_CartProfile);
        }

        [UnityTest]
        public IEnumerator ATractorRearEndingAParkedCartSendsItRollingAndIsSlowedByIt()
        {
            var cart = m_Apron.AddVehicle(m_CartProfile, "Cart 1", new Vector3(0f, 0f, 8f), Quaternion.identity, TestShapes.Cart());
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity, TestShapes.Tractor());
            yield return Steps.Seconds(2f);

            var hit = false;
            for (var i = 0; i < 200 && !hit; i++)
            {
                tractor.Body.linearVelocity = new Vector3(0f, 0f, 5f);
                yield return new WaitForFixedUpdate();
                hit = cart.Body.linearVelocity.z > 0.5f;
            }

            Assert.That(hit, Is.True, "the tractor never reached the cart");
            yield return Steps.Seconds(0.2f);

            var cartLeftAt = cart.Body.linearVelocity.z;
            var tractorAfter = tractor.Body.linearVelocity.z;
            Assert.That(cartLeftAt, Is.GreaterThanOrEqualTo(4.5f),
                $"the cart left at {cartLeftAt:F1} m/s from a 5 m/s hit by something five times its " +
                "weight. A dead bump makes a three tonne tractor feel like a shopping trolley");
            Assert.That(tractorAfter, Is.LessThan(5f), "and the tractor paid for it");

            yield return Steps.Seconds(2f);
            Assert.That(cart.Body.linearVelocity.z, Is.GreaterThanOrEqualTo(0.5f),
                $"two seconds later the cart is doing {cart.Body.linearVelocity.z:F1} m/s: a driveline " +
                "that drags a shoved cart to a halt in a couple of seconds hides the shove");
        }

        [UnityTest]
        public IEnumerator AShovedCartRollsWellAwayBeforeItStops()
        {
            var cart = m_Apron.AddVehicle(m_CartProfile, "Cart 1", Vector3.zero, Quaternion.identity, TestShapes.Cart());
            yield return Steps.Seconds(2f);
            var from = cart.transform.position;

            cart.Body.linearVelocity = new Vector3(0f, 0f, 5f);
            yield return Steps.Seconds(20f);

            var rolled = cart.transform.position.z - from.z;
            Assert.That(cart.Body.linearVelocity.magnitude, Is.LessThan(0.2f), "it does stop eventually");
            Assert.That(rolled, Is.GreaterThan(4f),
                $"shoved to 5 m/s, the cart rolled {rolled:F1} m. A cart shoved across the apron " +
                "has to coast rather than stop dead, and rolling much further than this reads as " +
                "weightless");
        }
    }
}
