using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class TowedTrainPlayTests
    {
        static readonly CorrectionSettings Settings = CorrectionSettings.Default;

        TestApron m_Apron;
        VehicleProfile m_TractorProfile;
        VehicleProfile m_CartProfile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
            m_CartProfile = TestProfiles.Cart();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
            Object.DestroyImmediate(m_CartProfile);
        }

        CartChain SomebodyElsesTrain(int carts = 4)
        {
            var train = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, carts, new Vector3(0f, 1f, 0f));
            foreach (var member in train.Members)
            {
                member.OursToMove = false;
            }

            return train;
        }

        static Rigidbody[] Bodies(CartChain train)
        {
            var bodies = new Rigidbody[train.Members.Count];
            for (var i = 0; i < bodies.Length; i++)
            {
                bodies[i] = train.Members[i].Body;
            }

            return bodies;
        }

        static float[] GapsBetweenMembers(CartChain train)
        {
            var gaps = new float[train.Members.Count - 1];
            for (var i = 0; i < gaps.Length; i++)
            {
                gaps[i] = Vector3.Distance(
                    train.Members[i].transform.position, train.Members[i + 1].transform.position);
            }

            return gaps;
        }

        [UnityTest]
        public IEnumerator ATrainNobodyHereOwnsIsStillHookedTogether()
        {
            var train = SomebodyElsesTrain();
            yield return Steps.Seconds(1f);

            Assert.That(train.CouplingsEngaged, Is.True,
                "a train left uncoupled on every machine but its owner's is five loose boxes. They " +
                "drift apart, wander off on their own physics, and no two screens agree where any of " +
                "them is");
        }

        [UnityTest]
        public IEnumerator ATrainNobodyHereOwnsDoesNotComeApart()
        {
            var train = SomebodyElsesTrain();
            yield return Steps.Seconds(1f);
            var spacedAt = GapsBetweenMembers(train);

            var said = VehicleState.Of(train.Leader.Body);
            var steps = Mathf.CeilToInt(8f / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                Correction.Apply(train.Leader.Body, said, secondsSince: 0f, Settings);
                yield return new WaitForFixedUpdate();
            }

            var now = GapsBetweenMembers(train);
            for (var i = 0; i < now.Length; i++)
            {
                Assert.That(now[i], Is.EqualTo(spacedAt[i]).Within(0.35f),
                    $"the gap behind vehicle {i} went from {spacedAt[i]:F2} m to {now[i]:F2} m. A train " +
                    "that stretches and shuffles while standing still is one nobody can line a cart up " +
                    "behind or load anything onto");
            }
        }

        [UnityTest]
        public IEnumerator ATrainNobodyHereOwnsStandsStillWhileBeingToldWhereItIs()
        {
            var train = SomebodyElsesTrain();
            var said = VehicleState.Of(train.Leader.Body);
            var stoodAt = train.Leader.transform.position;

            var steps = Mathf.CeilToInt(10f / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                Correction.Apply(train.Leader.Body, said, secondsSince: 0f, Settings);
                yield return new WaitForFixedUpdate();
            }

            var wandered = Vector3.Distance(train.Leader.transform.position, stoodAt);

            Assert.That(wandered, Is.LessThan(0.1f),
                $"a train being told ten seconds running that it is exactly where it already is " +
                $"drifted {wandered:F2} m. A correction is a force, and a force that never quite " +
                "cancels leaves every copy of every train on the apron creeping for the session");
        }

        [UnityTest]
        public IEnumerator ATrainALongWayOutIsDrivenBackRatherThanMoved()
        {
            var train = SomebodyElsesTrain();
            yield return Steps.Seconds(1f);
            var spacedAt = GapsBetweenMembers(train);
            var startedAt = train.Leader.transform.position;

            var said = VehicleState.Of(train.Leader.Body);
            said.Position += new Vector3(0f, 0f, 30f);

            var steps = Mathf.CeilToInt(6f / Time.fixedDeltaTime);
            var furthestInAStep = 0f;
            var was = train.Leader.transform.position;

            for (var i = 0; i < steps; i++)
            {
                Correction.Apply(train.Leader.Body, said, secondsSince: 0f, Settings);
                yield return new WaitForFixedUpdate();

                furthestInAStep = Mathf.Max(
                    furthestInAStep, Vector3.Distance(train.Leader.transform.position, was));
                was = train.Leader.transform.position;
            }

            Assert.That(Vector3.Distance(train.Leader.transform.position, startedAt), Is.GreaterThan(20f),
                "thirty metres out, it has to have made most of the way back");

            var ceiling = Settings.closingCeilingMetresPerSecond * Time.fixedDeltaTime;
            Assert.That(furthestInAStep, Is.LessThan(ceiling * 2f),
                $"it moved {furthestInAStep:F2} m in a single step, where the fastest it may close is " +
                $"{ceiling:F2} m. A body that arrives without having travelled arrives inside " +
                "whatever is standing there, and takes no part in the collision it should have had");

            var now = GapsBetweenMembers(train);
            for (var i = 0; i < now.Length; i++)
            {
                Assert.That(now[i], Is.EqualTo(spacedAt[i]).Within(0.4f),
                    $"the gap behind vehicle {i} is {now[i]:F2} m, was {spacedAt[i]:F2} m. Driven back " +
                    "from the front, the couplings have to carry the rest of the train with it");
            }
        }

        [UnityTest]
        public IEnumerator ATrainBeingPulledAlongKeepsItsShape()
        {
            var train = SomebodyElsesTrain();
            yield return Steps.Seconds(1f);
            var spacedAt = GapsBetweenMembers(train);

            var steps = Mathf.CeilToInt(6f / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                var said = VehicleState.Of(train.Leader.Body);
                said.Position += new Vector3(0f, 0f, 0.8f);
                said.Velocity = new Vector3(0f, 0f, 4f);

                Correction.Apply(train.Leader.Body, said, secondsSince: 0f, Settings);
                yield return new WaitForFixedUpdate();
            }

            var now = GapsBetweenMembers(train);
            for (var i = 0; i < now.Length; i++)
            {
                Assert.That(now[i], Is.EqualTo(spacedAt[i]).Within(0.5f),
                    $"under a steady correction the gap behind vehicle {i} went from {spacedAt[i]:F2} m " +
                    "to {now[i]:F2} m. Pushing only the front means the couplings have to drag the carts " +
                    "along, and the solver spends every step undoing what the correction just did");
            }
        }

        [UnityTest]
        public IEnumerator OnlyTheFrontOfATrainIsWorthCorrecting()
        {
            var train = SomebodyElsesTrain();
            yield return Steps.Seconds(1f);

            Assert.That(train.Leader, Is.SameAs(train.Members[0]));
            foreach (var cart in train.Members)
            {
                Assert.That(cart.Chain, Is.SameAs(train),
                    "a cart that does not know which train it is in cannot be recognised as a follower, " +
                    "so it gets corrected on its own and pulls against the hinge holding it");
            }
        }
    }
}
