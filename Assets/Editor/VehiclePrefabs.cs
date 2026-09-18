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
    internal static class VehiclePrefabs
    {
        internal static GameObject BuildTractor(VehicleProfile profile)
        {
            var go = NewVehicle("BaggageTractor", profile, ContentPaths.TractorModelPath, ModelMeasure.MeasureTheTractor);

            go.AddComponent<VehicleOccupant>();

            return SaveAndDiscard(go, $"{ContentPaths.PrefabFolder}/BaggageTractor.prefab");
        }

        internal static GameObject BuildCart(VehicleProfile profile)
        {
            var go = NewVehicle("BaggageCart", profile, ContentPaths.CartModelPath, ModelMeasure.MeasureTheCart);

            return SaveAndDiscard(go, $"{ContentPaths.PrefabFolder}/BaggageCart.prefab");
        }

        internal static GameObject NewVehicle(
            string name, VehicleProfile profile, string modelPath, Func<GameObject, Transform, MeasuredVehicle> measure)
        {
            var go = new GameObject(name);
            go.AddComponent<Rigidbody>();

            var vehicle = go.AddComponent<VehicleController>();
            SerializedFields.Set(vehicle, "m_Profile", profile);

            var measured = measure(go, AddModel(go, modelPath));

            var shape = go.AddComponent<VehicleShape>();
            shape.Describe(measured.Shape);

            go.AddComponent<WheelLook>().Watch(measured.Wheels);

            AddNetworking(go, outlivesItsOwner: true);
            go.AddComponent<TrainMember>();

            go.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            go.AddComponent<ApronAppearance>().DescribeAsModelled(
                shape.EnvelopeCentreLocal.y + (shape.EnvelopeSizeMetres.y * 0.6f));
            go.AddComponent<ApronIdentity>();

            return go;
        }

        internal static Transform AddModel(GameObject vehicle, string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"No model at {path}, so '{vehicle.name}' cannot be measured and the apron " +
                    "cannot be built. Check the file is in the project and has been imported.");
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            model.name = ApronAppearance.LookName;
            model.transform.SetParent(vehicle.transform, worldPositionStays: false);
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.localRotation;

            DiscardAnythingThatLightsOrLooks(model);

            return model.transform;
        }

        internal static void DiscardAnythingThatLightsOrLooks(GameObject model)
        {
            foreach (var camera in model.GetComponentsInChildren<Camera>(true))
            {
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }

            foreach (var light in model.GetComponentsInChildren<Light>(true))
            {
                UnityEngine.Object.DestroyImmediate(light.gameObject);
            }
        }

        internal static GameObject BuildAircraft(AircraftProfile profile)
        {
            var go = new GameObject("NarrowbodyAirliner");

            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            go.AddComponent<CapsuleCollider>();

            SerializedFields.Set(go.AddComponent<AircraftBody>(), "m_Profile", profile);
            go.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            AddNetworking(go, outlivesItsOwner: true);

            Dress(go, ApronAppearance.Shape.LyingCapsule,
                new Vector3(profile.fuselageDiameterMetres, profile.lengthMetres, profile.fuselageDiameterMetres),
                Palette.FuselageWhite,
                profile.fuselageDiameterMetres);

            return SaveAndDiscard(go, $"{ContentPaths.PrefabFolder}/NarrowbodyAirliner.prefab");
        }

        internal static GameObject BuildBag()
        {
            var profile = SceneBootstrap.Needed<BagProfile>(ContentPaths.BagProfilePath);

            var go = new GameObject("Bag");
            go.AddComponent<Rigidbody>();
            go.AddComponent<BoxCollider>();

            SerializedFields.Set(go.AddComponent<Bag>(), "m_Profile", profile);

            go.AddComponent<HandUse>().As = HandUse.Category.Carry;

            AddNetworking(go, outlivesItsOwner: true);

            Dress(go, ApronAppearance.Shape.Box, profile.sizeMetres, Palette.BagCanvas, profile.sizeMetres.y * 1.2f);

            return SaveAndDiscard(go, $"{ContentPaths.PrefabFolder}/Bag.prefab");
        }

        internal static GameObject BuildCrew(CrewProfile profile)
        {
            var go = new GameObject("RampWorker");
            go.AddComponent<Rigidbody>();
            go.AddComponent<CapsuleCollider>();

            SerializedFields.Set(go.AddComponent<CrewCharacter>(), "m_Profile", profile);

            HandAnchor(go, Hands.LeftAnchorName, profile.hands.AnchorLocal(profile.radiusMetres, left: true));
            HandAnchor(go, Hands.RightAnchorName, profile.hands.AnchorLocal(profile.radiusMetres, left: false));

            AddNetworking(go, outlivesItsOwner: false);

            Dress(go, ApronAppearance.Shape.UprightCapsule, profile.SizeMetres, Palette.HiVis, profile.heightMetres * 0.7f);

            return SaveAndDiscard(go, $"{ContentPaths.PrefabFolder}/RampWorker.prefab");
        }

        internal static void HandAnchor(GameObject character, string name, Vector3 local)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(character.transform, worldPositionStays: false);
            anchor.transform.localPosition = local;
        }

        internal static void Dress(
            GameObject go,
            ApronAppearance.Shape shape,
            Vector3 sizeMetres,
            Color colour,
            float labelHeightMetres,
            Vector3 drawnAtLocal = default)
        {
            go.AddComponent<ApronAppearance>()
                .DescribeAs(shape, sizeMetres, colour, labelHeightMetres, drawnAtLocal);
            go.AddComponent<ApronIdentity>();
        }

        internal static void AddNetworking(GameObject go, bool outlivesItsOwner)
        {
            var networked = go.AddComponent<NetworkObject>();
            networked.DontDestroyWithOwner = outlivesItsOwner;

            if (go.GetComponent<VehicleController>() != null)
            {
                go.AddComponent<VehicleMotion>();
            }
            else if (go.GetComponent<CrewCharacter>() != null)
            {
                go.AddComponent<CrewMotion>();
            }
            else if (go.GetComponent<Bag>() != null)
            {
                go.AddComponent<CargoMotion>();
            }
            else
            {
                go.AddComponent<AnticipatedNetworkTransform>();
            }
        }

        internal static GameObject SaveAndDiscard(GameObject go, string path)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return saved;
        }
    }
}
