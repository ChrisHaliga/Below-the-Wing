namespace BelowTheWing.Cargo
{
    /// <summary>
    /// Which machine should be simulating a bag.
    ///
    /// The rule this game is built on: things that touch each other are simulated together. A bag
    /// belongs to whoever last took hold of it, through the throw and wherever it lands -- unless it
    /// comes to rest on a vehicle somebody else is simulating, in which case it becomes theirs, so
    /// that a bag on a cart is real contact on one machine rather than a copy resting on a copy.
    ///
    /// Plain arithmetic on purpose, apart from the networking that acts on the answer.
    /// </summary>
    public static class CargoOwnership
    {
        /// <summary>Stands for no machine at all.</summary>
        public const ulong Nobody = ulong.MaxValue;

        /// <summary>
        /// Who should own a bag that is currently owned by <paramref name="owner"/>, given whether a
        /// hand on this machine has hold of it and whose vehicle, if any, it is resting on.
        ///
        /// A hand beats a deck. Whoever is about to throw the bag is the one whose physics the
        /// throw has to come from, and a bag in somebody's hand is not resting on anything for
        /// long.
        /// </summary>
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
