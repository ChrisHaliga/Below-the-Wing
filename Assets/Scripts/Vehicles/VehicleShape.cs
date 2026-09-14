using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [DisallowMultipleComponent]
    public sealed class VehicleShape : MonoBehaviour
    {
        [Serializable]
        public struct SolidPart
        {
            [Tooltip("What this part is")]
            public string Name;

            [Tooltip("Width, height, length, m")]
            public Vector3 SizeMetres;

            [Tooltip("Centre in the vehicle's own space, m")]
            public Vector3 CentreLocal;

            [Tooltip("Piece of the model this is solid in. Empty for a box")]
            public Mesh Piece;

            [Tooltip("How that piece is turned on the vehicle")]
            public Quaternion PieceTurn;

            [Tooltip("How that piece is scaled on the vehicle")]
            public Vector3 PieceScale;

            public SolidPart(string name, Vector3 sizeMetres, Vector3 centreLocal)
            {
                Name = name;
                SizeMetres = sizeMetres;
                CentreLocal = centreLocal;
                Piece = null;
                PieceTurn = Quaternion.identity;
                PieceScale = Vector3.one;
            }

            public SolidPart(
                string name, Mesh piece, Vector3 centreLocal, Quaternion pieceTurn, Vector3 pieceScale)
            {
                Name = name;
                SizeMetres = Vector3.zero;
                CentreLocal = centreLocal;
                Piece = piece;
                PieceTurn = pieceTurn;
                PieceScale = pieceScale;
            }

            public bool IsAPieceOfTheModel => Piece != null;
        }

        [Serializable]
        public struct WheelPlacement
        {
            [Tooltip("Centre in the vehicle's own space, m")]
            public Vector3 CentreLocal;

            [Tooltip("Wheel radius, m")]
            public float RadiusMetres;

            public WheelPlacement(Vector3 centreLocal, float radiusMetres)
            {
                CentreLocal = centreLocal;
                RadiusMetres = radiusMetres;
            }
        }

        public struct Measurements
        {
            public IReadOnlyList<WheelPlacement> Wheels;

            public Vector3? FrontCouplingLocal;

            public Vector3? RearCouplingLocal;

            public Vector3 EnvelopeSizeMetres;
            public Vector3 EnvelopeCentreLocal;
            public Bounds InteriorLocal;
            public IReadOnlyList<SolidPart> SolidParts;

            public VehicleFootprint Footprint
                => VehicleFootprint.Of(EnvelopeSizeMetres, FrontCouplingLocal, RearCouplingLocal);
        }

        [SerializeField, Tooltip("Wheels: where each sits and how big it is")]
        WheelPlacement[] m_Wheels = Array.Empty<WheelPlacement>();

        [SerializeField, Tooltip("Whether anything may hitch in front")]
        bool m_HasFrontCoupling;

        [SerializeField, Tooltip("Front coupling in the vehicle's own space, m")]
        Vector3 m_FrontCouplingLocal;

        [SerializeField, Tooltip("Whether anything may hitch behind")]
        bool m_HasRearCoupling;

        [SerializeField, Tooltip("Rear coupling in the vehicle's own space, m")]
        Vector3 m_RearCouplingLocal;

        [SerializeField, Tooltip("Room it takes up, m")]
        Vector3 m_EnvelopeSizeMetres = Vector3.one;

        [SerializeField, Tooltip("Centre of that room in its own space, m")]
        Vector3 m_EnvelopeCentreLocal;

        [SerializeField, Tooltip("Space kept clear for cargo, m. Empty for anything solid")]
        Bounds m_InteriorLocal;

        [SerializeField, Tooltip("Boxes this is solid in")]
        SolidPart[] m_SolidParts = Array.Empty<SolidPart>();

        public IReadOnlyList<WheelPlacement> Wheels => m_Wheels;

        public bool HasFrontCoupling => m_HasFrontCoupling;

        public bool HasRearCoupling => m_HasRearCoupling;

        public Vector3? FrontCouplingLocal => m_HasFrontCoupling ? m_FrontCouplingLocal : (Vector3?)null;

        public Vector3? RearCouplingLocal => m_HasRearCoupling ? m_RearCouplingLocal : (Vector3?)null;

        public VehicleFootprint Footprint
            => VehicleFootprint.Of(m_EnvelopeSizeMetres, FrontCouplingLocal, RearCouplingLocal);

        public float FrontReachMetres => Footprint.FrontReachMetres;

        public float RearReachMetres => Footprint.RearReachMetres;

        public Vector3 EnvelopeSizeMetres => m_EnvelopeSizeMetres;

        public Vector3 EnvelopeCentreLocal => m_EnvelopeCentreLocal;

        public Bounds InteriorLocal => m_InteriorLocal;

        public IReadOnlyList<SolidPart> SolidParts => m_SolidParts;

        const float LookingEitherSideOfTheFaceMetres = 0.25f;

        const float ShortOfTheCorners = 0.9f;

        const float StandsOnTheFloorWithin = 0.05f;

        LoadSpaceEdge[] m_LoadSpaceEdges;
        Bounds m_EdgesMeasuredFrom;

        public IReadOnlyList<LoadSpaceEdge> LoadSpaceEdges
        {
            get
            {
                if (m_LoadSpaceEdges != null && m_EdgesMeasuredFrom == m_InteriorLocal)
                {
                    return m_LoadSpaceEdges;
                }

                m_EdgesMeasuredFrom = m_InteriorLocal;
                m_LoadSpaceEdges = EdgesOf(m_InteriorLocal);

                return m_LoadSpaceEdges;
            }
        }

        LoadSpaceEdge[] EdgesOf(Bounds loadSpace)
        {
            if (loadSpace.size.x <= 0f || loadSpace.size.y <= 0f || loadSpace.size.z <= 0f)
            {
                return Array.Empty<LoadSpaceEdge>();
            }

            var floor = loadSpace.min.y;

            return new[]
            {
                EdgeFacing(loadSpace, Vector3.left, loadSpace.min.x, loadSpace.size.z, floor),
                EdgeFacing(loadSpace, Vector3.right, loadSpace.max.x, loadSpace.size.z, floor),
                EdgeFacing(loadSpace, Vector3.back, loadSpace.min.z, loadSpace.size.x, floor),
                EdgeFacing(loadSpace, Vector3.forward, loadSpace.max.z, loadSpace.size.x, floor)
            };
        }

        LoadSpaceEdge EdgeFacing(Bounds loadSpace, Vector3 outward, float face, float length, float floor)
        {
            var acrossX = Mathf.Abs(outward.x) > 0.5f;

            var middle = new Vector3(
                acrossX ? face : loadSpace.center.x,
                floor,
                acrossX ? loadSpace.center.z : face);

            var clearOfTheCorners = length * ShortOfTheCorners;

            var straddling = new Bounds(
                new Vector3(middle.x, loadSpace.center.y, middle.z),
                new Vector3(
                    acrossX ? LookingEitherSideOfTheFaceMetres * 2f : clearOfTheCorners,
                    loadSpace.size.y,
                    acrossX ? clearOfTheCorners : LookingEitherSideOfTheFaceMetres * 2f));

            var top = floor;

            foreach (var part in m_SolidParts)
            {
                var box = TheRoomAPartTakesUp(part);

                if (box.min.y > floor + StandsOnTheFloorWithin || !box.Intersects(straddling))
                {
                    continue;
                }

                top = Mathf.Max(top, box.max.y);
            }

            return new LoadSpaceEdge(middle, outward, top, floor, length, loadSpace.max.y);
        }

        public float WheelbaseMetres => SpanAlong(axis => axis.z);

        public float TrackMetres => SpanAlong(axis => axis.x);

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

        public static Bounds TheRoomAPartTakesUp(SolidPart part)
        {
            if (!part.IsAPieceOfTheModel)
            {
                return new Bounds(part.CentreLocal, part.SizeMetres);
            }

            var piece = part.Piece.bounds;
            var room = new Bounds(part.CentreLocal, Vector3.zero);

            for (var corner = 0; corner < 8; corner++)
            {
                var at = piece.center + Vector3.Scale(
                    piece.extents,
                    new Vector3(
                        (corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f));

                room.Encapsulate(
                    part.CentreLocal + (part.PieceTurn * Vector3.Scale(at, part.PieceScale)));
            }

            return room;
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
