using System;
using UnityEngine;

namespace BelowTheWing.Wiring
{
    public sealed class MisbuiltException : InvalidOperationException
    {
        public MisbuiltException(string message) : base(message)
        {
        }

        public static MisbuiltException For(UnityEngine.Object thing, string lacks)
            => new MisbuiltException($"'{thing.name}' {lacks}.");

        public static MisbuiltException Refuse(Behaviour thing, string lacks)
        {
            thing.enabled = false;

            return For(thing, lacks);
        }
    }
}
