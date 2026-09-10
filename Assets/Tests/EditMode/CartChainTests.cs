using System.Collections.Generic;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// A tractor and its carts, treated as one thing.
    ///
    /// The couplings themselves are checked here as configuration rather than by watching a train
    /// drive: what kind of joint, swinging about which axis, limited how far, and with what solver
    /// budget. How a coupled train actually behaves under load is a separate question and needs
    /// physics running to answer.
    /// </summary>
    public sealed class CartChainTests
    {
        TestApron m_Apron;
        VehicleProfile m_Tractor;
        VehicleProfile m_Cart;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_Tractor = TestProfiles.Tractor();
            m_Cart = TestProfiles.Cart();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_Tractor);
            Object.DestroyImmediate(m_Cart);
        }

        CartChain FourCartTrain() => m_Apron.AddTrain(m_Tractor, m_Cart, cartCount: 4, Vector3.zero);

        [Test]
        public void ATrainReportsEveryMemberFromTheTractorBackwards()
        {
            var train = FourCartTrain();

            Assert.That(train.Members.Count, Is.EqualTo(5), "a tractor and four carts");
            Assert.That(train.Members[0], Is.SameAs(train.Leader));
            Assert.That(train.Leader.Profile, Is.SameAs(m_Tractor), "the tractor is at the front");
            for (var i = 1; i < train.Members.Count; i++)
            {
                Assert.That(train.Members[i].Profile, Is.SameAs(m_Cart), $"member {i} should be a cart");
            }
        }

        [Test]
        public void EveryVehicleInATrainKnowsWhichTrainItIsIn()
        {
            var train = FourCartTrain();

            foreach (var member in train.Members)
            {
                Assert.That(member.Chain, Is.SameAs(train));
                Assert.That(train.Contains(member), Is.True);
            }
        }

        [Test]
        public void AnUncoupledVehicleIsATrainOfItselfAlone()
        {
            var lone = m_Apron.AddVehicle(m_Tractor, "Tug 2", new Vector3(30f, 0f, 0f), Quaternion.identity);

            Assert.That(lone.Chain, Is.Not.Null,
                "every vehicle belongs to a train, so taking one over is always the same operation");
            Assert.That(lone.Chain.Members.Count, Is.EqualTo(1));
            Assert.That(lone.Chain.Leader, Is.SameAs(lone));
        }

        [Test]
        public void CouplingsSwingAboutTheVerticalAndCannotFoldBackOnThemselves()
        {
            var train = FourCartTrain();

            for (var i = 0; i < train.Members.Count - 1; i++)
            {
                var coupling = train.CouplingBehind(i);
                Assert.That(coupling, Is.Not.Null, $"nothing is holding member {i + 1} on");

                var hinge = coupling as HingeJoint;
                Assert.That(hinge, Is.Not.Null, "a coupling swings, it does not weld");

                var worldAxis = hinge.transform.TransformDirection(hinge.axis).normalized;
                Assert.That(Mathf.Abs(Vector3.Dot(worldAxis, Vector3.up)), Is.EqualTo(1f).Within(1e-3f),
                    "a cart swings left and right behind the one in front, not up and over it");

                Assert.That(hinge.useLimits, Is.True, "an unlimited coupling lets a train fold through itself");
                Assert.That(hinge.limits.max, Is.LessThan(180f));
                Assert.That(hinge.limits.min, Is.GreaterThan(-180f));
            }
        }

        [Test]
        public void ATrainHasNoCouplingHangingOffItsLastMember()
        {
            var train = FourCartTrain();

            Assert.That(train.CouplingBehind(train.Members.Count - 1), Is.Null);
        }

        [Test]
        public void EveryMemberOfATrainGetsMoreSolverEffortThanTheProjectDefault()
        {
            var settings = ChainJointSettings.Default;
            var train = FourCartTrain();

            foreach (var member in train.Members)
            {
                Assert.That(member.Body.solverIterations, Is.EqualTo(settings.solverPositionIterations));
                Assert.That(member.Body.solverVelocityIterations, Is.EqualTo(settings.solverVelocityIterations));
                Assert.That(member.Body.solverIterations, Is.GreaterThan(Physics.defaultSolverIterations),
                    "coupled bodies need more than a lone body or the couplings visibly stretch");
            }
        }

        [Test]
        public void ATrainThisMachineIsNotSimulatingStillKnowsEveryMemberItHas()
        {
            var train = FourCartTrain();

            train.ReleaseCouplings();

            Assert.That(train.CouplingsEngaged, Is.False);
            for (var i = 0; i < train.Members.Count - 1; i++)
            {
                Assert.That(train.CouplingBehind(i), Is.Null,
                    "a machine watching somebody else's train must not hold hinges of its own, " +
                    "because half that constraint would be solving against a body it cannot move");
            }

            Assert.That(train.Members.Count, Is.EqualTo(5),
                "but it is still five vehicles. A machine that forgets the carts asks only for the " +
                "tractor, takes it, and drives off leaving four carts owned by somebody else");

            var broker = new RecordingBroker(grant: true);
            train.RequestOwnership(broker, _ => { });
            Assert.That(broker.Requests[0], Is.EquivalentTo(train.Members));
        }

        [Test]
        public void PickingUpATrainPutsItsCouplingsBackOn()
        {
            var train = FourCartTrain();
            train.ReleaseCouplings();

            train.EngageCouplings();

            Assert.That(train.CouplingsEngaged, Is.True);
            for (var i = 0; i < train.Members.Count - 1; i++)
            {
                Assert.That(train.CouplingBehind(i), Is.Not.Null);
            }
        }

        [Test]
        public void EngagingCouplingsTwiceDoesNotDoubleThemUp()
        {
            var train = FourCartTrain();

            train.EngageCouplings();
            train.EngageCouplings();

            foreach (var member in train.Members)
            {
                Assert.That(member.GetComponents<Joint>().Length, Is.LessThanOrEqualTo(1),
                    "confirming ownership again must not stack a second hinge on a vehicle that " +
                    "already has one, which would over-constrain it and make the train fight itself");
            }
        }

        [Test]
        public void TakingATrainAsksForEveryMemberInOneGo()
        {
            var train = FourCartTrain();
            var broker = new RecordingBroker(grant: true, localClientId: 7);
            foreach (var member in train.Members)
            {
                broker.SetOwner(member, 3);
            }

            var granted = false;
            train.RequestOwnership(broker, result => granted = result);

            Assert.That(granted, Is.True);
            Assert.That(broker.Requests.Count, Is.EqualTo(1),
                "a train asked for one member at a time can end up owned by several people");
            Assert.That(broker.Requests[0], Is.EquivalentTo(train.Members));
            foreach (var member in train.Members)
            {
                Assert.That(broker.OwnerOf(member), Is.EqualTo(7ul), "the whole train moved, not just the tractor");
            }
        }

        [Test]
        public void ATrainRefusedOwnershipKeepsTheOwnerItHad()
        {
            var train = FourCartTrain();
            var broker = new RecordingBroker(grant: false, localClientId: 7);
            foreach (var member in train.Members)
            {
                broker.SetOwner(member, 3);
            }

            var granted = true;
            train.RequestOwnership(broker, result => granted = result);

            Assert.That(granted, Is.False);
            foreach (var member in train.Members)
            {
                Assert.That(broker.OwnerOf(member), Is.EqualTo(3ul),
                    "a refused request must leave every member exactly where it was");
            }
        }

        [Test]
        public void SplittingATrainDestroysTheCouplingRatherThanSwitchingItOff()
        {
            var train = FourCartTrain();
            var atTheBreak = train.Members[2];
            var behindTheBreak = train.Members[3];

            var (front, back) = train.SplitAfter(2);

            Assert.That(front.Members.Count, Is.EqualTo(3), "tractor and the first two carts");
            Assert.That(back.Members.Count, Is.EqualTo(2), "the last two carts");
            Assert.That(front.CouplingBehind(2), Is.Null, "nothing should still be hanging off the break");
            Assert.That(atTheBreak.Chain, Is.SameAs(front));
            Assert.That(behindTheBreak.Chain, Is.SameAs(back));
            Assert.That(back.Leader, Is.SameAs(behindTheBreak));

            // A coupling lives on the vehicle being towed, so the one to check is the cart that was
            // behind the break: it is now leading its own train and should have nothing hooked to
            // its front. The cart in front of the break keeps its own coupling to the vehicle
            // ahead of it, which is still there and still correct.
            Assert.That(behindTheBreak.GetComponents<Joint>(), Is.Empty,
                "a coupling that is merely disabled is a coupling waiting to be found again by the solver");
        }

        [Test]
        public void SplitTrainsAreAskedForSeparately()
        {
            var train = FourCartTrain();
            var (front, back) = train.SplitAfter(2);
            var broker = new RecordingBroker(grant: true);

            front.RequestOwnership(broker, _ => { });

            Assert.That(broker.Requests[0], Is.EquivalentTo(front.Members));
            Assert.That(broker.Requests[0], Has.No.Member(back.Members[0]),
                "the carts that were unhitched are somebody else's problem now");
        }
    }
}
