using System.Collections.Generic;
using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    /// <summary>
    /// Builds the smallest apron a test can run on, and takes it down again afterwards.
    ///
    /// Tests that are about physics need real ground to push against and real bodies to push, but
    /// they do not need a session, a lobby or any of the rest of the game. This puts down a floor
    /// and whatever vehicles are asked for, and nothing else.
    /// </summary>
    public sealed class TestApron
    {
        readonly List<GameObject> m_Spawned = new List<GameObject>();

        /// <summary>The floor everything stands on.</summary>
        public GameObject Ground { get; }

        public TestApron(float sizeMetres = 200f)
        {
            // Physics has to be stepping for any of this to mean anything. The netcode integration
            // tests drive the world themselves and can leave it under script control, and a later
            // test that assumes otherwise measures a world that never moves: a vehicle released
            // from the throttle keeps its exact speed for ever, and every assertion about settling,
            // grip or collision quietly passes or fails for the wrong reason.
            Physics.simulationMode = SimulationMode.FixedUpdate;

            Ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Ground.name = "Apron";
            Ground.transform.position = new Vector3(0f, -0.5f, 0f);
            Ground.transform.localScale = new Vector3(sizeMetres, 1f, sizeMetres);
            m_Spawned.Add(Ground);
        }

        /// <summary>Puts one vehicle on the apron, configured from a profile, and returns it.</summary>
        public VehicleController AddVehicle(VehicleProfile profile, string displayName, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(displayName);
            go.transform.SetPositionAndRotation(position, rotation);
            var vehicle = go.AddComponent<VehicleController>();
            vehicle.Configure(profile, displayName);
            m_Spawned.Add(go);
            return vehicle;
        }

        /// <summary>
        /// A vehicle that is only ever asked where it is and whether it would take a driver.
        ///
        /// A real controller rather than a stand-in, because the question "which vehicle is
        /// offered" is answered against real ones in the game, and a stand-in that is near or far
        /// on its own terms can agree with a rule the real thing would break.
        /// </summary>
        public VehicleController AddMarker(VehicleProfile profile, string displayName, Vector3 position, bool driveable = true)
        {
            var vehicle = AddVehicle(profile, displayName, position, Quaternion.identity);
            vehicle.Occupied = !driveable;
            return vehicle;
        }

        /// <summary>Puts a tractor and a row of carts on the apron and hooks them together.</summary>
        public CartChain AddTrain(VehicleProfile tractor, VehicleProfile cart, int cartCount, Vector3 tractorPosition, string name = "Tug 1")
        {
            // Built from the real apron layout rather than by working the spacing out again here.
            // A test train parked differently from a shipped one exercises different geometry, and
            // this spacing is exactly the arithmetic whose mismatch made a parked train wander.
            var settings = ApronLayoutSettings.Default;
            settings.trainCount = 1;
            settings.cartsPerTrain = cartCount;
            settings.firstTractorPosition = tractorPosition;

            var plan = ApronLayout.Build(settings, tractor, cart, TestProfiles.Aircraft(), Vector3.one)
                .Trains[0];

            var members = new List<VehicleController>
            {
                AddVehicle(tractor, name, plan.Tractor.Position, plan.Tractor.Rotation)
            };

            for (var i = 0; i < plan.Carts.Count; i++)
            {
                members.Add(AddVehicle(cart, $"{name} cart {i + 1}", plan.Carts[i].Position, plan.Carts[i].Rotation));
            }

            // A test apron is a machine simulating this train, so it holds the couplings too.
            var train = CartChain.Couple(members, ChainJointSettings.Default);
            train.EngageCouplings();
            return train;
        }

        /// <summary>Puts one crew member on the apron, with nothing nearby to get into.</summary>
        public CrewCharacter AddCrew(CrewProfile profile, Vector3 position, IOwnershipBroker broker = null)
        {
            var go = new GameObject("Crew");
            go.transform.position = position;
            var crew = go.AddComponent<CrewCharacter>();
            crew.ConfigureBody(profile);
            crew.TakeTheSeat(broker ?? new RecordingBroker(grant: true), () => new List<VehicleController>());
            m_Spawned.Add(go);
            return crew;
        }

        /// <summary>
        /// Puts a copy of somebody else's character on the apron: configured from its profile, as
        /// every machine does for every character, but with no seat, no camera and no keyboard.
        /// That is exactly what a remote player's body is.
        /// </summary>
        public CrewCharacter AddRemoteCrew(CrewProfile profile, Vector3 position)
        {
            var go = new GameObject("Somebody else");
            go.transform.position = position;
            var crew = go.AddComponent<CrewCharacter>();
            crew.ConfigureBody(profile);
            m_Spawned.Add(go);
            return crew;
        }

        /// <summary>Registers an object so it is cleaned up with everything else.</summary>
        public T Track<T>(T component) where T : Component
        {
            m_Spawned.Add(component.gameObject);
            return component;
        }

        /// <summary>Removes everything this apron put into the scene.</summary>
        public void TearDown()
        {
            foreach (var go in m_Spawned)
            {
                if (go == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(go);
                }
                else
                {
                    Object.DestroyImmediate(go);
                }
            }

            m_Spawned.Clear();
        }
    }
}
