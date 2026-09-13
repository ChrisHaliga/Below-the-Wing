using System.Collections;
using System.Collections.Generic;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// A train of carts being pulled about.
    ///
    /// This is the part of the game most likely to come apart, and none of it can be checked
    /// without a solver running: whether the couplings hold, and whether the carts follow the
    /// tractor the way things on wheels do rather than sliding along behind it in formation.
    /// </summary>
    public sealed class CartTrainPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;
        VehicleProfile m_CartProfile;
        CartChain m_Train;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
            m_CartProfile = TestProfiles.Cart();
            m_Train = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 4, new Vector3(0f, 1f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
            Object.DestroyImmediate(m_CartProfile);
        }

        List<float> CouplingLengths()
        {
            var lengths = new List<float>();
            for (var i = 0; i < m_Train.Members.Count - 1; i++)
            {
                lengths.Add(Vector3.Distance(
                    m_Train.Members[i].transform.position,
                    m_Train.Members[i + 1].transform.position));
            }

            return lengths;
        }

        [UnityTest]
        public IEnumerator EveryCartIsStillThereAfterBeingTowedInAStraightLine()
        {
            yield return Steps.Seconds(2f);
            var atRest = CouplingLengths();

            m_Train.Leader.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(5f);

            var whileMoving = CouplingLengths();
            for (var i = 0; i < atRest.Count; i++)
            {
                Assert.That(m_Train.CouplingBehind(i), Is.Not.Null, $"the coupling behind member {i} has gone");
                Assert.That(whileMoving[i], Is.EqualTo(atRest[i]).Within(0.5f),
                    $"coupling {i} has stretched -- the solver is losing the constraint under load");
            }

            var travelled = Vector3.Distance(m_Train.Leader.transform.position, Vector3.zero);
            Assert.That(travelled, Is.GreaterThan(3f), "the train never actually went anywhere");
        }

        [UnityTest]
        public IEnumerator CartsFollowATractorRoundACornerRatherThanCopyingItsHeading()
        {
            yield return Steps.Seconds(2f);

            // At a towing throttle, where the tyres hold and the corner is tight enough to show the
            // articulation this test is about. Flat out is covered separately.
            m_Train.Leader.IntentSource = new FixedIntent(steer: 1f, throttle: 0.4f);
            yield return Steps.Seconds(6f);

            var tractorHeading = m_Train.Leader.transform.eulerAngles.y;
            var lastCartHeading = m_Train.Members[^1].transform.eulerAngles.y;
            var difference = Mathf.Abs(Mathf.DeltaAngle(tractorHeading, lastCartHeading));

            Assert.That(difference, Is.GreaterThan(10f),
                "the back of the train should still be coming round when the front has already turned; " +
                "a train whose members all face the same way is being dragged rigidly, not towed");

            for (var i = 0; i < m_Train.Members.Count - 1; i++)
            {
                Assert.That(m_Train.CouplingBehind(i), Is.Not.Null, $"coupling {i} broke during the turn");
            }
        }

        [UnityTest]
        public IEnumerator ATrainStaysUprightThroughAnOrdinaryTurn()
        {
            yield return Steps.Seconds(2f);

            m_Train.Leader.IntentSource = new FixedIntent(steer: 0.5f, throttle: 0.6f);
            yield return Steps.Seconds(6f);

            foreach (var member in m_Train.Members)
            {
                var lean = Vector3.Angle(member.transform.up, Vector3.up);
                Assert.That(lean, Is.LessThan(30f),
                    $"'{member.DisplayName}' is on its way over during a gentle turn");
            }
        }

        /// <summary>
        /// A train that has been driven and then let go of has to stop, and stay stopped.
        ///
        /// This is the thing a parked apron rests on. Everything that costs anything in this game
        /// is per awake rigidbody, and a train that keeps creeping after its driver has let go
        /// never sleeps -- so it goes on costing on every machine, for the rest of the session,
        /// and the carts are never quite where anybody left them.
        /// </summary>
        [UnityTest]
        public IEnumerator ATrainLetGoOfComesToRestAndStaysThere()
        {
            var train = m_Train;
            train.Leader.IntentSource = new FixedIntent(throttle: 1f);

            yield return Steps.Seconds(4f);
            Assert.That(train.Leader.Body.linearVelocity.magnitude, Is.GreaterThan(2f),
                "it has to have been moving for letting go to mean anything");

            // Let go. Nothing is driving it and nothing is correcting it.
            train.Leader.IntentSource = null;
            yield return Steps.Seconds(12f);

            foreach (var member in train.Members)
            {
                Assert.That(member.Body.linearVelocity.magnitude, Is.LessThan(0.2f),
                    $"'{member.DisplayName}' is still travelling at " +
                    $"{member.Body.linearVelocity.magnitude:F2} m/s twelve seconds after the throttle " +
                    "was released. Nothing is taking its speed away");
            }

            var restedAt = new Vector3[train.Members.Count];
            for (var i = 0; i < restedAt.Length; i++)
            {
                restedAt[i] = train.Members[i].transform.position;
            }

            yield return Steps.Seconds(5f);

            for (var i = 0; i < restedAt.Length; i++)
            {
                var member = train.Members[i];
                Assert.That(Vector3.Distance(restedAt[i], member.transform.position), Is.LessThan(0.15f),
                    $"'{member.DisplayName}' wandered while nothing was driving it");
                Assert.That(member.Body.IsSleeping(), Is.False,
                    $"'{member.DisplayName}' was put to sleep. Standing still has to come from the " +
                    "tyres holding it, because a sleeping vehicle is one whose suspension has " +
                    "stopped running, and the next thing to touch it goes straight through where " +
                    "its springs should have been");
            }
        }

        [UnityTest]
        public IEnumerator ASplitTrainLeavesItsBackHalfBehind()
        {
            yield return Steps.Seconds(2f);
            var (front, back) = m_Train.SplitAfter(2);
            var abandonedAt = back.Leader.transform.position;

            front.Leader.IntentSource = new FixedIntent(throttle: 1f);
            yield return Steps.Seconds(4f);

            Assert.That(Vector3.Distance(front.Leader.transform.position, Vector3.zero), Is.GreaterThan(3f));
            Assert.That(Vector3.Distance(back.Leader.transform.position, abandonedAt), Is.LessThan(1f),
                "carts that were unhitched must stay where they were left");
        }

        [UnityTest]
        public IEnumerator FlatOutOnFullLockTheTrainStillTurnsAndHoldsTogether()
        {
            yield return Steps.Seconds(2f);
            var headingBefore = m_Train.Leader.transform.eulerAngles.y;

            // More power than the tyres can turn into cornering: the tractor scrubs wide and the
            // train runs straighter behind it than at a towing pace, but it has to keep turning and
            // the couplings have to hold.
            m_Train.Leader.IntentSource = new FixedIntent(steer: 1f, throttle: 1f);
            yield return Steps.Seconds(6f);

            var turned = Mathf.Abs(Mathf.DeltaAngle(headingBefore, m_Train.Leader.transform.eulerAngles.y));
            Assert.That(turned, Is.GreaterThan(45f), $"turned {turned:F0} degrees in six seconds flat out on full lock");

            for (var i = 0; i < m_Train.Members.Count - 1; i++)
            {
                Assert.That(m_Train.CouplingBehind(i), Is.Not.Null, $"coupling {i} broke flat out");
            }
        }
    }
}
