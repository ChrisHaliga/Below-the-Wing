using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// What correction does to a real vehicle over time.
    ///
    /// No network here, deliberately. The three instances the multiplayer harness runs share one
    /// physics world, so every copy of a vehicle is a solid body standing in the same space as every
    /// other copy of it -- they collide with themselves and blow apart, and nothing about convergence
    /// can be measured. What crosses the wire is checked there; what the forces do is checked here,
    /// by handing a vehicle a report directly and watching where it ends up.
    /// </summary>
    public sealed class CorrectionPlayTests
    {
        static readonly CorrectionSettings Settings = CorrectionSettings.Default;

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

        VehicleController Copy(Vector3 where)
        {
            var vehicle = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", where, Quaternion.identity);
            vehicle.OursToMove = false;
            return vehicle;
        }

        /// <summary>Runs the simulation with the owner saying the same thing throughout.</summary>
        static IEnumerator KeptInStepFor(float seconds, VehicleController vehicle, VehicleState said)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                Correction.Apply(vehicle.Body, said, secondsSince: 0f, Settings);
                yield return new WaitForFixedUpdate();
            }
        }

        VehicleState Standing(Vector3 at)
            => new VehicleState
            {
                Position = new Vector3(at.x, VehicleController.RestingHeightMetres(m_TractorProfile), at.z),
                Rotation = Quaternion.identity,
                Velocity = Vector3.zero,
                Spin = Vector3.zero
            };

        [UnityTest]
        public IEnumerator ACopyLeftBehindCatchesUpWithoutBeingTeleported()
        {
            var vehicle = Copy(new Vector3(0f, 1f, 0f));
            yield return null;

            var said = Standing(new Vector3(0f, 0f, 1.5f));
            var startedAt = vehicle.transform.position;

            yield return KeptInStepFor(3f, vehicle, said);

            var gap = Vector3.Distance(vehicle.transform.position, said.Position);
            Assert.That(gap, Is.LessThan(0.3f),
                $"a metre and a half behind and still {gap:F2} m out after three seconds: correction " +
                "that cannot close an ordinary gap leaves every remote vehicle permanently misplaced");
            Assert.That(Vector3.Distance(startedAt, vehicle.transform.position), Is.GreaterThan(0.5f),
                "it has to have actually travelled there");
        }

        [UnityTest]
        public IEnumerator ACopyAlreadyInPlaceIsLeftAlone()
        {
            var vehicle = Copy(new Vector3(0f, 1f, 0f));
            yield return null;

            // Let it settle, then tell it exactly where it already is.
            var settleSteps = Mathf.CeilToInt(2f / Time.fixedDeltaTime);
            for (var i = 0; i < settleSteps; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            var said = VehicleState.Of(vehicle.Body);
            var parkedAt = vehicle.transform.position;

            yield return KeptInStepFor(4f, vehicle, said);

            Assert.That(Vector3.Distance(parkedAt, vehicle.transform.position), Is.LessThan(0.1f),
                "nobody is driving and it is already where it belongs. A correction that keeps pushing " +
                "anyway makes every parked tractor on the apron hum and creep, and none of them sleep");
        }

        [UnityTest]
        public IEnumerator ACopySomewhereElseEntirelyIsPutBackAtOnce()
        {
            var vehicle = Copy(new Vector3(0f, 1f, 0f));
            yield return null;

            var said = Standing(new Vector3(0f, 0f, 60f));

            // A single step is all a snap should need.
            Correction.Apply(vehicle.Body, said, secondsSince: 0f, Settings);
            yield return new WaitForFixedUpdate();

            Assert.That(Vector3.Distance(vehicle.transform.position, said.Position), Is.LessThan(1f),
                "sixty metres is not a disagreement, it is a different place. Easing across it takes " +
                "long enough to be watched, and what gets watched is a tractor gliding over the apron");
        }

        [UnityTest]
        public IEnumerator ACrashIsNotUndoneWhileItIsStillHappening()
        {
            var vehicle = Copy(new Vector3(0f, 1f, 0f));
            yield return null;

            // Its owner says it is parked. It is not: something has just hit it hard.
            var said = VehicleState.Of(vehicle.Body);
            vehicle.Body.linearVelocity = new Vector3(0f, 0f, 8f);
            var whenItWasHit = vehicle.transform.position;

            var steps = Mathf.CeilToInt(0.5f / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                Correction.Apply(vehicle.Body, said, secondsSince: 0f, Settings, say: 0f);
                yield return new WaitForFixedUpdate();
            }

            var carriedOn = vehicle.transform.position.z - whenItWasHit.z;
            Assert.That(carriedOn, Is.GreaterThan(1f),
                $"knocked at 8 m/s it travelled {carriedOn:F2} m. A vehicle hauled back through the " +
                "impact it just took reads as the game refusing what the player did, which is the " +
                "single most immersion-breaking thing networked physics does");
        }

        [UnityTest]
        public IEnumerator ACopyFacingTheWrongWayIsTurnedRound()
        {
            var vehicle = Copy(new Vector3(0f, 1f, 0f));
            yield return null;

            var said = Standing(Vector3.zero);
            said.Rotation = Quaternion.Euler(0f, 60f, 0f);

            yield return KeptInStepFor(3f, vehicle, said);

            var off = Quaternion.Angle(vehicle.transform.rotation, said.Rotation);
            Assert.That(off, Is.LessThan(15f),
                $"still {off:F0} degrees from the way its owner says it points. A tractor facing one " +
                "way here and another way there is one you cannot line a cart up behind");
        }
    }
}
