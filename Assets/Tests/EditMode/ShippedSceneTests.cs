using System.Collections.Generic;
using BelowTheWing.Apron;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Session;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class ShippedSceneTests
    {
        const string ScenePath = ShippedContent.ScenePath;
        const string PrefabFolder = ShippedContent.PrefabFolder;

        Scene m_Apron;
        Scene m_Menu;

        [SetUp]
        public void OpenBothScenes()
        {
            m_Menu = EditorSceneManager.OpenScene(ShippedContent.MenuScenePath, OpenSceneMode.Additive);
            m_Apron = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void CloseBothScenes()
        {
            EditorSceneManager.CloseScene(m_Apron, removeScene: true);
            EditorSceneManager.CloseScene(m_Menu, removeScene: true);
        }

        static T Find<T>() where T : Component
        {
            var found = Object.FindAnyObjectByType<T>();
            Assert.That(found, Is.Not.Null,
                $"neither shipped scene has a {typeof(T).Name}. The menu scene carries the netcode " +
                "and the apron carries the game, and the NetworkManager crosses between them by " +
                "marking itself DontDestroyOnLoad");
            return found;
        }

        static GameObject Prefab(string name) => ShippedContent.PrefabNamed(name);

        List<T> InTheApron<T>() where T : Component
        {
            var here = new List<T>();

            foreach (var found in Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (found.gameObject.scene == m_Apron)
                {
                    here.Add(found);
                }
            }

            return here;
        }

        [Test]
        public void TheNetworkManagerKnowsWhichPrefabsCanBeSpawned()
        {
            var lists = Find<NetworkManager>().NetworkConfig.Prefabs.NetworkPrefabsLists;

            Assert.That(lists, Is.Not.Empty,
                "with no prefab list, the machine that builds the apron spawns eleven objects and " +
                "every other machine reports that the prefab could not be found and creates nothing. " +
                "A player joining sees bare ground. Note that adding prefabs one at a time to " +
                "NetworkConfig.Prefabs does not survive saving the scene: that list is not serialized");

            var known = 0;
            foreach (var list in lists)
            {
                Assert.That(list, Is.Not.Null, "a null entry in the prefab list");
                known += list.PrefabList.Count;
            }

            Assert.That(known, Is.GreaterThanOrEqualTo(5),
                "the apron needs a tractor, a cart, an aircraft, a ramp worker and a bag");

            var bag = Prefab("Bag").GetComponent<NetworkObject>();
            var registered = false;
            foreach (var list in lists)
            {
                foreach (var entry in list.PrefabList)
                {
                    registered |= entry.Prefab != null
                                  && entry.Prefab.GetComponent<NetworkObject>() == bag;
                }
            }

            Assert.That(registered, Is.True,
                "the session spawns bags, so every other machine has to know how to make one. " +
                "Unregistered, the machine that builds the apron gets its bags and everybody else " +
                "is told the prefab could not be found and creates nothing");
        }

        [Test]
        public void TheSessionIsWiredToEverythingItSpawnsAndDrives()
        {
            var session = new SerializedObject(Find<RampSession>());

            Assert.That(session.FindProperty("m_Broker").objectReferenceValue, Is.Null,
                "the broker lives with the NetworkManager in the menu scene, so the session cannot " +
                "hold a reference to it and finds it at runtime instead");

            foreach (var field in new[]
                     {
                         "m_AircraftProfile", "m_CrewProfile",
                         "m_TractorPrefab", "m_CartPrefab", "m_AircraftPrefab", "m_CrewPrefab", "m_BagPrefab",
                         "m_Camera", "m_Readout"
                     })
            {
                var property = session.FindProperty(field);
                Assert.That(property, Is.Not.Null, $"{nameof(RampSession)} has no field called '{field}'");
                Assert.That(property.objectReferenceValue, Is.Not.Null,
                    $"'{field}' is not assigned in the scene. Nothing checks this at runtime, so the " +
                    "failure is a session that starts and then quietly does half its job");
            }
        }

        [Test]
        public void TheSceneRunsDistributedAuthorityAndNotClientServer()
        {
            Assert.That(Find<NetworkManager>().NetworkConfig.NetworkTopology,
                Is.EqualTo(NetworkTopologyTypes.DistributedAuthority),
                "every ownership rule in this game assumes no machine is a server. Under client-server " +
                "the disconnect handling listens for an event that never arrives and ownership requests " +
                "are answered by a machine that should not be answering them");
        }

        [Test]
        public void TheApronItselfHoldsNoEquipment()
        {
            Assert.That(InTheApron<VehicleController>(), Is.Empty,
                "vehicles are placed at runtime from one description of the layout. A vehicle sitting " +
                "in the apron scene as well is a second description that nothing keeps in agreement. " +
                "The menu scene's backdrop is not this: it is scenery, nothing on it simulates, and " +
                "it is gone by the time a shift starts");
            Assert.That(InTheApron<CrewCharacter>(), Is.Empty,
                "a character in the apron scene belongs to nobody and is simulated by everybody");
        }

        [Test]
        public void EveryCopyOnAnotherMachineIsSteeredWithFiguresThatCanActuallyCloseAGap()
        {
            foreach (var name in new[] { "BaggageTractor", "BaggageCart", "Bag", "RampWorker" })
            {
                var mover = Prefab(name).GetComponent<MotionReplication>();
                Assert.That(mover, Is.Not.Null,
                    $"{name}: nothing reports where this is or steers other machines' copies toward it");

                var saved = new SerializedObject(mover).FindProperty("m_Correction");

                float Dial(string called)
                {
                    var found = saved.FindPropertyRelative(called);
                    Assert.That(found, Is.Not.Null, $"{name} has no '{called}' to steer a copy with");
                    return found.floatValue;
                }

                Assert.That(Dial("closingRatePerSecond"), Is.GreaterThan(0f),
                    $"{name} turns a position error into no closing speed at all, so a copy matches " +
                    "its owner's velocity and keeps whatever gap it drifted into for the session");

                Assert.That(Dial("closingCeilingMetresPerSecond"), Is.GreaterThan(0f),
                    $"{name} may close a gap at zero metres a second, which is the same thing said " +
                    "twice: the copy tracks the speed and never the place");

                Assert.That(Dial("authority"), Is.GreaterThan(0f).And.LessThanOrEqualTo(1f),
                    $"{name} applies {Dial("authority")} of each wanted change per step. At zero " +
                    "nothing is applied; above one a correction overshoots and comes back, which is " +
                    "a copy oscillating around where its owner says it is");

                Assert.That(Dial("leaveAloneBelow"), Is.LessThan(Dial("closingCeilingMetresPerSecond")),
                    $"{name} skips every correction smaller than its own ceiling, so no correction " +
                    "is ever applied at all");
            }
        }

        [Test]
        public void EveryPrefabCarriesWhatTheCodeReachesForOnIt()
        {
            foreach (var name in new[] { "BaggageTractor", "BaggageCart" })
            {
                var vehicle = Prefab(name);
                Assert.That(vehicle.GetComponent<VehicleController>(), Is.Not.Null, $"{name}: no controller");
                Assert.That(vehicle.GetComponent<TrainMember>(), Is.Not.Null,
                    $"{name}: without this it belongs to no train, so taking it leaves its carts behind");
                Assert.That(vehicle.GetComponent<ApronIdentity>(), Is.Not.Null,
                    $"{name}: names arrive through this, and appearance is built when a name arrives");
                Assert.That(vehicle.GetComponent<ApronAppearance>(), Is.Not.Null, $"{name}: nothing to look at");
                Assert.That(vehicle.GetComponent<NetworkObject>().DontDestroyWithOwner, Is.True,
                    $"{name}: a player quitting would take this off the apron with them");

                Assert.That(vehicle.GetComponent<VehicleMotion>(), Is.Not.Null,
                    $"{name}: nothing reports where this is or steers other machines' copies toward it, " +
                    "so every copy drifts off on its own physics and never comes back");
                Assert.That(vehicle.GetComponent<AnticipatedNetworkTransform>(), Is.Null,
                    $"{name}: a transform component writes a position onto the copy every update, which " +
                    "fights the correction forces and cancels the impulse a collision should have left. " +
                    "One of the two has to own where a vehicle goes, and it is not this");
            }

            var cart = Prefab("BaggageCart");

            var cartShape = cart.GetComponent<VehicleShape>();
            Assert.That(cartShape, Is.Not.Null,
                "BaggageCart: nothing says where its wheels and couplings are, so its suspension has " +
                "nowhere to hang from and it falls through the apron");
            Assert.That(cartShape.Wheels.Count, Is.EqualTo(4),
                "BaggageCart: four wheels, read off the model");

            foreach (var wheel in cartShape.Wheels)
            {
                Assert.That(wheel.RadiusMetres, Is.EqualTo(0.157f).Within(0.005f),
                    "BaggageCart: a wheel is measured off its own mesh, so that the invisible " +
                    "wheels holding the cart up are the size of the visible ones turning on it. A " +
                    "radius written down anywhere else is a second copy of this figure that " +
                    "nothing keeps in agreement with the model");
            }
            Assert.That(cartShape.FrontReachMetres, Is.EqualTo(3.1617f).Within(0.02f),
                "BaggageCart: the front coupling is the HITCH_Male empty. Hitch, Hitch Pin and " +
                "Hitch_Female are drawbar meshes sitting near it, and their names differ from the " +
                "markers only by case -- picking one of those puts every coupling tens of " +
                "centimetres out");
            Assert.That(cartShape.RearReachMetres, Is.EqualTo(1.8159f).Within(0.02f),
                "BaggageCart: the rear coupling is the HITCH_Female empty");
            Assert.That(cartShape.InteriorLocal.size.y, Is.GreaterThan(1f),
                "BaggageCart: with no interior there is nowhere for a bag to be inside the cart");

            Assert.That(cart.transform.Find(ApronAppearance.LookName), Is.Not.Null,
                "BaggageCart: nothing to look at. The model is what a player sees, and a cart " +
                "drawn as nothing is a cart that appears to be a floating label");
            Assert.That(cart.GetComponentInChildren<WheelLook>(), Is.Not.Null,
                "BaggageCart: with nothing driving the visible wheels they neither turn nor stay on " +
                "the tarmac, and the body sinks onto its springs leaving them buried");

            Assert.That(cart.GetComponent<HandUse>()?.As, Is.EqualTo(HandUse.Category.HoldOnto),
                "BaggageCart: a cart that says nothing about what hands may do with it cannot be " +
                "held onto, and a rider who cannot hold on walks off it at the first corner");

            var bag = Prefab("Bag");
            Assert.That(bag.GetComponent<CargoMotion>(), Is.Not.Null,
                "Bag: nothing tells the other machines where this bag is, and nothing moves it to " +
                "the machine whose cart it lands in");
            Assert.That(bag.GetComponent<HandUse>()?.As, Is.EqualTo(HandUse.Category.Carry),
                "Bag: a bag that says nothing about what hands may do with it cannot be picked up");

            Assert.That(Prefab("BaggageTractor").GetComponent<HandUse>()?.As, Is.EqualTo(HandUse.Category.HoldOnto),
                "BaggageTractor: nothing to hold onto");

            var worker = Prefab("RampWorker");
            Assert.That(worker.transform.Find(Hands.LeftAnchorName), Is.Not.Null,
                "RampWorker: no left hand, so nothing can be held in it");
            Assert.That(worker.transform.Find(Hands.RightAnchorName), Is.Not.Null,
                "RampWorker: no right hand, so nothing can be held in it");
            Assert.That(bag.GetComponent<AnticipatedNetworkTransform>(), Is.Null,
                "Bag: a transform component writes a position onto the copy every update, so a " +
                "copy cannot take part in a collision, and it has nothing to say about which " +
                "machine should be simulating the bag");

            var tractor = Prefab("BaggageTractor");
            var tractorShape = tractor.GetComponent<VehicleShape>();
            Assert.That(tractorShape, Is.Not.Null,
                "BaggageTractor: nothing says where its wheels and couplings are, so its suspension " +
                "has nowhere to hang from and it falls through the apron");

            Assert.That(tractor.transform.Find(ApronAppearance.LookName), Is.Not.Null,
                "BaggageTractor: nothing to look at");
            Assert.That(tractor.GetComponentInChildren<WheelLook>(), Is.Not.Null,
                "BaggageTractor: with nothing driving the visible wheels they neither turn nor stay " +
                "on the tarmac");

            Assert.That(tractorShape.Wheels.Count, Is.EqualTo(4),
                "BaggageTractor: four wheels, read off the model. The front pair hang under the " +
                "steering pivots they were modelled on rather than off the model root, and a search " +
                "of direct children finds neither of them -- silently, leaving a tractor on two");

            var front = 0;
            var rear = 0;
            foreach (var wheel in tractorShape.Wheels)
            {
                if (wheel.CentreLocal.z > 0f)
                {
                    front++;
                    Assert.That(wheel.RadiusMetres, Is.EqualTo(0.2203f).Within(0.005f),
                        "BaggageTractor: the front wheels measure 0.2203 m off the model");
                }
                else
                {
                    rear++;
                    Assert.That(wheel.RadiusMetres, Is.EqualTo(0.2647f).Within(0.005f),
                        "BaggageTractor: the rear wheels measure 0.2647 m off the model");
                }

                Assert.That(wheel.CentreLocal.y, Is.EqualTo(wheel.RadiusMetres).Within(0.01f),
                    "BaggageTractor: the origin is on the tarmac, so a wheel's centre is its own " +
                    "radius above it");
            }

            Assert.That(front, Is.EqualTo(2), "BaggageTractor: two wheels ahead of the origin");
            Assert.That(rear, Is.EqualTo(2), "BaggageTractor: two behind it");

            Assert.That(tractorShape.HasFrontCoupling, Is.False,
                "BaggageTractor: nothing tows a tractor, and its model has no coupling at the front. " +
                "Recorded as (0,0,0) instead it is a hitch on the tarmac between its front wheels");
            Assert.That(tractorShape.RearReachMetres, Is.EqualTo(1.3692f).Within(0.02f),
                "BaggageTractor: the rear coupling is the HITCH_Female empty. Hitch_Pin is the mesh " +
                "sitting 8 cm below it");
            Assert.That(tractorShape.RearCouplingLocal.Value.y, Is.EqualTo(0.2075f).Within(0.01f),
                "BaggageTractor: a tractor's socket and a cart's drawbar meet at the same height, so " +
                "a coupled pair stands level");

            Assert.That(tractorShape.InteriorLocal.size, Is.EqualTo(Vector3.zero),
                "BaggageTractor: nothing rides inside a tractor, and an interior nobody can reach is " +
                "a hole in its side waiting to be found");
            Assert.That(tractorShape.SolidParts.Count, Is.GreaterThan(1),
                "BaggageTractor: a tractor is solid in the pieces it was modelled from, not in one " +
                "box around all of them. One box is a crate as tall as the machine: nothing to " +
                "stand on, nowhere to walk between the axles, and open sides that are a wall");

            var lowest = float.MaxValue;
            var everythingSolid = new Bounds(
                VehicleShape.TheRoomAPartTakesUp(tractorShape.SolidParts[0]).center, Vector3.zero);

            foreach (var part in tractorShape.SolidParts)
            {
                var room = VehicleShape.TheRoomAPartTakesUp(part);
                everythingSolid.Encapsulate(room);
                lowest = Mathf.Min(lowest, room.min.y);
            }

            Assert.That(lowest, Is.GreaterThan(0.1f),
                $"BaggageTractor: something solid reaches {lowest:F2} m, which is the tarmac. Resting " +
                "on the ground it carries the tractor's own weight, the suspension never compresses, " +
                "and what should be a tractor on wheels is a crate sliding about on the floor");

            Assert.That(everythingSolid.size.y, Is.LessThanOrEqualTo(tractorShape.EnvelopeSizeMetres.y + 0.01f),
                "BaggageTractor: what it is solid in has to fit inside the room it takes up, because " +
                "the apron is laid out from that room and a player steps clear of it when they get " +
                "off. Solid outside it and vehicles collide before they look like they have");

            var headlights = tractor.transform.Find($"{ApronAppearance.LookName}/Headlights");
            Assert.That(headlights, Is.Not.Null,
                "BaggageTractor: the model has no Headlights, so there is nothing here that can tell " +
                "which way round it ended up");
            Assert.That(tractor.transform.InverseTransformPoint(headlights.position).z, Is.GreaterThan(0f),
                "BaggageTractor: its headlights are behind it. Both models are built facing what " +
                "Unity calls backwards and are turned half a turn on the way in; a model exported " +
                "the other way round drives in reverse, steers with its driven axle and tows from " +
                "its nose, and every measurement taken off it is still perfectly self-consistent");

            Assert.That(tractor.GetComponent<VehicleOccupant>(), Is.Not.Null,
                "only the tractor can be sat in, and without this nothing refuses a request for one " +
                "somebody is already driving");

            var crew = Prefab("RampWorker");
            Assert.That(crew.GetComponent<CrewCharacter>(), Is.Not.Null, "RampWorker: no character");
            Assert.That(crew.GetComponent<ApronIdentity>(), Is.Not.Null, "RampWorker: no name");
            Assert.That(crew.GetComponent<NetworkObject>().DontDestroyWithOwner, Is.False,
                "a player who leaves takes their character with them; one left behind stands in the " +
                "way for the rest of the session with nobody driving it");
        }

        [Test]
        public void NothingSpawnedOntoTheApronBringsACameraOrALightOfItsOwn()
        {
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Content/Prefabs" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(path));

                var cameras = prefab.GetComponentsInChildren<Camera>(true);
                var lights = prefab.GetComponentsInChildren<Light>(true);

                Assert.That(cameras, Is.Empty,
                    $"'{prefab.name}' carries {cameras.Length} camera(s), which a model exported with " +
                    "cameras turned on will hand it. Spawn a dozen of these and the view belongs to " +
                    "whichever one wins on depth, so the player loads in looking through a baggage cart");
                Assert.That(lights, Is.Empty,
                    $"'{prefab.name}' carries {lights.Length} light(s). A dozen of them is a dozen " +
                    "shadow maps competing for the atlas, for something nobody asked to be lit");
            }
        }

        [Test]
        public void EveryPrefabIsConfiguredFromAProfileRatherThanFromNothing()
        {
            AssertHasProfile(Prefab("BaggageTractor").GetComponent<VehicleController>(), "m_Profile");
            AssertHasProfile(Prefab("BaggageCart").GetComponent<VehicleController>(), "m_Profile");
            AssertHasProfile(Prefab("NarrowbodyAirliner").GetComponent<AircraftBody>(), "m_Profile");
            AssertHasProfile(Prefab("RampWorker").GetComponent<CrewCharacter>(), "m_Profile");
        }

        static void AssertHasProfile(Component on, string field)
        {
            var property = new SerializedObject(on).FindProperty(field);
            Assert.That(property, Is.Not.Null, $"{on.GetType().Name} has no field '{field}'");
            Assert.That(property.objectReferenceValue, Is.Not.Null,
                $"{on.name} carries no profile, so it has no mass, no size and no wheels. What a thing " +
                "is comes from the prefab it was made from -- that is the whole reason there is one " +
                "prefab per kind of thing");
        }

        [Test]
        public void EverythingThatMovesSaysWhichMachineMovesIt()
        {
            foreach (var name in new[] { "BaggageTractor", "BaggageCart", "Bag", "RampWorker" })
            {
                var prefab = Prefab(name);

                Assert.That(prefab.GetComponent<IMovedFromHere>(), Is.Not.Null,
                    $"{name} carries nothing that can say whether this machine is the one moving it. " +
                    "What a player sees asks exactly that before deciding whether to trail a shape " +
                    "behind its body, and something that cannot answer is drawn where it was a tenth " +
                    "of a second ago -- over two metres behind itself at the speed a tractor tows");
            }
        }

        [Test]
        public void EveryVehicleIsDrawnAsItsModelRatherThanAsAStandInBox()
        {
            foreach (var name in new[] { "BaggageTractor", "BaggageCart" })
            {
                var prefab = Prefab(name);

                Assert.That(prefab.GetComponent<ApronAppearance>().DrawnAs,
                    Is.EqualTo(ApronAppearance.Shape.AlreadyModelled),
                    $"{name} would draw a stand-in box on top of the model it already has, and a " +
                    "player would see a grey crate with a tractor inside it");

                var look = prefab.transform.Find(ApronAppearance.LookName);
                Assert.That(look, Is.Not.Null, $"{name}: nothing to look at");
                Assert.That(look.GetComponentsInChildren<MeshRenderer>(), Is.Not.Empty,
                    $"{name} says it is already modelled and then draws nothing at all, which on the " +
                    "apron is a floating label with an invisible three-tonne machine under it");
            }
        }

        [Test]
        public void TheWheelsAVehicleDrawsAreTheOnesItsSuspensionProbesFrom()
        {
            foreach (var name in new[] { "BaggageTractor", "BaggageCart" })
            {
                var prefab = Prefab(name);
                var shape = prefab.GetComponent<VehicleShape>();
                var drawn = new SerializedObject(prefab.GetComponentInChildren<WheelLook>())
                    .FindProperty("m_Wheels");

                Assert.That(drawn.arraySize, Is.EqualTo(shape.Wheels.Count),
                    $"{name} draws {drawn.arraySize} wheels and hangs its suspension from " +
                    $"{shape.Wheels.Count}");

                for (var i = 0; i < drawn.arraySize; i++)
                {
                    var wheel = (Transform)drawn.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.That(wheel, Is.Not.Null, $"{name}: wheel {i + 1} is missing");

                    var where = prefab.transform.InverseTransformPoint(wheel.position);
                    var axle = shape.Wheels[i].CentreLocal;

                    Assert.That(new Vector2(where.x - axle.x, where.z - axle.z).magnitude,
                        Is.LessThan(0.02f),
                        $"{name}: the wheel drawn {i + 1}th is at {where}, and the {i + 1}th axle " +
                        $"its suspension probes from is at {axle}. These are two lists that have to " +
                        "stay in the same order: each visible wheel is moved to the height its own " +
                        "corner's ray found and turned at its own corner's radius, so a swap draws " +
                        "the rear wheels at the front axle's ride height and spins them 20% wrong");
                }
            }
        }
    }
}
