using System.Collections.Generic;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Hooking a cart onto the back of a train, and unhooking one, while a player is standing there.
    ///
    /// Trains have only ever existed in the shape the apron layout built them. Once a player can
    /// change that shape mid-session, two things that were previously guaranteed by construction
    /// have to be enforced: that a cart is standing in exactly the right place when its coupling is
    /// created, and that a cart belonging to another train cannot be stolen out of the middle of it.
    /// </summary>
    public sealed class CouplingTests
    {
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

        VehicleController LooseCart(string called, Vector3 where)
            => m_Apron.AddVehicle(m_CartProfile, called, where, Quaternion.identity);

        [Test]
        public void ACartIsPlacedSoItsCouplingStartsOutAlreadySatisfied()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);
            var cart = LooseCart("Cart", new Vector3(3f, 0f, -20f));

            var (position, rotation) = Coupling.WhereToStand(tractor, cart);
            cart.transform.SetPositionAndRotation(position, rotation);

            var tractorHitch = tractor.transform.TransformPoint(tractor.RearHitchLocal);
            var cartHitch = cart.transform.TransformPoint(cart.FrontHitchLocal);

            Assert.That(Vector3.Distance(tractorHitch, cartHitch), Is.LessThan(0.01f),
                "the two hitch points have to land on each other. A coupling created while they are " +
                "apart begins life violated and never stops trying to close, which drags the whole " +
                "train across the apron for as long as the session lasts");
        }

        [Test]
        public void ACartStandsBehindTheVehicleItIsHitchedTo()
        {
            var tractor = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);
            var cart = LooseCart("Cart", new Vector3(30f, 0f, 0f));

            var (position, _) = Coupling.WhereToStand(tractor, cart);

            Assert.That(position.z, Is.LessThan(0f), "behind, not in front of, and not on top of");
        }

        [Test]
        public void TheNearestFreeCartBehindATrainIsTheOneOffered()
        {
            var train = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 2, Vector3.zero);
            var back = train.Members[train.Members.Count - 1];

            var close = LooseCart("Close", back.transform.position - new Vector3(0f, 0f, 4f));
            var further = LooseCart("Further", back.transform.position - new Vector3(0f, 0f, 5.5f));

            var offered = Coupling.WorthHitching(train, new List<VehicleController> { further, close });

            Assert.That(offered, Is.SameAs(close), "reaching past a nearer cart to grab a further one is wrong");
        }

        [Test]
        public void ACartTooFarAwayIsNotOffered()
        {
            var train = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 2, Vector3.zero);
            var back = train.Members[train.Members.Count - 1];
            var miles = LooseCart("Miles away", back.transform.position - new Vector3(0f, 0f, 40f));

            Assert.That(Coupling.WorthHitching(train, new List<VehicleController> { miles }), Is.Null,
                "a cart across the apron hitching itself on would be a train assembling itself");
        }

        [Test]
        public void ACartAlreadyInAnotherTrainCannotBeTakenOutOfIt()
        {
            var mine = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 2, Vector3.zero, "Tug 1");
            var theirs = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 2, new Vector3(0f, 0f, -14f), "Tug 2");

            foreach (var member in theirs.Members)
            {
                Assert.That(Coupling.CanBeHitched(member, mine), Is.False,
                    "pulling a cart out of the middle of somebody else's train breaks that train in " +
                    "two and leaves the hole behind it unaccounted for");
            }
        }

        [Test]
        public void AVehicleSomebodyIsSittingInCannotBeHitchedOnTheBack()
        {
            var train = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 2, Vector3.zero);
            var back = train.Members[train.Members.Count - 1];
            var occupied = m_Apron.AddVehicle(
                m_TractorProfile, "Tug 2", back.transform.position - new Vector3(0f, 0f, 4f), Quaternion.identity);
            occupied.Occupied = true;

            Assert.That(Coupling.CanBeHitched(occupied, train), Is.False,
                "two drivers on one train pull against each other, and neither can let go");
        }

        [Test]
        public void ATrainCannotBeUnhookedBehindItsLastVehicle()
        {
            var train = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 3, Vector3.zero);

            Assert.That(Coupling.CanBeSplitAfter(train, train.Members.Count - 1), Is.False,
                "there is no coupling there to undo");
            Assert.That(Coupling.CanBeSplitAfter(train, 0), Is.True, "but the one behind the tractor is fair game");
        }

        [Test]
        public void ALoneVehicleReportsItselfAsHookedTogether()
        {
            var alone = LooseCart("Cart", Vector3.zero);
            var train = CartChain.Couple(new[] { alone }, ChainJointSettings.Default);

            Assert.That(train.CouplingsEngaged, Is.True,
                "a vehicle on its own has no couplings to engage and is as hooked together as it will " +
                "ever be. Answering false means every uncoupled vehicle in the game reports that it is " +
                "waiting to be joined up, and anything gating on it refuses to act on a lone tractor");
        }
    }
}
