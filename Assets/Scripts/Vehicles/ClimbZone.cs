using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ClimbZone : MonoBehaviour
    {
        public const string ZonesName = "Climb";

        public const float StandingWithinMetres = 0.9f;

        LoadSpaceEdge m_Edge;
        VehicleController m_Vehicle;

        public LoadSpaceEdge Edge => m_Edge;

        public VehicleController Vehicle => m_Vehicle;

        public void Describe(VehicleController vehicle, LoadSpaceEdge edge)
        {
            m_Vehicle = vehicle;
            m_Edge = edge;
        }

        public Vector3 TopOfTheEdge => Somewhere(m_Edge.TopMetres);

        public Vector3 FloorOfTheLoadSpace => Somewhere(m_Edge.FloorMetres);

        public Vector3 CeilingOfTheLoadSpace => Somewhere(m_Edge.CeilingMetres);

        public Vector3 Inward
            => m_Vehicle != null
                ? m_Vehicle.transform.TransformDirection(-m_Edge.OutwardLocal)
                : transform.forward;

        Vector3 Somewhere(float height)
            => m_Vehicle != null
                ? m_Vehicle.transform.TransformPoint(
                    new Vector3(m_Edge.MiddleLocal.x, height, m_Edge.MiddleLocal.z))
                : transform.position;

        public static void BuildOn(VehicleController vehicle, VehicleShape shape)
        {
            var existing = vehicle.transform.Find(ZonesName);
            if (existing != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existing.gameObject);
                }
                else
                {
                    DestroyImmediate(existing.gameObject);
                }
            }

            if (shape == null || shape.LoadSpaceEdges.Count == 0)
            {
                return;
            }

            var zones = new GameObject(ZonesName);
            zones.transform.SetParent(vehicle.transform, worldPositionStays: false);

            foreach (var edge in shape.LoadSpaceEdges)
            {
                var zone = new GameObject($"Edge {edge.OutwardLocal}");
                zone.transform.SetParent(zones.transform, worldPositionStays: false);

                var reachable = edge.TopMetres;
                var along = Mathf.Abs(edge.OutwardLocal.x) > 0.5f
                    ? new Vector3(StandingWithinMetres, reachable, edge.LengthMetres)
                    : new Vector3(edge.LengthMetres, reachable, StandingWithinMetres);

                var box = zone.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = along;
                box.center = Vector3.zero;

                zone.transform.localPosition = edge.MiddleLocal
                                               + (edge.OutwardLocal * (StandingWithinMetres * 0.5f))
                                               + (Vector3.up * (reachable * 0.5f));

                zone.AddComponent<ClimbZone>().Describe(vehicle, edge);
            }
        }
    }
}
