using System.Collections.Generic;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
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

        static RecordingBroker OwningEverything(CartChain train)
        {
            var broker = new RecordingBroker(grant: true, localClientId: 7);
            foreach (var member in train.Members)
            {
                broker.SetOwner(member, 7);
            }

            return broker;
        }

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
            var couplingBefore = before.CouplingBehind(0);
            Assert.That(couplingBefore, Is.Not.Null,
                "precondition: this machine is holding the train, so there is a coupling to preserve");

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
            var departing = described[4].Vehicle;

            Assert.That(departing.GetComponents<Joint>(), Is.Not.Empty,
                "precondition: the cart about to leave is hitched to the one in front of it");

            described.RemoveAt(4);
            m_Registry.Rebuild(described);

            Assert.That(m_Registry.Trains[0].Members.Count, Is.EqualTo(4));
            Assert.That(departing.GetComponents<Joint>(), Is.Empty,
                "a joint whose chain has been forgotten is a joint nothing will ever destroy");
        }

        [Test]
        public void ATrainWithAnyMemberBelongingToADepartedPlayerIsOneToTakeBack()
        {
            var described = OneTrain(trainIndex: 0);
            described.AddRange(OneTrain(trainIndex: 1, lane: 20f));
            m_Registry.Rebuild(described);

            var broker = new RecordingBroker(grant: true, localClientId: 7);
            foreach (var train in m_Registry.Trains)
            {
                foreach (var member in train.Members)
                {
                    broker.SetOwner(member, 7);
                }
            }

            broker.SetOwner(m_Registry.Trains[1].Members[3], 4);

            var toReclaim = m_Registry.TrainsHeldBy(4, broker);

            Assert.That(toReclaim.Count, Is.EqualTo(1));
            Assert.That(toReclaim[0], Is.SameAs(m_Registry.Trains[1]),
                "the train with the departed player's cart in it, and only that one");
        }

        [Test]
        public void ATrainNobodyWhoLeftWasHoldingIsLeftAlone()
        {
            m_Registry.Rebuild(OneTrain());
            var broker = OwningEverything(m_Registry.Trains[0]);

            Assert.That(m_Registry.TrainsHeldBy(4, broker), Is.Empty,
                "taking back a train nobody abandoned would drag it away from whoever is driving it");
        }

        [Test]
        public void ATrainIsHookedTogetherWhoeverOwnsIt()
        {
            m_Registry.Rebuild(OneTrain());

            Assert.That(m_Registry.Trains[0].CouplingsEngaged, Is.True,
                "what is built out of a lineup is decided by that lineup and nothing else. Gate it " +
                "on who owns the vehicles and a train nobody here owns is five loose boxes that " +
                "drift apart and shuffle about for the rest of the session, while a train changing " +
                "hands comes apart halfway through and is across the apron before the last answer " +
                "lands");
        }

    }
}
