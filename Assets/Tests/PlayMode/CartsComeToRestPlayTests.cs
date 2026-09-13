using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// A cart that has been pushed stops, and stays stopped.
    ///
    /// Every force a tyre makes here is read off a grip curve at the speed the contact patch is
    /// sliding, and a curve through the origin has nothing to say about a slide that is nearly
    /// over: the slower a cart drifts, the less there is to stop it, so it never quite arrives.
    /// A real tyre does the opposite -- below some small slip it simply holds, which is why a
    /// parked trolley stays where it was left instead of creeping across the floor all afternoon.
    /// </summary>
    public sealed class CartsComeToRestPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_CartProfile;
        VehicleController m_Cart;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron(400f);
            m_CartProfile = TestProfiles.Cart();
            m_Cart = m_Apron.AddVehicle(
                m_CartProfile, "Cart 1", Vector3.zero, Quaternion.identity, TestShapes.Cart());
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_CartProfile);
        }

        /// <summary>Waits until the cart has stopped, or gives up. Returns the seconds it took.</summary>
        IEnumerator StopsWithin(float seconds, System.Action<float, float> report)
        {
            var from = m_Cart.transform.position;
            var waited = 0f;

            while (waited < seconds && m_Cart.Body.linearVelocity.magnitude > 0.02f)
            {
                yield return new WaitForFixedUpdate();
                waited += Time.fixedDeltaTime;
            }

            report(waited, Vector3.Distance(m_Cart.transform.position, from));
        }

        [UnityTest]
        public IEnumerator ACartNudgedSidewaysStopsWhereItWasPushedTo()
        {
            yield return Steps.Seconds(2f);

            // Across the cart rather than along it: a bag landing against its side, or somebody
            // walking into it. Sideways is the direction its wheels cannot roll, so this is a
            // slide, and a slide is a thing tyres stop.
            m_Cart.Body.linearVelocity = new Vector3(0.85f, 0f, 0f);

            var took = 0f;
            var travelled = 0f;
            yield return StopsWithin(6f, (seconds, distance) =>
            {
                took = seconds;
                travelled = distance;
            });

            Assert.That(took, Is.LessThan(2f),
                $"a cart shoved sideways at 0.85 m/s was still sliding {took:F1} s later. With grip " +
                "read off a curve through the origin, the last tenth of a metre per second has " +
                "almost nothing opposing it and the cart drifts off across the apron");
            Assert.That(travelled, Is.LessThan(0.5f),
                $"it slid {travelled:F2} m sideways after a shove a person could give it by walking " +
                "into it");

            var stoppedAt = m_Cart.transform.position;
            yield return Steps.Seconds(10f);

            Assert.That(Vector3.Distance(m_Cart.transform.position, stoppedAt), Is.LessThan(0.01f),
                "and it stayed where it stopped, held by its tyres rather than by being frozen");
        }

        [UnityTest]
        public IEnumerator AShovedCartStillRollsAwayBeforeItStops()
        {
            yield return Steps.Seconds(2f);

            // Along the cart this time, which is the way its wheels turn: this one is meant to roll.
            m_Cart.Body.linearVelocity = new Vector3(0f, 0f, -3f);

            var took = 0f;
            var travelled = 0f;
            yield return StopsWithin(20f, (seconds, distance) =>
            {
                took = seconds;
                travelled = distance;
            });

            Assert.That(travelled, Is.GreaterThan(1.5f),
                $"shoved at 3 m/s the cart only rolled {travelled:F2} m. A cart on wheels has to roll " +
                "away when it is pushed -- stopping it dead is the other failure, and a tyre that " +
                "holds a standing cart must not also drag a moving one to a halt");
            Assert.That(took, Is.LessThan(20f),
                "and it has to stop eventually, under its own rolling resistance");
        }
    }
}
