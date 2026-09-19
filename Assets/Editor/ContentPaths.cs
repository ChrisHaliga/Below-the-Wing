using System;
using UnityEditor;

namespace BelowTheWing.EditorTools
{
    internal static class ContentPaths
    {
        internal const string PrefabFolder = "Assets/Content/Prefabs";

        internal const string BeltLoaderModelPath = "Assets/Content/Vehicles/belt_loader.fbx";

        internal const string RegionalJetModelPath = "Assets/Content/Vehicles/crj_200.fbx";

        internal const string ScenePath = "Assets/Scenes/Apron.unity";

        internal const string MenuScenePath = "Assets/Scenes/Menu.unity";

        internal const string ApronMaterialPath = "Assets/Content/ApronConcrete.mat";





        internal const string DefaultPrefabListPath = "Assets/DefaultNetworkPrefabs.asset";

        internal const string TractorProfilePath = "Assets/Content/Vehicles/BaggageTractor.asset";

        internal const string CartProfilePath = "Assets/Content/Vehicles/BaggageCart.asset";

        internal const string BeltLoaderProfilePath = "Assets/Content/Vehicles/BeltLoader.asset";

        internal const string AircraftProfilePath = "Assets/Content/Aircraft/RegionalJet.asset";

        internal const string CrewProfilePath = "Assets/Content/Crew/RampWorker.asset";

        internal const string BagProfilePath = "Assets/Content/Cargo/CheckedBag.asset";

        internal const string CartModelPath = "Assets/Content/Vehicles/baggage_cart.fbx";

        internal const string TractorModelPath = "Assets/Content/Vehicles/baggage_tractor.fbx";

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
