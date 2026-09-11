using System.IO;
using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    /// <summary>
    /// Creates the equipment profiles the game runs on, for a project that does not have them yet.
    ///
    /// The figures are real ones for the equipment being modelled. Masses in particular are not
    /// arbitrary: a spring rate only means something next to the weight it is holding up, and a
    /// three tonne tractor towing half-tonne carts behaves differently from any pair of numbers
    /// that happen to feel right.
    ///
    /// Drag is the deliberate exception. A real baggage tractor tops out around 23 km/h, which is
    /// accurate and no fun to drive: the design asks for plausible rather than precise, and a game
    /// where crossing the apron is a chore has chosen the wrong one. It is set to give roughly
    /// 36 km/h cruising and 54 km/h with sprint held.
    ///
    /// Only missing assets are created. Anything already on disk has been tuned by hand and is left
    /// exactly as it is, so this can be run again safely at any time.
    /// </summary>
    public static class ContentBootstrap
    {
        const string VehiclesFolder = "Assets/Content/Vehicles";
        const string AircraftFolder = "Assets/Content/Aircraft";
        const string CrewFolder = "Assets/Content/Crew";

        [MenuItem("Below the Wing/Create missing content")]
        public static void CreateMissingContent()
        {
            CreateIfMissing($"{VehiclesFolder}/BaggageTractor.asset", BuildTractor);
            CreateIfMissing($"{VehiclesFolder}/BaggageCart.asset", BuildCart);
            CreateIfMissing($"{AircraftFolder}/NarrowbodyAirliner.asset", BuildAircraft);
            CreateIfMissing($"{CrewFolder}/RampWorker.asset", BuildRampWorker);

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
                + "Around three tonnes empty, which is typical of the diesel and electric units in "
                + "this class.";
            profile.massKg = 3000f;
            profile.bodySizeMetres = new Vector3(1.3f, 1.6f, 3.0f);
            profile.centerOfMassOffset = new Vector3(0f, -0.45f, 0f);
            profile.drawbarLengthMetres = 0.3f;
            profile.wheelbaseMetres = 1.8f;
            profile.trackMetres = 1.1f;
            profile.wheelRadiusMetres = 0.3f;
            profile.suspensionRestLengthMetres = 0.35f;
            profile.springStrengthNewtons = 60000f;
            profile.damperNewtonsPerMetrePerSecond = 6000f;
            profile.coastingDragPerSecond = 0.4f;
            profile.lateralGripCurve = TireCurve();
            profile.maxDriveForceNewtons = 14000f;
            profile.sprintDriveMultiplier = 1.5f;
            profile.maxBrakeForceNewtons = 20000f;
            profile.maxSteerAngleDegrees = 45f;
            profile.steerRateDegreesPerSecond = 120f;
            profile.driveable = true;
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

            profile.bodySizeMetres = new Vector3(1.5f, 1.7f, 3.0f);
            profile.centerOfMassOffset = new Vector3(0f, -0.3f, 0f);
            profile.drawbarLengthMetres = 0.3f;
            profile.wheelbaseMetres = 2.0f;
            profile.trackMetres = 1.3f;
            profile.wheelRadiusMetres = 0.3f;
            profile.suspensionRestLengthMetres = 0.35f;
            profile.springStrengthNewtons = 12000f;
            profile.damperNewtonsPerMetrePerSecond = 1400f;
            profile.coastingDragPerSecond = 0.4f;
            profile.lateralGripCurve = TireCurve();
            profile.maxDriveForceNewtons = 0f;

            // A cart has no engine, so there is nothing for a sprint to multiply.
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
            profile.accelerationMetresPerSecondSquared = 30f;
            profile.turnRateDegreesPerSecond = 720f;
            return profile;
        }

        /// <summary>
        /// Sideways force one wheel can produce, in newtons per kilogram it carries, against how
        /// fast the contact patch is sliding sideways in metres per second.
        ///
        /// The shape matters more than the numbers. Grip climbs as a tire begins to slip, peaks,
        /// and then falls away -- and it is that falling half that lets a vehicle break traction at
        /// all. A curve that only ever rises puts everything on rails, and no amount of speed into
        /// a corner will ever make a train slide or jackknife.
        /// </summary>
        static AnimationCurve TireCurve()
            => new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(3f, 12f),
                new Keyframe(12f, 5f));
    }
}
