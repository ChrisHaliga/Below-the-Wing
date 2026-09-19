using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public readonly struct VehicleFootprint
    {
        public readonly Vector3 EnvelopeSizeMetres;

        public readonly Vector3 EnvelopeCentreLocal;

        public readonly float FrontReachMetres;

        public readonly float RearReachMetres;

        public VehicleFootprint(
            Vector3 envelopeSizeMetres,
            Vector3 envelopeCentreLocal,
            float frontReachMetres,
            float rearReachMetres)
        {
            EnvelopeSizeMetres = envelopeSizeMetres;
            EnvelopeCentreLocal = envelopeCentreLocal;
            FrontReachMetres = frontReachMetres;
            RearReachMetres = rearReachMetres;
        }

        public static VehicleFootprint Of(
            Vector3 envelopeSizeMetres,
            Vector3 envelopeCentreLocal,
            Vector3? frontCouplingLocal,
            Vector3? rearCouplingLocal)
            => new VehicleFootprint(
                envelopeSizeMetres,
                envelopeCentreLocal,
                frontCouplingLocal?.z ?? 0f,
                -(rearCouplingLocal?.z ?? 0f));
    }
}
