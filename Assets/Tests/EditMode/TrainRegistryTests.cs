using System.Collections.Generic;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Working out which vehicles form which trains, and which of them this machine is holding.
    ///
    /// This used to live inside the networked spawner where nothing could reach it, and every one
    /// of these cases was wrong: a machine that had not built the apron believed every tractor was
    /// standing alone, a train being driven had its couplings destroyed whenever anybody joined,
    /// and a train partway through changing hands was simulated by nobody at all.
    /// </summary>
    public sealed class TrainRegistryTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;
        VehicleProfile m_CartProfile;
        TrainRegistry m_Registry;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
            m_CartProfile = TestProfiles.Cart();
            m_Registry = new TrainRegistry(ChainJointSettings.Default);
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
            Object.DestroyImmediate(m_CartProfile);
        }

        /// <summary>A tractor and four carts, described the way every machine is told about them.</summary>
        List<TrainMembership> OneTrain(int trainIndex = 0, float lane = 0f)
        {
            var described = new List<TrainMembership>
            {
                new TrainMembership(
                    m_Apron.AddVehicle(m_TractorProfile, $"Tug {trainIndex + 1}", new Vector3(lane, 0f, 0f), Quaternion.identity),
                    trainIndex,
                    0)
            };

            for (var c = 0; c < 4; c++)
            {
                var at = new Vector3(lane, 0f, -3.6f * (c + 1));
                described.Add(new TrainMembership(
                    m_Apron.AddVehicle(m_CartProfile, $"Cart {trainIndex + 1}-{c + 1}", at, Quaternion.identity),
                    trainIndex,
                    c + 1));
            }

            return described;
        }

        [Test]
        public void VehiclesDescribedAsOneTrainBecomeOneTrainInOrder()
        {
            m_Registry.Rebuild(OneTrain());

            Assert.That(m_Registry.Trains.Count, Is.EqualTo(1));
            var train = m_Registry.Trains[0];
            Assert.That(train.Members.Count, Is.EqualTo(5));
            Assert.That(train.Leader.Profile, Is.SameAs(m_TractorProfile), "the tractor leads");
            for (var i = 1; i < 5; i++)
            {
                Assert.That(train.Members[i].DisplayName, Is.EqualTo($"Cart 1-{i}"),
                    "carts must come out in the order they were described, not the order they arrived");
            }
        }

        [Test]
        public void DescriptionsArrivingOutOfOrderStillProduceTheRightTrain()
        {
            var described = OneTrain();
            described.Reverse();

            m_Registry.Rebuild(described);

            var train = m_Registry.Trains[0];
            Assert.That(train.Leader.DisplayName, Is.EqualTo("Tug 1"),
                "objects arrive over the network in whatever order they arrive; place in train decides, " +
                "not arrival");
        }

        [Test]
        public void TwoTrainsStayApart()
        {
            var described = OneTrain(trainIndex: 0);
            described.AddRange(OneTrain(trainIndex: 1, lane: 20f));

            m_Registry.Rebuild(described);

            Assert.That(m_Registry.Trains.Count, Is.EqualTo(2));
            foreach (var train in m_Registry.Trains)
            {
                Assert.That(train.Members.Count, Is.EqualTo(5));
            }
        }

        [Test]
        public void AVehicleInNoTrainIsATrainOfItself()
        {
            var lone = m_Apron.AddVehicle(m_TractorProfile, "Tug 9", new Vector3(50f, 0f, 0f), Quaternion.identity);

            m_Registry.Rebuild(new List<TrainMembership> { new TrainMembership(lone) });

            Assert.That(m_Registry.Trains.Count, Is.EqualTo(1));
            Assert.That(m_Registry.Trains[0].Members.Count, Is.EqualTo(1));
        }

        [Test]
        public void RebuildingWithTheSameMembershipLeavesTheTrainAlone()
        {
            var described = OneTrain();
            m_Registry.Rebuild(described);
            var before = m_Registry.Trains[0];
            var broker = new RecordingBroker(grant: true);
            m_Registry.TakeUpWhatWeOwn(broker);
            var couplingBefore = before.CouplingBehind(0);

            m_Registry.Rebuild(described);

            Assert.That(m_Registry.Trains[0], Is.SameAs(before),
                "a train nobody has touched must survive somebody else's character spawning");
            Assert.That(m_Registry.Trains[0].CouplingBehind(0), Is.SameAs(couplingBefore),
                "a hinge re-created part way through a turn takes its jackknife limits from the angle " +
                "the cart happens to be at, so a driven train would quietly change how far it can bend");
        }

        [Test]
        public void ATrainThatLosesACartIsRebuiltAndItsOldCouplingsGoAway()
        {
            var described = OneTrain();
            m_Registry.Rebuild(described);
            m_Registry.TakeUpWhatWeOwn(new RecordingBroker(grant: true));
            var departing = described[4].Vehicle;

            described.RemoveAt(4);
            m_Registry.Rebuild(described);

            Assert.That(m_Registry.Trains[0].Members.Count, Is.EqualTo(4));
            Assert.That(departing.GetComponents<Joint>(), Is.Empty,
                "a joint whose chain has been forgotten is a joint nothing will ever destroy");
        }

        [Test]
        public void ATrainThisMachineOwnsOutrightIsHeldTogetherAndSimulated()
        {
            m_Registry.Rebuild(OneTrain());
            var broker = new RecordingBroker(grant: true, localClientId: 7);
            foreach (var member in m_Registry.Trains[0].Members)
            {
                broker.SetOwner(member, 7);
            }

            m_Registry.TakeUpWhatWeOwn(broker);

            Assert.That(m_Registry.Trains[0].CouplingsEngaged, Is.True);
            foreach (var member in m_Registry.Trains[0].Members)
            {
                Assert.That(member.Simulated, Is.True);
            }
        }

        [Test]
        public void ATrainThisMachineOwnsNoneOfIsLetGoOf()
        {
            m_Registry.Rebuild(OneTrain());
            var broker = new RecordingBroker(grant: true, localClientId: 7);
            foreach (var member in m_Registry.Trains[0].Members)
            {
                broker.SetOwner(member, 3);
            }

            m_Registry.TakeUpWhatWeOwn(broker);

            Assert.That(m_Registry.Trains[0].CouplingsEngaged, Is.False,
                "hinges between bodies another machine is integrating have one end nothing here can move");
            foreach (var member in m_Registry.Trains[0].Members)
            {
                Assert.That(member.Simulated, Is.False);
            }
        }

        [Test]
        public void ATrainPartWayThroughChangingHandsIsLeftExactlyAsItWas()
        {
            m_Registry.Rebuild(OneTrain());
            var broker = new RecordingBroker(grant: true, localClientId: 7);
            var train = m_Registry.Trains[0];
            foreach (var member in train.Members)
            {
                broker.SetOwner(member, 7);
            }

            m_Registry.TakeUpWhatWeOwn(broker);
            Assert.That(train.CouplingsEngaged, Is.True, "precondition: we were holding it");

            // Two of the five have gone across. The rest are still ours.
            broker.SetOwner(train.Members[3], 3);
            broker.SetOwner(train.Members[4], 3);
            m_Registry.TakeUpWhatWeOwn(broker);

            Assert.That(train.CouplingsEngaged, Is.True,
                "ownership of five vehicles does not move in one instant. If both machines let go on " +
                "a half-answer, the train is simulated by nobody until the last response lands and it " +
                "sits down on its bodywork");
            Assert.That(train.Members[0].Simulated, Is.True);
        }

        [Test]
        public void ATrainIsPickedUpOnlyOnceTheLastMemberHasArrived()
        {
            m_Registry.Rebuild(OneTrain());
            var broker = new RecordingBroker(grant: true, localClientId: 7);
            var train = m_Registry.Trains[0];
            foreach (var member in train.Members)
            {
                broker.SetOwner(member, 3);
            }

            m_Registry.TakeUpWhatWeOwn(broker);

            for (var i = 0; i < 4; i++)
            {
                broker.SetOwner(train.Members[i], 7);
            }

            m_Registry.TakeUpWhatWeOwn(broker);
            Assert.That(train.CouplingsEngaged, Is.False, "four out of five is not a train");

            broker.SetOwner(train.Members[4], 7);
            m_Registry.TakeUpWhatWeOwn(broker);

            Assert.That(train.CouplingsEngaged, Is.True);
        }
    }
}
