using System.Collections;
using BelowTheWing.Cargo;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// A cart with a hole in it that bags go into.
    ///
    /// The greybox cart was one solid box the size of the whole vehicle, which meant the inside of
    /// a cart was filled with collider and nothing could ever be in there. The real one is a floor,
    /// two lips, two ends and a roof, and the space between them is the point of the vehicle.
    ///
    /// The lip height is the dial this is all really about. Too low and a parked cart spills its
    /// load whenever anything nudges it; too high and no corner ever throws a bag out, which is the
    /// mechanic the whole game is built around.
    /// </summary>
    public sealed class CartAsAContainerPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_CartProfile;
        BagProfile m_BagProfile;
        VehicleController m_Cart;
        VehicleShape m_Shape;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_CartProfile = TestProfiles.Cart();
            m_BagProfile = TestProfiles.CheckedBag();

            m_Cart = m_Apron.AddVehicle(
                m_CartProfile, "Cart 1", Vector3.zero, Quaternion.identity, TestShapes.Cart());
            m_Shape = m_Cart.GetComponent<VehicleShape>();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_CartProfile);
            Object.DestroyImmediate(m_BagProfile);
        }

        static IEnumerator Step(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        /// <summary>A bag, dropped in the cart's own space at the given spot.</summary>
        Rigidbody ABagAt(Vector3 inTheCart)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Bag";
            go.transform.localScale = m_BagProfile.sizeMetres;
            go.transform.position = m_Cart.transform.TransformPoint(inTheCart);

            var body = go.AddComponent<Rigidbody>();
            body.mass = m_BagProfile.massKg;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            m_Apron.Track(body);

            return body;
        }

        [UnityTest]
        public IEnumerator TheInsideOfACartIsEmpty()
        {
            yield return Step(0.5f);

            var interior = m_Shape.InteriorLocal;
            var middle = m_Cart.transform.TransformPoint(interior.center);

            var filling = Physics.OverlapBox(
                middle, interior.size * 0.4f, m_Cart.transform.rotation, ~0, QueryTriggerInteraction.Ignore);

            foreach (var collider in filling)
            {
                Assert.That(collider.GetComponentInParent<VehicleController>(), Is.Null,
                    $"'{collider.name}' fills the space bags go in. One box the size of the vehicle " +
                    "is what made a cart something nothing could ever be inside");
            }
        }

        [UnityTest]
        public IEnumerator ABagPutInACartComesToRestOnItsDeck()
        {
            var deckTop = m_Shape.InteriorLocal.min.y;
            var bag = ABagAt(new Vector3(0f, deckTop + 0.6f, 0f));

            yield return Step(2.5f);

            var restingAt = m_Cart.transform.InverseTransformPoint(bag.position);

            Assert.That(restingAt.y, Is.EqualTo(deckTop + (m_BagProfile.sizeMetres.y * 0.5f)).Within(0.08f),
                $"the bag settled at {restingAt.y:F2} m in the cart's own space rather than on the " +
                "deck at " + $"{deckTop:F2} m. Through the floor or floating above it are the same " +
                "bug wearing different signs");
        }

        [UnityTest]
        public IEnumerator AParkedCartKeepsItsLoadAboard()
        {
            var deckTop = m_Shape.InteriorLocal.min.y;
            var bag = ABagAt(new Vector3(0.5f, deckTop + 0.3f, 0f));

            yield return Step(2f);

            // Shoved about, the way a cart is when something bumps into it on a busy apron.
            for (var i = 0; i < 5; i++)
            {
                bag.AddForce(new Vector3(40f, 0f, 0f), ForceMode.Impulse);
                yield return Step(0.3f);
            }

            var where = m_Cart.transform.InverseTransformPoint(bag.position);

            Assert.That(Mathf.Abs(where.x), Is.LessThan(m_Shape.InteriorLocal.extents.x + 0.3f),
                "the lips are what stop a parked cart quietly emptying itself whenever anything " +
                "touches it");
        }

        [UnityTest]
        public IEnumerator ACartThrownHardSidewaysLosesWhatIsLoose()
        {
            var deckTop = m_Shape.InteriorLocal.min.y;
            var bag = ABagAt(new Vector3(0f, deckTop + 0.2f, 0f));

            yield return Step(2f);

            // Far harder than a bump: the kind of sideways throw a corner taken too fast gives a
            // deck. The lips have to be a lip rather than a wall.
            for (var i = 0; i < 12; i++)
            {
                bag.AddForce(new Vector3(120f, 30f, 0f), ForceMode.Impulse);
                yield return new WaitForFixedUpdate();
            }

            yield return Step(1.5f);

            var where = m_Cart.transform.InverseTransformPoint(bag.position);

            Assert.That(Mathf.Abs(where.x), Is.GreaterThan(m_Shape.InteriorLocal.extents.x),
                "nothing ever coming off a deck is the same as the game not having the mechanic it " +
                "is built around");
        }
    }
}
