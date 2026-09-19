using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public static class ShippedContent
    {
        public const string PrefabFolder = "Assets/Content/Prefabs";

        public const string TractorPrefabPath = PrefabFolder + "/BaggageTractor.prefab";
        public const string CartPrefabPath = PrefabFolder + "/BaggageCart.prefab";
        public const string BagPrefabPath = PrefabFolder + "/Bag.prefab";
        public const string CrewPrefabPath = PrefabFolder + "/RampWorker.prefab";
        public const string AircraftPrefabPath = PrefabFolder + "/RegionalJet.prefab";

        public const string TractorProfilePath = "Assets/Content/Vehicles/BaggageTractor.asset";
        public const string CartProfilePath = "Assets/Content/Vehicles/BaggageCart.asset";
        public const string AircraftProfilePath = "Assets/Content/Aircraft/RegionalJet.asset";
        public const string CrewProfilePath = "Assets/Content/Crew/RampWorker.asset";

        public const string ScenePath = "Assets/Scenes/Apron.unity";
        public const string MenuScenePath = "Assets/Scenes/Menu.unity";

        public static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            Assert.That(asset, Is.Not.Null,
                $"there is no {typeof(T).Name} at {path}. Every fixture that reads shipped content " +
                "goes through here, so a moved or renamed asset says so once rather than as a null " +
                "reference somewhere further in");

            return asset;
        }

        public static GameObject Prefab(string path) => Load<GameObject>(path);

        public static GameObject PrefabNamed(string name) => Load<GameObject>($"{PrefabFolder}/{name}.prefab");

        public static VehicleShape Shape(string prefabPath)
        {
            var shape = Prefab(prefabPath).GetComponent<VehicleShape>();

            Assert.That(shape, Is.Not.Null,
                $"{prefabPath} has no VehicleShape, so nothing knows where its wheels, couplings or " +
                "bodywork are");

            return shape;
        }
    }
}
