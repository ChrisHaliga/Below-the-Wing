using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// Driving a vehicle from numbers instead of a keyboard.
    ///
    /// Holding a turn radius rather than a steering angle is what turns a vehicle driving in circles
    /// into a measuring instrument: sideways acceleration becomes a quantity that can be computed
    /// ahead of time and checked against, rather than one that can only be observed after the fact.
    ///
    /// Nothing here knows what it is being used for, and that is deliberate -- the same component
    /// would drive a vehicle along a path later, because a path is a sequence of radii.
    /// </summary>
    public sealed class ScriptedDriverPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
        }

        static IEnumerator Step(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        ScriptedDriver Driving(float speed, float radius = 0f)
        {
            var vehicle = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);
            var driver = vehicle.gameObject.AddComponent<ScriptedDriver>();
            vehicle.IntentSource = driver;

            driver.TargetSpeed = speed;
            driver.TurnRadiusMetres = radius;
            driver.RampMetresPerSecondSquared = 100f;

            return driver;
        }

        [UnityTest]
        public IEnumerator AVehicleToldToDriveDrivesItself()
        {
            var driver = Driving(speed: 5f);
            var from = driver.transform.position;

            yield return Step(4f);

            Assert.That(Vector3.Distance(from, driver.transform.position), Is.GreaterThan(3f),
                "nothing is at the keyboard, so if this does not move then nothing measured with it " +
                "means anything");
        }

        [UnityTest]
        public IEnumerator AVehicleToldToStandStillStandsStill()
        {
            var driver = Driving(speed: 0f);
            var from = driver.transform.position;

            yield return Step(3f);

            Assert.That(Vector3.Distance(from, driver.transform.position), Is.LessThan(0.5f));
        }

        [UnityTest]
        public IEnumerator AChangeOfSpeedRampsRatherThanJumping()
        {
            var driver = Driving(speed: 0f);
            driver.RampMetresPerSecondSquared = 2f;
            yield return Step(0.5f);

            driver.TargetSpeed = 10f;
            yield return new WaitForFixedUpdate();

            Assert.That(driver.Approaching, Is.LessThan(1f),
                "stepping straight to the target is an acceleration spike, and a spike throws every " +
                "bag off the cart for a reason nobody caused");

            yield return Step(6f);

            Assert.That(driver.Approaching, Is.EqualTo(10f).Within(0.1f), "but it does get there");
        }

        [UnityTest]
        public IEnumerator AVehicleHoldingARadiusTurns()
        {
            var driver = Driving(speed: 4f, radius: 12f);
            var facing = driver.transform.eulerAngles.y;

            yield return Step(5f);

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(facing, driver.transform.eulerAngles.y)), Is.GreaterThan(20f),
                "asked for a twelve metre circle and drove in a straight line");
        }

        [UnityTest]
        public IEnumerator ATighterRadiusMeansMoreSteering()
        {
            var wide = Driving(speed: 0f, radius: 30f);
            yield return new WaitForFixedUpdate();
            var wideSteer = Mathf.Abs(wide.Current.Steer);

            var tight = m_Apron.AddVehicle(m_TractorProfile, "Tug 2", new Vector3(40f, 0f, 0f), Quaternion.identity);
            var tightDriver = tight.gameObject.AddComponent<ScriptedDriver>();
            tight.IntentSource = tightDriver;
            tightDriver.TurnRadiusMetres = 5f;
            yield return new WaitForFixedUpdate();

            Assert.That(Mathf.Abs(tightDriver.Current.Steer), Is.GreaterThan(wideSteer),
                "a tighter circle needs more lock, and a component that does not know that cannot " +
                "hold a radius at all");
        }

        [UnityTest]
        public IEnumerator DrivingStraightMeansNoSteering()
        {
            var driver = Driving(speed: 4f, radius: 0f);
            yield return Step(1f);

            Assert.That(driver.Current.Steer, Is.EqualTo(0f).Within(1e-3f),
                "a radius of nothing is a straight line, not an infinitely tight corner");
        }

        [UnityTest]
        public IEnumerator ANegativeRadiusTurnsTheOtherWay()
        {
            var driver = Driving(speed: 0f, radius: 10f);
            yield return new WaitForFixedUpdate();
            var oneWay = driver.Current.Steer;

            driver.TurnRadiusMetres = -10f;
            yield return new WaitForFixedUpdate();

            Assert.That(driver.Current.Steer, Is.EqualTo(-oneWay).Within(1e-3f));
        }

        [UnityTest]
        public IEnumerator TheSidewaysPullIsReportedFromWhatTheVehicleIsActuallyDoing()
        {
            var driver = Driving(speed: 6f, radius: 10f);

            Assert.That(driver.LateralAcceleration, Is.Zero, "standing still it is pulling nothing");

            yield return Step(6f);

            Assert.That(driver.LateralAcceleration, Is.GreaterThan(0.5f),
                "reported from the speed it has reached rather than the speed it was asked for, so " +
                "the number stays honest while it is still getting up to it");
        }

        [UnityTest]
        public IEnumerator AVehicleGoingRoundACircleStaysNearIt()
        {
            var driver = Driving(speed: 4f, radius: 10f);
            yield return Step(2f);

            var somewhereOnIt = driver.transform.position;
            yield return Step(12f);

            // Anywhere on a circle of that radius is within two diameters of anywhere else on it.
            Assert.That(Vector3.Distance(somewhereOnIt, driver.transform.position), Is.LessThan(42f),
                "a vehicle that wandered off instead of going round is not holding a radius, and " +
                "everything measured against v squared over r is then measured against nothing");
        }
    }
}
