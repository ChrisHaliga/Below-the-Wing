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
        float m_TrackMetres;
        float m_OpeningMetres;

        public float Openness { get; private set; }

        public void Runs(
            SkinnedMeshRenderer panel, SkinnedMeshRenderer fabric, Transform cover,
            Vector3 shutAtLocal, Vector3 alongLocal, float trackMetres, float openingMetres)
        {
            m_Panel = panel;
            m_Fabric = fabric;
            m_Cover = cover;
            m_ShutAt = shutAtLocal;
            m_Along = alongLocal.normalized;
            m_TrackMetres = trackMetres;
            m_OpeningMetres = openingMetres;
        }

        void FixedUpdate()
        {
            if (transform.parent == null)
            {
                return;
            }

            Openness = SlidingDoor.OpennessAt(
                Vector3.Dot(transform.localPosition - m_ShutAt, m_Along), m_TrackMetres);

            Show(m_Panel, SlidingDoor.PanelWeight(Openness));
            Show(m_Fabric, SlidingDoor.FabricWeight(Openness));

            if (m_Cover == null)
            {
                return;
            }

            var covered = SlidingDoor.StillCoveredMetres(Openness, m_OpeningMetres);
            var sits = SlidingDoor.CoverSitsAt(Openness, m_OpeningMetres);

            m_Cover.localPosition = m_ShutAt + (m_Along * (sits + (m_OpeningMetres * 0.5f)));
            m_Cover.localScale = new Vector3(0.06f, m_Cover.localScale.y, Mathf.Max(covered, 0.001f));
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
