namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Something that is either moved by this machine or by another one.
    ///
    /// Every body a player can be in charge of -- a vehicle, their own character -- has a copy on
    /// every machine in the session, and exactly one of those copies is the real one. Some things
    /// need to know which they are looking at without caring what kind of thing it is: what you see
    /// trails the body a little to hide corrections, and the one body that must never trail is the
    /// one you are moving yourself.
    /// </summary>
    public interface IMovedFromHere
    {
        /// <summary>Whether this machine is the one that says where this goes.</summary>
        bool OursToMove { get; }
    }
}
