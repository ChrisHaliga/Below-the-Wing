using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
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
                Position = new Vector3(at.x, 0f, at.z),
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
        public IEnumerator ACopySomewhereElseEntirelyTravelsBackWithoutEverTeleporting()
        {
            var vehicle = Copy(new Vector3(0f, 1f, 0f));
            yield return null;

            var said = Standing(new Vector3(0f, 0f, 60f));
            var ceiling = Settings.closingCeilingMetresPerSecond * Time.fixedDeltaTime;
            var was = vehicle.transform.position;
            var furthestInAStep = 0f;

            var steps = Mathf.CeilToInt(10f / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                Correction.Apply(vehicle.Body, said, secondsSince: 0f, Settings);
                yield return new WaitForFixedUpdate();

                furthestInAStep = Mathf.Max(furthestInAStep, Vector3.Distance(vehicle.transform.position, was));
                was = vehicle.transform.position;
            }

            Assert.That(furthestInAStep, Is.LessThan(ceiling * 2f),
                $"it covered {furthestInAStep:F2} m in one step against a ceiling of {ceiling:F2} m. " +
                "A body that arrives somewhere without having travelled there arrives inside whatever " +
                "was standing in the way, and never took part in the collision it should have had");

            Assert.That(Vector3.Distance(vehicle.transform.position, said.Position), Is.LessThan(5f),
                "and it does have to get there: sixty metres out and still sixty metres out is a copy " +
                "nobody will ever see in the right place");
        }

        [UnityTest]
        public IEnumerator ACrashIsNotUndoneWhileItIsStillHappening()
        {
            var vehicle = Copy(new Vector3(0f, 1f, 0f));
            yield return null;

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

        [UnityTest]
        public IEnumerator SomethingBeingCarriedIsNotSteeredAtAll()
        {
            var vehicle = Copy(new Vector3(0f, 1f, 0f));
            yield return null;

            vehicle.Body.isKinematic = true;
            var restingAt = vehicle.transform.position;

            yield return KeptInStepFor(1f, vehicle, Standing(new Vector3(0f, 0f, 20f)));

            Assert.That(vehicle.transform.position, Is.EqualTo(restingAt).Using(Nearly.Within(1e-3f)),
                "a body being carried has to be left where its carrier put it. Pushing one does " +
                "nothing except fill the log with an error on every step of every copy of every " +
                "driver on the apron");
        }
    }
}
