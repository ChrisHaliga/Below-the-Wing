using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Apron
{
    public readonly struct ApronEquipment
    {
        public readonly VehicleFootprint Tractor;

        public readonly VehicleFootprint Cart;

        public readonly VehicleFootprint BeltLoader;

        public readonly VehicleFootprint Aircraft;

        public readonly Vector3 CrewSizeMetres;

        public ApronEquipment(
            VehicleFootprint tractor,
            VehicleFootprint cart,
            VehicleFootprint beltLoader,
            VehicleFootprint aircraft,
            Vector3 crewSizeMetres)
        {
            Tractor = tractor;
            Cart = cart;
            BeltLoader = beltLoader;
            Aircraft = aircraft;
            CrewSizeMetres = crewSizeMetres;
        }
    }
}
