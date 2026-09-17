using System.Collections;
using BelowTheWing.Cargo;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class BagsAreBodiesPlayTests
    {
        const float BagHalfWidthMetres = 0.2f;
        const float LipThicknessMetres = 0.05f;

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

        Bag ABagAt(Vector3 inTheCart)
        {
            var go = new GameObject("Bag");
            go.transform.position = m_Cart.transform.TransformPoint(inTheCart);
            go.AddComponent<Rigidbody>();
            go.AddComponent<BoxCollider>();
            var bag = go.AddComponent<Bag>();
            bag.Configure(m_BagProfile);
            m_Apron.Track(bag);
            return bag;
        }

        Vector3 InTheCart(Bag bag) => m_Cart.transform.InverseTransformPoint(bag.transform.position);

        IEnumerator DriveTo(Vector3 velocity, float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 1; i <= steps; i++)
            {
                m_Cart.Body.linearVelocity = velocity * ((float)i / steps);
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator ABagOnADeckStaysPutInAStraightLine()
        {
            var bag = ABagAt(new Vector3(0f, m_Shape.InteriorLocal.min.y + 0.3f, 0f));
            yield return Steps.Seconds(1.5f);
            var satAt = InTheCart(bag);

            yield return DriveTo(new Vector3(0f, 0f, 4f), seconds: 2f);
            yield return Steps.Seconds(2f);

            Assert.That(Vector3.Distance(InTheCart(bag), satAt), Is.LessThan(0.15f),
                $"the bag moved {Vector3.Distance(InTheCart(bag), satAt):F2} m about the deck while " +
                "the cart drove in a straight line. Friction has to carry a bag in a straight " +
                "line, or every load is lost before the first corner");
        }

        IEnumerator AHardCorner()
        {
            for (var i = 1; i <= 50; i++)
            {
                m_Cart.Body.linearVelocity = new Vector3(0.4f * i, 0f, 0f);
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator AHardCornerSlidesAFlatBagIntoTheLipAndNoFurther()
        {
            var bag = ABagAt(new Vector3(0f, m_Shape.InteriorLocal.min.y + 0.3f, 0f));
            yield return Steps.Seconds(1.5f);

            yield return AHardCorner();
            yield return Steps.Seconds(1.5f);

            var slidTo = InTheCart(bag);
            var againstTheLip = m_Shape.InteriorLocal.extents.x - (BagHalfWidthMetres + LipThicknessMetres);

            Assert.That(Mathf.Abs(slidTo.x), Is.GreaterThan(againstTheLip * 0.9f),
                $"the bag is at {slidTo.x:F2} m from the middle and the lip's inner face is at " +
                $"{againstTheLip:F2} m. Stopping short of it is friction holding a bag through a " +
                "corner that should have thrown it, and the lip never had a job to do");
            Assert.That(Mathf.Abs(slidTo.x), Is.LessThan(m_Shape.InteriorLocal.extents.x),
                "the lip is taller than a flat bag's middle, and that is what a lip is for");
        }

        [UnityTest]
        public IEnumerator AHardCornerTipsABagStandingOnEndOverTheLip()
        {
            var bag = ABagAt(new Vector3(0f, m_Shape.InteriorLocal.min.y + 0.4f, 0f));
            bag.Body.rotation = Quaternion.Euler(90f, 0f, 0f);
            yield return Steps.Seconds(1.5f);

            yield return AHardCorner();
            yield return Steps.Seconds(2f);

            Assert.That(Mathf.Abs(InTheCart(bag).x), Is.GreaterThan(m_Shape.InteriorLocal.extents.x),
                "a bag nothing can throw off is the game not having the mechanic it is built around");
        }

        static float OffItsFace(Transform box)
        {
            var worst = 180f;

            foreach (var face in new[] { box.up, box.right, box.forward })
            {
                worst = Mathf.Min(worst, Mathf.Min(
                    Vector3.Angle(face, Vector3.up), Vector3.Angle(face, Vector3.down)));
            }

            return worst;
        }

        [UnityTest]
        public IEnumerator ABagOnAParkedCartSettlesWithoutBeingFrozen()
        {
            var bag = ABagAt(new Vector3(0f, m_Shape.InteriorLocal.min.y + 0.3f, 0f));

            yield return Steps.Seconds(4f);

            Assert.That(bag.Body.linearVelocity.magnitude, Is.LessThan(0.05f), "it has settled");
            Assert.That(bag.Body.IsSleeping(), Is.False,
                "a sleeping bag is frozen in whatever pose it had when it dozed off, including one " +
                "it was halfway through falling out of, and it stays there until something sharp " +
                "enough to wake it comes along");
        }

        [UnityTest]
        public IEnumerator ABagTippedOnItsEdgeFallsOverRatherThanBalancing()
        {
            var bag = ABagAt(new Vector3(0f, m_Shape.InteriorLocal.min.y + 0.5f, 0f));
            bag.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

            yield return Steps.Seconds(5f);

            Assert.That(OffItsFace(bag.transform), Is.LessThan(10f),
                $"the bag came to rest {OffItsFace(bag.transform):F0} degrees off any face of its " +
                "own -- balanced on an edge. Dropped on a corner it has to fall over like anything " +
                "else; stopping where it happened to be when it went quiet is not resting");
        }

        [UnityTest]
        public IEnumerator ABagWithMostOfItselfPastTheEndOfADeckFallsOff()
        {
            var bag = ABagAt(new Vector3(
                0f, m_Shape.InteriorLocal.min.y + 0.2f, m_Shape.InteriorLocal.extents.z + 0.2f));

            yield return Steps.Seconds(5f);

            Assert.That(bag.transform.position.y, Is.LessThan(m_Shape.InteriorLocal.min.y),
                $"it is still up at {bag.transform.position.y:F2} m with its weight hanging past the " +
                "end of the deck. A bag resting on nothing is a bag that was frozen where it was " +
                "rather than left to fall");
        }

        [UnityTest]
        public IEnumerator ABagAtRestGoesWithTheCartWhenItDrivesAway()
        {
            var bag = ABagAt(new Vector3(0f, m_Shape.InteriorLocal.min.y + 0.3f, 0f));
            yield return Steps.Seconds(4f);

            var satAt = InTheCart(bag);
            yield return DriveTo(new Vector3(0f, 0f, 6f), seconds: 2f);
            yield return Steps.Seconds(1f);

            Assert.That(Vector3.Distance(InTheCart(bag), satAt), Is.LessThan(0.3f),
                "the cart drove out from under it. A bag that settled has to be carried by the deck " +
                "it settled on, which it cannot be if it was frozen in place in the world");
        }

        [UnityTest]
        public IEnumerator ABagThrownInThroughTheSideLandsOnTheDeck()
        {
            var bag = ABagAt(new Vector3(2.4f, m_Shape.InteriorLocal.min.y + 1.2f, 0f));
            bag.Body.linearVelocity = new Vector3(-4f, 0f, 0f);

            yield return Steps.Seconds(3f);

            var landed = InTheCart(bag);
            Assert.That(m_Shape.InteriorLocal.Contains(landed), Is.True,
                $"the bag ended at {landed} in the cart's space, which is not inside it");
            Assert.That(landed.y, Is.GreaterThan(m_Shape.InteriorLocal.min.y),
                "and not through the floor");
        }
    }
}
