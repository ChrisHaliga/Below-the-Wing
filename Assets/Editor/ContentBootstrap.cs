using System.IO;
using BelowTheWing.Apron;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    public static class ContentBootstrap
    {
        const string VehiclesFolder = "Assets/Content/Vehicles";
        const string AircraftFolder = "Assets/Content/Aircraft";
        const string CrewFolder = "Assets/Content/Crew";
        const string CargoFolder = "Assets/Content/Cargo";

        [MenuItem("Below the Wing/Create missing content")]
        public static void CreateMissingContent()
        {
            CreateIfMissing($"{VehiclesFolder}/BaggageTractor.asset", BuildTractor);
            CreateIfMissing($"{VehiclesFolder}/BaggageCart.asset", BuildCart);
            CreateIfMissing($"{AircraftFolder}/NarrowbodyAirliner.asset", BuildAircraft);
            CreateIfMissing($"{CrewFolder}/RampWorker.asset", BuildRampWorker);
            CreateIfMissing($"{CargoFolder}/CheckedBag.asset", BuildCheckedBag);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void CreateIfMissing<T>(string path, System.Func<T> build) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

            var asset = build();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);

            Debug.Log($"Created {path}");
        }

        static VehicleProfile BuildTractor()
        {
            var profile = ScriptableObject.CreateInstance<VehicleProfile>();
            profile.equipmentNote =
                "Baggage tractor: the small towing unit that pulls cart trains around an apron. "
                + "Two and a half tonnes empty, which is typical of the diesel and electric units "
                + "in this class at 2.8 m long.";
            profile.massKg = 2500f;

            profile.centerOfMassOffset = new Vector3(0f, 0.55f, 0f);

            profile.suspensionRestLengthMetres = 0.10f;

            profile.springStrengthNewtons = 41000f;
            profile.damperNewtonsPerMetrePerSecond = 14000f;
            profile.coastingDragPerSecond = 0.4f;
            profile.lateralGripCurve = DrivenTireCurve();

            profile.maxDriveForceNewtons = 20000f;
            profile.launchDriveMultiplier = 3f;
            profile.topSpeedMetresPerSecond = 20f;
            profile.bounciness = 0.4f;
            profile.sprintDriveMultiplier = 1.5f;
            profile.maxBrakeForceNewtons = 20000f;
            profile.maxSteerAngleDegrees = 60f;
            profile.steerLockAtTopSpeedDegrees = 30f;
            profile.steerRateDegreesPerSecond = 120f;
            profile.driveable = true;
            return profile;
        }

        static BagProfile BuildCheckedBag()
        {
            var profile = ScriptableObject.CreateInstance<BagProfile>();

            profile.massKg = 20f;
            profile.sizeMetres = new Vector3(0.4f, 0.25f, 0.6f);
            profile.frictionCoefficient = 0.3f;

            return profile;
        }

        static VehicleProfile BuildCart()
        {
            var profile = ScriptableObject.CreateInstance<VehicleProfile>();
            profile.equipmentNote =
                "Four-wheel baggage cart, tare weight with nothing in it. Around 550 kg empty, and "
                + "rated to carry roughly three times that again in bags. It is towed and never "
                + "driven, so it has no engine and no steering of its own.";
            profile.massKg = 550f;

            profile.centerOfMassOffset = new Vector3(0f, 0.5f, 0f);

            profile.suspensionRestLengthMetres = 0.08f;

            profile.springStrengthNewtons = 9000f;
            profile.damperNewtonsPerMetrePerSecond = 3500f;
            profile.coastingDragPerSecond = 0.1f;
            profile.lateralGripCurve = TireCurve();
            profile.maxDriveForceNewtons = 0f;
            profile.topSpeedMetresPerSecond = 0f;
            profile.bounciness = 0.4f;

            profile.sprintDriveMultiplier = 1f;
            profile.maxBrakeForceNewtons = 2000f;
            profile.maxSteerAngleDegrees = 0f;
            profile.steerRateDegreesPerSecond = 0f;
            profile.driveable = false;
            return profile;
        }

        static AircraftProfile BuildAircraft()
        {
            var profile = ScriptableObject.CreateInstance<AircraftProfile>();
            profile.equipmentNote =
                "Narrowbody airliner at operating empty weight: about forty tonnes before any fuel, "
                + "passengers or bags. It is kinematic and never simulated, so this figure is here "
                + "for reference rather than for physics.";
            profile.massKg = 41400f;
            profile.lengthMetres = 39.5f;
            profile.fuselageDiameterMetres = 3.76f;
            profile.centrelineHeightMetres = 3.4f;
            return profile;
        }

        static CrewProfile BuildRampWorker()
        {
            var profile = ScriptableObject.CreateInstance<CrewProfile>();
            profile.massKg = 80f;
            profile.heightMetres = 1.8f;
            profile.radiusMetres = 0.3f;
            profile.walkSpeedMetresPerSecond = 4f;
            profile.sprintSpeedMetresPerSecond = 7f;

            profile.footGripMetresPerSecondSquared = 10f;
            profile.gaitResponseMetresPerSecondSquared = 30f;
            return profile;
        }

        static AnimationCurve TireCurve()
            => new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(3f, 12f),
                new Keyframe(12f, 5f));

        static AnimationCurve DrivenTireCurve()
            => new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(3f, 16f),
                new Keyframe(8f, 18f),
                new Keyframe(20f, 13f));
    }
}
