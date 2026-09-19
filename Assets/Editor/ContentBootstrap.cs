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
            CreateIfMissing($"{VehiclesFolder}/BeltLoader.asset", BuildBeltLoader);
            CreateIfMissing($"{AircraftFolder}/RegionalJet.asset", BuildAircraft);
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
            profile.lateralGripCurve = TireCurve();

            profile.maxDriveForceNewtons = 30000f;
            profile.launchDriveMultiplier = 2.5f;
            profile.launchFadesByFractionOfTopSpeed = 0.6f;
            profile.topSpeedMetresPerSecond = 20f;
            profile.bounciness = 0.4f;
            profile.sprintDriveMultiplier = 1.5f;
            profile.maxBrakeForceNewtons = 20000f;
            profile.maxSteerAngleDegrees = 60f;
            profile.steerLockAtTopSpeedDegrees = 30f;
            profile.steerRateDegreesPerSecond = 90f;
            profile.driveable = true;
            return profile;
        }

        static VehicleProfile BuildBeltLoader()
        {
            var profile = ScriptableObject.CreateInstance<VehicleProfile>();
            profile.equipmentNote =
                "Belt loader: the conveyor that runs bags up to a hold door. Around three and a half "
                + "tonnes, and geared for the apron rather than the road, so it tops out near "
                + "25 km/h. It carries no hitch, because a loader is driven to the aircraft and "
                + "never tows a train.";
            profile.massKg = 3400f;

            profile.centerOfMassOffset = new Vector3(0f, 0.4f, 0f);

            profile.suspensionRestLengthMetres = 0.10f;

            profile.springStrengthNewtons = 56000f;
            profile.damperNewtonsPerMetrePerSecond = 19000f;
            profile.coastingDragPerSecond = 0.4f;
            profile.lateralGripCurve = TireCurve();

            profile.maxDriveForceNewtons = 34000f;
            profile.launchDriveMultiplier = 2.5f;
            profile.launchFadesByFractionOfTopSpeed = 0.6f;
            profile.topSpeedMetresPerSecond = 7f;
            profile.sprintDriveMultiplier = 1.2f;
            profile.maxBrakeForceNewtons = 27000f;
            profile.bounciness = 0.4f;

            profile.arcadeHandling = true;
            profile.fastestTurnDegreesPerSecond = 140f;
            profile.turnsIntoItPerSecond = 6f;
            profile.mostSideGripMetresPerSecondSquared = 20f;
            profile.gripHoldsHeadingPerSecond = 3f;

            profile.maxSteerAngleDegrees = 45f;
            profile.steerLockAtTopSpeedDegrees = 25f;
            profile.steerRateDegreesPerSecond = 70f;

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
            profile.coastingDragPerSecond = 0.8f;
            profile.lateralGripCurve = CartTireCurve();
            profile.maxDriveForceNewtons = 0f;
            profile.topSpeedMetresPerSecond = 0f;
            profile.bounciness = 0.4f;

            profile.sprintDriveMultiplier = 1f;
            profile.maxBrakeForceNewtons = 2000f;
            profile.maxSteerAngleDegrees = 55f;
            profile.steerRateDegreesPerSecond = 0f;
            profile.driveable = false;
            return profile;
        }

        static AircraftProfile BuildAircraft()
        {
            var profile = ScriptableObject.CreateInstance<AircraftProfile>();
            profile.equipmentNote =
                "Bombardier CRJ-200 at operating empty weight: near fourteen tonnes before any fuel, "
                + "passengers or bags. It is kinematic and never simulated, so this figure is here "
                + "for reference rather than for physics. Every dimension it has is measured off "
                + "crj_200.fbx when its prefab is built, so none is written down here to drift.";
            profile.massKg = 13835f;
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

        static AnimationCurve CartTireCurve()
            => new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(3f, 20f),
                new Keyframe(12f, 8f));

    }
}
