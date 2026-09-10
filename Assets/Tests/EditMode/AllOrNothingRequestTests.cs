using System.Collections.Generic;
using BelowTheWing.Vehicles;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Asking for several things at once and keeping none of them unless all of them arrive.
    ///
    /// This is the rule that stops a cart train ending up simulated by two machines. It used to
    /// live inside the networking layer, where it could only be exercised by two real clients
    /// racing each other for the same tractor -- which is to say, never on purpose.
    /// </summary>
    public sealed class AllOrNothingRequestTests
    {
        static IReadOnlyList<string> Wanted(params string[] items) => items;

        [Test]
        public void ADuplicateAnswerArrivingEarlyDoesNotSettleTheRequest()
        {
            var handedBack = new List<string>();
            var told = new List<bool>();
            var request = new AllOrNothingRequest<string>(Wanted("tractor", "cart 1", "cart 2"), handedBack.Add, told.Add);

            request.Answer("tractor", true);
            request.Answer("tractor", true);
            request.Answer("cart 1", true);

            Assert.That(told, Is.Empty,
                "counting answers rather than tracking which ones arrived makes the guarantee depend " +
                "on the caller never repeating itself. Cart 2 has not answered and the request is not done");
            Assert.That(request.Settled, Is.False);
        }

        [Test]
        public void AnAnswerForSomethingNeverAskedForIsIgnored()
        {
            var told = new List<bool>();
            var request = new AllOrNothingRequest<string>(Wanted("tractor", "cart 1"), _ => { }, told.Add);

            request.Answer("a cart from another train", false);
            request.Answer("tractor", true);
            request.Answer("cart 1", true);

            Assert.That(told, Is.EqualTo(new[] { true }),
                "a stray answer must not refuse a request that was in fact entirely granted");
        }

        [Test]
        public void NobodyIsToldAnythingUntilEveryAnswerIsIn()
        {
            var told = new List<bool>();
            var request = new AllOrNothingRequest<string>(Wanted("tractor", "cart 1", "cart 2"), _ => { }, granted => told.Add(granted));

            request.Answer("tractor", true);
            request.Answer("cart 1", true);

            Assert.That(told, Is.Empty, "two answers out of three decide nothing");
            Assert.That(request.Settled, Is.False);
        }

        [Test]
        public void EverythingGrantedIsReportedAsSuccessAndNothingIsHandedBack()
        {
            var handedBack = new List<string>();
            var told = new List<bool>();
            var request = new AllOrNothingRequest<string>(Wanted("tractor", "cart 1"), handedBack.Add, told.Add);

            request.Answer("tractor", true);
            request.Answer("cart 1", true);

            Assert.That(told, Is.EqualTo(new[] { true }));
            Assert.That(handedBack, Is.Empty);
        }

        [Test]
        public void OneRefusalHandsBackEverythingElseThatWasGranted()
        {
            var handedBack = new List<string>();
            var told = new List<bool>();
            var request = new AllOrNothingRequest<string>(Wanted("tractor", "cart 1", "cart 2"), handedBack.Add, told.Add);

            request.Answer("tractor", true);
            request.Answer("cart 1", true);
            request.Answer("cart 2", false);

            Assert.That(told, Is.EqualTo(new[] { false }));
            Assert.That(handedBack, Is.EquivalentTo(new[] { "tractor", "cart 1" }),
                "keeping the two that were granted is exactly the split train this rule exists to prevent");
        }

        [Test]
        public void ARefusalArrivingFirstStillHandsBackWhatComesAfterwards()
        {
            var handedBack = new List<string>();
            var told = new List<bool>();
            var request = new AllOrNothingRequest<string>(Wanted("tractor", "cart 1", "cart 2"), handedBack.Add, told.Add);

            request.Answer("tractor", false);
            request.Answer("cart 1", true);
            request.Answer("cart 2", true);

            Assert.That(told, Is.EqualTo(new[] { false }));
            Assert.That(handedBack, Is.EquivalentTo(new[] { "cart 1", "cart 2" }),
                "answers arrive in whatever order the network delivers them, so a refusal cannot rely " +
                "on being last");
        }

        [Test]
        public void NothingIsHandedBackTwiceAndNobodyIsToldTwice()
        {
            var handedBack = new List<string>();
            var told = new List<bool>();
            var request = new AllOrNothingRequest<string>(Wanted("tractor", "cart 1"), handedBack.Add, told.Add);

            request.Answer("tractor", true);
            request.Answer("cart 1", false);

            request.Answer("cart 1", false);
            request.Answer("tractor", true);

            Assert.That(told.Count, Is.EqualTo(1), "a late or repeated answer must not report a second outcome");
            Assert.That(handedBack, Is.EqualTo(new[] { "tractor" }));
            Assert.That(request.Settled, Is.True);
        }

        [Test]
        public void ARequestForOneThingIsSettledByItsSingleAnswer()
        {
            var told = new List<bool>();
            var request = new AllOrNothingRequest<string>(Wanted("tractor"), _ => { }, told.Add);

            request.Answer("tractor", true);

            Assert.That(told, Is.EqualTo(new[] { true }));
        }
    }
}
