namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// What a driver is asking a vehicle to do, independent of who or what is asking.
    ///
    /// This is the boundary between control and physics. A vehicle never reads a keyboard and
    /// never asks whether it is being driven locally or replicated from elsewhere; it asks its
    /// intent source what is being requested and turns that into forces.
    /// </summary>
    public readonly struct DriveIntent
    {
        /// <summary>Steering request from -1 (full left) to 1 (full right).</summary>
        public readonly float Steer;

        /// <summary>Throttle request from 0 (closed) to 1 (open). Negative values drive in reverse.</summary>
        public readonly float Throttle;

        /// <summary>Brake request from 0 (off) to 1 (full).</summary>
        public readonly float Brake;

        public DriveIntent(float steer, float throttle, float brake)
        {
            Steer = steer;
            Throttle = throttle;
            Brake = brake;
        }

        /// <summary>A vehicle nobody is driving: no steering, no throttle, no brake.</summary>
        public static DriveIntent Idle => new DriveIntent(0f, 0f, 0f);
    }

    /// <summary>
    /// Where a vehicle gets its <see cref="DriveIntent"/> from.
    ///
    /// A vehicle the local player is driving reads from an input-backed source. A vehicle owned by
    /// somebody else has no source at all and is reconciled toward its owner's version instead.
    /// </summary>
    public interface IDriveIntentSource
    {
        DriveIntent Current { get; }
    }
}
