using UnityEngine;

namespace BelowTheWing.Apron
{
    [CreateAssetMenu(menuName = "Below the Wing/Aircraft Profile", fileName = "AircraftProfile")]
    public sealed class AircraftProfile : ScriptableObject
    {
        [Header("What this represents")]
        [TextArea(2, 4)]
        public string equipmentNote = "";

        [Tooltip("Mass, kg")]
        public float massKg = 41400f;

        [Tooltip("Length, m")]
        public float lengthMetres = 39.5f;

        [Tooltip("Diameter, m")]
        public float fuselageDiameterMetres = 3.76f;

        [Tooltip("Height above the subject, m")]
        public float centrelineHeightMetres = 3.4f;
    }
}
