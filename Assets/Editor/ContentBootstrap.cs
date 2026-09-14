using System.IO;
using BelowTheWing.Apron;
using BelowTheWing.Cargo;
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

            // Low, and measured from an origin on the ground rather than in the middle of the
            // bodywork. The bodywork's own middle is a metre up; carrying the weight there would
            // have a tug roll over the first time it cornered with a loaded train behind it.
            profile.centerOfMassOffset = new Vector3(0f, 0.55f, 0f);

            // Little suspension, because these are little wheels: 0.2203 m at the front and
            // 0.2647 m at the back, measured off the model. Travel longer than half the smaller
            // wheel and the tractor visibly floats above its own axles.
            profile.suspensionRestLengthMetres = 0.10f;

            // 6131 N on each corner at 2500 kg gives about 15% compression at rest, leaving room to
            // squash under a load and to extend over a bump. The damper is around 0.45 of critical
            // for that stiffness, which settles a bounce inside one oscillation.
            profile.springStrengthNewtons = 41000f;
            profile.damperNewtonsPerMetrePerSecond = 14000f;
            profile.coastingDragPerSecond = 0.4f;
            profile.lateralGripCurve = TireCurve();

            // 20 kN on 2500 kg is 8 m/s^2 off the line: brisk for a tug, and enough to move a train
            // of loaded carts without spinning the wheels up.
            profile.maxDriveForceNewtons = 20000f;
            profile.topSpeedMetresPerSecond = 20f;
            profile.bounciness = 0.4f;
            profile.sprintDriveMultiplier = 1.5f;
            profile.maxBrakeForceNewtons = 20000f;
            profile.maxSteerAngleDegrees = 45f;
            profile.steerRateDegreesPerSecond = 120f;
            profile.driveable = true;
            return profile;
        }

        static BagProfile BuildCheckedBag()
        {
            var profile = ScriptableObject.CreateInstance<BagProfile>();

            // A checked suitcase. Airlines allow up to twenty-three kilograms and most people fill
            // it, so twenty is the everyday bag rather than a light one.
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

            // Low, and measured from an origin that is on the ground rather than in the middle of
            // the bodywork. A loaded cart that leans into a corner throws its load out of itself.
            profile.centerOfMassOffset = new Vector3(0f, 0.5f, 0f);

            // Small wheels -- 0.157 m, measured off the model -- and correspondingly little
            // suspension. Travel longer than the wheel has radius and the cart visibly floats
            // above its own axles.
            profile.suspensionRestLengthMetres = 0.08f;

            // 1349 N on each corner at 550 kg gives about 15% compression at rest, leaving room to
            // squash under a load and to extend over a bump. The damper is around 0.45 of critical
            // for that stiffness, which settles a bounce inside one oscillation without making the
            // cart feel welded to the ground.
            profile.springStrengthNewtons = 9000f;
            profile.damperNewtonsPerMetrePerSecond = 3500f;
            profile.coastingDragPerSecond = 0.1f;
            profile.lateralGripCurve = TireCurve();
            profile.maxDriveForceNewtons = 0f;
            profile.topSpeedMetresPerSecond = 0f;
            profile.bounciness = 0.4f;

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
            // Two figures, because they answer two questions. Grip is real: 10 m/s^2 is above the 8
            // a tractor pulls away at and below the 12 to 15 a cart makes cornering hard, so a
            // launch keeps its riders and a corner takes them. Gait is feel: legs that really only
            // managed the grip figure would take half a second to reach walking pace, which is felt
            // as the controls going soft, and it only applies while the feet still have the deck.
            profile.footGripMetresPerSecondSquared = 10f;
            profile.gaitResponseMetresPerSecondSquared = 30f;
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
