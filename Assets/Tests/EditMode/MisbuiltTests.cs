using BelowTheWing.Net;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class MisbuiltTests
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

        [Test]
        public void AVehicleWithNoShapeIsRefusedRatherThanLeftToFallThroughTheApron()
        {
            var bare = m_Apron.Track(new GameObject("No shape").AddComponent<VehicleController>());

            var refused = Assert.Throws<MisbuiltException>(() => bare.Configure(m_TractorProfile, "No shape"));

            Assert.That(refused.Message, Does.Contain("No shape").And.Contain("shape"),
                "the exception has to name the object and the part it is missing, or the person " +
                "reading the console is left grepping prefabs");
            Assert.That(bare.enabled, Is.False,
                "a component that threw out of its configuration must not go on running FixedUpdate " +
                "against the state it never finished building");
        }

        [Test]
        public void AVehicleWithItsCentreOfMassOnTheGroundIsRefused()
        {
            m_TractorProfile.centerOfMassOffset = Vector3.zero;

            Assert.Throws<MisbuiltException>(
                () => m_Apron.AddVehicle(m_TractorProfile, "Low", Vector3.zero, Quaternion.identity),
                "a centre of mass at or below the origin makes weight transfer work backwards, and " +
                "a vehicle that runs anyway with a log line is the fault the rule exists to stop");
        }

        [Test]
        public void ACouplingBetweenVehiclesWithNoHitchIsRefusedRatherThanRegisteredAsATrain()
        {
            var noHitch = TestShapes.BoxVehicle();
            noHitch.RearCouplingLocal = null;

            var inFront = m_Apron.AddVehicle(m_TractorProfile, "Tug", Vector3.zero, Quaternion.identity, noHitch);
            var behind = m_Apron.AddVehicle(m_CartProfile, "Cart", new Vector3(0f, 0f, -5f), Quaternion.identity);

            var refused = Assert.Throws<MisbuiltException>(
                () => CartChain.Couple(new[] { inFront, behind }, ChainJointSettings.Default));

            Assert.That(refused.Message, Does.Contain("Tug"),
                "a train registered with a member that is physically unconnected is one the " +
                "session then corrects and replicates as if it were whole");
        }

        [Test]
        public void StartingALocalSessionWithNoNetworkManagerIsRefusedRatherThanReportedFalse()
        {
            Assert.Throws<MisbuiltException>(() => LocalSession.Start(null),
                "false is what a refused StartHost returns; a scene with no NetworkManager is not " +
                "a refusal, it is a scene that was built wrong");
        }
    }
}
