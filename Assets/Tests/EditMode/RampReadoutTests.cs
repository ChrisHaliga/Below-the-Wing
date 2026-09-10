using System.Collections.Generic;
using BelowTheWing.Diagnostics;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Reporting who is simulating what.
    ///
    /// Ownership of a train has no appearance. Taking one over looks exactly like not taking one
    /// over until a second player turns up and the couplings start behaving strangely, so the only
    /// way to see it go wrong early is to have it written on the screen.
    /// </summary>
    public sealed class RampReadoutTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;
        VehicleProfile m_CartProfile;
        RampReadout m_Readout;
        CartChain m_Train;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
            m_CartProfile = TestProfiles.Cart();
            m_Train = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 4, Vector3.zero);
            m_Readout = m_Apron.Track(new GameObject("Readout").AddComponent<RampReadout>());
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
            Object.DestroyImmediate(m_CartProfile);
        }

        [Test]
        public void TheReadoutNamesWhoIsSimulatingEachTrain()
        {
            var broker = new RecordingBroker(grant: true, localClientId: 7);
            foreach (var member in m_Train.Members)
            {
                broker.SetOwner(member, 7);
            }

            m_Readout.Observe(new List<CartChain> { m_Train }, broker);

            Assert.That(m_Readout.OwnershipLines.Count, Is.EqualTo(1));
            Assert.That(m_Readout.OwnershipLines[0], Does.Contain(m_Train.Leader.DisplayName));
            Assert.That(m_Readout.OwnershipLines[0], Does.Contain("owner 7"),
                "the owner has to be named. A bare \"7\" would also be satisfied by the member count " +
                "printed alongside it, and would pass with the owner reported as anybody at all");
        }

        [Test]
        public void TheReadoutSaysSoWhenATrainIsSplitBetweenTwoMachines()
        {
            var broker = new RecordingBroker(grant: true, localClientId: 7);
            foreach (var member in m_Train.Members)
            {
                broker.SetOwner(member, 7);
            }

            broker.SetOwner(m_Train.Members[3], 9);
            m_Readout.Observe(new List<CartChain> { m_Train }, broker);

            Assert.That(m_Readout.OwnershipLines[0], Does.Contain("split").IgnoreCase,
                "a train owned by two machines is the failure this readout exists to catch, " +
                "so it must not be reported as though everything were fine");
        }

        [Test]
        public void TheReadoutFollowsOwnershipWhenATrainChangesHands()
        {
            var broker = new RecordingBroker(grant: true, localClientId: 7);
            foreach (var member in m_Train.Members)
            {
                broker.SetOwner(member, 9);
            }

            m_Readout.Observe(new List<CartChain> { m_Train }, broker);
            var beforeTakeover = m_Readout.OwnershipLines[0];

            m_Train.RequestOwnership(broker, _ => { });

            Assert.That(beforeTakeover, Does.Contain("owner 9"));
            Assert.That(m_Readout.OwnershipLines[0], Does.Contain("owner 7"),
                "the readout must reflect the takeover, not a snapshot from when it was wired up");
        }
    }
}
