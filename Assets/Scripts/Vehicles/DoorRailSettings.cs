using System;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    [Serializable]
    public struct DoorRailSettings
    {
        [Tooltip("Mass of the pole a hand pushes, kg")]
        public float poleKg;

        [Tooltip("Drag along the rail, N per m/s")]
        public float dragNewtonsPerMetrePerSecond;

        [Tooltip("Most the rail can hold against, N")]
        public float holdsAtNewtons;

        [Tooltip("Share of its speed a door keeps off the end stop, 0 to 1")]
        public float bounceOffTheEnd;

        [Tooltip("Pull toward whichever end the door was last sent to, N/m. Zero for none")]
        public float settlesAtNewtonsPerMetre;

        [Tooltip("Position error the solver snaps shut, m")]
        public float projectionDistanceMetres;

        [Tooltip("Angle error the solver snaps shut, degrees")]
        public float projectionAngleDegrees;

        public static DoorRailSettings Default => new DoorRailSettings
        {
            poleKg = 6f,
            dragNewtonsPerMetrePerSecond = 40f,
            holdsAtNewtons = 1500f,
            bounceOffTheEnd = 0f,
            settlesAtNewtonsPerMetre = 0f,
            projectionDistanceMetres = 0.005f,
            projectionAngleDegrees = 0.5f
        };
    }
}
