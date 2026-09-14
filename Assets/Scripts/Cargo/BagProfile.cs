using UnityEngine;

namespace BelowTheWing.Cargo
{
    [CreateAssetMenu(menuName = "Below the Wing/Bag Profile", fileName = "BagProfile")]
    public sealed class BagProfile : ScriptableObject
    {
        [Header("Mass and scale (real-world)")]
        [Tooltip("Mass, kg")]
        public float massKg = 20f;

        [Tooltip("Width, height, length, m")]
        public Vector3 sizeMetres = new Vector3(0.4f, 0.25f, 0.6f);

        [Header("Grip")]
        [Tooltip("Friction against what it lies on, 0 to 1")]
        public float frictionCoefficient = 0.3f;
    }
}
