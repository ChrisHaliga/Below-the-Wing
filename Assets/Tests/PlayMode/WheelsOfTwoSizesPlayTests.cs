using System.Collections;
using System.Collections.Generic;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class WheelsOfTwoSizesPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_Profile;
        VehicleController m_Tractor;
        VehicleShape m_Shape;
        WheelLook m_Look;
        Transform[] m_Wheels;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_Profile = TestProfiles.Tractor();

            m_Tractor = m_Apron.AddVehicle(
                m_Profile, "Tug 1", new Vector3(0f, 0f, 0f), Quaternion.identity, TestShapes.Tractor());

            m_Shape = m_Tractor.GetComponent<VehicleShape>();

            var wheels = new List<Transform>();
            for (var i = 0; i < m_Shape.Wheels.Count; i++)
            {
                var wheel = new GameObject($"Wheel {i + 1}").transform;
                wheel.SetParent(m_Tractor.transform, worldPositionStays: false);
                wheel.localPosition = m_Shape.Wheels[i].CentreLocal;
                wheels.Add(wheel);
            }

            m_Wheels = wheels.ToArray();
            m_Look = m_Tractor.gameObject.AddComponent<WheelLook>();
            m_Look.Watch(wheels);
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_Profile);
        }

        [UnityTest]
        public IEnumerator EveryWheelStandsOnTheTarmacWhateverSizeItIs()
        {
            yield return Steps.Seconds(2f);

            for (var i = 0; i < m_Wheels.Length; i++)
            {
                var radius = m_Shape.Wheels[i].RadiusMetres;
                var bottom = m_Wheels[i].position.y - radius;

                Assert.That(bottom, Is.EqualTo(0f).Within(0.01f),
                    $"wheel {i + 1} is {radius:F4} m in radius and its underside is at {bottom:F3} m. " +
                    "Hung from a height worked out from some other wheel's size, the small ones " +
                    "float and the big ones are buried by the difference between them");
            }
        }

        [UnityTest]
        public IEnumerator EachWheelTurnsAtRoadSpeedForItsOwnSize()
        {
            yield return Steps.Seconds(1f);

            const float roadSpeed = 5f;
            var before = new float[m_Wheels.Length];
            for (var i = 0; i < m_Wheels.Length; i++)
            {
                before[i] = m_Look.TurnedDegrees(i);
            }

            var until = Time.time + 1f;
            while (Time.time < until)
            {
                m_Tractor.Body.linearVelocity = new Vector3(0f, m_Tractor.Body.linearVelocity.y, roadSpeed);
                yield return new WaitForFixedUpdate();
            }

            for (var i = 0; i < m_Wheels.Length; i++)
            {
                var radius = m_Shape.Wheels[i].RadiusMetres;
                var expected = roadSpeed / radius * Mathf.Rad2Deg;
                var turned = m_Look.TurnedDegrees(i) - before[i];

                Assert.That(turned, Is.EqualTo(expected).Within(expected * 0.05f),
                    $"wheel {i + 1} is {radius:F4} m in radius and turned {turned:F0} degrees in a " +
                    $"second at {roadSpeed} m/s, where rolling would be {expected:F0}. A wheel " +
                    "turning at anything but its own road speed reads as the vehicle skidding");
            }
        }

        [UnityTest]
        public IEnumerator TheSmallWheelsTurnFurtherThanTheBigOnesOverTheSameGround()
        {
            yield return Steps.Seconds(1f);

            var front = Corner(mostForward: true);
            var rear = Corner(mostForward: false);

            var before = (Front: m_Look.TurnedDegrees(front), Rear: m_Look.TurnedDegrees(rear));

            var until = Time.time + 1f;
            while (Time.time < until)
            {
                m_Tractor.Body.linearVelocity = new Vector3(0f, m_Tractor.Body.linearVelocity.y, 5f);
                yield return new WaitForFixedUpdate();
            }

            var turnedFront = m_Look.TurnedDegrees(front) - before.Front;
            var turnedRear = m_Look.TurnedDegrees(rear) - before.Rear;
            var ratio = m_Shape.Wheels[rear].RadiusMetres / m_Shape.Wheels[front].RadiusMetres;

            Assert.That(turnedFront / turnedRear, Is.EqualTo(ratio).Within(0.05f),
                $"the front wheels turned {turnedFront:F0} degrees and the rear {turnedRear:F0} over " +
                $"the same ground, a ratio of {turnedFront / turnedRear:F2}. Their radii differ by " +
                $"a factor of {ratio:F2}, and one shared radius makes that ratio exactly 1");
        }

        [UnityTest]
        public IEnumerator ItDrivesRoundACornerOnItsOwnWheelsWithoutFallingOver()
        {
            yield return Steps.Seconds(1f);

            var from = m_Tractor.transform.position;
            var heading = m_Tractor.transform.eulerAngles.y;

            m_Tractor.IntentSource = new FixedIntent(steer: 1f, throttle: 1f);
            yield return Steps.Seconds(3f);

            Assert.That(Vector3.Distance(m_Tractor.transform.position, from), Is.GreaterThan(5f),
                "three seconds at full throttle and it has not gone five metres");

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(m_Tractor.transform.eulerAngles.y, heading)),
                Is.GreaterThan(45f),
                "on full lock for three seconds it has barely changed heading, so its steered wheels " +
                "are producing nothing");

            Assert.That(m_Tractor.transform.up.y, Is.GreaterThan(0.9f),
                "and it is still on its wheels rather than on its roof: this mass is carried 0.55 m " +
                "up over a 1.20 m track, which is not much to corner hard on");

            for (var i = 0; i < m_Wheels.Length; i++)
            {
                var bottom = m_Wheels[i].position.y - m_Shape.Wheels[i].RadiusMetres;
                Assert.That(bottom, Is.EqualTo(0f).Within(0.06f),
                    $"wheel {i + 1} is {bottom:F3} m off the tarmac mid-corner, so what a player " +
                    "sees is a tractor cornering on wheels that are not touching the ground");
            }
        }

        int Corner(bool mostForward)
        {
            var best = 0;
            for (var i = 1; i < m_Shape.Wheels.Count; i++)
            {
                var further = m_Shape.Wheels[i].CentreLocal.z > m_Shape.Wheels[best].CentreLocal.z;
                if (further == mostForward)
                {
                    best = i;
                }
            }

            return best;
        }
    }
}
