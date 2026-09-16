using BelowTheWing.Apron;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    public static class TestProfiles
    {
        public static VehicleProfile Tractor()
        {
            var p = ScriptableObject.CreateInstance<VehicleProfile>();
            p.equipmentNote = "Baggage tractor, empty. Roughly 2.5 tonnes.";
            p.massKg = 2500f;
            p.centerOfMassOffset = new Vector3(0f, 0.55f, 0f);
            p.suspensionRestLengthMetres = 0.10f;
            p.springStrengthNewtons = 41000f;
            p.damperNewtonsPerMetrePerSecond = 14000f;
            p.lateralGripCurve = PeakingGripCurve();
            p.maxDriveForceNewtons = 30000f;
            p.sprintDriveMultiplier = 1.5f;
            p.topSpeedMetresPerSecond = 20f;
            p.coastingDragPerSecond = 0.4f;
            p.bounciness = 0.4f;
            p.maxBrakeForceNewtons = 20000f;
            p.maxSteerAngleDegrees = 60f;
            p.steerLockAtTopSpeedDegrees = 30f;
            p.launchDriveMultiplier = 6f;
            p.launchFadesByFractionOfTopSpeed = 0.6f;
            p.steerRateDegreesPerSecond = 120f;
            p.driveable = true;
            return p;
        }

        public static VehicleProfile Cart()
        {
            var p = ScriptableObject.CreateInstance<VehicleProfile>();
            p.equipmentNote = "Four-wheel baggage cart, tare, unloaded. Roughly 550 kg.";
            p.massKg = 550f;
            p.centerOfMassOffset = new Vector3(0f, 0.5f, 0f);
            p.suspensionRestLengthMetres = 0.08f;
            p.springStrengthNewtons = 9000f;
            p.damperNewtonsPerMetrePerSecond = 3500f;
            p.lateralGripCurve = CartGripCurve();
            p.maxDriveForceNewtons = 0f;
            p.sprintDriveMultiplier = 1f;
            p.topSpeedMetresPerSecond = 0f;
            p.coastingDragPerSecond = 0.8f;
            p.bounciness = 0.4f;
            p.maxBrakeForceNewtons = 2000f;
            p.maxSteerAngleDegrees = 0f;
            p.steerRateDegreesPerSecond = 0f;
            p.driveable = false;
            return p;
        }

        public static CrewProfile CrewMember()
        {
            var p = ScriptableObject.CreateInstance<CrewProfile>();
            p.massKg = 80f;
            p.heightMetres = 1.8f;
            p.radiusMetres = 0.3f;
            p.walkSpeedMetresPerSecond = 4f;
            p.sprintSpeedMetresPerSecond = 7f;
            p.footGripMetresPerSecondSquared = 10f;
            p.gaitResponseMetresPerSecondSquared = 30f;
            return p;
        }

        public static BagProfile CheckedBag()
        {
            var p = ScriptableObject.CreateInstance<BagProfile>();
            p.massKg = 20f;
            p.sizeMetres = new Vector3(0.4f, 0.25f, 0.6f);
            p.frictionCoefficient = 0.3f;
            return p;
        }

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


        public static AnimationCurve CartGripCurve()
            => new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(3f, 20f),
                new Keyframe(12f, 8f));

        public static AnimationCurve PeakingGripCurve()
            => new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(3f, 12f),
                new Keyframe(12f, 5f));
    }
}
