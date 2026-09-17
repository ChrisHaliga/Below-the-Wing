using System;
using System.Threading.Tasks;
using BelowTheWing.Net;
using NUnit.Framework;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class ServiceLeaveTests
    {
        [Test]
        public void NothingIsOwedBeforeALeaveStarts()
        {
            Assert.That(new ServiceLeave().Pending, Is.False);
        }

        [Test]
        public async Task ALeaveIsOwedUntilTheServiceAnswers()
        {
            var service = new TaskCompletionSource<bool>();
            var leave = new ServiceLeave();

            var running = leave.Run(() => service.Task);

            Assert.That(leave.Pending, Is.True,
                "the leave has been asked for and the service has not answered, so the player " +
                "still owes it one; a quit that goes through now leaves a session record behind");

            service.SetResult(true);
            await running;

            Assert.That(leave.Pending, Is.False);
        }

        [Test]
        public async Task AFailedLeaveIsNoLongerOwedAndSaysWhy()
        {
            var service = new TaskCompletionSource<bool>();
            var leave = new ServiceLeave();

            var running = leave.Run(() => service.Task);
            service.SetException(new InvalidOperationException("no route"));

            try
            {
                await running;
                Assert.Fail("a leave the service refused has to surface, or the boundary cannot log it once");
            }
            catch (InvalidOperationException) { }

            Assert.That(leave.Pending, Is.False,
                "a leave that failed is over; holding the quit open on it would keep the game " +
                "from closing, which is the complaint the whole path exists to answer");
        }

        [Test]
        public async Task ALeaveThatIsAlreadyDoneIsNotOwed()
        {
            var leave = new ServiceLeave();

            await leave.Run(() => Task.CompletedTask);

            Assert.That(leave.Pending, Is.False);
        }
    }
}
