using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
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
        public void HowFarAWorkerReachesComesFromTheirProfileNotTheComponent()
        {
            m_Profile.reachMetres = 3.5f;
            m_Profile.lookConeDegrees = 33f;

            var character = m_Object.AddComponent<CrewCharacter>();
            character.ConfigureBody(m_Profile);

            Assert.That(character.ReachMetres, Is.EqualTo(3.5f).Within(1e-4f),
                "the profile is the one asset that describes a ramp worker, and reach is part of that");
            Assert.That(character.ConeDegrees, Is.EqualTo(33f).Within(1e-4f));
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
