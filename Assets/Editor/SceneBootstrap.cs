using System;
using System.Collections.Generic;
using System.IO;
using BelowTheWing.Apron;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Diagnostics;
using BelowTheWing.Menu;
using BelowTheWing.Net;
using BelowTheWing.Session;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace BelowTheWing.EditorTools
{
    public static class SceneBootstrap
    {
        const string PrefabFolder = "Assets/Content/Prefabs";
        const string BeltLoaderModelPath = "Assets/Content/Vehicles/belt_loader.fbx";
        const string RegionalJetModelPath = "Assets/Content/Vehicles/crj_200.fbx";

        const float JetAheadOfTheWideShotMetres = 48f;
        const float JetRightOfTheWideShotMetres = 26f;

        const float CrewStandBackMetres = 4.15f;
        const float CrewSpacingMetres = 1.2f;
        const float LoaderSitsBackMetres = 1.4f;
        const string ScenePath = "Assets/Scenes/Apron.unity";
        const string MenuScenePath = "Assets/Scenes/Menu.unity";
        const string ApronMaterialPath = "Assets/Content/ApronConcrete.mat";
        const string ThemePath = "Assets/UI/MenuTheme.tss";
        const string DisplayFontPath = "Assets/UI/Fonts/Inter-SemiBold.ttf";
        const string BodyFontPath = "Assets/UI/Fonts/Inter-Regular.ttf";
        const string DataFontPath = "Assets/UI/Fonts/RobotoMono-Bold.ttf";
        const string PanelSettingsPath = "Assets/UI/MenuPanelSettings.asset";

        const string DefaultPrefabListPath = "Assets/DefaultNetworkPrefabs.asset";

        const string TractorProfilePath = "Assets/Content/Vehicles/BaggageTractor.asset";
        const string CartProfilePath = "Assets/Content/Vehicles/BaggageCart.asset";
        const string AircraftProfilePath = "Assets/Content/Aircraft/NarrowbodyAirliner.asset";
        const string CrewProfilePath = "Assets/Content/Crew/RampWorker.asset";
        const string BagProfilePath = "Assets/Content/Cargo/CheckedBag.asset";
        const string CartModelPath = "Assets/Content/Vehicles/baggage_cart.fbx";
        const string TractorModelPath = "Assets/Content/Vehicles/baggage_tractor.fbx";

        static readonly Color HiVisYellow = new Color(0.95f, 0.75f, 0.15f);
        static readonly Color BagCanvas = new Color(0.45f, 0.38f, 0.32f);
        static readonly Color FuselageWhite = new Color(0.82f, 0.82f, 0.85f);

        [MenuItem("Below the Wing/Rebuild apron scene and prefabs")]
        public static void Rebuild()
        {
            ContentBootstrap.CreateMissingContent();

            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");

            var tractorProfile = AssetDatabase.LoadAssetAtPath<VehicleProfile>(TractorProfilePath);
            var cartProfile = AssetDatabase.LoadAssetAtPath<VehicleProfile>(CartProfilePath);
            var aircraftProfile = AssetDatabase.LoadAssetAtPath<AircraftProfile>(AircraftProfilePath);
            var crewProfile = AssetDatabase.LoadAssetAtPath<CrewProfile>(CrewProfilePath);

            var tractor = BuildTractor(tractorProfile);
            var cart = BuildCart(cartProfile);
            var aircraft = BuildAircraft(aircraftProfile);
            var crew = BuildCrew(crewProfile);
            var bagPrefab = BuildBag();

            BuildScene(aircraftProfile, crewProfile, tractor, cart, aircraft, crew);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Apron scene and prefabs rebuilt.");
        }

        static GameObject BuildTractor(VehicleProfile profile)
        {
            var go = NewVehicle("BaggageTractor", profile, TractorModelPath, MeasureTheTractor);

            go.AddComponent<VehicleOccupant>();

            return SaveAndDiscard(go, $"{PrefabFolder}/BaggageTractor.prefab");
        }

        static GameObject BuildCart(VehicleProfile profile)
        {
            var go = NewVehicle("BaggageCart", profile, CartModelPath, MeasureTheCart);

            return SaveAndDiscard(go, $"{PrefabFolder}/BaggageCart.prefab");
        }

        readonly struct MeasuredVehicle
        {
            public readonly VehicleShape.Measurements Shape;
            public readonly IReadOnlyList<Transform> Wheels;

            public MeasuredVehicle(VehicleShape.Measurements shape, IReadOnlyList<Transform> wheels)
            {
                Shape = shape;
                Wheels = wheels;
            }
        }

        static MeasuredVehicle MeasureTheTractor(GameObject tractor, Transform model)
        {
            var measured = Wheels(tractor, model, new[]
            {
                "Steer_Left/Wheel_Front_Left",
                "Steer_Right/Wheel_Front_Right",
                "Wheel_Back_Left",
                "Wheel_Back_Right"
            });

            var bodywork = MeshBoxLocal(tractor, model, "Body");

            return new MeasuredVehicle(
                new VehicleShape.Measurements
                {
                    Wheels = measured.Placements,
                    FrontCouplingLocal = null,
                    RearCouplingLocal = MarkerLocal(tractor, model, "HITCH_Female"),
                    SeatLocal = MarkerLocal(tractor, model, "SEAT"),
                    EnvelopeSizeMetres = bodywork.size,
                    EnvelopeCentreLocal = bodywork.center,
                    InteriorLocal = new Bounds(Vector3.zero, Vector3.zero),
                    SolidParts = SolidPieces(tractor, model, new[]
                    {
                        "Body",
                        "Frame",
                        "Frame_Supports",
                        "Tire_Cover",
                        "Cushion_Seat",
                        "Cushion_Backrest",
                        "Dashboard"
                    })
                },
                measured.Visible);
        }

        static List<VehicleShape.SolidPart> SolidPieces(
            GameObject vehicle, Transform model, IReadOnlyList<string> paths)
        {
            var parts = new List<VehicleShape.SolidPart>(paths.Count);

            foreach (var path in paths)
            {
                parts.Add(SolidAsModelled(vehicle, model, path));
            }

            return parts;
        }

        static MeasuredVehicle MeasureTheCart(GameObject cart, Transform model)
        {
            const float slabThicknessMetres = 0.15f;
            const float lipHeightMetres = 0.18f;
            const float lipThicknessMetres = 0.05f;

            var measured = Wheels(cart, model, new[] { "Wheel_1", "Wheel_2", "Wheel_3", "Wheel_4" });

            var loadSpace = TheSpaceTheDoorsCloseOver(cart, model);
            var envelope = EverythingItIsMadeOf(cart, model);

            var deckTopMetres = loadSpace.min.y;
            var roofUnderside = loadSpace.max.y;
            var clearInsideMetres = loadSpace.size.y;
            var deckWidthMetres = loadSpace.size.x;
            var deckLengthMetres = loadSpace.size.z;

            var solid = new List<VehicleShape.SolidPart>
            {
                new VehicleShape.SolidPart(
                    "Deck",
                    new Vector3(deckWidthMetres, slabThicknessMetres, deckLengthMetres),
                    new Vector3(0f, deckTopMetres - (slabThicknessMetres * 0.5f), 0f)),

                new VehicleShape.SolidPart(
                    "Lip left",
                    new Vector3(lipThicknessMetres, lipHeightMetres, deckLengthMetres),
                    new Vector3(
                        -((deckWidthMetres * 0.5f) - (lipThicknessMetres * 0.5f)),
                        deckTopMetres + (lipHeightMetres * 0.5f),
                        0f)),

                new VehicleShape.SolidPart(
                    "Lip right",
                    new Vector3(lipThicknessMetres, lipHeightMetres, deckLengthMetres),
                    new Vector3(
                        (deckWidthMetres * 0.5f) - (lipThicknessMetres * 0.5f),
                        deckTopMetres + (lipHeightMetres * 0.5f),
                        0f)),

                new VehicleShape.SolidPart(
                    "End front",
                    new Vector3(deckWidthMetres, clearInsideMetres, slabThicknessMetres),
                    new Vector3(
                        0f,
                        (deckTopMetres + roofUnderside) * 0.5f,
                        (deckLengthMetres + slabThicknessMetres) * 0.5f)),

                new VehicleShape.SolidPart(
                    "End rear",
                    new Vector3(deckWidthMetres, clearInsideMetres, slabThicknessMetres),
                    new Vector3(
                        0f,
                        (deckTopMetres + roofUnderside) * 0.5f,
                        -(deckLengthMetres + slabThicknessMetres) * 0.5f)),

                new VehicleShape.SolidPart(
                    "Roof",
                    new Vector3(deckWidthMetres, slabThicknessMetres, deckLengthMetres),
                    new Vector3(0f, roofUnderside + (slabThicknessMetres * 0.5f), 0f))
            };

            return new MeasuredVehicle(
                new VehicleShape.Measurements
                {
                    Wheels = measured.Placements,
                    FrontCouplingLocal = MarkerLocal(cart, model, "HITCH_Male"),
                    RearCouplingLocal = MarkerLocal(cart, model, "HITCH_Female"),
                    EnvelopeSizeMetres = envelope.size,
                    EnvelopeCentreLocal = envelope.center,
                    InteriorLocal = loadSpace,
                    SolidParts = solid
                },
                measured.Visible);
        }

        static Bounds TheSpaceTheDoorsCloseOver(GameObject cart, Transform model)
        {
            var doors = MeshBoxLocal(cart, model, "Door1");

            foreach (var door in new[] { "Door2", "Door3", "Door4" })
            {
                doors.Encapsulate(MeshBoxLocal(cart, model, door));
            }

            return doors;
        }

        static bool IsCouplingHardware(string name)
            => name.StartsWith("Hitch", StringComparison.OrdinalIgnoreCase);

        static Bounds EverythingItIsMadeOf(GameObject vehicle, Transform model)
        {
            var all = new Bounds();
            var anything = false;

            foreach (var part in model.GetComponentsInChildren<Transform>(true))
            {
                var filter = part.GetComponent<MeshFilter>();
                var skinned = part.GetComponent<SkinnedMeshRenderer>();
                var mesh = filter != null ? filter.sharedMesh
                    : skinned != null ? skinned.sharedMesh
                    : null;

                if (mesh == null || IsCouplingHardware(part.name))
                {
                    continue;
                }

                var box = MeshBoxLocal(vehicle, part);

                if (anything)
                {
                    all.Encapsulate(box);
                }
                else
                {
                    all = box;
                    anything = true;
                }
            }

            if (!anything)
            {
                throw Unmeasurable(vehicle, "its model has no meshes to take an envelope from");
            }

            return all;
        }

        static (List<VehicleShape.WheelPlacement> Placements, List<Transform> Visible) Wheels(
            GameObject vehicle, Transform model, IReadOnlyList<string> paths)
        {
            var placements = new List<VehicleShape.WheelPlacement>();
            var visible = new List<Transform>();

            foreach (var path in paths)
            {
                var wheel = PartOfTheModel(vehicle, model, path);
                var box = MeshBoxLocal(vehicle, wheel);
                var across = Mathf.Max(box.size.x, Mathf.Max(box.size.y, box.size.z));

                placements.Add(new VehicleShape.WheelPlacement(
                    vehicle.transform.InverseTransformPoint(wheel.position), across * 0.5f));

                visible.Add(wheel);
            }

            return (placements, visible);
        }

        static Vector3 MarkerLocal(GameObject vehicle, Transform model, string name)
        {
            var found = Named(vehicle, model, name);

            if (MeshOn(found) != null)
            {
                throw Unmeasurable(vehicle,
                    $"'{name}' is a mesh rather than a marker, and reading a coupling off the metal " +
                    "around it puts that coupling tens of centimetres out");
            }

            return vehicle.transform.InverseTransformPoint(found.position);
        }

        static Transform PartOfTheModel(GameObject vehicle, Transform model, string name)
        {
            var found = Named(vehicle, model, name);

            if (MeshOn(found) == null)
            {
                throw Unmeasurable(vehicle, $"'{name}' is a marker rather than a part of the model");
            }

            return found;
        }

        static Transform Named(GameObject vehicle, Transform model, string name)
        {
            var byPath = model.Find(name);
            if (byPath != null)
            {
                return byPath;
            }

            Transform found = null;

            foreach (var candidate in model.GetComponentsInChildren<Transform>(true))
            {
                if (candidate == model || candidate.name != name)
                {
                    continue;
                }

                if (found != null)
                {
                    throw Unmeasurable(vehicle,
                        $"its model has more than one '{name}', so there is no saying which one a " +
                        "measurement would be taken from");
                }

                found = candidate;
            }

            if (found != null)
            {
                return found;
            }

            var leaf = name.Substring(name.LastIndexOf('/') + 1);

            foreach (var candidate in model.GetComponentsInChildren<Transform>(true))
            {
                if (candidate == model || candidate.name != leaf)
                {
                    continue;
                }

                if (found != null)
                {
                    throw Unmeasurable(vehicle,
                        $"its model has more than one '{leaf}', so there is no saying which one a " +
                        "measurement would be taken from");
                }

                found = candidate;
            }

            return found ?? throw Unmeasurable(vehicle, $"its model has no '{name}' anywhere inside it");
        }

        static Mesh MeshOn(Transform part)
        {
            var filter = part.GetComponent<MeshFilter>();
            if (filter != null)
            {
                return filter.sharedMesh;
            }

            var skinned = part.GetComponent<SkinnedMeshRenderer>();
            return skinned != null ? skinned.sharedMesh : null;
        }

        static InvalidOperationException Unmeasurable(GameObject vehicle, string why)
            => new InvalidOperationException(
                $"'{vehicle.name}' cannot be measured and so cannot be built: {why}.");

        static Bounds MeshBoxLocal(GameObject vehicle, Transform model, string path)
            => MeshBoxLocal(vehicle, PartOfTheModel(vehicle, model, path));

        static VehicleShape.SolidPart SolidAsModelled(GameObject vehicle, Transform model, string path)
        {
            var part = PartOfTheModel(vehicle, model, path);
            var mesh = MeshOn(part);

            if (mesh == null)
            {
                throw Unmeasurable(vehicle, $"'{path}' in its model has no mesh to be solid in");
            }

            var onTheVehicle = vehicle.transform.worldToLocalMatrix * part.localToWorldMatrix;

            return new VehicleShape.SolidPart(
                part.name,
                mesh,
                onTheVehicle.GetPosition(),
                onTheVehicle.rotation,
                onTheVehicle.lossyScale);
        }

        static Bounds MeshBoxLocal(GameObject vehicle, Transform part)
        {
            var mesh = MeshOn(part);
            var least = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var most = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (var corner = 0; corner < 8; corner++)
            {
                var offset = Vector3.Scale(
                    mesh.bounds.extents,
                    new Vector3(
                        (corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f));

                var inVehicle = vehicle.transform.InverseTransformPoint(
                    part.TransformPoint(mesh.bounds.center + offset));

                least = Vector3.Min(least, inVehicle);
                most = Vector3.Max(most, inVehicle);
            }

            var box = new Bounds();
            box.SetMinMax(least, most);
            return box;
        }

        static GameObject NewVehicle(
            string name, VehicleProfile profile, string modelPath, Func<GameObject, Transform, MeasuredVehicle> measure)
        {
            var go = new GameObject(name);
            go.AddComponent<Rigidbody>();

            var vehicle = go.AddComponent<VehicleController>();
            Set(vehicle, "m_Profile", profile);

            var measured = measure(go, AddModel(go, modelPath));

            var shape = go.AddComponent<VehicleShape>();
            shape.Describe(measured.Shape);

            go.AddComponent<WheelLook>().Watch(measured.Wheels);

            AddNetworking(go, outlivesItsOwner: true);
            go.AddComponent<TrainMember>();

            go.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            go.AddComponent<ApronAppearance>().DescribeAsModelled(
                shape.EnvelopeCentreLocal.y + (shape.EnvelopeSizeMetres.y * 0.6f));
            go.AddComponent<ApronIdentity>();

            return go;
        }

        static Transform AddModel(GameObject vehicle, string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"No model at {path}, so '{vehicle.name}' cannot be measured and the apron " +
                    "cannot be built. Check the file is in the project and has been imported.");
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            model.name = ApronAppearance.LookName;
            model.transform.SetParent(vehicle.transform, worldPositionStays: false);
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.localRotation;

            DiscardAnythingThatLightsOrLooks(model);

            return model.transform;
        }

        static void DiscardAnythingThatLightsOrLooks(GameObject model)
        {
            foreach (var camera in model.GetComponentsInChildren<Camera>(true))
            {
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }

            foreach (var light in model.GetComponentsInChildren<Light>(true))
            {
                UnityEngine.Object.DestroyImmediate(light.gameObject);
            }
        }

        static GameObject BuildAircraft(AircraftProfile profile)
        {
            var go = new GameObject("NarrowbodyAirliner");

            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            go.AddComponent<CapsuleCollider>();

            Set(go.AddComponent<AircraftBody>(), "m_Profile", profile);
            go.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            AddNetworking(go, outlivesItsOwner: true);

            Dress(go, ApronAppearance.Shape.LyingCapsule,
                new Vector3(profile.fuselageDiameterMetres, profile.lengthMetres, profile.fuselageDiameterMetres),
                FuselageWhite,
                profile.fuselageDiameterMetres);

            return SaveAndDiscard(go, $"{PrefabFolder}/NarrowbodyAirliner.prefab");
        }

        static GameObject BuildBag()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BagProfile>(BagProfilePath);
            if (profile == null)
            {
                Debug.LogWarning($"No bag profile at {BagProfilePath}; skipping the bag prefab.");
                return null;
            }

            var go = new GameObject("Bag");
            go.AddComponent<Rigidbody>();
            go.AddComponent<BoxCollider>();

            Set(go.AddComponent<Bag>(), "m_Profile", profile);

            go.AddComponent<HandUse>().As = HandUse.Category.Carry;

            AddNetworking(go, outlivesItsOwner: true);

            Dress(go, ApronAppearance.Shape.Box, profile.sizeMetres, BagCanvas, profile.sizeMetres.y * 1.2f);

            return SaveAndDiscard(go, $"{PrefabFolder}/Bag.prefab");
        }

        static GameObject BuildCrew(CrewProfile profile)
        {
            var go = new GameObject("RampWorker");
            go.AddComponent<Rigidbody>();
            go.AddComponent<CapsuleCollider>();

            Set(go.AddComponent<CrewCharacter>(), "m_Profile", profile);

            var reach = profile.radiusMetres + 0.45f;
            HandAnchor(go, Hands.LeftAnchorName, new Vector3(-0.3f, 0.2f, reach));
            HandAnchor(go, Hands.RightAnchorName, new Vector3(0.3f, 0.2f, reach));

            AddNetworking(go, outlivesItsOwner: false);

            Dress(go, ApronAppearance.Shape.UprightCapsule,
                new Vector3(profile.radiusMetres * 2f, profile.heightMetres, profile.radiusMetres * 2f),
                HiVisYellow,
                profile.heightMetres * 0.7f);

            return SaveAndDiscard(go, $"{PrefabFolder}/RampWorker.prefab");
        }

        static void HandAnchor(GameObject character, string name, Vector3 local)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(character.transform, worldPositionStays: false);
            anchor.transform.localPosition = local;
        }

        static void Dress(
            GameObject go,
            ApronAppearance.Shape shape,
            Vector3 sizeMetres,
            Color colour,
            float labelHeightMetres,
            Vector3 drawnAtLocal = default)
        {
            go.AddComponent<ApronAppearance>()
                .DescribeAs(shape, sizeMetres, colour, labelHeightMetres, drawnAtLocal);
            go.AddComponent<ApronIdentity>();
        }

        static void AddNetworking(GameObject go, bool outlivesItsOwner)
        {
            var networked = go.AddComponent<NetworkObject>();
            networked.DontDestroyWithOwner = outlivesItsOwner;

            if (go.GetComponent<VehicleController>() != null)
            {
                go.AddComponent<VehicleMotion>();
            }
            else if (go.GetComponent<CrewCharacter>() != null)
            {
                go.AddComponent<CrewMotion>();
            }
            else if (go.GetComponent<Bag>() != null)
            {
                go.AddComponent<CargoMotion>();
            }
            else
            {
                go.AddComponent<AnticipatedNetworkTransform>();
            }
        }

        static GameObject SaveAndDiscard(GameObject go, string path)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return saved;
        }

        static void BuildScene(
            AircraftProfile aircraftProfile,
            CrewProfile crewProfile,
            GameObject tractor,
            GameObject cart,
            GameObject aircraft,
            GameObject crew)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Apron rebuild cancelled.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildApronFloor();
            BuildLighting();

            var camera = BuildCamera();
            var readout = new GameObject("Ramp Readout").AddComponent<RampReadout>();

            var sessionObject = new GameObject("Ramp Session");
            sessionObject.AddComponent<NetworkObject>();
            var session = sessionObject.AddComponent<RampSession>();

            Set(session, "m_AircraftProfile", aircraftProfile);
            Set(session, "m_CrewProfile", crewProfile);
            Set(session, "m_TractorPrefab", tractor.GetComponent<NetworkObject>());
            Set(session, "m_CartPrefab", cart.GetComponent<NetworkObject>());
            Set(session, "m_AircraftPrefab", aircraft.GetComponent<NetworkObject>());
            Set(session, "m_CrewPrefab", crew.GetComponent<NetworkObject>());

            var bag = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Bag.prefab");
            if (bag != null)
            {
                Set(session, "m_BagPrefab", bag.GetComponent<NetworkObject>());
            }
            Set(session, "m_Camera", camera);
            Set(session, "m_Readout", readout);

            EditorSceneManager.SaveScene(scene, ScenePath);
            GiveTheSceneObjectsTheirIdentities();

            BuildMenuScene(aircraftProfile, crewProfile, tractor, cart, aircraft, crew);

            AddToBuildSettings(MenuScenePath, first: true);
            AddToBuildSettings(ScenePath, first: false);
        }

        static void BuildMenuScene(
            AircraftProfile aircraftProfile,
            CrewProfile crewProfile,
            GameObject tractor,
            GameObject cart,
            GameObject aircraft,
            GameObject crew)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildApronFloor();
            BuildLighting();

            var manager = BuildNetworkManager();
            var gateway = manager.gameObject.AddComponent<SessionGateway>();
            manager.gameObject.AddComponent<NetworkOwnershipBroker>();

            var lobby = new GameObject("Lobby Roster");
            lobby.AddComponent<NetworkObject>();
            lobby.AddComponent<LobbyRoster>();

            var eye = new GameObject("Menu Camera");
            eye.AddComponent<Camera>();
            eye.AddComponent<AudioListener>();
            var menuCamera = eye.AddComponent<MenuCamera>();

            var backdrop = BuildBackdrop(
                aircraftProfile, crewProfile, tractor, cart, aircraft, crew);

            var menu = new GameObject("Menu");
            var document = menu.AddComponent<UIDocument>();
            document.panelSettings = MenuPanel();
            document.sortingOrder = 100f;

            var driver = menu.AddComponent<MenuDriver>();

            Set(driver, "m_Gateway", gateway);
            Set(driver, "m_Camera", menuCamera);
            Set(driver, "m_Backdrop", backdrop);

            Set(driver, "m_Display", AssetDatabase.LoadAssetAtPath<Font>(DisplayFontPath));
            Set(driver, "m_Body", AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath));
            Set(driver, "m_Data", AssetDatabase.LoadAssetAtPath<Font>(DataFontPath));

            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        static MenuBackdrop BuildBackdrop(
            AircraftProfile aircraftProfile,
            CrewProfile crewProfile,
            GameObject tractor,
            GameObject cart,
            GameObject aircraft,
            GameObject crew)
        {
            var holder = new GameObject("Backdrop");
            var backdrop = holder.AddComponent<MenuBackdrop>();

            var layout = ApronLayoutSettings.Default;
            var plan = ApronLayout.Build(
                layout,
                tractor.GetComponent<VehicleShape>().Footprint,
                cart.GetComponent<VehicleShape>().Footprint,
                aircraftProfile,
                new Vector3(
                    crewProfile.radiusMetres * 2f, crewProfile.heightMetres, crewProfile.radiusMetres * 2f));

            Dress(aircraft, plan.Aircraft, holder.transform);

            var train = plan.Trains[0];

            Dress(tractor, train.Tractor, holder.transform);

            var staged = (GameObject)null;

            foreach (var parked in train.Carts)
            {
                var placed = Dress(cart, parked, holder.transform);

                staged ??= placed;
            }

            var stage = train.Carts[0];
            var facing = stage.Rotation * Vector3.right;
            var alongTheCart = stage.Rotation * Vector3.forward;
            var crewLine = stage.Position - (facing * CrewStandBackMetres);

            var standing = new List<GameObject>(Shift.MostCrew);

            for (var i = 0; i < Shift.MostCrew; i++)
            {
                var at = crewLine
                         + (alongTheCart * ((i - ((Shift.MostCrew - 1) * 0.5f)) * CrewSpacingMetres))
                         + (Vector3.up * (crewProfile.heightMetres * 0.5f));

                var figure = Dress(
                    crew,
                    new Placement($"Crew {i + 1}", at, Quaternion.LookRotation(facing), Vector3.one),
                    holder.transform);

                GreyboxShape.AttachCapsule(
                    figure.transform,
                    crewProfile.heightMetres,
                    crewProfile.radiusMetres * 2f,
                    HiVisYellow);

                figure.SetActive(false);
                standing.Add(figure);
            }

            var loader = ParkTheBeltLoader(holder.transform, crewLine, facing, stage.Rotation);

            var nose = plan.Aircraft.Position;
            var tug = train.Tractor.Position;
            var eyeHeight = crewProfile.heightMetres * 0.92f;
            var doorwayHeight = DoorwayHeightOf(staged);

            var wide = Shot("Wide shot", holder.transform,
                tug + new Vector3(19f, 7.5f, -13f), Vector3.Lerp(nose, tug, 0.55f) + (Vector3.up * 2.5f));

            Set(backdrop, "m_WideShot", wide);
            Set(backdrop, "m_Airliner", ParkTheJet(holder.transform, wide).transform);

            Set(backdrop, "m_CartShot", Shot("Cart shot", holder.transform,
                stage.Position + (facing * 2.3f) + new Vector3(0f, eyeHeight, 0f),
                stage.Position + new Vector3(0f, doorwayHeight, 0f)));

            // The push ends past the cart's centre, close enough to the far doorway that the opening
            // fills the frame rather than sharing it with the inside of the cart.
            Set(backdrop, "m_InsideShot", Shot("Inside shot", holder.transform,
                stage.Position - (facing * 0.25f) + new Vector3(0f, doorwayHeight, 0f),
                crewLine + new Vector3(0f, crewProfile.heightMetres * 0.62f, 0f)));

            SetList(backdrop, "m_LobbyCrew", standing);
            // A figure's origin sits at half its height, and the plate hangs from just above the
            // top of its head.
            Set(backdrop, "m_PlateHeightMetres", crewProfile.heightMetres * 0.56f);
            Set(backdrop, "m_BeltLoader", loader.transform);
            Set(backdrop, "m_CartDoors", DoorsOf(holder, staged));

            return backdrop;
        }

        // The importer puts its own rotation and scale on an FBX root, and every vehicle model here
        // carries euler (270, 0, 0) at scale 100 from Blender's Z up. Rotating that root throws the
        // model onto its nose, so the holder turns and the model keeps what the importer gave it.
        static GameObject ParkTheBeltLoader(
            Transform under, Vector3 crewLine, Vector3 facing, Quaternion asTheTrainSits)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(BeltLoaderModelPath);

            if (model == null)
            {
                throw new InvalidOperationException($"There is no belt loader model at {BeltLoaderModelPath}.");
            }

            var loader = new GameObject("Belt loader");
            loader.transform.SetParent(under, worldPositionStays: false);
            // Square to the train rather than nose on to the camera, so its length lies across the
            // shot and the crew stand along it.
            loader.transform.SetPositionAndRotation(crewLine, asTheTrainSits);

            var drawn = (GameObject)PrefabUtility.InstantiatePrefab(model, loader.transform);
            drawn.transform.localPosition = Vector3.zero;

            loader.transform.position =
                crewLine - (facing * (ReachesForward(loader, facing) + LoaderSitsBackMetres));

            return loader;
        }

        // Placed against the shot that has to hold it rather than at a spot on the apron, so the
        // frame keeps its balance if that shot ever moves. Broadside to the camera fills width.
        static GameObject ParkTheJet(Transform under, Transform wide)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(RegionalJetModelPath);

            if (model == null)
            {
                throw new InvalidOperationException($"There is no regional jet model at {RegionalJetModelPath}.");
            }

            var jet = new GameObject("Regional jet");
            jet.transform.SetParent(under, worldPositionStays: false);

            var broadside = Vector3.ProjectOnPlane(wide.right, Vector3.up).normalized;
            var at = wide.position
                     + (wide.forward * JetAheadOfTheWideShotMetres)
                     + (broadside * JetRightOfTheWideShotMetres);

            jet.transform.SetPositionAndRotation(
                new Vector3(at.x, 0f, at.z),
                Quaternion.LookRotation(broadside));

            var drawn = (GameObject)PrefabUtility.InstantiatePrefab(model, jet.transform);
            drawn.transform.localPosition = Vector3.zero;

            foreach (var behaviour in jet.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            return jet;
        }

        static float ReachesForward(GameObject thing, Vector3 facing)
        {
            var from = thing.transform.position;
            var most = 0f;

            foreach (var drawn in thing.GetComponentsInChildren<Renderer>(true))
            {
                most = Mathf.Max(most, Vector3.Dot(drawn.bounds.max - from, facing));
                most = Mathf.Max(most, Vector3.Dot(drawn.bounds.min - from, facing));
            }

            return most;
        }

        static float DoorwayHeightOf(GameObject staged)
        {
            foreach (var skin in staged.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skin.sharedMesh != null && skin.sharedMesh.blendShapeCount > 0)
                {
                    return skin.bounds.center.y - staged.transform.position.y;
                }
            }

            throw new InvalidOperationException($"{staged.name} has no door to measure a doorway from.");
        }

        // The driver goes on the backdrop rather than on the cart. A component added to a prefab
        // instance is an override, and reimporting the prefab rebuilds the instance and drops it.
        // This rebuild writes BaggageCart.prefab and Menu.unity in the same pass.
        static MenuCartDoors DoorsOf(GameObject holder, GameObject staged)
        {
            var doors = holder.AddComponent<MenuCartDoors>();

            Set(doors, "m_Cart", staged);

            return doors;
        }

        static Transform Shot(string name, Transform under, Vector3 from, Vector3 at)
        {
            var shot = new GameObject(name).transform;

            shot.SetParent(under, worldPositionStays: true);
            shot.SetPositionAndRotation(from, Quaternion.LookRotation(at - from, Vector3.up));

            return shot;
        }

        static GameObject Dress(GameObject prefab, Placement where, Transform under)
        {
            var placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab, under);

            placed.transform.SetPositionAndRotation(where.Position, where.Rotation);
            placed.name = where.Name;

            foreach (var body in placed.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
            }

            foreach (var behaviour in placed.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (var networked in placed.GetComponentsInChildren<NetworkObject>(true))
            {
                UnityEngine.Object.DestroyImmediate(networked, allowDestroyingAssets: false);
            }

            return placed;
        }

        static PanelSettings MenuPanel()
        {
            Directory.CreateDirectory("Assets/UI");

            AssetDatabase.DeleteAsset(PanelSettingsPath);

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "Menu panel";
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);

            AssetDatabase.CreateAsset(settings, PanelSettingsPath);

            return settings;
        }

        static void GiveTheSceneObjectsTheirIdentities()
        {
            var saved = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            EditorSceneManager.MarkSceneDirty(saved);
            EditorSceneManager.SaveScene(saved, ScenePath);
        }

        static void BuildApronFloor()
        {
            AssetDatabase.DeleteAsset(ApronMaterialPath);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Apron";

            floor.transform.localScale = new Vector3(40f, 1f, 40f);

            var concrete = new Material(floor.GetComponent<MeshRenderer>().sharedMaterial)
            {
                name = "Apron concrete",
                color = new Color(0.32f, 0.33f, 0.34f)
            };

            AssetDatabase.CreateAsset(concrete, ApronMaterialPath);
            floor.GetComponent<MeshRenderer>().sharedMaterial = concrete;
        }

        static void BuildLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, 30f, 0f);
        }

        static FollowCamera BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            return go.AddComponent<FollowCamera>();
        }

        static NetworkManager BuildNetworkManager()
        {
            var go = new GameObject("Network Manager");
            var manager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();

            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,

                NetworkTopology = NetworkTopologyTypes.DistributedAuthority,

                PlayerPrefab = null,
                TickRate = 20
            };

            var known = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(DefaultPrefabListPath);
            if (known == null)
            {
                Debug.LogError(
                    $"No prefab list at {DefaultPrefabListPath}. Without it nothing spawned by the " +
                    "session owner can be created on any other machine.");
            }
            else
            {
                manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(known);
            }

            return manager;
        }

        static void AddToBuildSettings(string path, bool first)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            scenes.RemoveAll(s => s.path == path);

            if (first)
            {
                scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            }
            else
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void SetList(UnityEngine.Object target, string field, IReadOnlyList<UnityEngine.Object> values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);

            property.arraySize = values.Count;

            for (var i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(UnityEngine.Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogError($"{target.GetType().Name} has no serialized field called '{field}'.");
                return;
            }

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogError($"{target.GetType().Name} has no serialized field called '{field}'.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
