using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Apron
{
    public readonly struct ApronEquipment
    {
        public readonly VehicleFootprint Tractor;

        public readonly VehicleFootprint Cart;

        public readonly VehicleFootprint BeltLoader;

        public readonly Vector3 AircraftSizeMetres;

        public readonly Vector3 AircraftCentreLocal;

        public readonly Vector3 CrewSizeMetres;

        public ApronEquipment(
            VehicleFootprint tractor,
            VehicleFootprint cart,
            VehicleFootprint beltLoader,
            Vector3 aircraftSizeMetres,
            Vector3 aircraftCentreLocal,
            Vector3 crewSizeMetres)
        {
            Tractor = tractor;
            Cart = cart;
            BeltLoader = beltLoader;
            AircraftSizeMetres = aircraftSizeMetres;
            AircraftCentreLocal = aircraftCentreLocal;
            CrewSizeMetres = crewSizeMetres;
        }
    }
}
