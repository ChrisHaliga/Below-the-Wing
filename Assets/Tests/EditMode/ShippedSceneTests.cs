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
    /// <summary>
    /// The scene and prefabs the game actually ships with.
    ///
    /// Every serious defect found in this project so far has been here rather than in any of the
    /// logic: a value read before the network had delivered it, an appearance built from a name that
    /// had stopped being replicated, and a prefab list assigned to a field that is not serialized and
    /// so was silently empty in the saved scene. Each one broke the game completely for anybody who
    /// joined, and none of them could be seen by a test, because every other test in this project
    /// builds its objects in code and never opens the scene or loads a shipped prefab.
    ///
    /// These do the opposite. They assert nothing about behaviour and everything about whether the
    /// assets say what the code assumes they say.
    /// </summary>
    public sealed class ShippedSceneTests
    {
        const string ScenePath = "Assets/Scenes/Apron.unity";
        const string PrefabFolder = "Assets/Content/Prefabs";

        Scene m_Apron;

        [SetUp]
        public void OpenTheApron() => m_Apron = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        [TearDown]
        public void CloseTheApron() => EditorSceneManager.CloseScene(m_Apron, removeScene: true);

        static T Find<T>() where T : Component
        {
            var found = Object.FindAnyObjectByType<T>();
            Assert.That(found, Is.Not.Null, $"the apron scene has no {typeof(T).Name}");
            return found;
        }

        static GameObject Prefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{name}.prefab");
            Assert.That(prefab, Is.Not.Null, $"there is no prefab at {PrefabFolder}/{name}.prefab");
            return prefab;
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

            foreach (var field in new[]
                     {
                         "m_AircraftProfile", "m_CrewProfile",
                         "m_TractorPrefab", "m_CartPrefab", "m_AircraftPrefab", "m_CrewPrefab", "m_BagPrefab",
                         "m_Broker", "m_Camera", "m_Readout"
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
            Assert.That(Object.FindObjectsByType<VehicleController>(FindObjectsInactive.Exclude), Is.Empty,
                "vehicles are placed at runtime from one description of the layout. A vehicle sitting " +
                "in the scene as well is a second description that nothing keeps in agreement");
            Assert.That(Object.FindObjectsByType<CrewCharacter>(FindObjectsInactive.Exclude), Is.Empty,
                "a character in the scene belongs to nobody and is simulated by everybody");
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
            Assert.That(cartShape.WheelCentresLocal.Count, Is.EqualTo(4),
                "BaggageCart: four wheels, read off the model");
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
                "BaggageCart: nothing to look at. The model is what a player sees now, and a cart " +
                "drawn as nothing is a cart that appears to be a floating label");
            Assert.That(cart.GetComponentInChildren<WheelLook>(), Is.Not.Null,
                "BaggageCart: with nothing driving the visible wheels they neither turn nor stay on " +
                "the tarmac, and the body sinks onto its springs leaving them buried");

            var carriers = cart.GetComponentsInChildren<Carrier>();
            Assert.That(carriers.Length, Is.EqualTo(2),
                "BaggageCart: two places to ride -- inside it, where bags go and somebody crouching " +
                "can stand, and on top of it");

            Assert.That(cart.GetComponentInChildren<CarrierWatch>(), Is.Not.Null,
                "BaggageCart: nothing decides that a corner was taken too hard, so a cart can be " +
                "rolled onto its roof with its bags still glued to the deck");
            Assert.That(cart.GetComponent<CarrierAuthority>(), Is.Not.Null,
                "BaggageCart: with nothing saying which machine decides, every machine decides. A " +
                "copy is steered toward its owner's reports twenty times a second and those nudges " +
                "read as sideways acceleration the cart never felt, so bags leap off decks on every " +
                "screen except the one where the cart is actually being driven");

            var bag = Prefab("Bag");
            Assert.That(bag.GetComponent<CargoMotion>(), Is.Not.Null,
                "Bag: nothing tells the other machines which cart this is riding on, and no force " +
                "exists that could pull a bag from a deck on one screen onto a deck on another");
            Assert.That(bag.GetComponent<AnticipatedNetworkTransform>(), Is.Null,
                "Bag: a transform component writes a position onto the copy every update and has " +
                "nothing at all to say about what the bag is riding on, which is the only part " +
                "players would notice being wrong");

            var tractor = Prefab("BaggageTractor");
            Assert.That(tractor.GetComponent<VehicleShape>(), Is.Not.Null,
                "BaggageTractor: a vehicle with no model is described in exactly the same terms as " +
                "one with a model, or everything downstream needs a branch for it");
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
        public void WhatAVehicleLooksLikeMatchesWhatItCollidesAs()
        {
            foreach (var name in new[] { "BaggageTractor", "BaggageCart" })
            {
                var prefab = Prefab(name);
                var profile = (VehicleProfile)new SerializedObject(prefab.GetComponent<VehicleController>())
                    .FindProperty("m_Profile").objectReferenceValue;

                var drawnAt = prefab.GetComponent<ApronAppearance>().SizeMetres;

                Assert.That(drawnAt, Is.EqualTo(profile.bodySizeMetres),
                    $"{name} is drawn at {drawnAt} and collides as {profile.bodySizeMetres}. Everything " +
                    "on the apron is a grey box, so its size is the only thing telling a player how " +
                    "much room it needs");
            }
        }
    }
}
