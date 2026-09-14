using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ClimbZone : MonoBehaviour
    {
        public const string ZonesName = "Climb";

        LoadSpaceEdge m_Edge;
        VehicleController m_Vehicle;

        public LoadSpaceEdge Edge => m_Edge;

        public VehicleController Vehicle => m_Vehicle;

        public void Describe(VehicleController vehicle, LoadSpaceEdge edge)
        {
            m_Vehicle = vehicle;
            m_Edge = edge;
        }

        public Vector3 TopOfTheEdge
            => m_Vehicle != null
                ? m_Vehicle.transform.TransformPoint(
                    new Vector3(m_Edge.MiddleLocal.x, m_Edge.TopMetres, m_Edge.MiddleLocal.z))
                : transform.position;

        public Vector3 Inward
            => m_Vehicle != null
                ? m_Vehicle.transform.TransformDirection(-m_Edge.OutwardLocal)
                : transform.forward;
    }
}
