using System.Collections;
using System.Collections.Generic;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// Getting out of a vehicle, as a body.
    ///
    /// A vehicle's origin is on the tarmac between its wheels, so "beside the vehicle" is not a
    /// place a person can stand: it is half a person underground. Stepping out has to put the body
    /// -- not just the transform -- on its feet beside the bodywork, moving as the vehicle was.
    /// </summary>
    public sealed class DismountPlayTests
    {
        TestApron m_Apron;
        CrewProfile m_Profile;
        VehicleProfile m_TractorProfile;
        CrewCharacter m_Crew;
        VehicleController m_Tractor;
        FixedIntent m_Wheel;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_Profile = TestProfiles.CrewMember();
            m_TractorProfile = TestProfiles.Tractor();

            m_Tractor = m_Apron.AddVehicle(
                m_TractorProfile, "Tug 1", new Vector3(0f, 0f, 2f), Quaternion.identity, TestShapes.Tractor());

            var crewGo = new GameObject("Crew");
            crewGo.transform.position = new Vector3(0f, 0.9f, 0f);
            m_Crew = crewGo.AddComponent<CrewCharacter>();
            m_Crew.ConfigureBody(m_Profile);
            m_Crew.TakeTheSeat(new RecordingBroker(grant: true), () => new List<VehicleController> { m_Tractor });
            m_Apron.Track(m_Crew);

            m_Wheel = new FixedIntent();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_Profile);
            Object.DestroyImmediate(m_TractorProfile);
        }

        float FeetHeight => m_Crew.transform.position.y - (m_Crew.HeightMetres * 0.5f);

        IEnumerator GetIn()
        {
            yield return Steps.Seconds(1f);
            m_Crew.Seat.Refresh();
            m_Crew.Seat.Toggle(m_Wheel);
            Assert.That(m_Crew.Seat.IsDriving, Is.True, "got in");
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator SteppingOutPutsYouOnYourFeetBesideTheVehicle()
        {
            yield return GetIn();

            m_Crew.Seat.Toggle(m_Wheel);
            yield return Steps.Seconds(2f);

            Assert.That(FeetHeight, Is.GreaterThan(-0.05f),
                $"feet at {FeetHeight:F2} m: a person put down at the vehicle's origin, which is on " +
                "the tarmac, is half underground, and the solver settles that either way -- popped " +
                "up or dropped through the apron");
            Assert.That(FeetHeight, Is.LessThan(0.2f), "and standing, not still falling");

            var sideways = Mathf.Abs(m_Crew.transform.position.x - m_Tractor.transform.position.x);
            Assert.That(sideways, Is.GreaterThan(m_Tractor.Shape.EnvelopeSizeMetres.x * 0.5f),
                "clear of the bodywork");
        }

        [UnityTest]
        public IEnumerator TheBodyIsAtTheDismountSpotOnTheVeryNextStep()
        {
            yield return GetIn();

            var expected = m_Crew.Seat.DismountPosition(m_Tractor, m_Crew.HeightMetres);
            m_Crew.Seat.Toggle(m_Wheel);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(Vector3.Distance(m_Crew.Body.position, expected), Is.LessThan(0.1f),
                $"the body is at {m_Crew.Body.position}, the dismount spot is {expected}. A position " +
                "written to the transform of a kinematic body is not where the body is until the " +
                "physics has caught up, and by then it has been made dynamic where it sat");
        }

        [UnityTest]
        public IEnumerator SteppingOutOfAMovingVehicleKeepsItsSpeed()
        {
            yield return GetIn();

            for (var i = 0; i < 25; i++)
            {
                m_Tractor.Body.linearVelocity = new Vector3(0f, 0f, 5f);
                yield return new WaitForFixedUpdate();
            }

            m_Crew.Seat.Toggle(m_Wheel);
            yield return new WaitForFixedUpdate();

            Assert.That(m_Crew.Body.linearVelocity.z, Is.EqualTo(5f).Within(1f),
                $"they left a tractor doing 5 m/s at {m_Crew.Body.linearVelocity.z:F1} m/s. A body " +
                "put down beside a moving vehicle without its speed has a five metre a second " +
                "tractor sliding past a person who was never moving, and whatever they were carrying " +
                "is torn out of their hands by the difference. Read on the step they step out: their " +
                "own feet take it off them over the next fifth of a second, which is them planting " +
                "their feet rather than the dismount having lost it");

            yield return Steps.Seconds(0.5f);
            Assert.That(m_Crew.Body.linearVelocity.magnitude, Is.LessThan(0.5f),
                "and half a second later their feet have brought them to a stop");
        }
    }
}
