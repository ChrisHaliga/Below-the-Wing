using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Where a vehicle's parts physically are.
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
    /// For a vehicle that has a model, these are filled in from the model's own landmarks when the
    /// prefab is built. For one that does not, they are filled in from numbers. Nothing downstream
    /// can tell which, and that is the point.
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

        /// <summary>Everything measured about one vehicle, for filling a shape in at once.</summary>
        public struct Measurements
        {
            public IReadOnlyList<Vector3> WheelCentresLocal;
            public Vector3 FrontCouplingLocal;
            public Vector3 RearCouplingLocal;
            public Vector3 EnvelopeSizeMetres;
            public Vector3 EnvelopeCentreLocal;
            public Bounds InteriorLocal;
            public IReadOnlyList<SolidPart> SolidParts;

            /// <summary>What laying this vehicle out needs to know about it.</summary>
            public VehicleFootprint Footprint
                => VehicleFootprint.Of(EnvelopeSizeMetres, FrontCouplingLocal, RearCouplingLocal);
        }

        [SerializeField, Tooltip("Where the four wheels sit, in this vehicle's own space.")]
        Vector3[] m_WheelCentresLocal = new Vector3[4];

        [SerializeField, Tooltip("Where a vehicle in front of this one attaches.")]
        Vector3 m_FrontCouplingLocal;

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

        /// <summary>Where the four wheels sit, in this vehicle's own space.</summary>
        public IReadOnlyList<Vector3> WheelCentresLocal => m_WheelCentresLocal;

        /// <summary>Where a vehicle in front of this one attaches, in this vehicle's own space.</summary>
        public Vector3 FrontCouplingLocal => m_FrontCouplingLocal;

        /// <summary>Where a vehicle behind this one attaches, in this vehicle's own space.</summary>
        public Vector3 RearCouplingLocal => m_RearCouplingLocal;

        /// <summary>
        /// What laying this vehicle out needs to know about it: the room it takes up and how far
        /// each coupling reaches.
        ///
        /// The two reaches are nothing like each other on a real cart -- the drawbar sticks out in
        /// front and the socket is recessed behind -- which is why they are two numbers.
        /// </summary>
        public VehicleFootprint Footprint
            => VehicleFootprint.Of(m_EnvelopeSizeMetres, m_FrontCouplingLocal, m_RearCouplingLocal);

        /// <summary>How far the front coupling reaches past the origin, in metres.</summary>
        public float FrontReachMetres => Footprint.FrontReachMetres;

        /// <summary>How far the rear coupling reaches past the origin, in metres.</summary>
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
            m_WheelCentresLocal = measurements.WheelCentresLocal != null
                ? new List<Vector3>(measurements.WheelCentresLocal).ToArray()
                : Array.Empty<Vector3>();

            m_FrontCouplingLocal = measurements.FrontCouplingLocal;
            m_RearCouplingLocal = measurements.RearCouplingLocal;
            m_EnvelopeSizeMetres = measurements.EnvelopeSizeMetres;
            m_EnvelopeCentreLocal = measurements.EnvelopeCentreLocal;
            m_InteriorLocal = measurements.InteriorLocal;

            m_SolidParts = measurements.SolidParts != null
                ? new List<SolidPart>(measurements.SolidParts).ToArray()
                : Array.Empty<SolidPart>();
        }

        float SpanAlong(Func<Vector3, float> pick)
        {
            if (m_WheelCentresLocal == null || m_WheelCentresLocal.Length == 0)
            {
                return 0f;
            }

            var least = float.MaxValue;
            var most = float.MinValue;

            foreach (var wheel in m_WheelCentresLocal)
            {
                var along = pick(wheel);
                least = Mathf.Min(least, along);
                most = Mathf.Max(most, along);
            }

            return most - least;
        }
    }
}
