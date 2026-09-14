namespace BelowTheWing.Cargo
{
    public static class CargoOwnership
    {
        public const ulong Nobody = ulong.MaxValue;

        public static ulong WhoShouldOwn(ulong owner, ulong thisMachine, bool heldHere, ulong restingOnVehicleOwnedBy)
        {
            if (heldHere)
            {
                return thisMachine;
            }

            if (restingOnVehicleOwnedBy != Nobody)
            {
                return restingOnVehicleOwnedBy;
            }

            return owner;
        }
    }
}
