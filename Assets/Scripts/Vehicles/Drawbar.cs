using BelowTheWing.Cargo;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class Drawbar
    {
        public const string BarName = "Drawbar";

        const float ThicknessMetres = 0.12f;

        public readonly struct Reach
        {
            public Reach(Vector3 pivotLocal, float reachesMetres)
            {
                PivotLocal = pivotLocal;
                ReachesMetres = reachesMetres;
            }

            public Vector3 PivotLocal { get; }

            public float ReachesMetres { get; }

            public float LengthMetres => Mathf.Abs(ReachesMetres);
        }

        public static Reach Spans(Vector3 couplingLocal, float noseAtZ)
            => new Reach(
                new Vector3(couplingLocal.x, couplingLocal.y, noseAtZ), couplingLocal.z - noseAtZ);

        public static bool CanBePulled(bool parked, bool hitched) => !parked && !hitched;

        public static Transform Build(GameObject vehicle, VehicleShape shape, PhysicsMaterial bodywork)
        {
            var already = vehicle.transform.Find(BarName);

            if (already != null)
            {
                return already;
            }

            if (shape == null || !shape.HasFrontCoupling)
            {
                return null;
            }

            var coupling = shape.FrontCouplingLocal.Value;
            var nose = shape.EnvelopeCentreLocal.z
                       + (Mathf.Sign(coupling.z) * shape.EnvelopeSizeMetres.z * 0.5f);

            var reach = Spans(coupling, nose);

            if (reach.LengthMetres < 0.05f)
            {
                return null;
            }

            var pivot = new GameObject(BarName).transform;
            pivot.SetParent(vehicle.transform, worldPositionStays: false);
            pivot.localPosition = reach.PivotLocal;

            var bar = new GameObject("Bar");
            bar.transform.SetParent(pivot, worldPositionStays: false);
            bar.transform.localPosition = new Vector3(0f, 0f, reach.ReachesMetres * 0.5f);

            var grab = bar.AddComponent<BoxCollider>();
            grab.size = new Vector3(ThicknessMetres, ThicknessMetres, reach.LengthMetres);
            grab.sharedMaterial = bodywork;

            bar.AddComponent<HandUse>().As = HandUse.Category.HoldOnto;

            return pivot;
        }
    }
}
