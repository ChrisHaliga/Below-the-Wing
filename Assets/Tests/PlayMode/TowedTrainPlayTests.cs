using System.Collections;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// A train on a machine that does not own it.
    ///
    /// It is hooked together here exactly as it is on the machine that does own it, and towed by
    /// the local copy of the tractor. Only that tractor is corrected; the carts follow it through
    /// the hinges. Correcting each cart on its own is what tears these trains apart -- a chain is a
    /// set of constraints and a correction is a force on one body, so told that cart three is
    /// behind and cart four is left, correction pushes each toward a place the hinge between them
    /// forbids, and the solver and the network take turns losing.
    /// </summary>
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

        static IEnumerator Step(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        /// <summary>A train nobody here owns: coupled up, and not driven by anything local.</summary>
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
            yield return Step(1f);

            Assert.That(train.CouplingsEngaged, Is.True,
                "a train left uncoupled on every machine but its owner's is five loose boxes. They " +
                "drift apart, wander off on their own physics, and no two screens agree where any of " +
                "them is");
        }

        [UnityTest]
        public IEnumerator ATrainNobodyHereOwnsDoesNotComeApart()
        {
            var train = SomebodyElsesTrain();
            yield return Step(1f);
            var spacedAt = GapsBetweenMembers(train);

            // The owner says the tractor is exactly where it already is, throughout.
            var said = VehicleState.Of(train.Leader.Body);
            var steps = Mathf.CeilToInt(8f / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                Correction.Apply(Bodies(train), train.Leader.Body, said, secondsSince: 0f, Settings);
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
        public IEnumerator ATrainNobodyHereOwnsGoesToSleep()
        {
            var train = SomebodyElsesTrain();
            var said = VehicleState.Of(train.Leader.Body);

            // Ten seconds of being told, twenty times a second, exactly what it already knows.
            var steps = Mathf.CeilToInt(10f / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                Correction.Apply(Bodies(train), train.Leader.Body, said, secondsSince: 0f, Settings);
                yield return new WaitForFixedUpdate();
            }

            var awake = 0;
            foreach (var member in train.Members)
            {
                if (!member.Body.IsSleeping())
                {
                    awake++;
                }
            }

            Assert.That(awake, Is.Zero,
                $"{awake} of {train.Members.Count} still awake. Any force at all wakes a rigidbody, so " +
                "a correction applied every step -- however small -- keeps every vehicle on the apron " +
                "awake for the rest of the session on every machine that does not own it");
        }

        [UnityTest]
        public IEnumerator ATrainPutBackArrivesStillHitchedUpInOrder()
        {
            var train = SomebodyElsesTrain();
            yield return Step(1f);
            var spacedAt = GapsBetweenMembers(train);

            // Far enough away that blending is given up on and the train is moved outright.
            var said = VehicleState.Of(train.Leader.Body);
            said.Position += new Vector3(0f, 0f, 30f);

            Correction.Apply(Bodies(train), train.Leader.Body, said, secondsSince: 0f, Settings);
            yield return Step(1.5f);

            Assert.That(Vector3.Distance(train.Leader.transform.position, said.Position), Is.LessThan(2f),
                "it has to have actually arrived");

            var now = GapsBetweenMembers(train);
            for (var i = 0; i < now.Length; i++)
            {
                Assert.That(now[i], Is.EqualTo(spacedAt[i]).Within(0.4f),
                    $"the gap behind vehicle {i} is {now[i]:F2} m, was {spacedAt[i]:F2} m. Moving only the " +
                    "front of a train leaves every coupling violated by the distance travelled, and the " +
                    "solver answers that by throwing the carts apart -- the train twists itself inside " +
                    "out instead of arriving");
            }
        }

        [UnityTest]
        public IEnumerator ATrainBeingPulledAlongKeepsItsShape()
        {
            var train = SomebodyElsesTrain();
            yield return Step(1f);
            var spacedAt = GapsBetweenMembers(train);

            // Its owner is driving away steadily: always a little ahead of where this copy is.
            var steps = Mathf.CeilToInt(6f / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                var said = VehicleState.Of(train.Leader.Body);
                said.Position += new Vector3(0f, 0f, 0.8f);
                said.Velocity = new Vector3(0f, 0f, 4f);

                Correction.Apply(Bodies(train), train.Leader.Body, said, secondsSince: 0f, Settings);
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
            yield return Step(1f);

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
