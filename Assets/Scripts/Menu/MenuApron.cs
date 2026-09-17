using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Menu
{
    public sealed class MenuApron
    {
        public const string HolderName = "Menu apron";

        readonly GameObject m_Holder;

        public MenuApron(
            ApronLayoutSettings layout,
            AircraftProfile aircraftProfile,
            CrewProfile crewProfile,
            GameObject tractorPrefab,
            GameObject cartPrefab,
            GameObject aircraftPrefab)
        {
            m_Holder = new GameObject(HolderName);

            Plan = ApronLayout.Build(
                layout,
                tractorPrefab.GetComponent<VehicleShape>().Footprint,
                cartPrefab.GetComponent<VehicleShape>().Footprint,
                aircraftProfile,
                new Vector3(
                    crewProfile.radiusMetres * 2f, crewProfile.heightMetres, crewProfile.radiusMetres * 2f));

            Place(aircraftPrefab, Plan.Aircraft);

            foreach (var train in Plan.Trains)
            {
                Place(tractorPrefab, train.Tractor);

                foreach (var cart in train.Carts)
                {
                    Place(cartPrefab, cart);
                }
            }
        }

        public ApronPlan Plan { get; }

        public void TakeItDown() => Discard.Now(m_Holder);

        void Place(GameObject prefab, Placement where)
        {
            var placed = Object.Instantiate(prefab, where.Position, where.Rotation, m_Holder.transform);

            placed.name = where.Name;

            foreach (var body in placed.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
            }

            foreach (var behaviour in placed.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is ApronAppearance)
                {
                    continue;
                }

                behaviour.enabled = false;
            }

            placed.GetComponent<ApronAppearance>()?.Show(where.Name);
        }
    }
}
