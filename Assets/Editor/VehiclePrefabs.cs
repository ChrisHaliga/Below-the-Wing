using System;
using BelowTheWing.Apron;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Session;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    internal static class VehiclePrefabs
    {
        const float ModelFacesAwayDegrees = 180f;
        const float ModelFacesAlongDegrees = 0f;

        const float VehicleLabelAboveTheEnvelopeFraction = 0.6f;
        const float BagLabelAboveTheBagFraction = 1.2f;
        const float CrewLabelUpTheBodyFraction = 0.7f;
        const float AircraftLabelAboveTheTailMetres = 2f;

        internal static GameObject BuildTractor(VehicleProfile profile)
        {
            var go = NewVehicle(
                "BaggageTractor", profile, ContentPaths.TractorModelPath, ModelMeasure.MeasureTheTractor);

            go.AddComponent<VehicleOccupant>();

            return SaveAndDiscard(go, $"{ContentPaths.PrefabFolder}/BaggageTractor.prefab");
        }

        internal static GameObject BuildBeltLoader(VehicleProfile profile)
        {
            var go = NewVehicle(
                "BeltLoader", profile, ContentPaths.BeltLoaderModelPath, ModelMeasure.MeasureTheBeltLoader);

            go.AddComponent<VehicleOccupant>();

            return SaveAndDiscard(go, $"{ContentPaths.PrefabFolder}/BeltLoader.prefab");
        }

        internal static GameObject BuildCart(VehicleProfile profile)
        {
            var go = NewVehicle("BaggageCart", profile, ContentPaths.CartModelPath, ModelMeasure.MeasureTheCart);

            return SaveAndDiscard(go, $"{ContentPaths.PrefabFolder}/BaggageCart.prefab");
        }

        static GameObject NewVehicle(
            string name, VehicleProfile profile, string modelPath, Func<GameObject, Transform, MeasuredVehicle> measure)
        {
            var go = new GameObject(name);
            go.AddComponent<Rigidbody>();

            var vehicle = go.AddComponent<VehicleController>();
            SerializedFields.Set(vehicle, "m_Profile", profile);

            var measured = measure(go, AddModel(go, modelPath, ModelFacesAwayDegrees));

            var shape = go.AddComponent<VehicleShape>();
            shape.Describe(measured.Shape);

            go.AddComponent<WheelLook>().Watch(measured.Wheels);

            AddNetworking(go, outlivesItsOwner: true);
            go.AddComponent<TrainMember>();

            go.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            go.AddComponent<ApronAppearance>().DescribeAsModelled(
                shape.EnvelopeCentreLocal.y + (shape.EnvelopeSizeMetres.y * VehicleLabelAboveTheEnvelopeFraction));
            go.AddComponent<ApronIdentity>();

            return go;
        }

        static Transform AddModel(GameObject thing, string path, float modelFacesAwayDegrees)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"No model at {path}, so '{thing.name}' cannot be measured and the apron " +
                    "cannot be built. Check the file is in the project and has been imported.");
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            model.name = GreyboxShape.LookName;
            model.transform.SetParent(thing.transform, worldPositionStays: false);
            model.transform.localRotation =
                Quaternion.Euler(0f, modelFacesAwayDegrees, 0f) * model.transform.localRotation;

            return model.transform;
        }

        internal static GameObject BuildAircraft(AircraftProfile profile)
        {
            var go = new GameObject("RegionalJet");

            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;

            var model = AddModel(go, ContentPaths.RegionalJetModelPath, ModelFacesAlongDegrees);
            var envelope = ModelMeasure.MeasureTheAircraft(go, model);

            go.AddComponent<AircraftShape>().Describe(envelope.size, envelope.center);
            BeSolidWhereItIsDrawn(model);

            SerializedFields.Set(go.AddComponent<AircraftBody>(), "m_Profile", profile);
            go.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            AddNetworking(go, outlivesItsOwner: true);

            go.AddComponent<ApronAppearance>().DescribeAsModelled(
                envelope.max.y + AircraftLabelAboveTheTailMetres);
            go.AddComponent<ApronIdentity>();

            return SaveAndDiscard(go, $"{ContentPaths.PrefabFolder}/RegionalJet.prefab");
        }

        static void BeSolidWhereItIsDrawn(Transform model)
        {
            foreach (var drawn in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (drawn.sharedMesh == null)
                {
                    continue;
                }

                var solid = drawn.gameObject.AddComponent<MeshCollider>();
                solid.sharedMesh = drawn.sharedMesh;
                solid.convex = false;
            }
        }

        internal static GameObject BuildBag()
        {
            var profile = ContentPaths.Needed<BagProfile>(ContentPaths.BagProfilePath);

            var go = new GameObject("Bag");
            go.AddComponent<Rigidbody>();
            go.AddComponent<BoxCollider>();

            SerializedFields.Set(go.AddComponent<Bag>(), "m_Profile", profile);

            go.AddComponent<HandUse>().As = HandUse.Category.Carry;

            AddNetworking(go, outlivesItsOwner: true);

            Dress(go, ApronAppearance.Shape.Box, profile.sizeMetres, Palette.BagCanvas, profile.sizeMetres.y * BagLabelAboveTheBagFraction);

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

            Dress(go, ApronAppearance.Shape.UprightCapsule, profile.SizeMetres, Palette.HiVis, profile.heightMetres * CrewLabelUpTheBodyFraction);

            return SaveAndDiscard(go, $"{ContentPaths.PrefabFolder}/RampWorker.prefab");
        }

        static void HandAnchor(GameObject character, string name, Vector3 local)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(character.transform, worldPositionStays: false);
            anchor.transform.localPosition = local;
        }

        static void Dress(
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

        static void AddNetworking(GameObject go, bool outlivesItsOwner)
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

        static GameObject SaveAndDiscard(GameObject go, string path)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return saved;
        }
    }
}
