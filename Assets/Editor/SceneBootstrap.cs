using System.IO;
using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    public static class SceneBootstrap
    {
        [MenuItem("Below the Wing/Rebuild apron scene and prefabs")]
        public static void Rebuild()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Apron rebuild cancelled; nothing was written.");
                return;
            }

            ContentBootstrap.CreateMissingContent();

            Directory.CreateDirectory(ContentPaths.PrefabFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ContentPaths.ScenePath) ?? "Assets/Scenes");

            var tractorProfile = ContentPaths.Needed<VehicleProfile>(ContentPaths.TractorProfilePath);
            var cartProfile = ContentPaths.Needed<VehicleProfile>(ContentPaths.CartProfilePath);
            var aircraftProfile = ContentPaths.Needed<AircraftProfile>(ContentPaths.AircraftProfilePath);
            var crewProfile = ContentPaths.Needed<CrewProfile>(ContentPaths.CrewProfilePath);

            var tractor = VehiclePrefabs.BuildTractor(tractorProfile);
            var cart = VehiclePrefabs.BuildCart(cartProfile);
            var aircraft = VehiclePrefabs.BuildAircraft(aircraftProfile);
            var crew = VehiclePrefabs.BuildCrew(crewProfile);
            var bag = VehiclePrefabs.BuildBag();

            ApronScene.BuildScene(aircraftProfile, crewProfile, tractor, cart, aircraft, crew, bag);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Apron scene and prefabs rebuilt.");
        }

        [MenuItem("Below the Wing/Lay out the menu scene")]
        public static void LayOutTheMenuScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Menu scene layout cancelled; nothing was written.");
                return;
            }

            MenuScene.BuildMenuScene(
                ContentPaths.Needed<AircraftProfile>(ContentPaths.AircraftProfilePath),
                ContentPaths.Needed<CrewProfile>(ContentPaths.CrewProfilePath),
                ContentPaths.Needed<GameObject>($"{ContentPaths.PrefabFolder}/BaggageTractor.prefab"),
                ContentPaths.Needed<GameObject>($"{ContentPaths.PrefabFolder}/BaggageCart.prefab"),
                ContentPaths.Needed<GameObject>($"{ContentPaths.PrefabFolder}/NarrowbodyAirliner.prefab"),
                ContentPaths.Needed<GameObject>($"{ContentPaths.PrefabFolder}/RampWorker.prefab"));

            Scenery.AddToBuildSettings(ContentPaths.MenuScenePath, first: true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
