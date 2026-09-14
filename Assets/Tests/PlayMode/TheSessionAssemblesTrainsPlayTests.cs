using System.Collections;
using BelowTheWing.Session;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class TheSessionAssemblesTrainsPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;
        VehicleProfile m_CartProfile;
        RampSession m_Session;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
            m_CartProfile = TestProfiles.Cart();

            var go = new GameObject("Ramp Session");
            go.AddComponent<NetworkObject>();
            m_Session = go.AddComponent<RampSession>();
            m_Apron.Track(m_Session);
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
            Object.DestroyImmediate(m_CartProfile);
        }

        TrainMember Vehicle(VehicleProfile profile, string name, Vector3 at)
        {
            var vehicle = m_Apron.AddVehicle(profile, name, at, Quaternion.identity);
            vehicle.gameObject.AddComponent<NetworkObject>();
            return vehicle.gameObject.AddComponent<TrainMember>();
        }

        [UnityTest]
        public IEnumerator TheSessionHooksUpWhateverItIsToldIsATrain()
        {
            var tractor = Vehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f));
            var cart = Vehicle(m_CartProfile, "Cart 1", new Vector3(0f, 1f, -4f));

            yield return null;

            m_Session.Arrived(tractor);
            m_Session.Arrived(cart);

            m_Session.Reshaped(new[] { tractor.Vehicle, cart.Vehicle }, trainIndex: 0);

            Assert.That(m_Session.Trains.Count, Is.EqualTo(1),
                $"the session was told these two are one train and made {m_Session.Trains.Count} of " +
                "them. Nothing else on a machine that does not own a train assembles one, so a " +
                "session that does not rebuild leaves every copy of every train as loose vehicles");

            var train = m_Session.Trains[0];

            Assert.That(train.Members.Count, Is.EqualTo(2));
            Assert.That(train.Leader, Is.SameAs(tractor.Vehicle),
                "and in the order it was given, because the front of a train is the end that tows");
            Assert.That(train.CouplingsEngaged, Is.True,
                "with its couplings made. A train the session groups but never hitches is a cart " +
                "dragged along by a correction rather than towed by the vehicle in front of it");
        }

        [UnityTest]
        public IEnumerator AVehicleTheSessionHasNotBeenToldAboutStandsAlone()
        {
            var tractor = Vehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f));
            var cart = Vehicle(m_CartProfile, "Cart 1", new Vector3(0f, 1f, -4f));

            yield return null;

            m_Session.Arrived(tractor);
            m_Session.Arrived(cart);

            Assert.That(m_Session.Trains.Count, Is.EqualTo(2),
                "two vehicles nobody has put in a train are two trains of one. Grouped anyway, a " +
                "tractor and a cart parked near each other would be hitched together by nothing " +
                "more than having arrived at the same time");
        }

        [UnityTest]
        public IEnumerator AVehicleThatLeavesIsTakenOutOfItsTrain()
        {
            var tractor = Vehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f));
            var cart = Vehicle(m_CartProfile, "Cart 1", new Vector3(0f, 1f, -4f));

            yield return null;

            m_Session.Arrived(tractor);
            m_Session.Arrived(cart);
            m_Session.Reshaped(new[] { tractor.Vehicle, cart.Vehicle }, trainIndex: 0);

            m_Session.Left(cart);

            Assert.That(m_Session.Trains.Count, Is.EqualTo(1));
            Assert.That(m_Session.Trains[0].Members.Count, Is.EqualTo(1),
                "a vehicle that has gone has to leave the train it was in. Left in, the train holds " +
                "a coupling to a destroyed body and every machine keeps towing something that is " +
                "not there");
        }
    }
}
