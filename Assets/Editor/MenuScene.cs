using System;
using System.Collections.Generic;
using System.IO;
using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Menu;
using BelowTheWing.Net;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace BelowTheWing.EditorTools
{
    internal static class MenuScene
    {
        const float JetAheadOfTheWideShotMetres = 48f;

        const float JetRightOfTheWideShotMetres = 26f;

        const float CrewStandBackMetres = 4.15f;

        const float CrewSpacingMetres = 1.2f;

        const float LoaderSitsBackMetres = 1.4f;

        const float EyeHeightFractionOfTheCrew = 0.92f;
        const float InsideShotLooksAtCrewHeightFraction = 0.62f;
        const float PlateHangsAtCrewHeightFraction = 0.56f;
        const float CartShotStandsOffMetres = 2.3f;
        const float InsideShotPastTheCentreMetres = 0.25f;
        const float OpeningLooksTowardTheNoseFraction = 0.55f;
        const float OpeningLooksAtHeightMetres = 0.88f;
        static readonly Vector3 OpeningStandsOffTheTug = new Vector3(19.18f, 4.41f, -13.38f);
        static readonly Vector2Int MenuReferenceResolution = new Vector2Int(1920, 1080);

        internal static void BuildMenuScene(
            CrewProfile crewProfile,
            GameObject tractor,
            GameObject cart,
            GameObject beltLoader,
            GameObject aircraft,
            GameObject crew)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Scenery.BuildApronFloor();
            Scenery.BuildLighting();

            var manager = Scenery.BuildNetworkManager();
            var gateway = manager.gameObject.AddComponent<SessionGateway>();
            manager.gameObject.AddComponent<NetworkOwnershipBroker>();

            var lobby = new GameObject("Lobby Roster");
            lobby.AddComponent<NetworkObject>();
            lobby.AddComponent<LobbyRoster>();

            var eye = new GameObject("Menu Camera");
            eye.AddComponent<Camera>();
            eye.AddComponent<AudioListener>();
            var menuCamera = eye.AddComponent<MenuCamera>();

            var backdrop = BuildBackdrop(
                eye.transform, crewProfile, tractor, cart, beltLoader, aircraft, crew);

            var menu = new GameObject("Menu");
            var document = menu.AddComponent<UIDocument>();
            document.panelSettings = MenuPanel();
            document.sortingOrder = 100f;

            var driver = menu.AddComponent<MenuDriver>();

            SerializedFields.Set(driver, "m_Gateway", gateway);
            SerializedFields.Set(driver, "m_Camera", menuCamera);
            SerializedFields.Set(driver, "m_Backdrop", backdrop);

            SerializedFields.Set(driver, "m_Display", AssetDatabase.LoadAssetAtPath<Font>(ContentPaths.DisplayFontPath));
            SerializedFields.Set(driver, "m_Body", AssetDatabase.LoadAssetAtPath<Font>(ContentPaths.BodyFontPath));
            SerializedFields.Set(driver, "m_Data", AssetDatabase.LoadAssetAtPath<Font>(ContentPaths.DataFontPath));

            SerializedFields.Set(driver, "m_ReadyIcon", ContentPaths.Needed<Texture2D>(ContentPaths.ReadyIconPath));
            SerializedFields.Set(driver, "m_UnreadyIcon", ContentPaths.Needed<Texture2D>(ContentPaths.UnreadyIconPath));

            EditorSceneManager.SaveScene(scene, ContentPaths.MenuScenePath);
        }

        static MenuBackdrop BuildBackdrop(
            Transform eye,
            CrewProfile crewProfile,
            GameObject tractor,
            GameObject cart,
            GameObject beltLoader,
            GameObject aircraft,
            GameObject crew)
        {
            var holder = new GameObject("Backdrop");
            var backdrop = holder.AddComponent<MenuBackdrop>();

            var layout = ApronLayoutSettings.Default;
            var plan = ApronLayout.Build(layout, new ApronEquipment(
                tractor.GetComponent<VehicleShape>().Footprint,
                cart.GetComponent<VehicleShape>().Footprint,
                beltLoader.GetComponent<VehicleShape>().Footprint,
                aircraft.GetComponent<AircraftShape>().EnvelopeSizeMetres,
                aircraft.GetComponent<AircraftShape>().EnvelopeCentreLocal,
                crewProfile.SizeMetres));

            Stage(aircraft, plan.Aircraft, holder.transform);

            var train = plan.Trains[0];

            Stage(tractor, train.Tractor, holder.transform);

            var staged = (GameObject)null;

            foreach (var parked in train.Carts)
            {
                var placed = Stage(cart, parked, holder.transform);

                staged ??= placed;
            }

            var stage = train.Carts[0];
            var facing = stage.Rotation * Vector3.right;
            var alongTheCart = stage.Rotation * Vector3.forward;
            var crewLine = stage.Position - (facing * CrewStandBackMetres);

            var standing = new List<GameObject>(Shift.MostCrew);

            for (var i = 0; i < Shift.MostCrew; i++)
            {
                var at = crewLine
                         + (alongTheCart * ((i - ((Shift.MostCrew - 1) * 0.5f)) * CrewSpacingMetres))
                         + (Vector3.up * (crewProfile.heightMetres * 0.5f));

                var figure = Stage(
                    crew,
                    new Placement($"Crew {i + 1}", at, Quaternion.LookRotation(facing), Vector3.one),
                    holder.transform);

                GreyboxShape.AttachCapsule(
                    figure.transform,
                    crewProfile.heightMetres,
                    crewProfile.radiusMetres * 2f,
                    Palette.HiVis);

                figure.SetActive(false);
                standing.Add(figure);
            }

            var loader = ParkTheBeltLoader(holder.transform, crewLine, facing, stage.Rotation);

            var nose = plan.Aircraft.Position;
            var tug = train.Tractor.Position;
            var eyeHeight = crewProfile.heightMetres * EyeHeightFractionOfTheCrew;
            var doorwayHeight = DoorwayHeightOf(staged);

            var opening = tug + OpeningStandsOffTheTug;

            eye.SetPositionAndRotation(
                opening,
                Quaternion.LookRotation(
                    (Vector3.Lerp(nose, tug, OpeningLooksTowardTheNoseFraction) + (Vector3.up * OpeningLooksAtHeightMetres)) - opening,
                    Vector3.up));

            SerializedFields.Set(backdrop, "m_Airliner", ParkTheJet(holder.transform, eye).transform);

            SerializedFields.Set(backdrop, "m_CartShot", Shot("Cart shot", holder.transform,
                stage.Position + (facing * CartShotStandsOffMetres) + new Vector3(0f, eyeHeight, 0f),
                stage.Position + new Vector3(0f, doorwayHeight, 0f)));

            SerializedFields.Set(backdrop, "m_InsideShot", Shot("Inside shot", holder.transform,
                stage.Position - (facing * InsideShotPastTheCentreMetres) + new Vector3(0f, doorwayHeight, 0f),
                crewLine + new Vector3(0f, crewProfile.heightMetres * InsideShotLooksAtCrewHeightFraction, 0f)));

            SerializedFields.SetList(backdrop, "m_LobbyCrew", standing);
            SerializedFields.Set(backdrop, "m_PlateHeightMetres", crewProfile.heightMetres * PlateHangsAtCrewHeightFraction);
            SerializedFields.Set(backdrop, "m_BeltLoader", loader.transform);
            SerializedFields.Set(backdrop, "m_CartDoors", DoorsOf(holder, staged));

            return backdrop;
        }

        static GameObject ParkTheBeltLoader(
            Transform under, Vector3 crewLine, Vector3 facing, Quaternion asTheTrainSits)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ContentPaths.BeltLoaderModelPath);

            if (model == null)
            {
                throw new InvalidOperationException($"There is no belt loader model at {ContentPaths.BeltLoaderModelPath}.");
            }

            var loader = new GameObject("Belt loader");
            loader.transform.SetParent(under, worldPositionStays: false);
            loader.transform.SetPositionAndRotation(crewLine, asTheTrainSits);

            var drawn = (GameObject)PrefabUtility.InstantiatePrefab(model, loader.transform);
            drawn.transform.localPosition = Vector3.zero;

            loader.transform.position =
                crewLine - (facing * (ReachesForward(loader, facing) + LoaderSitsBackMetres));

            return loader;
        }

        static GameObject ParkTheJet(Transform under, Transform wide)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ContentPaths.RegionalJetModelPath);

            if (model == null)
            {
                throw new InvalidOperationException($"There is no regional jet model at {ContentPaths.RegionalJetModelPath}.");
            }

            var jet = new GameObject("Regional jet");
            jet.transform.SetParent(under, worldPositionStays: false);

            var broadside = Vector3.ProjectOnPlane(wide.right, Vector3.up).normalized;
            var at = wide.position
                     + (wide.forward * JetAheadOfTheWideShotMetres)
                     + (broadside * JetRightOfTheWideShotMetres);

            jet.transform.SetPositionAndRotation(
                new Vector3(at.x, 0f, at.z),
                Quaternion.LookRotation(broadside));

            var drawn = (GameObject)PrefabUtility.InstantiatePrefab(model, jet.transform);
            drawn.transform.localPosition = Vector3.zero;

            return jet;
        }

        static float ReachesForward(GameObject thing, Vector3 facing)
        {
            var from = thing.transform.position;
            var most = 0f;

            foreach (var drawn in thing.GetComponentsInChildren<Renderer>(true))
            {
                most = Mathf.Max(most, Vector3.Dot(drawn.bounds.max - from, facing));
                most = Mathf.Max(most, Vector3.Dot(drawn.bounds.min - from, facing));
            }

            return most;
        }

        static float DoorwayHeightOf(GameObject staged)
        {
            foreach (var skin in staged.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skin.sharedMesh != null && skin.sharedMesh.blendShapeCount > 0)
                {
                    return skin.bounds.center.y - staged.transform.position.y;
                }
            }

            throw new InvalidOperationException($"{staged.name} has no door to measure a doorway from.");
        }

        static MenuCartDoors DoorsOf(GameObject holder, GameObject staged)
        {
            var doors = holder.AddComponent<MenuCartDoors>();

            SerializedFields.Set(doors, "m_Cart", staged);

            return doors;
        }

        static Transform Shot(string name, Transform under, Vector3 from, Vector3 at)
        {
            var shot = new GameObject(name).transform;

            shot.SetParent(under, worldPositionStays: true);
            shot.SetPositionAndRotation(from, Quaternion.LookRotation(at - from, Vector3.up));

            return shot;
        }

        static GameObject Stage(GameObject prefab, Placement where, Transform under)
        {
            var placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab, under);

            placed.transform.SetPositionAndRotation(where.Position, where.Rotation);
            placed.name = where.Name;

            foreach (var body in placed.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
            }

            foreach (var behaviour in placed.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (var networked in placed.GetComponentsInChildren<NetworkObject>(true))
            {
                UnityEngine.Object.DestroyImmediate(networked, allowDestroyingAssets: false);
            }

            return placed;
        }

        static PanelSettings MenuPanel()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ContentPaths.PanelSettingsPath) ?? "Assets/UI");

            AssetDatabase.DeleteAsset(ContentPaths.PanelSettingsPath);

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "Menu panel";
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ContentPaths.ThemePath);
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = MenuReferenceResolution;

            AssetDatabase.CreateAsset(settings, ContentPaths.PanelSettingsPath);

            return settings;
        }
    }
}
