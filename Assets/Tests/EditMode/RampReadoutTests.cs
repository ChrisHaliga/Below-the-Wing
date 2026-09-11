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

        /// <summary>A vehicle that reports whatever drift it is told to report.</summary>
        sealed class DriftingCopy : MonoBehaviour, IKeepsInStep
        {
            public float MetresOutOfPlace { get; set; }
        }

        [Test]
        public void TheReadoutSaysWhetherThisMachineIsInChargeOfEachTrain()
        {
            var broker = new RecordingBroker(grant: true, localClientId: 7);
            foreach (var member in m_Train.Members)
            {
                broker.SetOwner(member, 3);
            }

            m_Train.Leader.OursToMove = false;
            m_Readout.Observe(new[] { m_Train }, broker);

            Assert.That(m_Readout.OwnershipLines[0], Does.Contain("theirs"),
                "which machine is in charge of a train cannot be seen by looking at the apron, and it " +
                "is the first thing worth knowing when two screens disagree");
        }

        [Test]
        public void TheReadoutSaysHowFarOutOfPlaceATrainHasDrifted()
        {
            var broker = new RecordingBroker(grant: true, localClientId: 7);
            foreach (var member in m_Train.Members)
            {
                broker.SetOwner(member, 3);
            }

            m_Train.Leader.OursToMove = false;
            m_Train.Members[2].gameObject.AddComponent<DriftingCopy>().MetresOutOfPlace = 3.75f;
            m_Readout.Observe(new[] { m_Train }, broker);

            Assert.That(m_Readout.OwnershipLines[0], Does.Contain("3.75"),
                "without a number, whether a disagreement is a tuning problem or an architectural one " +
                "is decided by eye, and both screens look perfectly reasonable on their own");
        }

        [Test]
        public void TheWorstPlacedVehicleInATrainIsTheOneReported()
        {
            m_Train.Members[1].gameObject.AddComponent<DriftingCopy>().MetresOutOfPlace = 0.2f;
            m_Train.Members[4].gameObject.AddComponent<DriftingCopy>().MetresOutOfPlace = 6f;

            Assert.That(RampReadout.WorstDrift(m_Train), Is.EqualTo(6f).Within(1e-3f),
                "a train comes apart one vehicle at a time, and an average over five hides the one " +
                "that has gone");
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
