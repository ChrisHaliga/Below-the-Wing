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
    internal static class ApronScene
    {
        internal static void BuildScene(
            AircraftProfile aircraftProfile,
            CrewProfile crewProfile,
            GameObject tractor,
            GameObject cart,
            GameObject aircraft,
            GameObject crew)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Apron rebuild cancelled.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Scenery.BuildApronFloor();
            Scenery.BuildLighting();

            var camera = BuildCamera();
            var readout = new GameObject("Ramp Readout").AddComponent<RampReadout>();

            var sessionObject = new GameObject("Ramp Session");
            sessionObject.AddComponent<NetworkObject>();
            var session = sessionObject.AddComponent<RampSession>();

            SerializedFields.Set(session, "m_AircraftProfile", aircraftProfile);
            SerializedFields.Set(session, "m_CrewProfile", crewProfile);
            SerializedFields.Set(session, "m_TractorPrefab", tractor.GetComponent<NetworkObject>());
            SerializedFields.Set(session, "m_CartPrefab", cart.GetComponent<NetworkObject>());
            SerializedFields.Set(session, "m_AircraftPrefab", aircraft.GetComponent<NetworkObject>());
            SerializedFields.Set(session, "m_CrewPrefab", crew.GetComponent<NetworkObject>());

            SerializedFields.Set(
                session,
                "m_BagPrefab",
                SceneBootstrap.Needed<GameObject>($"{ContentPaths.PrefabFolder}/Bag.prefab").GetComponent<NetworkObject>());
            SerializedFields.Set(session, "m_Camera", camera);
            SerializedFields.Set(session, "m_Readout", readout);

            EditorSceneManager.SaveScene(scene, ContentPaths.ScenePath);
            GiveTheSceneObjectsTheirIdentities();

            if (File.Exists(ContentPaths.MenuScenePath))
            {
                Scenery.AddToBuildSettings(ContentPaths.MenuScenePath, first: true);
            }

            Scenery.AddToBuildSettings(ContentPaths.ScenePath, first: false);
        }

        internal static void GiveTheSceneObjectsTheirIdentities()
        {
            var saved = EditorSceneManager.OpenScene(ContentPaths.ScenePath, OpenSceneMode.Single);

            EditorSceneManager.MarkSceneDirty(saved);
            EditorSceneManager.SaveScene(saved, ContentPaths.ScenePath);
        }

        internal static FollowCamera BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            return go.AddComponent<FollowCamera>();
        }
    }
}
