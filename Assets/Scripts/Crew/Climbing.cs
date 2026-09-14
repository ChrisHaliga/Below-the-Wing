using BelowTheWing.Vehicles;
using UnityEngine;

namespace BelowTheWing.Crew
{
    public sealed class Climbing
    {
        public readonly struct Arc
        {
            public readonly float Up;

            public readonly float Inward;

            public Arc(float up, float inward)
            {
                Up = up;
                Inward = inward;
            }
        }

        public const string WhatToPress = "Press space to climb in";

        public ClimbZone Offered { get; private set; }

        public string Message => Offered != null ? WhatToPress : "";

        public void Look(Vector3 standingAt, float canGetOverMetres, int zones)
        {
            Offered = null;
        }

        public static Arc Launch(float riseMetres, float inwardMetres, float gravity)
            => new Arc(0f, 0f);
    }
}
