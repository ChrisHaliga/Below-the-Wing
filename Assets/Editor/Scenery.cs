using System.Collections.Generic;
using BelowTheWing.Wiring;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    internal static class Scenery
    {
        const float ApronSideMetres = 400f;
        const float UnityPlaneSideMetres = 10f;
        const float SunIntensity = 1.1f;
        static readonly Vector3 SunAnglesDegrees = new Vector3(48f, 30f, 0f);

        internal static void BuildApronFloor()
        {
            AssetDatabase.DeleteAsset(ContentPaths.ApronMaterialPath);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Apron";

            floor.transform.localScale = new Vector3(ApronSideMetres / UnityPlaneSideMetres, 1f, ApronSideMetres / UnityPlaneSideMetres);

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
            sun.intensity = SunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(SunAnglesDegrees);
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
                ContentPaths.Needed<NetworkPrefabsList>(ContentPaths.DefaultPrefabListPath));

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
