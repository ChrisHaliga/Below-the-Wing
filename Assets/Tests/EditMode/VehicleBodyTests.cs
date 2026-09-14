using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class VehicleBodyTests
    {
        GameObject m_Vehicle;
        VehicleProfile m_Profile;

        [SetUp]
        public void SetUp()
        {
            m_Vehicle = new GameObject("Cart");
            m_Vehicle.AddComponent<Rigidbody>();
            m_Profile = TestProfiles.Cart();
            TestShapes.On(m_Vehicle, TestShapes.Cart());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Vehicle);
            Object.DestroyImmediate(m_Profile);
        }

        [Test]
        public void EverySolidPartBouncesAsMuchAsTheProfileSays()
        {
            m_Profile.bounciness = 0.37f;

            m_Vehicle.AddComponent<VehicleController>().Configure(m_Profile, "Cart 1");

            var parts = m_Vehicle.GetComponentsInChildren<Collider>();
            Assert.That(parts, Is.Not.Empty);
            foreach (var part in parts)
            {
                Assert.That(part.sharedMaterial, Is.Not.Null, $"{part.name} has no material, so it bumps like clay");
                Assert.That(part.sharedMaterial.bounciness, Is.EqualTo(0.37f).Within(1e-4f), part.name);
                Assert.That(part.sharedMaterial.bounceCombine, Is.EqualTo(PhysicsMaterialCombine.Average),
                    "taking the greater bounce of the pair would make a soft bag spring off a cart's lip; " +
                    "averaging gives vehicles the full figure against each other and lets a bag say no");
            }
        }
    }
}
