using System.Collections.Generic;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class FollowCameraTests
    {
        GameObject m_Object;
        FollowCamera m_Camera;

        readonly List<GameObject> m_Subjects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            m_Object = new GameObject("Camera");
            m_Camera = m_Object.AddComponent<FollowCamera>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Object);

            foreach (var subject in m_Subjects)
            {
                Object.DestroyImmediate(subject);
            }

            m_Subjects.Clear();
        }

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
            var subject = ASubjectAt(new Vector3(1f, 2f, 3f));
            m_Camera.Subject = subject;
            m_Camera.Frame(CameraFraming.OnFoot(0.75f));
            m_Camera.Look(new Vector2(200f, 0f));

            m_Camera.Place();

            Assert.That(m_Object.transform.position, Is.EqualTo(new Vector3(1f, 2.75f, 3f)).Using(Nearly.Within(1e-3f)),
                "first person is the camera at the eyes, not somewhere behind the head");
            Assert.That(Mathf.DeltaAngle(m_Object.transform.eulerAngles.y, m_Camera.YawDegrees), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void DrivingTheCameraSitsCloseBehindTheVehicle()
        {
            var subject = ASubjectAt(new Vector3(0f, 0f, 10f));
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
        }

        [Test]
        public void PitchLimitsComeFromTheFraming()
        {
            foreach (var framing in new[] { CameraFraming.OnFoot(0.75f), CameraFraming.Driving })
            {
                m_Camera.Frame(framing);

                LookAllTheWay(up: true);
                Assert.That(m_Camera.PitchDegrees, Is.EqualTo(framing.MaxPitchDegrees).Within(0.5f),
                    "looking up stops where the framing says it stops, not at a figure typed here");

                LookAllTheWay(up: false);
                Assert.That(m_Camera.PitchDegrees, Is.EqualTo(framing.MinPitchDegrees).Within(0.5f),
                    "and looking down does too, so behind a vehicle the camera cannot go under the tarmac");
            }

            Assert.That(CameraFraming.Driving.MinPitchDegrees,
                Is.GreaterThan(CameraFraming.OnFoot(0.75f).MinPitchDegrees),
                "a camera behind a vehicle has the tarmac in the way, so it cannot look as far down " +
                "as one at a standing person's eyes");
        }

        [Test]
        public void TheFramingFollowsWhatThePlayerIsInChargeOf()
        {
            Assert.That(CameraFraming.For(driving: false, eyeMetresAboveOrigin: 0.75f), Is.EqualTo(CameraFraming.OnFoot(0.75f)));
            Assert.That(CameraFraming.For(driving: true, eyeMetresAboveOrigin: 0.75f), Is.EqualTo(CameraFraming.Driving));
        }
    
        Transform ASubjectAt(Vector3 position)
        {
            var subject = new GameObject("Subject").transform;
            subject.position = position;
            m_Subjects.Add(subject.gameObject);

            return subject;
        }

        void LookAllTheWay(bool up)
        {
            for (var i = 0; i < 200; i++)
            {
                m_Camera.Look(new Vector2(0f, up ? -50f : 50f));
            }
        }
    }
}
