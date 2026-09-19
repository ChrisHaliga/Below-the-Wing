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
        public float massKg = 21523f;
    }
}
