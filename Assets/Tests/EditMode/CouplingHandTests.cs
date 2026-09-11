using System;
using System.Collections.Generic;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// A player hooking a cart on and dropping one off while driving.
    ///
    /// Both are ownership events before they are physical ones, and that is the part worth testing.
    /// A train with a cart on the back that another machine is still simulating has a coupling with
    /// one end on each side of an authority boundary -- half its solver working against a body it
    /// cannot move -- which is precisely the failure this game was built to avoid.
    /// </summary>
    public sealed class CouplingHandTests
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
            m_Train = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 2, Vector3.zero);

            m_Hitched = null;
            m_Front = null;
            m_Dropped = null;
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            UnityEngine.Object.DestroyImmediate(m_TractorProfile);
            UnityEngine.Object.DestroyImmediate(m_CartProfile);
        }

        /// <summary>A broker that records what it was asked for and answers as told.</summary>
        sealed class Answering : IOwnershipBroker
        {
            public bool Granting { get; set; } = true;
            public List<IReadOnlyList<VehicleController>> Requested { get; } = new List<IReadOnlyList<VehicleController>>();
            public List<IReadOnlyList<VehicleController>> HandedBack { get; } = new List<IReadOnlyList<VehicleController>>();

            public ulong LocalClientId => 7;

            public ulong OwnerOf(VehicleController vehicle) => 7;

            public bool OwnedByUs(VehicleController vehicle) => true;

            public void RequestAll(IReadOnlyList<VehicleController> vehicles, Action<bool> onResult)
            {
                Requested.Add(new List<VehicleController>(vehicles));
                onResult(Granting);
            }

            public void HandBack(IReadOnlyList<VehicleController> vehicles)
                => HandedBack.Add(new List<VehicleController>(vehicles));
        }

        VehicleController LooseCartBehindTheTrain()
        {
            var back = m_Train.Members[m_Train.Members.Count - 1];
            return m_Apron.AddVehicle(
                m_CartProfile, "Spare", back.transform.position - new Vector3(0.4f, 0f, 4f), Quaternion.identity);
        }

        IReadOnlyList<VehicleController> m_Hitched;
        IReadOnlyList<VehicleController> m_Front;
        IReadOnlyList<VehicleController> m_Dropped;

        CouplingHand HandAt(Answering broker, params VehicleController[] nearby)
        {
            var list = new List<VehicleController>(nearby);
            return new CouplingHand(
                broker,
                () => list,
                hitched: made => m_Hitched = made,
                split: (front, back) => { m_Front = front; m_Dropped = back; })
            {
                Driving = m_Train
            };
        }

        [Test]
        public void ACartInReachIsOffered()
        {
            var hand = HandAt(new Answering(), LooseCartBehindTheTrain());

            hand.Refresh();

            Assert.That(hand.Prompt, Is.EqualTo(CouplingPrompt.OfferToHitch));
        }

        [Test]
        public void ATrainWithCartsOnAndNothingInReachIsOfferedTheChanceToDropOne()
        {
            var hand = HandAt(new Answering());

            hand.Refresh();

            Assert.That(hand.Prompt, Is.EqualTo(CouplingPrompt.OfferToUnhitch));
        }

        [Test]
        public void HitchingAsksForTheWholeResultingTrainAtOnce()
        {
            var broker = new Answering();
            var spare = LooseCartBehindTheTrain();
            var hand = HandAt(broker, spare);
            hand.Refresh();

            hand.Hitch();

            Assert.That(broker.Requested, Has.Count.EqualTo(1));
            Assert.That(broker.Requested[0], Has.Count.EqualTo(4), "three already on the train, plus the new one");
            Assert.That(broker.Requested[0], Does.Contain(spare),
                "asking only for the cart leaves a coupling with one end on a machine that did not " +
                "agree to any of this");
        }

        [Test]
        public void AHitchedCartEndsUpWhereItsCouplingWantsIt()
        {
            var broker = new Answering();
            var spare = LooseCartBehindTheTrain();
            var hand = HandAt(broker, spare);
            hand.Refresh();

            hand.Hitch();
            var longer = m_Hitched;

            var back = longer[longer.Count - 2];
            var theirHitch = back.transform.TransformPoint(back.RearHitchLocal);
            var itsHitch = spare.transform.TransformPoint(spare.FrontHitchLocal);

            Assert.That(Vector3.Distance(theirHitch, itsHitch), Is.LessThan(0.01f),
                "a coupling created while its two ends are apart begins violated, never stops trying " +
                "to close, and drags the train sideways for the rest of the session");
        }

        [Test]
        public void ARefusedHitchChangesNothingAtAll()
        {
            var broker = new Answering { Granting = false };
            var spare = LooseCartBehindTheTrain();
            var whereItWas = spare.transform.position;
            var hand = HandAt(broker, spare);
            hand.Refresh();

            hand.Hitch();

            Assert.That(hand.Prompt, Is.EqualTo(CouplingPrompt.Refused));
            Assert.That(hand.Driving.Members, Has.Count.EqualTo(3), "the train is the length it was");
            Assert.That(spare.transform.position, Is.EqualTo(whereItWas),
                "a cart another machine is still simulating must not be shoved about by this one: it " +
                "would simply put it back, and the two would argue over it");
        }

        [Test]
        public void DroppingCartsOffHandsThemBackRatherThanKeepingThem()
        {
            var broker = new Answering();
            var hand = HandAt(broker);

            hand.UnhitchTheBack();
            var front = m_Front;
            var dropped = m_Dropped;

            Assert.That(front, Has.Count.EqualTo(2));
            Assert.That(dropped, Has.Count.EqualTo(1));
            Assert.That(broker.HandedBack, Has.Count.EqualTo(1),
                "a player who drops a cart and drives away is not driving that cart. Held on to, it is " +
                "equipment nobody else can ever take");
            Assert.That(broker.HandedBack[0], Does.Contain(dropped[0]));
        }

        [Test]
        public void BothHalvesOfASplitTrainAreStillHookedTogether()
        {
            var hand = HandAt(new Answering());

            hand.UnhitchAfter(0);
            var front = m_Front;
            var dropped = m_Dropped;

            Assert.That(front, Has.Count.EqualTo(1), "the tractor on its own");
            Assert.That(dropped, Has.Count.EqualTo(2), "and two carts left standing together");
            Assert.That(dropped, Does.Contain(m_Train.Members[2]),
                "the carts left behind stay together as one train, so they are rebuilt hitched to " +
                "each other rather than as loose boxes to be collected one at a time");
        }

        [Test]
        public void ThePlayerCarriesOnDrivingTheFrontHalf()
        {
            var hand = HandAt(new Answering());
            var tractor = m_Train.Leader;

            hand.UnhitchTheBack();

            Assert.That(m_Front[0], Is.SameAs(tractor),
                "dropping a cart must not take the wheel out of the player's hands");
        }

        [Test]
        public void OneKeyPressHitchesWhenThereIsSomethingToHitch()
        {
            var hand = HandAt(new Answering(), LooseCartBehindTheTrain());
            hand.Refresh();

            hand.Act();

            Assert.That(m_Hitched, Is.Not.Null, "a cart was in reach and offered, so acting should take it");
            Assert.That(m_Dropped, Is.Null, "and nothing should have been dropped");
        }

        [Test]
        public void TheSameKeyDropsACartWhenThereIsNothingToPickUp()
        {
            var hand = HandAt(new Answering());
            hand.Refresh();

            hand.Act();

            Assert.That(m_Dropped, Is.Not.Null,
                "one key doing whichever of the two makes sense is what stops a player having to " +
                "remember which of two keys they wanted");
        }

        [Test]
        public void ActingOnNothingDoesNothing()
        {
            var alone = m_Apron.AddVehicle(m_TractorProfile, "Tug 9", new Vector3(60f, 0f, 0f), Quaternion.identity);
            var hand = new CouplingHand(
                new Answering(),
                () => new List<VehicleController>(),
                hitched: made => m_Hitched = made,
                split: (front, back) => { m_Front = front; m_Dropped = back; })
            {
                Driving = CartChain.Couple(new[] { alone }, ChainJointSettings.Default)
            };
            hand.Refresh();

            hand.Act();

            Assert.That(m_Hitched, Is.Null);
            Assert.That(m_Dropped, Is.Null);
        }

        [Test]
        public void ALoneTractorHasNothingToDrop()
        {
            var alone = m_Apron.AddVehicle(m_TractorProfile, "Tug 9", new Vector3(60f, 0f, 0f), Quaternion.identity);
            var broker = new Answering();
            var hand = new CouplingHand(broker, () => new List<VehicleController>())
            {
                Driving = CartChain.Couple(new[] { alone }, ChainJointSettings.Default)
            };

            hand.UnhitchTheBack();

            Assert.That(m_Dropped, Is.Null, "there is nothing behind it to drop");

            Assert.That(broker.HandedBack, Is.Empty);
        }
    }
}
