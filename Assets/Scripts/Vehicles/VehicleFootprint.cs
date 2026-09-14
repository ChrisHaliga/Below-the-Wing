using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public readonly struct VehicleFootprint
    {
        public readonly Vector3 EnvelopeSizeMetres;

        public readonly float FrontReachMetres;

        public readonly float RearReachMetres;

        public VehicleFootprint(Vector3 envelopeSizeMetres, float frontReachMetres, float rearReachMetres)
        {
            EnvelopeSizeMetres = envelopeSizeMetres;
            FrontReachMetres = frontReachMetres;
            RearReachMetres = rearReachMetres;
        }

        public static VehicleFootprint Of(
            Vector3 envelopeSizeMetres, Vector3? frontCouplingLocal, Vector3? rearCouplingLocal)
            => new VehicleFootprint(
                envelopeSizeMetres,
                frontCouplingLocal?.z ?? 0f,
                -(rearCouplingLocal?.z ?? 0f));
    }
}
