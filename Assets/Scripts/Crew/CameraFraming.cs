using System;
using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// How the camera sits on what it is following: how far back, how high it aims, and how far it
    /// may tip. One of these for being on foot and one for driving.
    /// </summary>
    [Serializable]
    public struct CameraFraming : IEquatable<CameraFraming>
    {
        [Tooltip("Metres back from the point aimed at. Zero puts the camera at that point.")]
        public float DistanceMetres;

        [Tooltip("Metres above the subject's origin that the camera aims at.")]
        public float HeightMetres;

        [Tooltip("How far the camera may look down, in degrees below level.")]
        public float MinPitchDegrees;

        [Tooltip("How far the camera may look up, in degrees above level.")]
        public float MaxPitchDegrees;

        /// <summary>First person: in the character's head, free to look almost straight up or down.</summary>
        public static CameraFraming OnFoot(float eyeMetresAboveOrigin) => new CameraFraming
        {
            DistanceMetres = 0f,
            HeightMetres = eyeMetresAboveOrigin,
            MinPitchDegrees = -80f,
            MaxPitchDegrees = 80f
        };

        /// <summary>
        /// The framing for what the player is in charge of: in their own head on foot, behind the
        /// vehicle when driving one.
        /// </summary>
        public static CameraFraming For(bool driving, float eyeMetresAboveOrigin)
            => driving ? Driving : OnFoot(eyeMetresAboveOrigin);

        /// <summary>Third person, close behind a vehicle.</summary>
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
