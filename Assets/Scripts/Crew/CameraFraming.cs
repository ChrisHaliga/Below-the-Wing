using System;
using UnityEngine;

namespace BelowTheWing.Crew
{
    [Serializable]
    public struct CameraFraming : IEquatable<CameraFraming>
    {
        [Tooltip("Distance behind the subject, m")]
        public float DistanceMetres;

        [Tooltip("Height above the subject, m")]
        public float HeightMetres;

        [Tooltip("Lowest pitch, degrees")]
        public float MinPitchDegrees;

        [Tooltip("Highest pitch, degrees")]
        public float MaxPitchDegrees;

        public static CameraFraming OnFoot(float eyeMetresAboveOrigin) => new CameraFraming
        {
            DistanceMetres = 0f,
            HeightMetres = eyeMetresAboveOrigin,
            MinPitchDegrees = -80f,
            MaxPitchDegrees = 80f
        };

        public static CameraFraming For(bool driving, float eyeMetresAboveOrigin)
            => driving ? Driving : OnFoot(eyeMetresAboveOrigin);

        public static CameraFraming Driving => new CameraFraming
        {
            DistanceMetres = 5f,
            HeightMetres = 1.5f,
            MinPitchDegrees = -20f,
            MaxPitchDegrees = 60f
        };

        public bool Equals(CameraFraming other)
            => Mathf.Approximately(DistanceMetres, other.DistanceMetres)
               && Mathf.Approximately(HeightMetres, other.HeightMetres)
               && Mathf.Approximately(MinPitchDegrees, other.MinPitchDegrees)
               && Mathf.Approximately(MaxPitchDegrees, other.MaxPitchDegrees);

        public override bool Equals(object obj) => obj is CameraFraming other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(DistanceMetres, HeightMetres, MinPitchDegrees, MaxPitchDegrees);

        public override string ToString()
            => $"{DistanceMetres} m back, aimed {HeightMetres} m up, pitch {MinPitchDegrees}..{MaxPitchDegrees}";
    }
}
