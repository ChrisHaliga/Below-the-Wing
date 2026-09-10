using System.Collections;
using System.Collections.Generic;
using BelowTheWing.Apron;
using BelowTheWing.Diagnostics;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// Being able to tell what you are looking at, and what physics is doing.
    ///
    /// Everything on the apron is a grey primitive, so the labels are the only thing separating a
    /// cart from a tractor. The readout covers the opposite problem: things that are true but have
    /// no appearance at all, like whether a train has actually settled or is quietly jittering.
    /// </summary>
    public sealed class ApronLegibilityPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;
        VehicleProfile m_CartProfile;
        Camera m_Camera;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
            m_CartProfile = TestProfiles.Cart();
            m_Camera = m_Apron.Track(new GameObject("Camera").AddComponent<Camera>());

            // Labels turn to face the main camera, which is how the game identifies the one a
            // player is looking through.
            m_Camera.tag = "MainCamera";
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
            Object.DestroyImmediate(m_CartProfile);
        }

        static IEnumerator Step(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator EveryVehicleCarriesALabelSayingWhatItIs()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);

            var label = WorldLabel.Attach(tractor.transform, tractor.DisplayName, 3f);
            yield return null;

            Assert.That(label, Is.Not.Null);
            Assert.That(label.Text, Is.EqualTo("Tug 1"));
            Assert.That(label.transform.position.y, Is.GreaterThan(tractor.transform.position.y),
                "the label floats above the thing it names");
        }

        [UnityTest]
        public IEnumerator ALabelTurnsToFaceWhoeverIsLookingAtIt()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);
            var label = WorldLabel.Attach(tractor.transform, "Tug 1", 3f);

            m_Camera.transform.position = new Vector3(20f, 4f, 0f);
            m_Camera.transform.LookAt(label.transform);
            yield return null;
            yield return null;
            var fromTheSide = Vector3.Angle(label.transform.forward, label.transform.position - m_Camera.transform.position);

            m_Camera.transform.position = new Vector3(0f, 4f, -20f);
            m_Camera.transform.LookAt(label.transform);
            yield return null;
            yield return null;
            var fromBehind = Vector3.Angle(label.transform.forward, label.transform.position - m_Camera.transform.position);

            Assert.That(fromTheSide, Is.LessThan(5f), "text edge-on to the camera cannot be read");
            Assert.That(fromBehind, Is.LessThan(5f), "and it must keep turning as the camera moves");
        }

        [UnityTest]
        public IEnumerator ALabelIsSomethingToReadRatherThanSomethingToDriveInto()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", new Vector3(0f, 1f, 0f), Quaternion.identity);
            var before = tractor.GetComponentsInChildren<Collider>().Length;

            var label = WorldLabel.Attach(tractor.transform, "Tug 1", 3f);
            yield return null;

            Assert.That(label.GetComponentsInChildren<Collider>(), Is.Empty);
            Assert.That(tractor.GetComponentsInChildren<Collider>().Length, Is.EqualTo(before),
                "hanging a label on a vehicle must not change its shape");
        }

        [UnityTest]
        public IEnumerator TheReadoutShowsFewerBodiesAwakeOnceATrainHasSettled()
        {
            var train = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 4, new Vector3(0f, 1.5f, 0f));
            var readout = m_Apron.Track(new GameObject("Readout").AddComponent<RampReadout>());
            readout.Observe(new List<CartChain> { train }, new RecordingBroker(grant: true));

            train.Leader.IntentSource = new FixedIntent(throttle: 1f);
            yield return Step(2f);
            var whileDriving = readout.AwakeBodyCount;

            train.Leader.IntentSource = new FixedIntent();
            yield return Step(12f);
            var afterSettling = readout.AwakeBodyCount;

            Assert.That(whileDriving, Is.GreaterThan(0), "a train being driven has bodies awake");
            Assert.That(afterSettling, Is.LessThan(whileDriving),
                "a train that never goes back to sleep costs solver time and bandwidth for ever");
        }

        [UnityTest]
        public IEnumerator TheReadoutSaysHowLongPhysicsIsTaking()
        {
            var train = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 4, new Vector3(0f, 1.5f, 0f));
            var readout = m_Apron.Track(new GameObject("Readout").AddComponent<RampReadout>());
            readout.Observe(new List<CartChain> { train }, new RecordingBroker(grant: true));

            yield return Step(1f);

            Assert.That(readout.PhysicsStepMilliseconds, Is.GreaterThan(0f));
        }
    }
}
