using BelowTheWing.Cargo;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// A piece of baggage as a physical object.
    ///
    /// The simplest thing in the game and the one there will be most of. Forty of them exist on
    /// every machine at once, so what matters about a bag is that it costs almost nothing and that
    /// it weighs the same everywhere -- a bag that arrives at the wrong weight on one machine is
    /// thrown a different distance there, and nothing looks wrong until two players disagree about
    /// where it landed.
    /// </summary>
    public sealed class BagTests
    {
        GameObject m_Object;
        BagProfile m_Profile;

        [SetUp]
        public void SetUp()
        {
            m_Profile = ScriptableObject.CreateInstance<BagProfile>();
            m_Profile.massKg = 20f;
            m_Profile.sizeMetres = new Vector3(0.4f, 0.25f, 0.6f);
            m_Profile.damagedAtImpulse = 250f;

            m_Object = new GameObject("Bag");
            m_Object.AddComponent<Rigidbody>();
            m_Object.AddComponent<BoxCollider>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Object);
            Object.DestroyImmediate(m_Profile);
        }

        Bag Configured()
        {
            var bag = m_Object.AddComponent<Bag>();
            bag.Configure(m_Profile);
            return bag;
        }

        [Test]
        public void ABagWeighsWhatItsProfileSays()
        {
            var bag = Configured();

            Assert.That(bag.Body.mass, Is.EqualTo(20f).Within(0.01f),
                "a bag left at the rigidbody default weighs one kilogram and is thrown across the " +
                "apron by anything that brushes it");
        }

        [Test]
        public void ABagIsTheSizeItsProfileSays()
        {
            Configured();

            Assert.That(m_Object.GetComponent<BoxCollider>().size, Is.EqualTo(new Vector3(0.4f, 0.25f, 0.6f)),
                "drawn at one size and collided at another, a bag will not sit in a cart it looks " +
                "like it should fit in");
        }

        [Test]
        public void ABagIsASingleBoxAndNothingMoreComplicated()
        {
            Configured();

            Assert.That(m_Object.GetComponents<Collider>(), Has.Length.EqualTo(1),
                "forty bags in a cart is forty contact pairs, on every machine, whether or not " +
                "anybody is looking at that cart. A second collider on each doubles that for nothing");
            Assert.That(m_Object.GetComponent<BoxCollider>(), Is.Not.Null,
                "and it is a box: a convex mesh costs more to test against and buys nothing a bag needs");
        }

        [Test]
        public void ABagSweepsRatherThanStepping()
        {
            var bag = Configured();

            Assert.That(bag.Body.collisionDetectionMode, Is.Not.EqualTo(CollisionDetectionMode.Discrete),
                "at twelve metres a second a thrown bag moves most of its own length in one step, so " +
                "a discrete check has it on one side of a cart wall and then the other");
        }

        [Test]
        public void ABagStartsUndamaged()
        {
            Assert.That(Configured().Damaged, Is.False);
        }

        [Test]
        public void ABagWithNoProfileRefusesToPretendItIsFine()
        {
            var bag = m_Object.AddComponent<Bag>();

            Assert.That(bag.Profile, Is.Null,
                "a bag with no profile has to be visibly wrong rather than quietly weighing a " +
                "kilogram, because the second of those looks like a physics bug for the rest of the game");
        }
    }
}
