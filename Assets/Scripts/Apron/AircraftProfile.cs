using UnityEngine;

namespace BelowTheWing.Apron
{
    /// <summary>
    /// The aircraft being turned around.
    ///
    /// It is never a dynamic rigidbody. Forty tonnes of airframe barely moves under anything that
    /// happens on the ramp, and simulating it would cost a great deal to achieve almost nothing;
    /// it is kinematic, and the only reason it has a mass here at all is so the figure is written
    /// down honestly next to the equipment that does move.
    /// </summary>
    [CreateAssetMenu(menuName = "Below the Wing/Aircraft Profile", fileName = "AircraftProfile")]
    public sealed class AircraftProfile : ScriptableObject
    {
        [Header("What this represents")]
        [TextArea(2, 4)]
        public string equipmentNote = "";

        [Tooltip("Operating empty mass in kilograms. Recorded for reference; the aircraft is kinematic.")]
        public float massKg = 41400f;

        [Tooltip("Overall length in metres, nose to tail.")]
        public float lengthMetres = 39.5f;

        [Tooltip("Fuselage diameter in metres.")]
        public float fuselageDiameterMetres = 3.76f;

        [Tooltip("Height of the fuselage centreline above the apron, in metres.")]
        public float centrelineHeightMetres = 3.4f;
    }
}
