using BelowTheWing.Settings;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class DisplayIdentityTests
    {
        static DisplayInfo AScreen(string name, int width, int height, int left)
            => new DisplayInfo
            {
                name = name,
                width = width,
                height = height,
                workArea = new RectInt(left, 0, width, height)
            };

        [Test]
        public void TwoIdenticalMonitorsSideBySideAreNotTheSameScreen()
        {
            var first = AScreen("DELL U2723QE", 3840, 2160, left: 0);
            var second = AScreen("DELL U2723QE", 3840, 2160, left: 3840);

            Assert.That(DisplayIdentity.Same(first, second), Is.False,
                "a pair of matching monitors is the common case; telling them apart by name and " +
                "size alone leaves the window on the first one whichever the player picks");
        }

        [Test]
        public void TheSameMonitorReadTwiceIsTheSameScreen()
        {
            var once = AScreen("DELL U2723QE", 3840, 2160, left: 0);
            var again = AScreen("DELL U2723QE", 3840, 2160, left: 0);

            Assert.That(DisplayIdentity.Same(once, again), Is.True);
        }
    }
}
