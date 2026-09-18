using System.Collections.Generic;
using BelowTheWing.Vehicles;
using BelowTheWing.Wiring;
using UnityEngine;

namespace BelowTheWing.Menu
{
    [DisallowMultipleComponent]
    public sealed class MenuCartDoors : MonoBehaviour
    {
        public const float SwingSeconds = 1.1f;

        [SerializeField, Tooltip("Door panels and their fabric, each slid by its first blend shape")]
        List<SkinnedMeshRenderer> m_Leaves = new List<SkinnedMeshRenderer>();

        float m_Openness;
        float m_Wanted;

        public float Openness => m_Openness;

        public int LeafCount => m_Leaves.Count;

        public static float Toward(float openness, float wanted, float seconds, float step)
            => Mathf.MoveTowards(openness, wanted, seconds <= 0f ? 1f : step / seconds);

        public static float WeightFor(float openness) => SlidingDoor.ShapeWeight(openness);

        public void Open(bool open) => m_Wanted = open ? 1f : 0f;

        void OnEnable()
        {
            foreach (var leaf in m_Leaves)
            {
                if (leaf == null || leaf.sharedMesh == null || leaf.sharedMesh.blendShapeCount == 0)
                {
                    throw MisbuiltException.Refuse(this, "carries a door leaf with no blend shape to slide");
                }
            }

            m_Openness = 0f;
            m_Wanted = 0f;

            Paint();
        }

        void Update()
        {
            m_Openness = Toward(m_Openness, m_Wanted, SwingSeconds, Time.unscaledDeltaTime);

            Paint();
        }

        void Paint()
        {
            var weight = WeightFor(m_Openness);

            foreach (var leaf in m_Leaves)
            {
                leaf.SetBlendShapeWeight(0, weight);
            }
        }
    }
}
