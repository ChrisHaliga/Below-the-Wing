using BelowTheWing.Cargo;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class HandUseTests
    {
        GameObject m_Thing;
        Collider m_Part;

        [SetUp]
        public void SetUp()
        {
            m_Thing = new GameObject("Thing");

            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.transform.SetParent(m_Thing.transform, worldPositionStays: false);
            m_Part = part.GetComponent<Collider>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Thing);

        [Test]
        public void AThingThatCanBeCarriedSaysSo()
        {
            m_Thing.AddComponent<HandUse>().As = HandUse.Category.Carry;

            Assert.That(HandUse.TryFind(m_Part, out var use), Is.True);
            Assert.That(use.As, Is.EqualTo(HandUse.Category.Carry));
        }

        [Test]
        public void AThingThatCanBeHeldOntoSaysSo()
        {
            m_Thing.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            Assert.That(HandUse.TryFind(m_Part, out var use), Is.True);
            Assert.That(use.As, Is.EqualTo(HandUse.Category.HoldOnto));
        }

        [Test]
        public void AThingThatSaysNothingCannotBeUsedAtAll()
        {
            Assert.That(HandUse.TryFind(m_Part, out _), Is.False,
                "the tarmac and other people have no say and so cannot be picked up or grabbed. " +
                "A hand that fell back to a default here would grab whatever it touched");
        }
    }
}
