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
    /// a cart: not a value it is told after it exists, but the thing it was made from. The stand-in
    /// shape is filled in here from the same profile, so what a vehicle looks like and what it
    /// collides as cannot drift apart.
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

        static readonly Color TractorBlue = new Color(0.35f, 0.55f, 0.75f);
        static readonly Color CartGrey = new Color(0.55f, 0.55f, 0.58f);
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

            BuildScene(tractorProfile, cartProfile, aircraftProfile, crewProfile, tractor, cart, aircraft, crew);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Apron scene and prefabs rebuilt.");
        }

        static GameObject BuildTractor(VehicleProfile profile)
        {
            var go = NewVehicle("BaggageTractor", profile, TractorBlue);

            // Only something a player can sit in needs to say whether somebody is sitting in it, and
            // to refuse to change hands while they are.
            go.AddComponent<VehicleOccupant>();

            return SaveAndDiscard(go, $"{PrefabFolder}/BaggageTractor.prefab");
        }

        static GameObject BuildCart(VehicleProfile profile)
        {
            var go = NewVehicle("BaggageCart", profile, CartGrey, CartModelPath);

            return SaveAndDiscard(go, $"{PrefabFolder}/BaggageCart.prefab");
        }

        /// <summary>
        /// Measures the baggage cart model and fills in everything the game needs to know about its
        /// geometry.
        ///
        /// Read off the model rather than typed in here, because a number typed in here is a second
        /// copy of a measurement and a second copy eventually disagrees with the first. The one
        /// thing that is decided rather than measured is which way round the model goes: it was
        /// exported with its drawbar along what Unity calls backwards, so it is turned to face the
        /// way the game drives.
        ///
        /// Note the two kinds of hitch in the model. HITCH_Male and HITCH_Female are empties marking
        /// the exact points a coupling meets; Hitch, Hitch Pin and Hitch_Female are the drawbar
        /// meshes that sit near them. The names differ only by case, so these are looked up exactly
        /// and checked for being what they claim to be -- taking the mesh instead would put every
        /// coupling in the game tens of centimetres out.
        /// </summary>
        static void ShapeFromTheCartModel(GameObject cart, Transform model)
        {
            const float deckTopMetres = 0.4727f;
            const float deckWidthMetres = 1.7211f;
            const float deckLengthMetres = 3.1538f;
            const float slabThicknessMetres = 0.15f;
            const float clearInsideMetres = 1.626f;
            const float lipHeightMetres = 0.18f;
            const float lipThicknessMetres = 0.05f;

            Vector3 Local(string landmark)
            {
                var found = model.Find(landmark);
                if (found == null)
                {
                    Debug.LogError($"The cart model has no '{landmark}'.");
                    return Vector3.zero;
                }

                if (found.GetComponent<MeshFilter>() != null)
                {
                    Debug.LogError(
                        $"'{landmark}' is a mesh rather than a marker. The cart model has both, and " +
                        "their names differ only by case.");
                }

                return cart.transform.InverseTransformPoint(found.position);
            }

            var wheels = new List<Vector3>();
            for (var i = 1; i <= 4; i++)
            {
                var wheel = model.Find($"Wheel_{i}");
                if (wheel == null)
                {
                    Debug.LogError($"The cart model has no 'Wheel_{i}'.");
                    continue;
                }

                wheels.Add(cart.transform.InverseTransformPoint(wheel.position));
            }

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

            cart.AddComponent<VehicleShape>().Describe(new VehicleShape.Measurements
            {
                WheelCentresLocal = wheels,
                FrontCouplingLocal = Local("HITCH_Male"),
                RearCouplingLocal = Local("HITCH_Female"),
                EnvelopeSizeMetres = new Vector3(1.8855f, 2.0155f, 3.8152f),
                EnvelopeCentreLocal = new Vector3(0f, 1.0909f, 0.1296f),
                InteriorLocal = new Bounds(
                    new Vector3(0f, deckTopMetres + (clearInsideMetres * 0.5f), 0f),
                    new Vector3(deckWidthMetres, clearInsideMetres, deckLengthMetres)),
                SolidParts = solid
            });

            AddCarriers(cart, deckTopMetres, deckWidthMetres, deckLengthMetres, clearInsideMetres);
            WatchTheWheels(cart, model);
        }

        /// <summary>
        /// The two places things ride on a cart: inside it, and on top of it.
        ///
        /// Inside is where bags go and where somebody crouching can stand. On top is out of jumping
        /// reach from the tarmac, so getting up there means climbing from the drawbar or from
        /// another cart -- which is the point of it being worth doing.
        /// </summary>
        static void AddCarriers(
            GameObject cart, float deckTop, float deckWidth, float deckLength, float clearInside)
        {
            var inside = new GameObject("Deck");
            inside.transform.SetParent(cart.transform, worldPositionStays: false);
            inside.transform.localPosition = new Vector3(0f, deckTop + (clearInside * 0.5f), 0f);
            inside.AddComponent<Carrier>()
                .Covers(Vector3.zero, new Vector3(deckWidth, clearInside, deckLength));
            inside.AddComponent<CarrierWatch>();

            var onTop = new GameObject("Roof");
            onTop.transform.SetParent(cart.transform, worldPositionStays: false);
            onTop.transform.localPosition = new Vector3(0f, deckTop + clearInside + 0.15f, 0f);
            onTop.AddComponent<Carrier>()
                .Covers(new Vector3(0f, 1f, 0f), new Vector3(deckWidth, 2f, deckLength));
            onTop.AddComponent<CarrierWatch>();

            // One machine decides what comes off, for both of them. Judged on a copy, the nudges
            // that keep the copy in step read as sideways acceleration the cart never felt, and bags
            // leap off decks on every screen except the one where the cart is really being driven.
            cart.AddComponent<CarrierAuthority>();
        }

        /// <summary>Hands the visible wheels to whatever turns them and keeps them on the ground.</summary>
        static void WatchTheWheels(GameObject cart, Transform model)
        {
            var wheels = new List<Transform>();
            for (var i = 1; i <= 4; i++)
            {
                var wheel = model.Find($"Wheel_{i}");
                if (wheel != null)
                {
                    wheels.Add(wheel);
                }
            }

            cart.AddComponent<WheelLook>().Watch(wheels);
        }

        /// <summary>
        /// A vehicle: its body, its shape, and something to look at.
        ///
        /// No collider is added here. What a vehicle is solid where is described by its shape and
        /// built from it when the vehicle is configured, which is what lets a cart be a container
        /// rather than a solid block the size of a cart.
        /// </summary>
        static GameObject NewVehicle(string name, VehicleProfile profile, Color colour, string modelPath = null)
        {
            var go = new GameObject(name);
            go.AddComponent<Rigidbody>();

            var vehicle = go.AddComponent<VehicleController>();
            Set(vehicle, "m_Profile", profile);

            var model = modelPath != null ? AddModel(go, modelPath) : null;
            if (model != null)
            {
                ShapeFromTheCartModel(go, model);
            }
            else
            {
                ShapeFromNumbers(go, profile);
            }

            AddNetworking(go, outlivesItsOwner: true);
            go.AddComponent<TrainMember>();

            var shape = go.GetComponent<VehicleShape>();
            Dress(go,
                model != null ? ApronAppearance.Shape.AlreadyModelled : ApronAppearance.Shape.Box,
                profile.bodySizeMetres,
                colour,
                shape.EnvelopeCentreLocal.y + (shape.EnvelopeSizeMetres.y * 0.6f),
                shape.EnvelopeCentreLocal);

            return go;
        }

        /// <summary>
        /// Puts the model on a vehicle, facing the way the game drives.
        ///
        /// The cart was modelled with its drawbar along what Unity ends up calling backwards, so the
        /// whole model is turned half a turn. Doing it here, once, means nothing downstream has to
        /// know which way any particular artist happened to build something.
        /// </summary>
        static Transform AddModel(GameObject vehicle, string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning($"No model at {path}; leaving {vehicle.name} as a grey box.");
                return null;
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            model.name = ApronAppearance.LookName;
            model.transform.SetParent(vehicle.transform, worldPositionStays: false);
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.localRotation;

            return model.transform;
        }

        /// <summary>
        /// Describes a vehicle that has no model, from the numbers in its profile.
        ///
        /// Exactly the same terms a measured vehicle is described in, so nothing downstream can tell
        /// which kind it is dealing with. A box on four wheels with a coupling at each end, its
        /// origin on the ground between them.
        /// </summary>
        static void ShapeFromNumbers(GameObject vehicle, VehicleProfile profile)
        {
            var size = profile.bodySizeMetres;
            var halfWheelbase = size.z * 0.35f;
            var halfTrack = size.x * 0.42f;
            var reach = (size.z * 0.5f) + 0.3f;
            var couplingHeight = profile.wheelRadiusMetres + 0.05f;

            // Clear of the tarmac by a wheel's radius, because the origin is on the ground now and a
            // body box sitting on that origin has its underside level with the apron. It then
            // carries the vehicle's weight itself, the suspension never compresses, and what should
            // be a tractor on wheels is a crate sliding about on the floor -- which steers nowhere.
            var underside = profile.wheelRadiusMetres;
            var middle = new Vector3(0f, underside + (size.y * 0.5f), 0f);

            vehicle.AddComponent<VehicleShape>().Describe(new VehicleShape.Measurements
            {
                WheelCentresLocal = new List<Vector3>
                {
                    new Vector3(-halfTrack, profile.wheelRadiusMetres, halfWheelbase),
                    new Vector3(halfTrack, profile.wheelRadiusMetres, halfWheelbase),
                    new Vector3(-halfTrack, profile.wheelRadiusMetres, -halfWheelbase),
                    new Vector3(halfTrack, profile.wheelRadiusMetres, -halfWheelbase)
                },
                FrontCouplingLocal = new Vector3(0f, couplingHeight, reach),
                RearCouplingLocal = new Vector3(0f, couplingHeight, -reach),
                EnvelopeSizeMetres = size,
                EnvelopeCentreLocal = middle,
                InteriorLocal = new Bounds(Vector3.zero, Vector3.zero),
                SolidParts = new List<VehicleShape.SolidPart>
                {
                    new VehicleShape.SolidPart("Body", size, middle)
                }
            });
        }

        static GameObject BuildAircraft(AircraftProfile profile)
        {
            var go = new GameObject("NarrowbodyAirliner");

            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            go.AddComponent<CapsuleCollider>();

            Set(go.AddComponent<AircraftBody>(), "m_Profile", profile);

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
            go.AddComponent<Carried>();
            go.AddComponent<SettlesOntoCarriers>();

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

            // A person is something that can be carried -- by a cart deck now, by a belt or a pit
            // later -- and something that can carry, because hands are a carrier like any other.
            go.AddComponent<Carried>();

            var hands = new GameObject("Hands");
            hands.transform.SetParent(go.transform, worldPositionStays: false);
            hands.transform.localPosition = new Vector3(0f, 0f, profile.radiusMetres + 0.25f);
            hands.AddComponent<Carrier>().Covers(Vector3.zero, new Vector3(0.8f, 0.8f, 0.8f), holdsAtItsCentre: true);

            AddNetworking(go, outlivesItsOwner: false);

            Dress(go, ApronAppearance.Shape.UprightCapsule,
                new Vector3(profile.radiusMetres * 2f, profile.heightMetres, profile.radiusMetres * 2f),
                HiVisYellow,
                profile.heightMetres * 0.7f);

            return SaveAndDiscard(go, $"{PrefabFolder}/RampWorker.prefab");
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
            else if (go.GetComponent<Carried>() != null)
            {
                // Cargo has the same problem and one more besides: which cart a bag is riding on has
                // to agree everywhere, and no transform component has anything to say about that.
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
            Object.DestroyImmediate(go);
            return saved;
        }

        static void BuildScene(
            VehicleProfile tractorProfile,
            VehicleProfile cartProfile,
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
        static void Set(Object target, string field, Object value)
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
