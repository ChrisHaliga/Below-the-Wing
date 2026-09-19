using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Apron
{
    public readonly struct ApronEquipment
    {
        public readonly VehicleFootprint Tractor;

        public readonly VehicleFootprint Cart;

        public readonly Vector3 AircraftSizeMetres;

        public readonly Vector3 AircraftCentreLocal;

        public readonly Vector3 CrewSizeMetres;

        public ApronEquipment(
            VehicleFootprint tractor,
            VehicleFootprint cart,
            Vector3 aircraftSizeMetres,
            Vector3 aircraftCentreLocal,
            Vector3 crewSizeMetres)
        {
            Tractor = tractor;
            Cart = cart;
            AircraftSizeMetres = aircraftSizeMetres;
            AircraftCentreLocal = aircraftCentreLocal;
            CrewSizeMetres = crewSizeMetres;
        }
    }
}
