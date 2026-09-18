using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [DisallowMultipleComponent]
    public sealed class SlidingDoorPole : MonoBehaviour
    {
        SkinnedMeshRenderer m_Panel;
        SkinnedMeshRenderer m_Fabric;
        Transform m_Cover;
        Vector3 m_ShutAt;
        Vector3 m_Along;
        float m_TravelMetres;
        float m_FixedPoleAt;

        public float Openness { get; private set; }

        public Vector3 OpensToward
            => transform.parent != null
                ? transform.parent.TransformDirection(m_Along)
                : m_Along;

        public void Runs(
            SkinnedMeshRenderer panel, SkinnedMeshRenderer fabric, Transform cover,
            Vector3 shutAtLocal, Vector3 alongLocal, float travelMetres, float fixedPoleAt)
        {
            m_Panel = panel;
            m_Fabric = fabric;
            m_Cover = cover;
            m_ShutAt = shutAtLocal;
            m_Along = alongLocal.normalized;
            m_TravelMetres = travelMetres;
            m_FixedPoleAt = fixedPoleAt;
        }

        void FixedUpdate()
        {
            if (transform.parent == null)
            {
                return;
            }

            Openness = SlidingDoor.OpennessAt(
                Vector3.Dot(transform.localPosition - m_ShutAt, m_Along), m_TravelMetres);

            var weight = SlidingDoor.ShapeWeight(Openness);

            Show(m_Panel, weight);
            Show(m_Fabric, weight);

            if (m_Cover == null)
            {
                return;
            }

            var along = transform.localPosition.z;
            var was = m_Cover.localScale;

            m_Cover.localPosition = new Vector3(
                m_ShutAt.x, m_ShutAt.y, SlidingDoor.CoverSitsAt(along, m_FixedPoleAt));

            m_Cover.localScale = new Vector3(
                was.x, was.y, Mathf.Max(SlidingDoor.CoveredMetres(along, m_FixedPoleAt), 0.001f));
        }

        static void Show(SkinnedMeshRenderer on, float weight)
        {
            if (on == null || on.sharedMesh == null || on.sharedMesh.blendShapeCount == 0)
            {
                return;
            }

            on.SetBlendShapeWeight(0, weight);
        }
    }
}
