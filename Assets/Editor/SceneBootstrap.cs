using System;
using System.Collections.Generic;
using System.IO;
using BelowTheWing.Apron;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Diagnostics;
using BelowTheWing.Net;
using BelowTheWing.Session;
using BelowTheWing.Vehicles;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BelowTheWing.EditorTools
{
    /// <summary>
    /// Builds the apron scene and the four prefabs it spawns.
    ///
    /// One prefab per kind of thing, each carrying the profile it runs on. That is what makes a cart
    /// a cart: not a value it is told after it exists, but the thing it was made from. A vehicle's
    /// geometry is measured off its model here, so what a player sees, what the physics hangs its
    /// suspension from and what the vehicle collides as cannot drift apart -- they are one
    /// measurement, taken once.
    ///
    /// The scene deliberately contains no aircraft, tractors or carts. It holds the machinery --
    /// networking, the camera, the readout -- and everything on the apron is put there at runtime by
    /// <see cref="RampSession"/>, so there is one description of what stands where.
    ///
    /// Running this again replaces the scene and prefabs it made. Anything hand-edited in them will
    /// be lost, which is fine while they are scaffolding and worth remembering once they are not.
    /// </summary>
    public static class SceneBootstrap
    {
        const string PrefabFolder = "Assets/Content/Prefabs";
        const string ScenePath = "Assets/Scenes/Apron.unity";
        const string ApronMaterialPath = "Assets/Content/ApronConcrete.mat";

        /// <summary>
        /// The prefab list Netcode maintains as prefabs are added to the project. Registering this
        /// is what makes spawned objects creatable on machines other than the one that spawned them.
        /// </summary>
        const string DefaultPrefabListPath = "Assets/DefaultNetworkPrefabs.asset";

        const string TractorProfilePath = "Assets/Content/Vehicles/BaggageTractor.asset";
        const string CartProfilePath = "Assets/Content/Vehicles/BaggageCart.asset";
        const string AircraftProfilePath = "Assets/Content/Aircraft/NarrowbodyAirliner.asset";
        const string CrewProfilePath = "Assets/Content/Crew/RampWorker.asset";
        const string BagProfilePath = "Assets/Content/Cargo/CheckedBag.asset";
        const string CartModelPath = "Assets/Content/Vehicles/baggage_cart.fbx";
        const string TractorModelPath = "Assets/Content/Vehicles/baggage_tractor.fbx";

        static readonly Color HiVisYellow = new Color(0.95f, 0.75f, 0.15f);
        static readonly Color BagCanvas = new Color(0.45f, 0.38f, 0.32f);
        static readonly Color FuselageWhite = new Color(0.82f, 0.82f, 0.85f);

        [MenuItem("Below the Wing/Rebuild apron scene and prefabs")]
        public static void Rebuild()
        {
            ContentBootstrap.CreateMissingContent();

            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");

            var tractorProfile = AssetDatabase.LoadAssetAtPath<VehicleProfile>(TractorProfilePath);
            var cartProfile = AssetDatabase.LoadAssetAtPath<VehicleProfile>(CartProfilePath);
            var aircraftProfile = AssetDatabase.LoadAssetAtPath<AircraftProfile>(AircraftProfilePath);
            var crewProfile = AssetDatabase.LoadAssetAtPath<CrewProfile>(CrewProfilePath);

            var tractor = BuildTractor(tractorProfile);
            var cart = BuildCart(cartProfile);
            var aircraft = BuildAircraft(aircraftProfile);
            var crew = BuildCrew(crewProfile);
            var bagPrefab = BuildBag();

            BuildScene(aircraftProfile, crewProfile, tractor, cart, aircraft, crew);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Apron scene and prefabs rebuilt.");
        }

        static GameObject BuildTractor(VehicleProfile profile)
        {
            var go = NewVehicle("BaggageTractor", profile, TractorModelPath, MeasureTheTractor);

            // Only something a player can sit in needs to say whether somebody is sitting in it, and
            // to refuse to change hands while they are.
            go.AddComponent<VehicleOccupant>();

            return SaveAndDiscard(go, $"{PrefabFolder}/BaggageTractor.prefab");
        }

        static GameObject BuildCart(VehicleProfile profile)
        {
            var go = NewVehicle("BaggageCart", profile, CartModelPath, MeasureTheCart);

            return SaveAndDiscard(go, $"{PrefabFolder}/BaggageCart.prefab");
        }

        /// <summary>
        /// What measuring a vehicle's model produces: where its parts are, and the visible wheels.
        ///
        /// The two come back together because their order has to match. The suspension hangs from
        /// the wheel positions in the shape, and the visible wheels are moved to agree with what
        /// each of those rays found, so the third wheel in one list must be the third in the other.
        /// Searched for twice, in two places, they agree only until somebody edits one of them.
        /// </summary>
        readonly struct MeasuredVehicle
        {
            public readonly VehicleShape.Measurements Shape;
            public readonly IReadOnlyList<Transform> Wheels;

            public MeasuredVehicle(VehicleShape.Measurements shape, IReadOnlyList<Transform> wheels)
            {
                Shape = shape;
                Wheels = wheels;
            }
        }

        /// <summary>
        /// Measures the baggage tractor model and fills in everything the game needs to know about
        /// its geometry.
        ///
        /// A tractor is solid through and through: nothing rides inside one, so the box it collides
        /// as is the box its bodywork fills. That box deliberately stops above the wheels -- the
        /// bodywork's underside is 0.15 m off the tarmac on this model -- because a solid part that
        /// reaches the ground carries the vehicle's weight itself, and then the suspension never
        /// compresses and what should be a tractor on wheels is a crate sliding about on the floor.
        ///
        /// Nothing tows a tractor, so it has no coupling at the front and says so. It is not a
        /// coupling at the origin: the origin is a point on the tarmac between the front wheels,
        /// and a train hitched there would drag the tractor along by its own axle.
        /// </summary>
        static MeasuredVehicle MeasureTheTractor(GameObject tractor, Transform model)
        {
            // Named by their path, because the front pair hang under the steering pivots they were
            // modelled on rather than off the root of the model. Transform.Find looks at direct
            // children only: asked for "Wheel_Front_Left" it answers null, and the tractor quietly
            // ends up with two wheels and nothing to say why.
            var measured = Wheels(tractor, model, new[]
            {
                "Steer_Left/Wheel_Front_Left",
                "Steer_Right/Wheel_Front_Right",
                "Wheel_Back_Left",
                "Wheel_Back_Right"
            });

            var bodywork = MeshBoxLocal(tractor, model, "Body");

            return new MeasuredVehicle(
                new VehicleShape.Measurements
                {
                    Wheels = measured.Placements,
                    FrontCouplingLocal = null,
                    RearCouplingLocal = MarkerLocal(tractor, model, "HITCH_Female"),
                    EnvelopeSizeMetres = bodywork.size,
                    EnvelopeCentreLocal = bodywork.center,
                    InteriorLocal = new Bounds(Vector3.zero, Vector3.zero),
                    SolidParts = new List<VehicleShape.SolidPart>
                    {
                        new VehicleShape.SolidPart("Body", bodywork.size, bodywork.center)
                    }
                },
                measured.Visible);
        }

        /// <summary>
        /// Measures the baggage cart model and fills in everything the game needs to know about its
        /// geometry.
        ///
        /// The wheels and the two couplings come from the model, so re-exporting the cart moves
        /// them without anybody editing code. The deck and envelope figures below are typed in, and
        /// that is worth being honest about: they were measured off the model by hand, and the mesh
        /// merges the deck into the rest of the bodywork so there is no node to read them from. If
        /// the cart is remodelled they have to be re-measured.
        ///
        /// Note the two kinds of hitch in the model. HITCH_Male and HITCH_Female are empties marking
        /// the exact points a coupling meets; Hitch, Hitch Pin and Hitch_Female are the drawbar
        /// meshes that sit near them. The names differ only by case, so a marker that turns out to
        /// be a mesh is refused -- taking one would put every coupling in the game tens of
        /// centimetres out.
        /// </summary>
        static MeasuredVehicle MeasureTheCart(GameObject cart, Transform model)
        {
            const float deckTopMetres = 0.4727f;
            const float deckWidthMetres = 1.7211f;
            const float deckLengthMetres = 3.1538f;
            const float slabThicknessMetres = 0.15f;
            const float clearInsideMetres = 1.626f;
            const float lipHeightMetres = 0.18f;
            const float lipThicknessMetres = 0.05f;

            // Bumper to bumper and axle to roof, which is wider and lower than the bodywork mesh
            // alone. The drawbar is left out of it: how far that reaches is the front coupling's
            // business, and counting it here would have carts laid out a drawbar's length apart.
            var envelopeSizeMetres = new Vector3(1.8855f, 2.0155f, 3.8152f);
            var envelopeCentreLocal = new Vector3(0f, 1.0909f, 0.1296f);

            var measured = Wheels(cart, model, new[] { "Wheel_1", "Wheel_2", "Wheel_3", "Wheel_4" });

            var roofUnderside = deckTopMetres + clearInsideMetres;

            var solid = new List<VehicleShape.SolidPart>
            {
                new VehicleShape.SolidPart(
                    "Deck",
                    new Vector3(deckWidthMetres, slabThicknessMetres, deckLengthMetres),
                    new Vector3(0f, deckTopMetres - (slabThicknessMetres * 0.5f), 0f)),

                new VehicleShape.SolidPart(
                    "Lip left",
                    new Vector3(lipThicknessMetres, lipHeightMetres, deckLengthMetres),
                    new Vector3(
                        -((deckWidthMetres * 0.5f) - (lipThicknessMetres * 0.5f)),
                        deckTopMetres + (lipHeightMetres * 0.5f),
                        0f)),

                new VehicleShape.SolidPart(
                    "Lip right",
                    new Vector3(lipThicknessMetres, lipHeightMetres, deckLengthMetres),
                    new Vector3(
                        (deckWidthMetres * 0.5f) - (lipThicknessMetres * 0.5f),
                        deckTopMetres + (lipHeightMetres * 0.5f),
                        0f)),

                new VehicleShape.SolidPart(
                    "End front",
                    new Vector3(deckWidthMetres, clearInsideMetres, slabThicknessMetres),
                    new Vector3(
                        0f,
                        (deckTopMetres + roofUnderside) * 0.5f,
                        (deckLengthMetres + slabThicknessMetres) * 0.5f)),

                new VehicleShape.SolidPart(
                    "End rear",
                    new Vector3(deckWidthMetres, clearInsideMetres, slabThicknessMetres),
                    new Vector3(
                        0f,
                        (deckTopMetres + roofUnderside) * 0.5f,
                        -(deckLengthMetres + slabThicknessMetres) * 0.5f)),

                new VehicleShape.SolidPart(
                    "Roof",
                    new Vector3(deckWidthMetres, slabThicknessMetres, deckLengthMetres),
                    new Vector3(0f, roofUnderside + (slabThicknessMetres * 0.5f), 0f))
            };

            return new MeasuredVehicle(
                new VehicleShape.Measurements
                {
                    Wheels = measured.Placements,
                    FrontCouplingLocal = MarkerLocal(cart, model, "HITCH_Male"),
                    RearCouplingLocal = MarkerLocal(cart, model, "HITCH_Female"),
                    EnvelopeSizeMetres = envelopeSizeMetres,
                    EnvelopeCentreLocal = envelopeCentreLocal,
                    InteriorLocal = new Bounds(
                        new Vector3(0f, deckTopMetres + (clearInsideMetres * 0.5f), 0f),
                        new Vector3(deckWidthMetres, clearInsideMetres, deckLengthMetres)),
                    SolidParts = solid
                },
                measured.Visible);
        }

        /// <summary>
        /// Measures a named set of wheels: where each one's centre is in the vehicle's own frame,
        /// how big it is, and the transform a player sees.
        ///
        /// A wheel's radius is half the largest span of its own mesh, so a wheel modelled along any
        /// axis measures the same. Read per wheel rather than once per vehicle because axles need
        /// not match: the tractor runs 0.22 m wheels at the front and 0.26 m at the back.
        /// </summary>
        static (List<VehicleShape.WheelPlacement> Placements, List<Transform> Visible) Wheels(
            GameObject vehicle, Transform model, IReadOnlyList<string> paths)
        {
            var placements = new List<VehicleShape.WheelPlacement>();
            var visible = new List<Transform>();

            foreach (var path in paths)
            {
                var wheel = PartOfTheModel(vehicle, model, path);
                var box = MeshBoxLocal(vehicle, wheel);
                var across = Mathf.Max(box.size.x, Mathf.Max(box.size.y, box.size.z));

                placements.Add(new VehicleShape.WheelPlacement(
                    vehicle.transform.InverseTransformPoint(wheel.position), across * 0.5f));

                visible.Add(wheel);
            }

            return (placements, visible);
        }

        /// <summary>
        /// Where a marker in the model sits, in the vehicle's own frame.
        ///
        /// A marker is an empty, and this refuses a mesh. Both models carry a marker and a mesh
        /// under near-identical names for the same piece of hardware -- the cart's HITCH_Female and
        /// Hitch_Female differ only by case, the tractor's HITCH_Female sits 8 cm above its
        /// Hitch_Pin -- and reading the mesh puts a coupling tens of centimetres out while still
        /// looking like a plausible measurement.
        /// </summary>
        static Vector3 MarkerLocal(GameObject vehicle, Transform model, string path)
        {
            var found = model.Find(path)
                        ?? throw Unmeasurable(vehicle, $"its model has no '{path}'");

            if (found.GetComponent<MeshFilter>() != null)
            {
                throw Unmeasurable(vehicle,
                    $"'{path}' is a mesh rather than a marker, and reading a coupling off the metal " +
                    "around it puts that coupling tens of centimetres out");
            }

            return vehicle.transform.InverseTransformPoint(found.position);
        }

        /// <summary>A part of the model that has to be a mesh.</summary>
        static Transform PartOfTheModel(GameObject vehicle, Transform model, string path)
        {
            var found = model.Find(path)
                        ?? throw Unmeasurable(vehicle,
                            $"its model has no '{path}'. Anything that is not a direct child of the " +
                            "model root needs its path: a plain name finds nothing at all");

            if (found.GetComponent<MeshFilter>() == null)
            {
                throw Unmeasurable(vehicle, $"'{path}' is a marker rather than a part of the model");
            }

            return found;
        }

        /// <summary>
        /// A vehicle that cannot be measured, which is not something to carry on past.
        ///
        /// Everything about a vehicle -- where its wheels are, how big they are, where it couples,
        /// how much room it takes up -- is measured off its model. A measurement that quietly
        /// returns nothing instead produces a prefab that is saved, shipped and perfectly
        /// self-consistent: a cart claiming a drawbar at its own origin parks every train a
        /// drawbar's length too close, and nothing downstream can tell.
        /// </summary>
        static InvalidOperationException Unmeasurable(GameObject vehicle, string why)
            => new InvalidOperationException(
                $"'{vehicle.name}' cannot be measured and so cannot be built: {why}.");

        /// <summary>The box a named mesh in the model fills, in the vehicle's own frame.</summary>
        static Bounds MeshBoxLocal(GameObject vehicle, Transform model, string path)
            => MeshBoxLocal(vehicle, PartOfTheModel(vehicle, model, path));

        /// <summary>
        /// The box a mesh fills, in the vehicle's own frame.
        ///
        /// Worked out from the mesh's own corners rather than from a renderer's world bounds, which
        /// are an axis-aligned box around whatever rotation the object happens to be at, and the
        /// model is turned half a turn on the way in.
        /// </summary>
        static Bounds MeshBoxLocal(GameObject vehicle, Transform part)
        {
            var mesh = part.GetComponent<MeshFilter>().sharedMesh;
            var least = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var most = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (var corner = 0; corner < 8; corner++)
            {
                var offset = Vector3.Scale(
                    mesh.bounds.extents,
                    new Vector3(
                        (corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f));

                var inVehicle = vehicle.transform.InverseTransformPoint(
                    part.TransformPoint(mesh.bounds.center + offset));

                least = Vector3.Min(least, inVehicle);
                most = Vector3.Max(most, inVehicle);
            }

            var box = new Bounds();
            box.SetMinMax(least, most);
            return box;
        }

        /// <summary>
        /// A vehicle: its body, its shape, and the model a player sees.
        ///
        /// No collider is added here. What a vehicle is solid where is described by its shape and
        /// built from it when the vehicle is configured, which is what lets a cart be a container
        /// rather than a solid block the size of a cart.
        /// </summary>
        static GameObject NewVehicle(
            string name, VehicleProfile profile, string modelPath, Func<GameObject, Transform, MeasuredVehicle> measure)
        {
            var go = new GameObject(name);
            go.AddComponent<Rigidbody>();

            var vehicle = go.AddComponent<VehicleController>();
            Set(vehicle, "m_Profile", profile);

            var measured = measure(go, AddModel(go, modelPath));

            var shape = go.AddComponent<VehicleShape>();
            shape.Describe(measured.Shape);

            // Handed the wheels the shape was measured from, in that order, so that the wheel a
            // player sees is the one whose suspension ray found the ground under it.
            go.AddComponent<WheelLook>().Watch(measured.Wheels);

            AddNetworking(go, outlivesItsOwner: true);
            go.AddComponent<TrainMember>();

            // Something to hold onto, and so something a rider goes round corners with.
            go.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            go.AddComponent<ApronAppearance>().DescribeAsModelled(
                shape.EnvelopeCentreLocal.y + (shape.EnvelopeSizeMetres.y * 0.6f));
            go.AddComponent<ApronIdentity>();

            return go;
        }

        /// <summary>
        /// Puts the model on a vehicle, facing the way the game drives.
        ///
        /// Both vehicles were modelled with their fronts along what Unity ends up calling backwards,
        /// so the whole model is turned half a turn. Doing it here, once, means nothing downstream
        /// has to know which way any particular artist happened to build something.
        ///
        /// A missing model is fatal rather than something to carry on past. Everything about a
        /// vehicle -- where its wheels are, how big they are, where it couples, how much room it
        /// takes up -- is measured off the model, so a prefab built without one is a vehicle with no
        /// suspension and no couplings that falls through the apron.
        /// </summary>
        static Transform AddModel(GameObject vehicle, string path)
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

            return model.transform;
        }

        static GameObject BuildAircraft(AircraftProfile profile)
        {
            var go = new GameObject("NarrowbodyAirliner");

            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            go.AddComponent<CapsuleCollider>();

            Set(go.AddComponent<AircraftBody>(), "m_Profile", profile);
            go.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            AddNetworking(go, outlivesItsOwner: true);

            Dress(go, ApronAppearance.Shape.LyingCapsule,
                new Vector3(profile.fuselageDiameterMetres, profile.lengthMetres, profile.fuselageDiameterMetres),
                FuselageWhite,
                profile.fuselageDiameterMetres);

            return SaveAndDiscard(go, $"{PrefabFolder}/NarrowbodyAirliner.prefab");
        }

        /// <summary>
        /// A piece of baggage: a box that can be carried, thrown, and stood on top of.
        ///
        /// Built here with everything else rather than by hand, so that the one description of what
        /// a bag is lives in one place and every bag in the game is that.
        /// </summary>
        static GameObject BuildBag()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BagProfile>(BagProfilePath);
            if (profile == null)
            {
                Debug.LogWarning($"No bag profile at {BagProfilePath}; skipping the bag prefab.");
                return null;
            }

            var go = new GameObject("Bag");
            go.AddComponent<Rigidbody>();
            go.AddComponent<BoxCollider>();

            Set(go.AddComponent<Bag>(), "m_Profile", profile);

            // What hands may do with it, said on the bag so that hands never have to know what a
            // bag is.
            go.AddComponent<HandUse>().As = HandUse.Category.Carry;

            AddNetworking(go, outlivesItsOwner: true);

            Dress(go, ApronAppearance.Shape.Box, profile.sizeMetres, BagCanvas, profile.sizeMetres.y * 1.2f);

            return SaveAndDiscard(go, $"{PrefabFolder}/Bag.prefab");
        }

        static GameObject BuildCrew(CrewProfile profile)
        {
            var go = new GameObject("RampWorker");
            go.AddComponent<Rigidbody>();
            go.AddComponent<CapsuleCollider>();

            Set(go.AddComponent<CrewCharacter>(), "m_Profile", profile);

            // Where the hands are: a little apart, a little below the shoulders, and far enough
            // in front that a bag pulled to one hangs clear of the body.
            var reach = profile.radiusMetres + 0.45f;
            HandAnchor(go, Hands.LeftAnchorName, new Vector3(-0.3f, 0.2f, reach));
            HandAnchor(go, Hands.RightAnchorName, new Vector3(0.3f, 0.2f, reach));

            AddNetworking(go, outlivesItsOwner: false);

            Dress(go, ApronAppearance.Shape.UprightCapsule,
                new Vector3(profile.radiusMetres * 2f, profile.heightMetres, profile.radiusMetres * 2f),
                HiVisYellow,
                profile.heightMetres * 0.7f);

            return SaveAndDiscard(go, $"{PrefabFolder}/RampWorker.prefab");
        }

        static void HandAnchor(GameObject character, string name, Vector3 local)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(character.transform, worldPositionStays: false);
            anchor.transform.localPosition = local;
        }

        /// <summary>
        /// Gives an object its stand-in shape and the name-carrying component that shows it.
        ///
        /// The name travels over the network, and appearance is built when it arrives, so a player
        /// who joins sees the apron rather than an empty grey plane full of invisible colliders.
        /// </summary>
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

        /// <param name="outlivesItsOwner">
        /// Whether this should survive the departure of whoever owns it.
        ///
        /// True for equipment: a player quitting must not take a tractor and four carts off the
        /// apron with them. Netcode then hands their objects to remaining clients one at a time,
        /// which can leave a train split across machines, so the session owner reclaims each train
        /// whole afterwards.
        ///
        /// False for a person: a player who leaves takes their character with them, and one left
        /// behind would be a body nobody is driving standing in the way for the rest of the session.
        /// </param>
        static void AddNetworking(GameObject go, bool outlivesItsOwner)
        {
            var networked = go.AddComponent<NetworkObject>();
            networked.DontDestroyWithOwner = outlivesItsOwner;

            // Movement is not replicated by a transform component at all. Both of the ones netcode
            // offers write a position onto the copy -- and anything whose position is written
            // arrives somewhere without having travelled, so the impulse a collision should have
            // exchanged never happens and the crash comes out different on each screen.
            // NetworkRigidbody goes further and makes every non-owning copy kinematic, which is
            // infinite mass: you would drive into somebody else's tractor and bounce off a wall
            // while on their screen the mirror image happened.
            //
            // Everything that can be crashed into reports what it is doing and is steered toward
            // that with force instead. That includes people: players run each other over on purpose.
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
                // Bags have the same problem and one more besides: which machine simulates a bag
                // changes with who picks it up and whose cart it lands in, and no transform
                // component has anything to say about that.
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

        static void BuildScene(
            AircraftProfile aircraftProfile,
            CrewProfile crewProfile,
            GameObject tractor,
            GameObject cart,
            GameObject aircraft,
            GameObject crew)
        {
            // Replacing the open scene throws away anything unsaved in it, so ask first. Somebody
            // running this from the menu has other work open more often than not.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Apron rebuild cancelled.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildApronFloor();
            BuildLighting();

            var camera = BuildCamera();
            var manager = BuildNetworkManager();
            var gateway = manager.gameObject.AddComponent<SessionGateway>();
            var broker = manager.gameObject.AddComponent<NetworkOwnershipBroker>();

            Set(new GameObject("Session Entry Screen").AddComponent<SessionEntryScreen>(), "m_Gateway", gateway);
            var readout = new GameObject("Ramp Readout").AddComponent<RampReadout>();

            var sessionObject = new GameObject("Ramp Session");
            sessionObject.AddComponent<NetworkObject>();
            var session = sessionObject.AddComponent<RampSession>();

            Set(session, "m_AircraftProfile", aircraftProfile);
            Set(session, "m_CrewProfile", crewProfile);
            Set(session, "m_TractorPrefab", tractor.GetComponent<NetworkObject>());
            Set(session, "m_CartPrefab", cart.GetComponent<NetworkObject>());
            Set(session, "m_AircraftPrefab", aircraft.GetComponent<NetworkObject>());
            Set(session, "m_CrewPrefab", crew.GetComponent<NetworkObject>());

            var bag = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Bag.prefab");
            if (bag != null)
            {
                Set(session, "m_BagPrefab", bag.GetComponent<NetworkObject>());
            }
            Set(session, "m_Broker", broker);
            Set(session, "m_Camera", camera);
            Set(session, "m_Readout", readout);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
        }

        static void BuildApronFloor()
        {
            AssetDatabase.DeleteAsset(ApronMaterialPath);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Apron";

            // Unity's plane primitive is ten metres across per unit of scale, so this is 400 metres
            // square -- room for a train to be got badly wrong in.
            floor.transform.localScale = new Vector3(40f, 1f, 40f);

            // A material of its own. Writing a colour onto sharedMaterial would repaint the render
            // pipeline's default material, and with it every other object in the project using it.
            var concrete = new Material(floor.GetComponent<MeshRenderer>().sharedMaterial)
            {
                name = "Apron concrete",
                color = new Color(0.32f, 0.33f, 0.34f)
            };

            AssetDatabase.CreateAsset(concrete, ApronMaterialPath);
            floor.GetComponent<MeshRenderer>().sharedMaterial = concrete;
        }

        static void BuildLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, 30f, 0f);
        }

        static FollowCamera BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            return go.AddComponent<FollowCamera>();
        }

        static NetworkManager BuildNetworkManager()
        {
            var go = new GameObject("Network Manager");
            var manager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();

            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,

                // Every client simulates what it owns and sees replicated copies of the rest. One
                // client is nominated session owner to look after state that belongs to nobody.
                NetworkTopology = NetworkTopologyTypes.DistributedAuthority,

                // Players are given a character by the session rather than receiving one
                // automatically, because a character needs wiring to a camera and a broker that a
                // default player prefab knows nothing about.
                PlayerPrefab = null,
                TickRate = 20
            };

            // Registered as a prefab *list* asset, not by adding prefabs one at a time.
            //
            // NetworkConfig.Prefabs keeps its prefabs in a field marked NonSerialized, so anything
            // added here is thrown away the moment the scene is saved -- and the failure is silent
            // and total: the session owner spawns the apron, and every other machine reports that
            // the prefab could not be found and creates nothing. A player joining sees bare ground.
            //
            // The list asset is maintained by Netcode's own prefab processor as prefabs are added to
            // the project, so this stays correct without being edited again.
            var known = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(DefaultPrefabListPath);
            if (known == null)
            {
                Debug.LogError(
                    $"No prefab list at {DefaultPrefabListPath}. Without it nothing spawned by the " +
                    "session owner can be created on any other machine.");
            }
            else
            {
                manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(known);
            }

            return manager;
        }

        static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath))
            {
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>
        /// Assigns a private serialized field on a component.
        ///
        /// The fields these set are deliberately private: nothing at runtime should be reaching into
        /// a session to swap its prefabs. They are inspector wiring, and this is the editor doing
        /// the wiring, so it goes through the same serialization the inspector uses.
        /// </summary>
        static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogError($"{target.GetType().Name} has no serialized field called '{field}'.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
