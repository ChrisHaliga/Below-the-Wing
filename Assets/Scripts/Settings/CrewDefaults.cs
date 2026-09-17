using UnityEngine;

namespace BelowTheWing.Settings
{
    public readonly struct CrewDefaults
    {
        public CrewDefaults(
            FullScreenMode mode, int monitor, float lookSensitivity, bool invertLookY,
            float masterVolume, float effectsVolume, float fieldOfViewDegrees)
        {
            Mode = mode;
            Monitor = monitor;
            LookSensitivity = lookSensitivity;
            InvertLookY = invertLookY;
            MasterVolume = masterVolume;
            EffectsVolume = effectsVolume;
            FieldOfViewDegrees = fieldOfViewDegrees;
        }

        public FullScreenMode Mode { get; }

        public int Monitor { get; }

        public float LookSensitivity { get; }

        public bool InvertLookY { get; }

        public float MasterVolume { get; }

        public float EffectsVolume { get; }

        public float FieldOfViewDegrees { get; }
    }
}
