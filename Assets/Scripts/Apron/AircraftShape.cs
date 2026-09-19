using UnityEngine;

namespace BelowTheWing.Apron
{
    [DisallowMultipleComponent]
    public sealed class AircraftShape : MonoBehaviour
    {
        [SerializeField, Tooltip("Room it takes up, m, measured from the model")]
        Vector3 m_EnvelopeSizeMetres = Vector3.one;

        [SerializeField, Tooltip("Centre of that room in its own space, m")]
        Vector3 m_EnvelopeCentreLocal;

        public Vector3 EnvelopeSizeMetres => m_EnvelopeSizeMetres;

        public Vector3 EnvelopeCentreLocal => m_EnvelopeCentreLocal;

        public void Describe(Vector3 sizeMetres, Vector3 centreLocal)
        {
            m_EnvelopeSizeMetres = sizeMetres;
            m_EnvelopeCentreLocal = centreLocal;
        }
    }
}
