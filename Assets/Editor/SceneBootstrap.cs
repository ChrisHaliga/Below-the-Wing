using System.Collections.Generic;
using System.IO;
using BelowTheWing.Apron;
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
    /// <summary>
    /// Builds the apron scene and the four prefabs it spawns.
    ///
    /// One prefab per kind of thing, each carrying the profile it runs on. That is what makes a cart
    /// a cart: not a value it is told after it exists, but the thing it was made from. The stand-in
    /// shape is filled in here from the same profile, so what a vehicle looks like and what it
    /// collides as cannot drift apart.
    ///
    /// The scene deliberately contains no aircraft, tractors or carts. It holds the machinery --
    /// networking, the camera, the readout -- and everything on the apron is put there at runtime by
    /// <see cref="RampSession"/>, so there is one description of what stands where.
    ///
    /// Running this again replaces the scene and prefabs it made. Anything hand-edited in them will
    /// be lost, which is fine while they are scaffolding and worth remembering once they are not.
    /// </summary>
    public static class SceneBootstrap
    {
        const string PrefabFolder = "Assets/Content/Prefabs";
        const string ScenePath = "Assets/Scenes/Apron.unity";
        const string ApronMaterialPath = "Assets/Content/ApronConcrete.mat";

        const string TractorProfilePath = "Assets/Content/Vehicles/BaggageTractor.asset";
        const string CartProfilePath = "Assets/Content/Vehicles/BaggageCart.asset";
        const string AircraftProfilePath = "Assets/Content/Aircraft/NarrowbodyAirliner.asset";
        const string CrewProfilePath = "Assets/Content/Crew/RampWorker.asset";

        static readonly Color TractorBlue = new Color(0.35f, 0.55f, 0.75f);
        static readonly Color CartGrey = new Color(0.55f, 0.55f, 0.58f);
        static readonly Color HiVisYellow = new Color(0.95f, 0.75f, 0.15f);
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

            BuildScene(tractorProfile, cartProfile, aircraftProfile, crewProfile, tractor, cart, aircraft, crew);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Apron scene and prefabs rebuilt.");
        }

        static GameObject BuildTractor(VehicleProfile profile)
        {
            var go = NewVehicle("BaggageTractor", profile, TractorBlue);

            // Only something a player can sit in needs to say whether somebody is sitting in it, and
            // to refuse to change hands while they are.
            go.AddComponent<VehicleOccupant>();

            return SaveAndDiscard(go, $"{PrefabFolder}/BaggageTractor.prefab");
        }

        static GameObject BuildCart(VehicleProfile profile)
            => SaveAndDiscard(NewVehicle("BaggageCart", profile, CartGrey), $"{PrefabFolder}/BaggageCart.prefab");

        static GameObject NewVehicle(string name, VehicleProfile profile, Color colour)
        {
            var go = new GameObject(name);
            go.AddComponent<Rigidbody>();
            go.AddComponent<BoxCollider>();

            var vehicle = go.AddComponent<VehicleController>();
            Set(vehicle, "m_Profile", profile);

            AddNetworking(go);
            go.AddComponent<TrainMember>();

            Dress(go, ApronAppearance.Shape.Box,
                profile.bodySizeMetres,
                colour,
                profile.bodySizeMetres.y * 0.7f);

            return go;
        }

        static GameObject BuildAircraft(AircraftProfile profile)
        {
            var go = new GameObject("NarrowbodyAirliner");

            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            go.AddComponent<CapsuleCollider>();

            Set(go.AddComponent<AircraftBody>(), "m_Profile", profile);

            AddNetworking(go);

            Dress(go, ApronAppearance.Shape.LyingCapsule,
                new Vector3(profile.fuselageDiameterMetres, profile.lengthMetres, profile.fuselageDiameterMetres),
                FuselageWhite,
                profile.fuselageDiameterMetres);

            return SaveAndDiscard(go, $"{PrefabFolder}/NarrowbodyAirliner.prefab");
        }

        static GameObject BuildCrew(CrewProfile profile)
        {
            var go = new GameObject("RampWorker");
            go.AddComponent<Rigidbody>();
            go.AddComponent<CapsuleCollider>();

            Set(go.AddComponent<CrewCharacter>(), "m_Profile", profile);

            AddNetworking(go);

            Dress(go, ApronAppearance.Shape.UprightCapsule,
                new Vector3(profile.radiusMetres * 2f, profile.heightMetres, profile.radiusMetres * 2f),
                HiVisYellow,
                profile.heightMetres * 0.7f);

            return SaveAndDiscard(go, $"{PrefabFolder}/RampWorker.prefab");
        }

        /// <summary>
        /// Gives an object its stand-in shape and the name-carrying component that shows it.
        ///
        /// The name travels over the network, and appearance is built when it arrives, so a player
        /// who joins sees the apron rather than an empty grey plane full of invisible colliders.
        /// </summary>
        static void Dress(GameObject go, ApronAppearance.Shape shape, Vector3 sizeMetres, Color colour, float labelHeightMetres)
        {
            go.AddComponent<ApronAppearance>().DescribeAs(shape, sizeMetres, colour, labelHeightMetres);
            go.AddComponent<ApronIdentity>();
        }

        static void AddNetworking(GameObject go)
        {
            go.AddComponent<NetworkObject>();

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

        static void BuildScene(
            VehicleProfile tractorProfile,
            VehicleProfile cartProfile,
            AircraftProfile aircraftProfile,
            CrewProfile crewProfile,
            GameObject tractor,
            GameObject cart,
            GameObject aircraft,
            GameObject crew)
        {
            // Replacing the open scene throws away anything unsaved in it, so ask first. Somebody
            // running this from the menu has other work open more often than not.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Apron rebuild cancelled.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildApronFloor();
            BuildLighting();

            var camera = BuildCamera();
            var manager = BuildNetworkManager(tractor, cart, aircraft, crew);
            var gateway = manager.gameObject.AddComponent<SessionGateway>();
            var broker = manager.gameObject.AddComponent<NetworkOwnershipBroker>();

            Set(new GameObject("Session Entry Screen").AddComponent<SessionEntryScreen>(), "m_Gateway", gateway);
            var readout = new GameObject("Ramp Readout").AddComponent<RampReadout>();

            var sessionObject = new GameObject("Ramp Session");
            sessionObject.AddComponent<NetworkObject>();
            var session = sessionObject.AddComponent<RampSession>();

            Set(session, "m_TractorProfile", tractorProfile);
            Set(session, "m_CartProfile", cartProfile);
            Set(session, "m_AircraftProfile", aircraftProfile);
            Set(session, "m_CrewProfile", crewProfile);
            Set(session, "m_TractorPrefab", tractor.GetComponent<NetworkObject>());
            Set(session, "m_CartPrefab", cart.GetComponent<NetworkObject>());
            Set(session, "m_AircraftPrefab", aircraft.GetComponent<NetworkObject>());
            Set(session, "m_CrewPrefab", crew.GetComponent<NetworkObject>());
            Set(session, "m_Broker", broker);
            Set(session, "m_Camera", camera);
            Set(session, "m_Readout", readout);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
        }

        static void BuildApronFloor()
        {
            AssetDatabase.DeleteAsset(ApronMaterialPath);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Apron";

            // Unity's plane primitive is ten metres across per unit of scale, so this is 400 metres
            // square -- room for a train to be got badly wrong in.
            floor.transform.localScale = new Vector3(40f, 1f, 40f);

            // A material of its own. Writing a colour onto sharedMaterial would repaint the render
            // pipeline's default material, and with it every other object in the project using it.
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

                // Players are given a character by the session rather than receiving one
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
        /// a session to swap its prefabs. They are inspector wiring, and this is the editor doing
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
