using System.Collections.Generic;
using System.Linq;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Getting in and out of a vehicle, including being told no.
    ///
    /// The refusal is the case worth having tests for. Two players reaching for the same tractor
    /// at the same instant is rare enough to be impossible to arrange on purpose and common enough
    /// to happen to real players, and half-taking a vehicle you were refused would put a player in
    /// a seat on their own screen and nowhere on anybody else's.
    /// </summary>
    public sealed class VehicleOccupancyTests
    {
        const float Reach = 3f;

        TestApron m_Apron;
        VehicleProfile m_TractorProfile;
        VehicleProfile m_CartProfile;
        Transform m_Crew;
        CartChain m_Train;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
            m_CartProfile = TestProfiles.Cart();
            m_Crew = m_Apron.Track(new GameObject("Crew").transform);
            m_Crew.position = Vector3.zero;
            m_Train = m_Apron.AddTrain(m_TractorProfile, m_CartProfile, cartCount: 4, new Vector3(0f, 0f, 2f));
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
            Object.DestroyImmediate(m_CartProfile);
        }

        VehicleOccupancy SeatWith(IOwnershipBroker broker)
        {
            IReadOnlyList<IDriveable> Nearby() => m_Train.Members.Cast<IDriveable>().ToList();
            return new VehicleOccupancy(m_Crew, broker, Nearby, Reach);
        }

        [Test]
        public void WalkingUpToATractorOffersIt()
        {
            var seat = SeatWith(new RecordingBroker(grant: true));

            seat.Refresh();

            Assert.That(seat.Prompt, Is.EqualTo(OccupancyPrompt.OfferToDrive));
            Assert.That(seat.Offer, Is.SameAs(m_Train.Leader));
            Assert.That(seat.Message, Does.Contain(m_Train.Leader.DisplayName),
                "the offer must say which vehicle it is offering");
        }

        [Test]
        public void StandingWellClearOfEverythingOffersNothing()
        {
            m_Crew.position = new Vector3(0f, 0f, -60f);
            var seat = SeatWith(new RecordingBroker(grant: true));

            seat.Refresh();

            Assert.That(seat.Prompt, Is.EqualTo(OccupancyPrompt.None));
            Assert.That(seat.Offer, Is.Null);
        }

        [Test]
        public void StandingBesideACartOffersNothing()
        {
            m_Crew.position = m_Train.Members[3].transform.position + new Vector3(1.2f, 0f, 0f);
            var seat = SeatWith(new RecordingBroker(grant: true));

            seat.Refresh();

            Assert.That(seat.Offer, Is.Null, "carts are towed, not driven");
        }

        [Test]
        public void TakingATractorAsksToOwnItsWholeTrainBeforeAnythingElseHappens()
        {
            var broker = new RecordingBroker(grant: true);
            var seat = SeatWith(broker);
            seat.Refresh();

            seat.Toggle(new FixedIntent());

            Assert.That(broker.Requests.Count, Is.EqualTo(1));
            Assert.That(broker.Requests[0], Is.EquivalentTo(m_Train.Members),
                "the carts come too, or the couplings end up spanning two machines");
        }

        [Test]
        public void ControlPassesOnceOwnershipIsGranted()
        {
            var seat = SeatWith(new RecordingBroker(grant: true));
            var driver = new FixedIntent();
            seat.Refresh();

            seat.Toggle(driver);

            Assert.That(seat.IsDriving, Is.True);
            Assert.That(seat.Driving, Is.SameAs(m_Train.Leader));
            Assert.That(m_Train.Leader.IntentSource, Is.SameAs(driver),
                "the vehicle takes its steering and throttle from the player who got in");
            Assert.That(seat.Subject, Is.SameAs(m_Train.Leader.transform),
                "the camera follows the vehicle once the player is in it");
        }

        [Test]
        public void ARefusedVehicleLeavesThePlayerStandingWhereTheyWere()
        {
            var seat = SeatWith(new RecordingBroker(grant: false));
            var driver = new FixedIntent();
            seat.Refresh();

            seat.Toggle(driver);

            Assert.That(seat.IsDriving, Is.False);
            Assert.That(seat.Driving, Is.Null);
            Assert.That(m_Train.Leader.IntentSource, Is.Null,
                "a vehicle somebody else owns must not start reading this player's controls");
            Assert.That(seat.Subject, Is.SameAs(m_Crew), "and the camera stays on the character");
            Assert.That(seat.Prompt, Is.EqualTo(OccupancyPrompt.VehicleTaken));
        }

        [Test]
        public void AVehicleBeingDrivenIsNotOfferedToItsOwnDriver()
        {
            var seat = SeatWith(new RecordingBroker(grant: true));
            seat.Refresh();
            seat.Toggle(new FixedIntent());

            seat.Refresh();

            Assert.That(seat.Prompt, Is.EqualTo(OccupancyPrompt.None));
            Assert.That(m_Train.Leader.AcceptsDriver, Is.False, "there is no room for a second driver");
        }

        [Test]
        public void GettingOutLeavesTheVehicleAvailableAndTakesTheCameraOffIt()
        {
            var seat = SeatWith(new RecordingBroker(grant: true));
            var driver = new FixedIntent();
            seat.Refresh();
            seat.Toggle(driver);
            var tractor = m_Train.Leader;

            seat.Toggle(driver);

            Assert.That(seat.IsDriving, Is.False);
            Assert.That(tractor.IntentSource, Is.Null, "the vehicle stops taking orders from somebody who left");
            Assert.That(seat.Subject, Is.SameAs(m_Crew));
            Assert.That(tractor.AcceptsDriver, Is.True, "and it is available again");
        }

        [Test]
        public void SomebodyGettingOutIsPutClearOfTheVehicleRatherThanInsideIt()
        {
            var seat = SeatWith(new RecordingBroker(grant: true));
            var tractor = m_Train.Leader;

            var placed = seat.DismountPosition(tractor);

            var sideways = Vector3.Distance(
                new Vector3(placed.x, 0f, placed.z),
                new Vector3(tractor.transform.position.x, 0f, tractor.transform.position.z));
            var halfWidth = tractor.Profile.bodySizeMetres.x * 0.5f;

            Assert.That(sideways, Is.GreaterThan(halfWidth),
                "stepping out inside the bodywork wedges a rigidbody inside another one");
        }

        [Test]
        public void ASecondPressWhileTheFirstIsStillInFlightDoesNotAskTwice()
        {
            var broker = new SilentBroker();
            var seat = SeatWith(broker);
            seat.Refresh();

            seat.Toggle(new FixedIntent());
            seat.Toggle(new FixedIntent());

            Assert.That(broker.Requests.Count, Is.EqualTo(1),
                "two requests for the same train race each other, and whichever answer arrives second " +
                "overwrites the outcome of the first");
        }

        [Test]
        public void WalkingAwayFromARequestThatIsNeverAnsweredLetsThePlayerAskAgain()
        {
            var broker = new SilentBroker();
            var seat = SeatWith(broker);
            seat.Refresh();
            seat.Toggle(new FixedIntent());
            Assert.That(broker.Requests.Count, Is.EqualTo(1), "precondition: one request is in flight");

            // A request goes to whichever machine is recorded as owning the vehicle. If that machine
            // has left the session, no answer is ever coming.
            m_Crew.position = new Vector3(0f, 0f, -60f);
            seat.Refresh();

            m_Crew.position = Vector3.zero;
            seat.Refresh();
            seat.Toggle(new FixedIntent());

            Assert.That(broker.Requests.Count, Is.EqualTo(2),
                "waiting for ever on an answer that is not coming leaves this player unable to get " +
                "into anything at all for the rest of the session");
        }

        [Test]
        public void PressingTheKeyWithNothingInReachDoesNothing()
        {
            m_Crew.position = new Vector3(0f, 0f, -60f);
            var broker = new RecordingBroker(grant: true);
            var seat = SeatWith(broker);
            seat.Refresh();

            seat.Toggle(new FixedIntent());

            Assert.That(seat.IsDriving, Is.False);
            Assert.That(broker.Requests, Is.Empty, "nothing was offered, so nothing should have been asked for");
        }
    }
}
