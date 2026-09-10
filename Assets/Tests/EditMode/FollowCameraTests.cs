using BelowTheWing.Crew;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// How far round and how far up the third-person camera will go.
    /// </summary>
    public sealed class FollowCameraTests
    {
        GameObject m_Object;
        FollowCamera m_Camera;

        [SetUp]
        public void SetUp()
        {
            m_Object = new GameObject("Camera");
            m_Camera = m_Object.AddComponent<FollowCamera>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Object);

        [Test]
        public void LookingUpHardStopsBeforeTheCameraGoesOverTheTop()
        {
            for (var i = 0; i < 200; i++)
            {
                m_Camera.Look(new Vector2(0f, -50f));
            }

            Assert.That(m_Camera.PitchDegrees, Is.LessThan(90f),
                "past vertical the view rolls over the subject and the world turns upside down");
        }

        [Test]
        public void LookingDownHardStopsBeforeTheCameraGoesUnderTheFloor()
        {
            for (var i = 0; i < 200; i++)
            {
                m_Camera.Look(new Vector2(0f, 50f));
            }

            Assert.That(m_Camera.PitchDegrees, Is.GreaterThan(-90f));
        }

        [Test]
        public void TheCameraCanBeSwungAllTheWayRoundTheSubject()
        {
            var start = m_Camera.YawDegrees;

            m_Camera.Look(new Vector2(400f, 0f));
            var quarterTurn = m_Camera.YawDegrees;
            m_Camera.Look(new Vector2(4000f, 0f));

            Assert.That(quarterTurn, Is.Not.EqualTo(start).Within(1e-3f), "the camera must turn at all");
            Assert.That(m_Camera.YawDegrees, Is.Not.EqualTo(quarterTurn).Within(1e-3f),
                "yaw has no limits: a player can walk round their own tractor");
        }

        [Test]
        public void MovingTheMouseFurtherTurnsTheCameraFurther()
        {
            m_Camera.Look(new Vector2(10f, 0f));
            var small = Mathf.Abs(Mathf.DeltaAngle(0f, m_Camera.YawDegrees));

            m_Camera.Look(new Vector2(30f, 0f));
            var larger = Mathf.Abs(Mathf.DeltaAngle(0f, m_Camera.YawDegrees));

            Assert.That(larger, Is.GreaterThan(small));
        }
    }
}
