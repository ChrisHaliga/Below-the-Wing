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
    internal static class Scenery
    {
        internal static void BuildApronFloor()
        {
            AssetDatabase.DeleteAsset(ContentPaths.ApronMaterialPath);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Apron";

            floor.transform.localScale = new Vector3(40f, 1f, 40f);

            var concrete = new Material(floor.GetComponent<MeshRenderer>().sharedMaterial)
            {
                name = "Apron concrete",
                color = Palette.ApronConcrete
            };

            AssetDatabase.CreateAsset(concrete, ContentPaths.ApronMaterialPath);
            floor.GetComponent<MeshRenderer>().sharedMaterial = concrete;
        }

        internal static void BuildLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, 30f, 0f);
        }

        internal static NetworkManager BuildNetworkManager()
        {
            var go = new GameObject("Network Manager");
            var manager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();

            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,

                NetworkTopology = NetworkTopologyTypes.DistributedAuthority,

                PlayerPrefab = null,
                TickRate = NetworkClock.TicksPerSecond
            };

            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(
                SceneBootstrap.Needed<NetworkPrefabsList>(ContentPaths.DefaultPrefabListPath));

            return manager;
        }

        internal static void AddToBuildSettings(string path, bool first)
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
    }
}
