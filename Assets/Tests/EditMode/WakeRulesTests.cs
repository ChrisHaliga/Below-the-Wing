using BelowTheWing.Cargo;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// When something riding on a carrier has been shaken loose.
    ///
    /// This is the number the whole slice is about. A bag leaving a cart is the game's central
    /// joke, and it only works if a player can learn where the edge is -- which means the edge has
    /// to be somewhere, and the same somewhere on every screen.
    /// </summary>
    public sealed class WakeRulesTests
    {
        static readonly WakeThresholds Bag = new WakeThresholds(lateralAcceleration: 6f, tiltDegrees: 25f, impulse: 400f);

        [Test]
        public void GoingTwiceAsFastRoundTheSameCornerThrowsThingsFourTimesAsHard()
        {
            var gentle = WakeRules.LateralAcceleration(speedMetresPerSecond: 4f, turnRadiusMetres: 10f);
            var quick = WakeRules.LateralAcceleration(speedMetresPerSecond: 8f, turnRadiusMetres: 10f);

            Assert.That(quick, Is.EqualTo(gentle * 4f).Within(1e-3f),
                "speed rather than steering is what loses a load, and this is why");
        }

        [Test]
        public void AWiderCornerThrowsThingsLessHard()
        {
            var tight = WakeRules.LateralAcceleration(6f, turnRadiusMetres: 5f);
            var wide = WakeRules.LateralAcceleration(6f, turnRadiusMetres: 20f);

            Assert.That(wide, Is.LessThan(tight));
        }

        [Test]
        public void DrivingStraightThrowsNothing()
        {
            Assert.That(WakeRules.LateralAcceleration(12f, turnRadiusMetres: 0f), Is.Zero,
                "a radius of nothing is a straight line, not an infinitely tight corner");
        }

        [Test]
        public void ACarrierStandingLevelIsNotLeaning()
        {
            Assert.That(WakeRules.TiltDegrees(Quaternion.identity), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void TurningDoesNotCountAsLeaning()
        {
            Assert.That(WakeRules.TiltDegrees(Quaternion.Euler(0f, 90f, 0f)), Is.EqualTo(0f).Within(1e-3f),
                "a cart driving round a corner is not tipping, and treating a heading as a lean " +
                "would drop its load the moment it turned at all");
        }

        [Test]
        public void GoingOverCountsAsLeaning()
        {
            Assert.That(WakeRules.TiltDegrees(Quaternion.Euler(0f, 0f, 40f)), Is.EqualTo(40f).Within(0.5f));
        }

        [Test]
        public void NothingComesOffACarrierDoingNothingInParticular()
        {
            Assert.That(WakeRules.ShakenLoose(0.5f, 2f, 0f, Bag), Is.False,
                "a cart idling on the apron has to be able to hold its load indefinitely");
        }

        [Test]
        public void AHardCornerIsEnoughOnItsOwn()
        {
            Assert.That(WakeRules.ShakenLoose(7f, 0f, 0f, Bag), Is.True);
        }

        [Test]
        public void TippingIsEnoughOnItsOwn()
        {
            Assert.That(WakeRules.ShakenLoose(0f, 30f, 0f, Bag), Is.True,
                "a cart that is going over loses its load whether or not it was also cornering hard");
        }

        [Test]
        public void BeingHitIsEnoughOnItsOwn()
        {
            Assert.That(WakeRules.ShakenLoose(0f, 0f, 500f, Bag), Is.True,
                "a parked cart rammed by a tractor sheds its bags, and it was neither turning nor leaning");
        }

        [Test]
        public void HoldingOnRaisesTheBarWithoutRemovingIt()
        {
            var gripping = Bag.HoldingOn(3f);

            Assert.That(WakeRules.ShakenLoose(7f, 0f, 0f, gripping), Is.False,
                "a corner that would throw a standing rider is survivable by one holding on");
            Assert.That(WakeRules.ShakenLoose(20f, 0f, 0f, gripping), Is.True,
                "but holding on is a better grip, not immunity");
        }

        [Test]
        public void BeingHitThroughAHandholdStillThrowsYou()
        {
            var gripping = Bag.HoldingOn(3f);

            Assert.That(WakeRules.ShakenLoose(0f, 0f, 500f, gripping), Is.True,
                "holding on is a grip against being swung about. A head-on collision is not that, " +
                "and surviving one by holding a rail would read as the game ignoring the crash");
        }

        [Test]
        public void TheSafeSpeedForACornerIsTheOneTheThresholdImplies()
        {
            var fastest = WakeRules.FastestSafeSpeed(turnRadiusMetres: 10f, Bag);

            Assert.That(WakeRules.LateralAcceleration(fastest, 10f), Is.EqualTo(Bag.LateralAcceleration).Within(0.01f),
                "driving at exactly the speed this reports has to put the load exactly on its limit, " +
                "or the number is no use for deciding what a safe speed is");
        }

        [Test]
        public void AWiderCornerCanBeTakenFaster()
        {
            Assert.That(WakeRules.FastestSafeSpeed(20f, Bag), Is.GreaterThan(WakeRules.FastestSafeSpeed(5f, Bag)));
        }
    }
}
