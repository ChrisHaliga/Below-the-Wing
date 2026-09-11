using System;
using System.Collections.Generic;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Taking back a train whose owner has left the session.
    ///
    /// The failure this guards against is silent. Ownership of five vehicles does not move in one
    /// instant, so a request made while one of them is still mid-transfer is refused -- and nothing
    /// about the apron looks wrong at that moment. The train simply belongs to a machine that has
    /// gone. Nobody simulates it, and it either stands there for the rest of the session or keeps
    /// whatever speed it had when its owner vanished and rolls away with nobody able to stop it.
    /// </summary>
    public sealed class ReclaimingTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;
        VehicleProfile m_CartProfile;
        CartChain m_Train;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
            m_CartProfile = TestProfiles.Cart();
            m_Train = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 4, Vector3.zero);
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            UnityEngine.Object.DestroyImmediate(m_TractorProfile);
            UnityEngine.Object.DestroyImmediate(m_CartProfile);
        }

        /// <summary>A broker that refuses everything until it is told to start saying yes.</summary>
        sealed class StubbornBroker : IOwnershipBroker
        {
            public bool Granting { get; set; }
            public int TimesAsked { get; private set; }

            public ulong LocalClientId => 7;

            public ulong OwnerOf(VehicleController vehicle) => Granting ? 7ul : 3ul;

            public bool OwnedByUs(VehicleController vehicle) => Granting;

            public void RequestAll(IReadOnlyList<VehicleController> vehicles, Action<bool> onResult)
            {
                TimesAsked++;
                onResult(Granting);
            }

            public void HandBack(IReadOnlyList<VehicleController> vehicles)
            {
            }
        }

        [Test]
        public void ATrainThatComesBackFirstTimeIsNotChasedAgain()
        {
            var broker = new StubbornBroker() { Granting = true };
            var reclaiming = new Reclaiming(retryAfterSeconds: 1f);

            reclaiming.TakeBack(m_Train);
            reclaiming.Chase(broker, 0.02f);

            Assert.That(reclaiming.StillMissing, Is.Zero, "it came back, so there is nothing left to chase");
        }

        [Test]
        public void ARefusedReclaimIsAskedForAgain()
        {
            var broker = new StubbornBroker() { Granting = false };
            var reclaiming = new Reclaiming(retryAfterSeconds: 0.5f);

            reclaiming.TakeBack(m_Train);
            reclaiming.Chase(broker, 0.02f);
            var afterTheFirstRefusal = broker.TimesAsked;

            for (var i = 0; i < 60; i++)
            {
                reclaiming.Chase(broker, 0.02f);
            }

            Assert.That(afterTheFirstRefusal, Is.EqualTo(1), "it should have been asked for once to begin with");
            Assert.That(broker.TimesAsked, Is.GreaterThan(1),
                "asked once and given up on, a train refused because one member was mid-transfer belongs " +
                "to a machine that has left for the rest of the session, simulated by nobody");
        }

        [Test]
        public void ChasingStopsAsSoonAsTheTrainIsBack()
        {
            var broker = new StubbornBroker() { Granting = false };
            var reclaiming = new Reclaiming(retryAfterSeconds: 0.1f);

            reclaiming.TakeBack(m_Train);
            for (var i = 0; i < 20; i++)
            {
                reclaiming.Chase(broker, 0.02f);
            }

            broker.Granting = true;
            reclaiming.Chase(broker, 0.02f);
            var askedByThen = broker.TimesAsked;

            for (var i = 0; i < 40; i++)
            {
                reclaiming.Chase(broker, 0.02f);
            }

            Assert.That(reclaiming.StillMissing, Is.Zero);
            Assert.That(broker.TimesAsked, Is.EqualTo(askedByThen),
                "a train that is already back must stop being asked for, or the session owner spends " +
                "the rest of the game requesting things it already has");
        }

        [Test]
        public void ATrainIsNotChasedTwiceOver()
        {
            var broker = new StubbornBroker() { Granting = false };
            var reclaiming = new Reclaiming(retryAfterSeconds: 10f);

            reclaiming.TakeBack(m_Train);
            reclaiming.TakeBack(m_Train);

            Assert.That(reclaiming.StillMissing, Is.EqualTo(1),
                "two players leaving in quick succession can both name the same train, and chasing it " +
                "twice means two requests racing each other for the same five vehicles");
        }

        [Test]
        public void ATrainIsNotAskedForAgainBeforeItHasHadTimeToAnswer()
        {
            var broker = new StubbornBroker() { Granting = false };
            var reclaiming = new Reclaiming(retryAfterSeconds: 1f);

            reclaiming.TakeBack(m_Train);
            reclaiming.Chase(broker, 0.02f);

            for (var i = 0; i < 10; i++)
            {
                reclaiming.Chase(broker, 0.02f);
            }

            Assert.That(broker.TimesAsked, Is.EqualTo(1),
                "asking fifty times a second floods the machine being asked and races every answer " +
                "against the next request");
        }
    }
}
