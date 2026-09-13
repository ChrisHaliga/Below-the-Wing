using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
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

        [Test]
        public void OnFootTheCameraSitsInTheHead()
        {
            var subject = new GameObject("Subject").transform;
            subject.position = new Vector3(1f, 2f, 3f);
            m_Camera.Subject = subject;
            m_Camera.Frame(CameraFraming.OnFoot(0.75f));
            m_Camera.Look(new Vector2(200f, 0f));

            m_Camera.Place();

            Assert.That(m_Object.transform.position, Is.EqualTo(new Vector3(1f, 2.75f, 3f)).Using(Nearly.Within(1e-3f)),
                "first person is the camera at the eyes, not somewhere behind the head");
            Assert.That(Mathf.DeltaAngle(m_Object.transform.eulerAngles.y, m_Camera.YawDegrees), Is.EqualTo(0f).Within(1e-3f));
            Object.DestroyImmediate(subject.gameObject);
        }

        [Test]
        public void DrivingTheCameraSitsCloseBehindTheVehicle()
        {
            var subject = new GameObject("Subject").transform;
            subject.position = new Vector3(0f, 0f, 10f);
            m_Camera.Subject = subject;
            m_Camera.Frame(CameraFraming.Driving);

            m_Camera.Place();

            var aimedAt = subject.position + (Vector3.up * CameraFraming.Driving.HeightMetres);
            Assert.That(Vector3.Distance(m_Object.transform.position, aimedAt),
                Is.EqualTo(CameraFraming.Driving.DistanceMetres).Within(1e-3f));
            Assert.That(CameraFraming.Driving.DistanceMetres, Is.LessThanOrEqualTo(5f),
                "eight metres back, a tractor is a toy in the middle distance");
            Assert.That(Vector3.Angle(m_Object.transform.forward, aimedAt - m_Object.transform.position), Is.LessThan(1f),
                "and it looks at the vehicle");
            Object.DestroyImmediate(subject.gameObject);
        }

        [Test]
        public void PitchLimitsComeFromTheFraming()
        {
            m_Camera.Frame(CameraFraming.OnFoot(0.75f));
            for (var i = 0; i < 200; i++) m_Camera.Look(new Vector2(0f, -50f));
            Assert.That(m_Camera.PitchDegrees, Is.EqualTo(80f).Within(0.5f), "on foot you can look almost straight up");
            for (var i = 0; i < 200; i++) m_Camera.Look(new Vector2(0f, 50f));
            Assert.That(m_Camera.PitchDegrees, Is.EqualTo(-80f).Within(0.5f), "and down at your own feet");

            m_Camera.Frame(CameraFraming.Driving);
            for (var i = 0; i < 200; i++) m_Camera.Look(new Vector2(0f, -50f));
            Assert.That(m_Camera.PitchDegrees, Is.EqualTo(60f).Within(0.5f));
            for (var i = 0; i < 200; i++) m_Camera.Look(new Vector2(0f, 50f));
            Assert.That(m_Camera.PitchDegrees, Is.EqualTo(-20f).Within(0.5f),
                "behind a vehicle the camera cannot go under the tarmac");
        }

        [Test]
        public void TheFramingFollowsWhatThePlayerIsInChargeOf()
        {
            Assert.That(CameraFraming.For(driving: false, eyeMetresAboveOrigin: 0.75f), Is.EqualTo(CameraFraming.OnFoot(0.75f)));
            Assert.That(CameraFraming.For(driving: true, eyeMetresAboveOrigin: 0.75f), Is.EqualTo(CameraFraming.Driving));
        }
    }
}
