using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// What a person is made of, as far as the solver is concerned: nothing. The feet are the grip,
    /// and a body that is hit is carried off by momentum rather than sprung back by a bump.
    /// </summary>
    public sealed class CrewBodyTests
    {
        GameObject m_Object;
        CrewProfile m_Profile;

        [SetUp]
        public void SetUp()
        {
            m_Object = new GameObject("Crew");
            m_Object.AddComponent<Rigidbody>();
            m_Object.AddComponent<CapsuleCollider>();
            m_Profile = TestProfiles.CrewMember();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Object);
            Object.DestroyImmediate(m_Profile);
        }

        [Test]
        public void TheBodyHasNoFrictionOfItsOwnBecauseTheFeetAreTheGrip()
        {
            m_Object.AddComponent<CrewCharacter>().ConfigureBody(m_Profile);

            var skin = m_Object.GetComponent<CapsuleCollider>().sharedMaterial;
            Assert.That(skin, Is.Not.Null, "with no material the capsule has the default friction, which fights every step");
            Assert.That(skin.dynamicFriction, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(skin.staticFriction, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(skin.frictionCombine, Is.EqualTo(PhysicsMaterialCombine.Minimum),
                "averaged with a steel deck, the body would have half the deck's friction after all");
        }

        [Test]
        public void ThePersonDoesNotBounceOffABouncyVehicle()
        {
            m_Object.AddComponent<CrewCharacter>().ConfigureBody(m_Profile);

            var skin = m_Object.GetComponent<CapsuleCollider>().sharedMaterial;
            Assert.That(skin.bounciness, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(skin.bounceCombine, Is.EqualTo(PhysicsMaterialCombine.Minimum),
                "vehicles average their bounce with whatever they hit; a person has to say no in the " +
                "mode that beats averaging, or a 5 m/s hit springs them back at 1 m/s");
        }
    }
}
