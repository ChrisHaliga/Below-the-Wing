namespace BelowTheWing.Vehicles
{
    public readonly struct DriveIntent
    {
        public readonly float Steer;

        public readonly float Throttle;

        public readonly float Brake;

        public readonly bool Sprint;

        public DriveIntent(float steer, float throttle, float brake, bool sprint = false)
        {
            Steer = steer;
            Throttle = throttle;
            Brake = brake;
            Sprint = sprint;
        }

        public static DriveIntent Idle => new DriveIntent(0f, 0f, 0f);
    }

    public interface IDriveIntentSource
    {
        DriveIntent Current { get; }
    }
}
