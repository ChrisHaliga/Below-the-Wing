using BelowTheWing.Wiring;
namespace BelowTheWing.Cargo
{
    public static class CargoOwnership
    {
        public static ulong WhoShouldOwn(ulong owner, ulong thisMachine, bool heldHere, ulong restingOnVehicleOwnedBy)
        {
            if (heldHere)
            {
                return thisMachine;
            }

            if (restingOnVehicleOwnedBy != Shift.Nobody)
            {
                return restingOnVehicleOwnedBy;
            }

            return owner;
        }
    }
}
