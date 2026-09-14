using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Where a vehicle's parts physically are, and how big they are.
    ///
    /// The companion to <see cref="VehicleProfile"/>, and the division between them is worth
    /// stating: a profile says how a vehicle <em>drives</em> -- its mass, its springs, its grip,
    /// its brakes -- and a shape says where it <em>is</em>. Wheels, couplings, the room it takes
    /// up, and which parts of it are solid.
    ///
    /// One place answers that question, and everything else asks. It used to be answered four
    /// separate times: the suspension worked its wheel positions out from a wheelbase and a track,
    /// the couplings worked their positions out from a body length and a drawbar, the apron layout
    /// worked train spacing out from the same two numbers again, and the prefab builder worked the
    /// deck out from a body size. Every one of those was a second copy of a measurement, and a
    /// second copy is how the invisible suspension probes ended up fourteen centimetres from the
    /// visible wheels with one side of a cart sitting permanently compressed.
    ///
    /// These are filled in from the model's own landmarks when the prefab is built.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleShape : MonoBehaviour
    {
        /// <summary>One solid box a vehicle is built from.</summary>
        [Serializable]
        public struct SolidPart
        {
            [Tooltip("What this part is, so a collider on the prefab can be recognised.")]
            public string Name;

            [Tooltip("Width, height and length in metres.")]
            public Vector3 SizeMetres;

            [Tooltip("Centre of the box in the vehicle's own space.")]
            public Vector3 CentreLocal;

            public SolidPart(string name, Vector3 sizeMetres, Vector3 centreLocal)
            {
                Name = name;
                SizeMetres = sizeMetres;
                CentreLocal = centreLocal;
            }
        }

        /// <summary>
        /// One wheel: where its centre sits in the vehicle's own space, and how big it is.
        ///
        /// The two travel together because everything the suspension does with one it does with the
        /// other. A wheel's radius decides how far its ground ray has to reach, how high above it
        /// the body hangs, and how far it has dropped when that ray finds the tarmac further away
        /// than expected. A vehicle whose axles carry different wheels -- the baggage tractor has
        /// 0.22 m at the front and 0.26 m at the back -- gets all three wrong on one axle the
        /// moment those two facts are stored apart and one figure is used for every corner.
        /// </summary>
        [Serializable]
        public struct WheelPlacement
        {
            [Tooltip("Centre of the wheel in the vehicle's own space. The origin is on the ground, " +
                     "so a wheel standing on the tarmac has its centre one radius up.")]
            public Vector3 CentreLocal;

            [Tooltip("Radius of the wheel in metres.")]
            public float RadiusMetres;

            public WheelPlacement(Vector3 centreLocal, float radiusMetres)
            {
                CentreLocal = centreLocal;
                RadiusMetres = radiusMetres;
            }
        }

        /// <summary>Everything measured about one vehicle, for filling a shape in at once.</summary>
        public struct Measurements
        {
            public IReadOnlyList<WheelPlacement> Wheels;

            /// <summary>
            /// Where a vehicle in front of this one attaches, or null if nothing can tow it.
            ///
            /// Null rather than the origin. A tractor has no coupling at its front, and a point at
            /// (0,0,0) is not "no coupling" -- it is a coupling on the tarmac between the front
            /// wheels, which is where a joint would drag the tractor from.
            /// </summary>
            public Vector3? FrontCouplingLocal;

            /// <summary>Where a vehicle behind this one attaches, or null if nothing can follow it.</summary>
            public Vector3? RearCouplingLocal;

            public Vector3 EnvelopeSizeMetres;
            public Vector3 EnvelopeCentreLocal;
            public Bounds InteriorLocal;
            public IReadOnlyList<SolidPart> SolidParts;

            /// <summary>What laying this vehicle out needs to know about it.</summary>
            public VehicleFootprint Footprint
                => VehicleFootprint.Of(EnvelopeSizeMetres, FrontCouplingLocal, RearCouplingLocal);
        }

        [SerializeField, Tooltip("Where the wheels sit and how big they are.")]
        WheelPlacement[] m_Wheels = Array.Empty<WheelPlacement>();

        [SerializeField, Tooltip("Whether anything can be hitched in front of this vehicle at all.")]
        bool m_HasFrontCoupling;

        [SerializeField, Tooltip("Where a vehicle in front of this one attaches.")]
        Vector3 m_FrontCouplingLocal;

        [SerializeField, Tooltip("Whether anything can be hitched behind this vehicle at all.")]
        bool m_HasRearCoupling;

        [SerializeField, Tooltip("Where a vehicle behind this one attaches.")]
        Vector3 m_RearCouplingLocal;

        [SerializeField, Tooltip("Width, height and length of the room this vehicle takes up.")]
        Vector3 m_EnvelopeSizeMetres = Vector3.one;

        [SerializeField, Tooltip("Centre of that room, in this vehicle's own space. Not the origin: " +
                                 "the origin is on the ground, between the wheels.")]
        Vector3 m_EnvelopeCentreLocal;

        [SerializeField, Tooltip("The space inside that has to stay clear for cargo. Empty for " +
                                 "anything solid.")]
        Bounds m_InteriorLocal;

        [SerializeField, Tooltip("The boxes this vehicle is solid in.")]
        SolidPart[] m_SolidParts = Array.Empty<SolidPart>();

        /// <summary>Where this vehicle's wheels sit and how big each one is.</summary>
        public IReadOnlyList<WheelPlacement> Wheels => m_Wheels;

        /// <summary>Whether anything can be hitched in front of this vehicle.</summary>
        public bool HasFrontCoupling => m_HasFrontCoupling;

        /// <summary>Whether anything can be hitched behind this vehicle.</summary>
        public bool HasRearCoupling => m_HasRearCoupling;

        /// <summary>
        /// Where a vehicle in front of this one attaches, or null if nothing can tow it.
        ///
        /// Nothing tows a baggage tractor, so it answers null here, and a caller that wants to
        /// hitch something has to deal with that rather than being handed a point on the tarmac.
        /// </summary>
        public Vector3? FrontCouplingLocal => m_HasFrontCoupling ? m_FrontCouplingLocal : (Vector3?)null;

        /// <summary>Where a vehicle behind this one attaches, or null if nothing can follow it.</summary>
        public Vector3? RearCouplingLocal => m_HasRearCoupling ? m_RearCouplingLocal : (Vector3?)null;

        /// <summary>
        /// What laying this vehicle out needs to know about it: the room it takes up and how far
        /// each coupling reaches.
        ///
        /// The two reaches are nothing like each other on a real cart -- the drawbar sticks out in
        /// front and the socket is recessed behind -- which is why they are two numbers.
        /// </summary>
        public VehicleFootprint Footprint
            => VehicleFootprint.Of(m_EnvelopeSizeMetres, FrontCouplingLocal, RearCouplingLocal);

        /// <summary>How far the front coupling reaches past the origin, in metres. Zero if it has none.</summary>
        public float FrontReachMetres => Footprint.FrontReachMetres;

        /// <summary>How far the rear coupling reaches past the origin, in metres. Zero if it has none.</summary>
        public float RearReachMetres => Footprint.RearReachMetres;

        /// <summary>Width, height and length of the room this vehicle takes up, in metres.</summary>
        public Vector3 EnvelopeSizeMetres => m_EnvelopeSizeMetres;

        /// <summary>The centre of that room, in this vehicle's own space.</summary>
        public Vector3 EnvelopeCentreLocal => m_EnvelopeCentreLocal;

        /// <summary>The space inside that has to stay clear for cargo. Size zero if there is none.</summary>
        public Bounds InteriorLocal => m_InteriorLocal;

        /// <summary>The boxes this vehicle is solid in.</summary>
        public IReadOnlyList<SolidPart> SolidParts => m_SolidParts;

        /// <summary>Distance between the front and rear axles, from where the wheels actually are.</summary>
        public float WheelbaseMetres => SpanAlong(axis => axis.z);

        /// <summary>Distance between the left and right wheels, from where the wheels actually are.</summary>
        public float TrackMetres => SpanAlong(axis => axis.x);

        /// <summary>Fills this in. Used when a prefab is built, and by tests.</summary>
        public void Describe(Measurements measurements)
        {
            m_Wheels = measurements.Wheels != null
                ? new List<WheelPlacement>(measurements.Wheels).ToArray()
                : Array.Empty<WheelPlacement>();

            m_HasFrontCoupling = measurements.FrontCouplingLocal.HasValue;
            m_FrontCouplingLocal = measurements.FrontCouplingLocal ?? Vector3.zero;

            m_HasRearCoupling = measurements.RearCouplingLocal.HasValue;
            m_RearCouplingLocal = measurements.RearCouplingLocal ?? Vector3.zero;

            m_EnvelopeSizeMetres = measurements.EnvelopeSizeMetres;
            m_EnvelopeCentreLocal = measurements.EnvelopeCentreLocal;
            m_InteriorLocal = measurements.InteriorLocal;

            m_SolidParts = measurements.SolidParts != null
                ? new List<SolidPart>(measurements.SolidParts).ToArray()
                : Array.Empty<SolidPart>();
        }

        float SpanAlong(Func<Vector3, float> pick)
        {
            if (m_Wheels == null || m_Wheels.Length == 0)
            {
                return 0f;
            }

            var least = float.MaxValue;
            var most = float.MinValue;

            foreach (var wheel in m_Wheels)
            {
                var along = pick(wheel.CentreLocal);
                least = Mathf.Min(least, along);
                most = Mathf.Max(most, along);
            }

            return most - least;
        }
    }
}
