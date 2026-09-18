using System.Collections.Generic;

namespace BelowTheWing.Vehicles
{
    public readonly struct WheelRole
    {
        public readonly bool Steers;
        public readonly float DriveShare;
        public readonly float BrakeShare;

        public WheelRole(bool steers, float driveShare, float brakeShare)
        {
            Steers = steers;
            DriveShare = driveShare;
            BrakeShare = brakeShare;
        }
    }

    public static class WheelShares
    {
        public static WheelRole[] Of(IReadOnlyList<VehicleShape.WheelPlacement> wheels)
        {
            var middle = MiddleOfTheWheelbase(wheels);
            var rear = 0;

            foreach (var wheel in wheels)
            {
                if (!AheadOf(middle, wheel))
                {
                    rear++;
                }
            }

            var roles = new WheelRole[wheels.Count];

            for (var i = 0; i < wheels.Count; i++)
            {
                var front = AheadOf(middle, wheels[i]);

                roles[i] = new WheelRole(
                    steers: front,
                    driveShare: front ? 0f : 1f / rear,
                    brakeShare: 1f / wheels.Count);
            }

            return roles;
        }

        static float MiddleOfTheWheelbase(IReadOnlyList<VehicleShape.WheelPlacement> wheels)
        {
            var sum = 0f;

            foreach (var wheel in wheels)
            {
                sum += wheel.CentreLocal.z;
            }

            return sum / wheels.Count;
        }

        static bool AheadOf(float middle, VehicleShape.WheelPlacement wheel) => wheel.CentreLocal.z > middle;
    }
}
