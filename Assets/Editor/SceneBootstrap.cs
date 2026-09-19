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
            var crewProfile = ContentPaths.Needed<CrewProfile>(ContentPaths.CrewProfilePath);

            var tractor = VehiclePrefabs.BuildTractor(tractorProfile);
            var cart = VehiclePrefabs.BuildCart(cartProfile);
            var beltLoader = VehiclePrefabs.BuildBeltLoader(
                ContentPaths.Needed<VehicleProfile>(ContentPaths.BeltLoaderProfilePath));
            var aircraft = VehiclePrefabs.BuildAircraft(
                ContentPaths.Needed<AircraftProfile>(ContentPaths.AircraftProfilePath));
            var crew = VehiclePrefabs.BuildCrew(crewProfile);
            var bag = VehiclePrefabs.BuildBag();

            ApronScene.BuildScene(crewProfile, tractor, cart, beltLoader, aircraft, crew, bag);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Apron scene and prefabs rebuilt.");
        }
    }
}
