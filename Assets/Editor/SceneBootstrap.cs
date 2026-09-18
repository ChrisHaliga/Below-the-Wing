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
        [MenuItem("Below the Wing/Rebuild apron scene and prefabs")]
        public static void Rebuild()
        {
            ContentBootstrap.CreateMissingContent();

            Directory.CreateDirectory(ContentPaths.PrefabFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ContentPaths.ScenePath) ?? "Assets/Scenes");

            var tractorProfile = AssetDatabase.LoadAssetAtPath<VehicleProfile>(ContentPaths.TractorProfilePath);
            var cartProfile = AssetDatabase.LoadAssetAtPath<VehicleProfile>(ContentPaths.CartProfilePath);
            var aircraftProfile = AssetDatabase.LoadAssetAtPath<AircraftProfile>(ContentPaths.AircraftProfilePath);
            var crewProfile = AssetDatabase.LoadAssetAtPath<CrewProfile>(ContentPaths.CrewProfilePath);

            var tractor = VehiclePrefabs.BuildTractor(tractorProfile);
            var cart = VehiclePrefabs.BuildCart(cartProfile);
            var aircraft = VehiclePrefabs.BuildAircraft(aircraftProfile);
            var crew = VehiclePrefabs.BuildCrew(crewProfile);
            VehiclePrefabs.BuildBag();

            ApronScene.BuildScene(aircraftProfile, crewProfile, tractor, cart, aircraft, crew);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Apron scene and prefabs rebuilt.");
        }

        [MenuItem("Below the Wing/Lay out the menu scene")]
        public static void LayOutTheMenuScene()
        {
            var aircraftProfile = Needed<AircraftProfile>(ContentPaths.AircraftProfilePath);
            var crewProfile = Needed<CrewProfile>(ContentPaths.CrewProfilePath);

            MenuScene.BuildMenuScene(
                aircraftProfile,
                crewProfile,
                Needed<GameObject>($"{ContentPaths.PrefabFolder}/BaggageTractor.prefab"),
                Needed<GameObject>($"{ContentPaths.PrefabFolder}/BaggageCart.prefab"),
                Needed<GameObject>($"{ContentPaths.PrefabFolder}/NarrowbodyAirliner.prefab"),
                Needed<GameObject>($"{ContentPaths.PrefabFolder}/RampWorker.prefab"));

            Scenery.AddToBuildSettings(ContentPaths.MenuScenePath, first: true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        internal static T Needed<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"There is no {typeof(T).Name} at {path}. Run Below the Wing/Rebuild apron scene " +
                    "and prefabs first, which is what writes the prefabs the menu scene is dressed with.");
            }

            return asset;
        }
    }
}
