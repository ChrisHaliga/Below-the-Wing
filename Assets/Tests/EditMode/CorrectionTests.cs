using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Bringing a vehicle back to where its owner says it is, without stopping it being a vehicle.
    ///
    /// The tension underneath all of these: the owner decides where a vehicle is, and the vehicle is
    /// also a physical body here with momentum that other things are pushing against. Satisfying the
    /// first by assignment destroys the second, so the owner's word arrives as a change in what the
    /// vehicle is doing rather than in where it is.
    /// </summary>
    public sealed class CorrectionTests
    {
        static readonly CorrectionSettings Settings = CorrectionSettings.Default;

        static VehicleState Said(Vector3 position, Vector3 velocity = default, Quaternion? facing = null)
            => new VehicleState
            {
                Position = position,
                Rotation = facing ?? Quaternion.identity,
                Velocity = velocity,
                Spin = Vector3.zero
            };

        [Test]
        public void AMovingVehicleIsExpectedToHaveCarriedOnMoving()
        {
            var said = Said(Vector3.zero, velocity: new Vector3(0f, 0f, 10f));

            var shouldBe = Correction.WhereItShouldBeNow(said, secondsSince: 0.15f, Settings);

            Assert.That(shouldBe.z, Is.EqualTo(1.5f).Within(1e-3f),
                "a message that took 150 ms describes where the vehicle was, not where it is. Measured " +
                "against the raw position, a vehicle driven straight down the apron is permanently " +
                "'wrong' by a metre and a half and gets fought all the way");
        }

        [Test]
        public void AStaleUpdateStopsBeingExtrapolatedFrom()
        {
            var said = Said(Vector3.zero, velocity: new Vector3(0f, 0f, 10f));

            var atTheCeiling = Correction.WhereItShouldBeNow(said, Settings.extrapolationCeilingSeconds, Settings);
            var longAfter = Correction.WhereItShouldBeNow(said, secondsSince: 30f, Settings);

            Assert.That(longAfter, Is.EqualTo(atTheCeiling),
                "a carried velocity stops being a prediction once it is old. Thirty seconds on, the " +
                "vehicle has braked, turned or hit something, and running its last known speed onward " +
                "invents three hundred metres of travel nobody drove");
        }

        [Test]
        public void AVehicleAlreadyWhereItShouldBeIsLeftAlone()
        {
            var said = Said(new Vector3(0f, 0f, 5f), velocity: new Vector3(0f, 0f, 10f));
            var here = Correction.WhereItShouldBeNow(said, secondsSince: 0.1f, Settings);

            var nudge = Correction.Nudge(here, said.Velocity, said, secondsSince: 0.1f, Settings);

            Assert.That(nudge.magnitude, Is.LessThan(1e-3f),
                "a vehicle keeping up with its owner must not be pushed at all, or correction becomes " +
                "a permanent force acting on every remote vehicle in the session");
        }

        [Test]
        public void AVehicleLeftBehindIsPushedTowardsWhereItShouldBe()
        {
            var said = Said(new Vector3(0f, 0f, 10f));
            var here = new Vector3(0f, 0f, 8f);

            var nudge = Correction.Nudge(here, Vector3.zero, said, secondsSince: 0f, Settings);

            Assert.That(nudge.z, Is.GreaterThan(0f), "it is two metres behind, so it has to be sped up");
            Assert.That(nudge.x, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void AVehicleRunningAheadIsPulledBack()
        {
            var said = Said(new Vector3(0f, 0f, 10f));
            var here = new Vector3(0f, 0f, 12f);

            var nudge = Correction.Nudge(here, Vector3.zero, said, secondsSince: 0f, Settings);

            Assert.That(nudge.z, Is.LessThan(0f), "it is two metres ahead, so it has to be slowed");
        }

        [Test]
        public void ABiggerErrorIsCorrectedHarder()
        {
            var said = Said(Vector3.zero);

            var slightly = Correction.Nudge(new Vector3(0f, 0f, -0.5f), Vector3.zero, said, 0f, Settings).magnitude;
            var badly = Correction.Nudge(new Vector3(0f, 0f, -1.5f), Vector3.zero, said, 0f, Settings).magnitude;

            Assert.That(badly, Is.GreaterThan(slightly),
                "a standing error has to be walked in rather than tolerated at whatever size it reached");
        }

        [Test]
        public void CorrectionCarriesTheOwnersSpeedRatherThanOnlyClosingTheGap()
        {
            // Exactly where it should be, but standing still while its owner is driving away.
            var said = Said(Vector3.zero, velocity: new Vector3(0f, 0f, 10f));

            var nudge = Correction.Nudge(Vector3.zero, Vector3.zero, said, secondsSince: 0f, Settings);

            Assert.That(nudge.z, Is.GreaterThan(0f),
                "matching only the position leaves a vehicle repeatedly falling behind and being hauled " +
                "back, which is what rubber-banding looks like from the outside");
        }

        [Test]
        public void AVehicleSomewhereElseEntirelyIsNotBlendedAcrossTheApron()
        {
            var said = Said(new Vector3(0f, 0f, 40f));

            Assert.That(Correction.TooFarToBlend(Vector3.zero, said.Position, Settings), Is.True,
                "easing forty metres takes long enough to be watched, and what gets watched is a " +
                "tractor sliding across the apron under its own power");
        }

        [Test]
        public void AnOrdinaryErrorIsNotSnapped()
        {
            var said = Said(new Vector3(0f, 0f, 1f));

            Assert.That(Correction.TooFarToBlend(Vector3.zero, said.Position, Settings), Is.False,
                "snapping what could have been blended is the artefact blending exists to avoid");
        }

        [Test]
        public void AVehicleFacingTheWrongWayIsSpunTowardsTheRightWay()
        {
            var facing = Quaternion.identity;
            var said = Said(Vector3.zero, facing: Quaternion.Euler(0f, 30f, 0f));

            var spin = Correction.SpinNudge(facing, Vector3.zero, said, Settings);

            Assert.That(spin.y, Is.GreaterThan(0f), "it is thirty degrees short, so it has to turn that way");
            Assert.That(spin.x, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(spin.z, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void AVehicleAlreadyFacingTheRightWayIsNotSpun()
        {
            var facing = Quaternion.Euler(0f, 40f, 0f);
            var said = Said(Vector3.zero, facing: facing);

            Assert.That(Correction.SpinNudge(facing, Vector3.zero, said, Settings).magnitude, Is.LessThan(1e-3f),
                "a vehicle pointing where its owner says it points must not be twisted anyway");
        }

        [Test]
        public void AVehicleSlightlyPastTheRightWayTurnsBackTheShortWayRound()
        {
            // Ten degrees beyond where it should be: the short way back is a small negative turn,
            // and the long way round is three hundred and fifty degrees of spin nobody performed.
            var facing = Quaternion.Euler(0f, 10f, 0f);
            var said = Said(Vector3.zero, facing: Quaternion.identity);

            var spin = Correction.SpinNudge(facing, Vector3.zero, said, Settings);

            Assert.That(spin.y, Is.LessThan(0f),
                "an angle reported as the positive turn is nearly a full circle when the real one is " +
                "small and the other way. Taken at face value the vehicle whips round the long way");
            Assert.That(spin.magnitude, Is.LessThan(3f), "and it is a small correction, not a full revolution");
        }
    }
}
