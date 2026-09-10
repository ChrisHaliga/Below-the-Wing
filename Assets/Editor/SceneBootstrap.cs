using System.Collections.Generic;
using System.IO;
using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Diagnostics;
using BelowTheWing.Net;
using BelowTheWing.Vehicles;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BelowTheWing.EditorTools
{
    /// <summary>
    /// Builds the apron scene and the prefabs it spawns, for a project that does not have them yet.
    ///
    /// The scene deliberately contains no aircraft, tractors or carts. It holds the machinery --
    /// networking, the camera, the readout -- and everything on the apron itself is put there at
    /// runtime by <see cref="RampSpawner"/>, so that there is one description of what stands where
    /// rather than a scene and a spawner that can disagree.
    ///
    /// Running this again replaces the scene and prefabs it made. Anything hand-edited in them will
    /// be lost, which is fine while they are scaffolding and worth remembering once they are not.
    /// </summary>
    public static class SceneBootstrap
    {
        const string PrefabFolder = "Assets/Content/Prefabs";
        const string ScenePath = "Assets/Scenes/Apron.unity";

        const string TractorProfilePath = "Assets/Content/Vehicles/BaggageTractor.asset";
        const string CartProfilePath = "Assets/Content/Vehicles/BaggageCart.asset";
        const string AircraftProfilePath = "Assets/Content/Aircraft/NarrowbodyAirliner.asset";
        const string CrewProfilePath = "Assets/Content/Crew/RampWorker.asset";

        [MenuItem("Below the Wing/Rebuild apron scene and prefabs")]
        public static void Rebuild()
        {
            ContentBootstrap.CreateMissingContent();

            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");

            var vehicle = BuildVehiclePrefab();
            var crew = BuildCrewPrefab();
            var aircraft = BuildAircraftPrefab();

            BuildScene(vehicle, crew, aircraft);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Apron scene and prefabs rebuilt.");
        }

        static GameObject BuildVehiclePrefab()
        {
            var go = new GameObject("Vehicle");

            // Everything the profile decides -- mass, size, wheel positions -- is applied when the
            // vehicle is configured at spawn, so the prefab only needs the components to exist.
            go.AddComponent<Rigidbody>();
            go.AddComponent<BoxCollider>();
            go.AddComponent<VehicleController>();

            AddNetworking(go);

            return SaveAndDiscard(go, $"{PrefabFolder}/Vehicle.prefab");
        }

        static GameObject BuildCrewPrefab()
        {
            var go = new GameObject("Crew");

            go.AddComponent<Rigidbody>();
            go.AddComponent<CapsuleCollider>();
            go.AddComponent<CrewCharacter>();

            AddNetworking(go);

            return SaveAndDiscard(go, $"{PrefabFolder}/Crew.prefab");
        }

        static GameObject BuildAircraftPrefab()
        {
            var go = new GameObject("Aircraft");

            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            go.AddComponent<CapsuleCollider>();

            var aircraft = go.AddComponent<AircraftBody>();
            Set(aircraft, "m_Profile", AssetDatabase.LoadAssetAtPath<AircraftProfile>(AircraftProfilePath));

            AddNetworking(go);

            return SaveAndDiscard(go, $"{PrefabFolder}/Aircraft.prefab");
        }

        static void AddNetworking(GameObject go)
        {
            go.AddComponent<NetworkObject>();
            go.AddComponent<RampObject>();

            // Movement is replicated as transform state rather than through NetworkRigidbody, which
            // forces every non-owning copy kinematic. A kinematic vehicle has infinite mass: you
            // would drive into somebody else's tractor and bounce off a wall, while on their screen
            // the mirror image happened. Both players get shoved, or neither does.
            go.AddComponent<AnticipatedNetworkTransform>();
        }

        static GameObject SaveAndDiscard(GameObject go, string path)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        static void BuildScene(GameObject vehiclePrefab, GameObject crewPrefab, GameObject aircraftPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildApronFloor();
            BuildLighting();

            var camera = BuildCamera();
            var manager = BuildNetworkManager(vehiclePrefab, crewPrefab, aircraftPrefab);
            var gateway = manager.gameObject.AddComponent<SessionGateway>();
            var broker = manager.gameObject.AddComponent<NetworkOwnershipBroker>();

            var screen = new GameObject("Session Entry Screen").AddComponent<SessionEntryScreen>();
            Set(screen, "m_Gateway", gateway);

            var readout = new GameObject("Ramp Readout").AddComponent<RampReadout>();

            var spawner = new GameObject("Ramp Spawner");
            spawner.AddComponent<NetworkObject>();
            var ramp = spawner.AddComponent<RampSpawner>();

            Set(ramp, "m_TractorProfile", AssetDatabase.LoadAssetAtPath<VehicleProfile>(TractorProfilePath));
            Set(ramp, "m_CartProfile", AssetDatabase.LoadAssetAtPath<VehicleProfile>(CartProfilePath));
            Set(ramp, "m_AircraftProfile", AssetDatabase.LoadAssetAtPath<AircraftProfile>(AircraftProfilePath));
            Set(ramp, "m_CrewProfile", AssetDatabase.LoadAssetAtPath<CrewProfile>(CrewProfilePath));
            Set(ramp, "m_VehiclePrefab", vehiclePrefab.GetComponent<NetworkObject>());
            Set(ramp, "m_CrewPrefab", crewPrefab.GetComponent<NetworkObject>());
            Set(ramp, "m_AircraftPrefab", aircraftPrefab.GetComponent<NetworkObject>());
            Set(ramp, "m_Broker", broker);
            Set(ramp, "m_Camera", camera);
            Set(ramp, "m_Readout", readout);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
        }

        static void BuildApronFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Apron";

            // Unity's plane primitive is ten metres across per unit of scale, so this is 400 metres
            // square -- room for a train to be got badly wrong in.
            floor.transform.localScale = new Vector3(40f, 1f, 40f);
            floor.GetComponent<MeshRenderer>().sharedMaterial.color = new Color(0.32f, 0.33f, 0.34f);
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

        static NetworkManager BuildNetworkManager(params GameObject[] prefabs)
        {
            var go = new GameObject("Network Manager");
            var manager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();

            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,

                // Every client simulates what it owns and sees replicated copies of the rest. One
                // client is nominated session owner to look after state that belongs to nobody.
                NetworkTopology = NetworkTopologyTypes.DistributedAuthority,

                // Players are given a character by the spawner rather than receiving one
                // automatically, because a character needs wiring to a camera and a broker that a
                // default player prefab knows nothing about.
                PlayerPrefab = null,
                TickRate = 20
            };

            foreach (var prefab in prefabs)
            {
                manager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = prefab });
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

        /// <summary>
        /// Assigns a private serialized field on a component.
        ///
        /// The fields these set are deliberately private: nothing at runtime should be reaching into
        /// a spawner to swap its prefabs. They are inspector wiring, and this is the editor doing
        /// the wiring, so it goes through the same serialization the inspector uses.
        /// </summary>
        static void Set(Object target, string field, Object value)
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
