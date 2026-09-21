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

        [Tooltip("Steady pull toward whichever end the door is nearer, N. Zero for none")]
        public float seatsAtNewtons;

        [Tooltip("Force needed to drag a seated door off its end, N. Zero for no latch and no hook")]
        public float latchHoldsAtNewtons;

        [Tooltip("Shake of the cart that throws a shut door's hook off, m/s^2. Zero leaves only a hand")]
        public float unhooksAboveMetresPerSecondSquared;

        [Tooltip("How near an end counts as seated, as a share of the travel")]
        public float seatedWithinFraction;

        [Tooltip("How hard a held door follows the hand along its rail, N/m. Zero for none")]
        public float followsAHandNewtonsPerMetre;

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
            seatsAtNewtons = 200f,
            latchHoldsAtNewtons = 800f,
            unhooksAboveMetresPerSecondSquared = 25f,
            seatedWithinFraction = 0.04f,
            followsAHandNewtonsPerMetre = 4000f,
            projectionDistanceMetres = 0.005f,
            projectionAngleDegrees = 0.5f
        };
    }
}
