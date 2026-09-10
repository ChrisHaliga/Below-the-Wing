using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    /// <summary>
    /// Profiles for tests to run against, built in memory rather than loaded from project assets.
    ///
    /// Tests use these instead of the assets the game ships with so that retuning a vehicle for
    /// feel cannot turn a test red. The numbers here are the same real-world figures the shipped
    /// profiles use, because several tests are about those figures being real.
    /// </summary>
    public static class TestProfiles
    {
        /// <summary>
        /// A baggage tractor: the small diesel or electric unit that tows carts around an apron.
        /// Around three tonnes empty.
        /// </summary>
        public static VehicleProfile Tractor()
        {
            var p = ScriptableObject.CreateInstance<VehicleProfile>();
            p.equipmentNote = "Baggage tractor, empty. Roughly 3 tonnes.";
            p.massKg = 3000f;
            p.bodySizeMetres = new Vector3(1.3f, 1.6f, 3.0f);
            p.centerOfMassOffset = new Vector3(0f, -0.45f, 0f);
            p.wheelbaseMetres = 1.8f;
            p.trackMetres = 1.1f;
            p.wheelRadiusMetres = 0.3f;
            p.suspensionRestLengthMetres = 0.35f;
            p.springStrengthNewtons = 60000f;
            p.damperNewtonsPerMetrePerSecond = 6000f;
            p.lateralGripCurve = PeakingGripCurve();
            p.maxDriveForceNewtons = 14000f;
            p.sprintDriveMultiplier = 1.5f;
            p.coastingDragPerSecond = 0.4f;
            p.maxBrakeForceNewtons = 20000f;
            p.maxSteerAngleDegrees = 45f;
            p.steerRateDegreesPerSecond = 120f;
            p.driveable = true;
            return p;
        }

        /// <summary>
        /// A baggage cart: the four-wheeled box towed behind a tractor. Around half a tonne empty,
        /// and never driven by anybody -- it goes where it is pulled.
        /// </summary>
        public static VehicleProfile Cart()
        {
            var p = ScriptableObject.CreateInstance<VehicleProfile>();
            p.equipmentNote = "Four-wheel baggage cart, tare, unloaded. Roughly 550 kg.";
            p.massKg = 550f;
            p.bodySizeMetres = new Vector3(1.5f, 1.7f, 3.0f);
            p.centerOfMassOffset = new Vector3(0f, -0.3f, 0f);
            p.wheelbaseMetres = 2.0f;
            p.trackMetres = 1.3f;
            p.wheelRadiusMetres = 0.3f;
            p.suspensionRestLengthMetres = 0.35f;
            p.springStrengthNewtons = 12000f;
            p.damperNewtonsPerMetrePerSecond = 1400f;
            p.lateralGripCurve = PeakingGripCurve();
            p.maxDriveForceNewtons = 0f;
            p.sprintDriveMultiplier = 1f;
            p.coastingDragPerSecond = 0.4f;
            p.maxBrakeForceNewtons = 2000f;
            p.maxSteerAngleDegrees = 0f;
            p.steerRateDegreesPerSecond = 0f;
            p.driveable = false;
            return p;
        }

        /// <summary>A ramp worker: eighty kilograms of person.</summary>
        public static CrewProfile CrewMember()
        {
            var p = ScriptableObject.CreateInstance<CrewProfile>();
            p.massKg = 80f;
            p.heightMetres = 1.8f;
            p.radiusMetres = 0.3f;
            p.walkSpeedMetresPerSecond = 4f;
            p.sprintSpeedMetresPerSecond = 7f;
            p.accelerationMetresPerSecondSquared = 30f;
            p.turnRateDegreesPerSecond = 720f;
            return p;
        }

        /// <summary>A narrowbody airliner, at its operating empty weight.</summary>
        public static AircraftProfile Aircraft()
        {
            var p = ScriptableObject.CreateInstance<AircraftProfile>();
            p.equipmentNote = "Narrowbody airliner at operating empty weight, roughly 41 tonnes.";
            p.massKg = 41400f;
            p.lengthMetres = 39.5f;
            p.fuselageDiameterMetres = 3.76f;
            p.centrelineHeightMetres = 3.4f;
            return p;
        }

        /// <summary>
        /// A tire curve that rises to a peak and then falls away: sideways force in newtons per
        /// kilogram carried, against how fast the contact patch is sliding sideways.
        ///
        /// The falling half is the part that matters. A curve that only ever rises puts the vehicle
        /// on rails, and nothing can slide, spin or jackknife however hard it is thrown into a corner.
        /// </summary>
        public static AnimationCurve PeakingGripCurve()
            => new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(3f, 12f),
                new Keyframe(12f, 5f));
    }
}
