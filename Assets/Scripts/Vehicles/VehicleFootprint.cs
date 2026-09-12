using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// The room a vehicle takes up and how far its couplings reach: what laying vehicles out on an
    /// apron needs to know about one, and nothing else.
    ///
    /// A value rather than the component it is read off, so that working out where things stand
    /// stays arithmetic about the apron. The layout runs before any vehicle exists and is checked
    /// by tests that never build one; neither should need a scene object to hold three numbers.
    /// </summary>
    public readonly struct VehicleFootprint
    {
        /// <summary>Width, height and length of the room this vehicle takes up, in metres.</summary>
        public readonly Vector3 EnvelopeSizeMetres;

        /// <summary>How far the front coupling reaches past the origin, in metres.</summary>
        public readonly float FrontReachMetres;

        /// <summary>How far the rear coupling reaches past the origin, in metres.</summary>
        public readonly float RearReachMetres;

        public VehicleFootprint(Vector3 envelopeSizeMetres, float frontReachMetres, float rearReachMetres)
        {
            EnvelopeSizeMetres = envelopeSizeMetres;
            FrontReachMetres = frontReachMetres;
            RearReachMetres = rearReachMetres;
        }

        /// <summary>
        /// The footprint implied by where a vehicle's couplings are.
        ///
        /// The one place that turns a coupling point into a reach. A front coupling sits ahead of
        /// the origin, so its reach is how far along it is; a rear one sits behind, so its reach is
        /// how far back. Two hitched vehicles stand the rear reach of the one in front plus the
        /// front reach of the one behind apart, and anything that assumes one number for both
        /// leaves every coupling in a train holding a gap open.
        /// </summary>
        public static VehicleFootprint Of(Vector3 envelopeSizeMetres, Vector3 frontCouplingLocal, Vector3 rearCouplingLocal)
            => new VehicleFootprint(envelopeSizeMetres, frontCouplingLocal.z, -rearCouplingLocal.z);
    }
}
