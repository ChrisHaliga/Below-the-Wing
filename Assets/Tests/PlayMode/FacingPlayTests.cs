using System.Collections;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// Which way a person on foot faces: the way the camera looks, always.
    ///
    /// On foot the view is first person, so the body and the camera are the same thing turned by
    /// the same mouse. A body that turned toward its travel instead would have you walk sideways
    /// while looking ahead, and a body that turned at a rate would lag the view it is supposed to be.
    /// </summary>
    public sealed class FacingPlayTests
    {
        TestApron m_Apron;
        CrewProfile m_Profile;
        CrewCharacter m_Crew;
        FollowCamera m_Camera;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_Profile = TestProfiles.CrewMember();
            m_Crew = m_Apron.AddCrew(m_Profile, new Vector3(0f, 0.9f, 0f));

            m_Camera = m_Apron.Track(new GameObject("Camera").AddComponent<FollowCamera>());
            m_Camera.Subject = m_Crew.transform;
            m_Crew.Camera = m_Camera;
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_Profile);
        }

        /// <summary>Turns the camera until its yaw is close to the given heading.</summary>
        void LookTowards(float yawDegrees)
        {
            for (var i = 0; i < 100 && Mathf.Abs(Mathf.DeltaAngle(m_Camera.YawDegrees, yawDegrees)) > 0.5f; i++)
            {
                m_Camera.Look(new Vector2(Mathf.DeltaAngle(m_Camera.YawDegrees, yawDegrees), 0f));
            }
        }

        float BodyYaw => m_Crew.transform.eulerAngles.y;

        [UnityTest]
        public IEnumerator TheBodyFacesWhereTheCameraLooksOnTheSameStep()
        {
            yield return Steps.Seconds(0.5f);

            LookTowards(90f);
            yield return new WaitForFixedUpdate();

            Assert.That(Mathf.DeltaAngle(BodyYaw, m_Camera.YawDegrees), Is.EqualTo(0f).Within(0.5f),
                $"body at {BodyYaw:F0}, camera at {m_Camera.YawDegrees:F0}. In first person the body " +
                "is the camera; a step of lag is a view that swims when you turn");

            LookTowards(-135f);
            yield return new WaitForFixedUpdate();

            Assert.That(Mathf.DeltaAngle(BodyYaw, m_Camera.YawDegrees), Is.EqualTo(0f).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator WalkingSidewaysIsAStrafeNotATurn()
        {
            yield return Steps.Seconds(0.5f);
            LookTowards(0f);

            m_Crew.IntentSource = new HeldKeys(new Vector2(1f, 0f));
            yield return Steps.Seconds(1f);

            Assert.That(m_Crew.Body.linearVelocity.x, Is.GreaterThan(2f), "moving to the camera's right");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(BodyYaw, 0f)), Is.LessThan(0.5f),
                $"and still facing the camera's way, not turned to {BodyYaw:F0} to face the travel");
        }

        [UnityTest]
        public IEnumerator WalkingForwardGoesTheWayTheCameraLooks()
        {
            yield return Steps.Seconds(0.5f);
            LookTowards(90f);

            m_Crew.IntentSource = new HeldKeys(new Vector2(0f, 1f));
            yield return Steps.Seconds(1f);

            Assert.That(m_Crew.Body.linearVelocity.x, Is.GreaterThan(2f), "camera looks east; forward is east");
            Assert.That(Mathf.Abs(m_Crew.Body.linearVelocity.z), Is.LessThan(0.5f));
        }

        [UnityTest]
        public IEnumerator ACharacterWithNoCameraKeepsItsOwnFacing()
        {
            m_Crew.Camera = null;
            m_Crew.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            yield return Steps.Seconds(0.5f);

            m_Crew.IntentSource = new HeldKeys(new Vector2(1f, 0f));
            yield return Steps.Seconds(1f);

            Assert.That(Mathf.DeltaAngle(BodyYaw, 30f), Is.EqualTo(0f).Within(0.5f),
                "a copy of somebody else's character, or a test's, has nothing here deciding its yaw");
        }
    }
}
