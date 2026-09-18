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
    internal static class ContentPaths
    {
        internal const string PrefabFolder = "Assets/Content/Prefabs";

        internal const string BeltLoaderModelPath = "Assets/Content/Vehicles/belt_loader.fbx";

        internal const string RegionalJetModelPath = "Assets/Content/Vehicles/crj_200.fbx";

        internal const string ScenePath = "Assets/Scenes/Apron.unity";

        internal const string MenuScenePath = "Assets/Scenes/Menu.unity";

        internal const string ApronMaterialPath = "Assets/Content/ApronConcrete.mat";

        internal const string ThemePath = "Assets/UI/MenuTheme.tss";

        internal const string DisplayFontPath = "Assets/UI/Fonts/Inter-SemiBold.ttf";

        internal const string BodyFontPath = "Assets/UI/Fonts/Inter-Regular.ttf";

        internal const string DataFontPath = "Assets/UI/Fonts/RobotoMono-Bold.ttf";

        internal const string ReadyIconPath = "Assets/UI/Icons/Ready.png";

        internal const string UnreadyIconPath = "Assets/UI/Icons/Unready.png";

        internal const string PanelSettingsPath = "Assets/UI/MenuPanelSettings.asset";

        internal const string DefaultPrefabListPath = "Assets/DefaultNetworkPrefabs.asset";

        internal const string TractorProfilePath = "Assets/Content/Vehicles/BaggageTractor.asset";

        internal const string CartProfilePath = "Assets/Content/Vehicles/BaggageCart.asset";

        internal const string AircraftProfilePath = "Assets/Content/Aircraft/NarrowbodyAirliner.asset";

        internal const string CrewProfilePath = "Assets/Content/Crew/RampWorker.asset";

        internal const string BagProfilePath = "Assets/Content/Cargo/CheckedBag.asset";

        internal const string CartModelPath = "Assets/Content/Vehicles/baggage_cart.fbx";

        internal const string TractorModelPath = "Assets/Content/Vehicles/baggage_tractor.fbx";
    }
}
