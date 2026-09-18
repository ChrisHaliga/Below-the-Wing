using BelowTheWing.Menu;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class MenuCameraTravelTests
    {
        [Test]
        public void HalfTheTimeIsHalfTheTravelWhateverTheDurationIs()
        {
            Assert.That(MenuCamera.HowFar(1f, 2f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(MenuCamera.HowFar(0.35f, 0.7f), Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void ATravelPastItsDurationStaysAtItsEnd()
        {
            Assert.That(MenuCamera.HowFar(9f, 2f), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void ATravelWithNoDurationIsAlreadyOver()
        {
            Assert.That(MenuCamera.HowFar(0f, 0f), Is.EqualTo(1f).Within(1e-4f),
                "a zero duration must not divide by zero and must not leave the camera stranded " +
                "at the station it came from");
        }

        [Test]
        public void TravellingBetweenTwoPosesStartsAtOneAndEndsAtTheOther()
        {
            var from = new Pose(Vector3.zero, Quaternion.identity);
            var to = new Pose(new Vector3(0f, 0f, 20f), Quaternion.Euler(0f, 90f, 0f));

            Assert.That(MenuCamera.Between(from, to, 0f).position.z, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(MenuCamera.Between(from, to, 1f).position.z, Is.EqualTo(20f).Within(1e-3f));
        }

        [Test]
        public void ATravelEasesRatherThanStartingAndStoppingDead()
        {
            var from = new Pose(Vector3.zero, Quaternion.identity);
            var to = new Pose(new Vector3(0f, 0f, 100f), Quaternion.identity);

            Assert.That(MenuCamera.Between(from, to, 0.5f).position.z, Is.EqualTo(50f).Within(1f));
            Assert.That(MenuCamera.Between(from, to, 0.1f).position.z, Is.LessThan(10f),
                "a linear move covers a tenth of the ground in a tenth of the time, and reads as " +
                "the camera being yanked rather than flown");
        }
    }
}
