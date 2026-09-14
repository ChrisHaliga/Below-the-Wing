using System;
using System.Collections.Generic;
using System.IO;
using BelowTheWing.Apron;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Diagnostics;
using BelowTheWing.Net;
using BelowTheWing.Session;
using BelowTheWing.Vehicles;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    public static class SceneBootstrap
    {
        const string PrefabFolder = "Assets/Content/Prefabs";
        const string ScenePath = "Assets/Scenes/Apron.unity";
        const string ApronMaterialPath = "Assets/Content/ApronConcrete.mat";

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
                    EnvelopeSizeMetres = bodywork.size,
                    EnvelopeCentreLocal = bodywork.center,
                    InteriorLocal = new Bounds(Vector3.zero, Vector3.zero),
                    SolidParts = new List<VehicleShape.SolidPart>
                    {
                        new VehicleShape.SolidPart("Body", bodywork.size, bodywork.center)
                    }
                },
                measured.Visible);
        }

        static MeasuredVehicle MeasureTheCart(GameObject cart, Transform model)
        {
            const float deckTopMetres = 0.4727f;
            const float deckWidthMetres = 1.7211f;
            const float deckLengthMetres = 3.1538f;
            const float slabThicknessMetres = 0.15f;
            const float clearInsideMetres = 1.626f;
            const float lipHeightMetres = 0.18f;
            const float lipThicknessMetres = 0.05f;

            var envelopeSizeMetres = new Vector3(1.8855f, 2.0155f, 3.8152f);
            var envelopeCentreLocal = new Vector3(0f, 1.0909f, 0.1296f);

            var measured = Wheels(cart, model, new[] { "Wheel_1", "Wheel_2", "Wheel_3", "Wheel_4" });

            var roofUnderside = deckTopMetres + clearInsideMetres;

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
                    EnvelopeSizeMetres = envelopeSizeMetres,
                    EnvelopeCentreLocal = envelopeCentreLocal,
                    InteriorLocal = new Bounds(
                        new Vector3(0f, deckTopMetres + (clearInsideMetres * 0.5f), 0f),
                        new Vector3(deckWidthMetres, clearInsideMetres, deckLengthMetres)),
                    SolidParts = solid
                },
                measured.Visible);
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

        static Vector3 MarkerLocal(GameObject vehicle, Transform model, string path)
        {
            var found = model.Find(path)
                        ?? throw Unmeasurable(vehicle, $"its model has no '{path}'");

            if (found.GetComponent<MeshFilter>() != null)
            {
                throw Unmeasurable(vehicle,
                    $"'{path}' is a mesh rather than a marker, and reading a coupling off the metal " +
                    "around it puts that coupling tens of centimetres out");
            }

            return vehicle.transform.InverseTransformPoint(found.position);
        }

        static Transform PartOfTheModel(GameObject vehicle, Transform model, string path)
        {
            var found = model.Find(path)
                        ?? throw Unmeasurable(vehicle,
                            $"its model has no '{path}'. Anything that is not a direct child of the " +
                            "model root needs its path: a plain name finds nothing at all");

            if (found.GetComponent<MeshFilter>() == null)
            {
                throw Unmeasurable(vehicle, $"'{path}' is a marker rather than a part of the model");
            }

            return found;
        }

        static InvalidOperationException Unmeasurable(GameObject vehicle, string why)
            => new InvalidOperationException(
                $"'{vehicle.name}' cannot be measured and so cannot be built: {why}.");

        static Bounds MeshBoxLocal(GameObject vehicle, Transform model, string path)
            => MeshBoxLocal(vehicle, PartOfTheModel(vehicle, model, path));

        static Bounds MeshBoxLocal(GameObject vehicle, Transform part)
        {
            var mesh = part.GetComponent<MeshFilter>().sharedMesh;
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

            return model.transform;
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
            var manager = BuildNetworkManager();
            var gateway = manager.gameObject.AddComponent<SessionGateway>();
            var broker = manager.gameObject.AddComponent<NetworkOwnershipBroker>();

            Set(new GameObject("Session Entry Screen").AddComponent<SessionEntryScreen>(), "m_Gateway", gateway);
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
            Set(session, "m_Broker", broker);
            Set(session, "m_Camera", camera);
            Set(session, "m_Readout", readout);

            EditorSceneManager.SaveScene(scene, ScenePath);
            GiveTheSceneObjectsTheirIdentities();
            AddToBuildSettings();
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

        static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath))
            {
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
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
