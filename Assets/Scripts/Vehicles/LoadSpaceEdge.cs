using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public readonly struct LoadSpaceEdge
    {
        public readonly Vector3 MiddleLocal;

        public readonly Vector3 OutwardLocal;

        public readonly float TopMetres;

        public readonly float FloorMetres;

        public readonly float LengthMetres;

        public readonly float CeilingMetres;

        public LoadSpaceEdge(
            Vector3 middleLocal, Vector3 outwardLocal, float topMetres, float floorMetres,
            float lengthMetres, float ceilingMetres)
        {
            MiddleLocal = middleLocal;
            OutwardLocal = outwardLocal;
            TopMetres = topMetres;
            FloorMetres = floorMetres;
            LengthMetres = lengthMetres;
            CeilingMetres = ceilingMetres;
        }

        public float HeightAboveTheFloor => TopMetres - FloorMetres;

        public float WayInMetres => CeilingMetres - TopMetres;
    }
}
