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
        const float GapDriftMetres = 0.05f;

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
                Assert.That(now[i], Is.EqualTo(spacedAt[i]).Within(GapDriftMetres),
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
                Assert.That(now[i], Is.EqualTo(spacedAt[i]).Within(GapDriftMetres),
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
                Assert.That(now[i], Is.EqualTo(spacedAt[i]).Within(GapDriftMetres),
                    $"under a steady correction the gap behind vehicle {i} went from {spacedAt[i]:F2} m " +
                    $"to {now[i]:F2} m. The front of the train is the only part being pushed, so the " +
                    "couplings are what carry the rest of it along; a train that stretches while it is " +
                    "steered is one whose carts arrive somewhere their couplings say they cannot be");
            }
        }

        [UnityTest]
        public IEnumerator PushingEveryMemberTowardTheFrontsReportConcertinasTheTrain()
        {
            var train = SomebodyElsesTrain();
            yield return Steps.Seconds(1f);
            var spacedAt = GapsBetweenMembers(train);

            var said = VehicleState.Of(train.Leader.Body);
            var steps = Mathf.CeilToInt(4f / Time.fixedDeltaTime);

            for (var i = 0; i < steps; i++)
            {
                foreach (var body in Bodies(train))
                {
                    Correction.Apply(body, said, secondsSince: 0f, Settings);
                }

                yield return new WaitForFixedUpdate();
            }

            var now = GapsBetweenMembers(train);
            var squeezed = 0;
            for (var i = 0; i < now.Length; i++)
            {
                if (now[i] < spacedAt[i] - 0.2f)
                {
                    squeezed++;
                }
            }

            Assert.That(squeezed, Is.GreaterThan(0),
                "a machine that does not own a train is told where the front of it is and nothing " +
                "about the carts. Pushing every member toward that one report drives each cart at " +
                "the tractor while its coupling holds it back, and the gaps have to close. If they " +
                "do not, there is nothing to stop somebody steering every body in a train toward a " +
                $"report meant for one of them. Gaps went from {string.Join(", ", spacedAt)} to " +
                $"{string.Join(", ", now)}");
        }

    }
}
