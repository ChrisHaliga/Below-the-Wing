using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class BeltLoaderPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_Profile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_Profile = TestProfiles.BeltLoader();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_Profile);
        }

        VehicleController ABeltLoaderAt(Vector3 at)
            => m_Apron.AddVehicle(m_Profile, "Belt loader", at, Quaternion.identity, TestShapes.BeltLoader());

        [UnityTest]
        public IEnumerator TheBeltLoaderSettlesOnItsWheelsRatherThanSinkingOrBouncing()
        {
            var loader = ABeltLoaderAt(Vector3.zero);
            var shape = loader.GetComponent<VehicleShape>();

            yield return Steps.Seconds(2f);

            Assert.That(loader.transform.position.y, Is.EqualTo(0f).Within(0.05f),
                $"the loader settled at {loader.transform.position.y:F3} m. A vehicle's origin is " +
                "ground level, so anything else is a machine buried in the apron or standing on air");

            foreach (var wheel in shape.Wheels)
            {
                var bottom = loader.transform.TransformPoint(wheel.CentreLocal).y - wheel.RadiusMetres;

                Assert.That(bottom, Is.EqualTo(0f).Within(0.08f),
                    $"a wheel's underside rests at {bottom:F3} m");
            }
        }

        [UnityTest]
        public IEnumerator ADriverAtFullThrottleTakesTheBeltLoaderSomewhere()
        {
            var loader = ABeltLoaderAt(Vector3.zero);

            yield return Steps.Seconds(1f);

            var from = loader.transform.position;

            loader.Occupied = true;
            loader.IntentSource = new FixedIntent(throttle: 1f);

            yield return Steps.Seconds(2f);

            var travelled = Vector3.Distance(from, loader.transform.position);

            Assert.That(travelled, Is.GreaterThan(3f),
                $"two seconds of full throttle moved the loader {travelled:F2} m. A machine nobody " +
                "can drive to the hold is a machine that may as well be scenery");
        }

        [UnityTest]
        public IEnumerator TheBeltLoaderTopsOutSlowerThanATractor()
        {
            var loader = ABeltLoaderAt(Vector3.zero);

            loader.Occupied = true;
            loader.IntentSource = new FixedIntent(throttle: 1f);

            yield return Steps.Seconds(6f);

            var speed = loader.GetComponent<Rigidbody>().linearVelocity.magnitude;

            Assert.That(speed, Is.LessThan(9f),
                $"the loader reached {speed:F2} m/s. It is geared for the apron at around 7 m/s, " +
                "and a conveyor that keeps up with a tractor is a conveyor nobody walks beside");
        }

        [UnityTest]
        public IEnumerator TheBeltLoaderStaysPutWithNobodyDrivingIt()
        {
            var loader = ABeltLoaderAt(Vector3.zero);

            yield return Steps.Seconds(1f);

            var from = loader.transform.position;

            yield return Steps.Seconds(2f);

            Assert.That(Vector3.Distance(from, loader.transform.position), Is.LessThan(0.1f),
                "a parked loader with no driver crept across the apron on its own");
        }
    }
}
