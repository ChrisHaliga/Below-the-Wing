using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Apron
{
    public readonly struct ApronEquipment
    {
        public readonly VehicleFootprint Tractor;

        public readonly VehicleFootprint Cart;

        public readonly AircraftProfile Aircraft;

        public readonly Vector3 CrewSizeMetres;

        public ApronEquipment(
            VehicleFootprint tractor,
            VehicleFootprint cart,
            AircraftProfile aircraft,
            Vector3 crewSizeMetres)
        {
            Tractor = tractor;
            Cart = cart;
            Aircraft = aircraft;
            CrewSizeMetres = crewSizeMetres;
        }
    }
}
