using System.Collections.Generic;
using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Tests.Support
{
    public sealed class TestApron
    {
        readonly List<GameObject> m_Spawned = new List<GameObject>();

        public GameObject Ground { get; }

        public TestApron(float sizeMetres = 1200f)
        {
            Physics.simulationMode = SimulationMode.FixedUpdate;

            Ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Ground.name = "Apron";
            Ground.transform.position = new Vector3(0f, -0.5f, 0f);
            Ground.transform.localScale = new Vector3(sizeMetres, 1f, sizeMetres);
            m_Spawned.Add(Ground);
        }

        public VehicleController AddVehicle(
            VehicleProfile profile,
            string displayName,
            Vector3 position,
            Quaternion rotation,
            VehicleShape.Measurements? shape = null)
        {
            var go = new GameObject(displayName);
            go.transform.SetPositionAndRotation(position, rotation);

            TestShapes.On(go, shape ?? TestShapes.BoxVehicle());

            m_Spawned.Add(go);

            var vehicle = go.AddComponent<VehicleController>();
            vehicle.Configure(profile, displayName);

            return vehicle;
        }

        public VehicleController AddMarker(VehicleProfile profile, string displayName, Vector3 position, bool driveable = true)
        {
            var vehicle = AddVehicle(profile, displayName, position, Quaternion.identity);
            vehicle.Occupied = !driveable;
            return vehicle;
        }

        public CartChain AddTrain(
            VehicleProfile tractor,
            VehicleProfile cart,
            int cartCount,
            Vector3 tractorPosition,
            string name = "Tug 1",
            VehicleShape.Measurements? tractorShape = null,
            VehicleShape.Measurements? cartShape = null)
        {
            var tractorMeasurements = tractorShape ?? TestShapes.BoxVehicle();
            var cartMeasurements = cartShape ?? TestShapes.BoxVehicle();

            var settings = ApronLayoutSettings.Default;
            settings.trainCount = 1;
            settings.cartsPerTrain = cartCount;
            settings.firstTractorPosition = tractorPosition;

            var plan = ApronLayout.Build(
                    settings, tractorMeasurements.Footprint, cartMeasurements.Footprint, TestProfiles.Aircraft(), Vector3.one)
                .Trains[0];

            var members = new List<VehicleController>
            {
                AddVehicle(tractor, name, plan.Tractor.Position, plan.Tractor.Rotation, tractorMeasurements)
            };

            for (var i = 0; i < plan.Carts.Count; i++)
            {
                members.Add(AddVehicle(
                    cart, $"{name} cart {i + 1}", plan.Carts[i].Position, plan.Carts[i].Rotation, cartMeasurements));
            }

            var lineup = new List<TrainMembership>(members.Count);
            for (var place = 0; place < members.Count; place++)
            {
                lineup.Add(new TrainMembership(members[place], trainIndex: 0, placeInTrain: place));
            }

            var registry = new TrainRegistry(ChainJointSettings.Default);
            registry.Rebuild(lineup);

            return registry.Trains[0];
        }

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

        public CrewCharacter AddRemoteCrew(CrewProfile profile, Vector3 position)
        {
            var go = new GameObject("Somebody else");
            go.transform.position = position;
            var crew = go.AddComponent<CrewCharacter>();
            crew.ConfigureBody(profile);

            crew.OursToMove = false;

            m_Spawned.Add(go);
            return crew;
        }

        public T Track<T>(T component) where T : Component
        {
            m_Spawned.Add(component.gameObject);
            return component;
        }

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
