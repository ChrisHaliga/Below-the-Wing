using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Cargo
{
    public static class Aiming
    {
        static readonly List<Collider> Nearby = new List<Collider>();

        public static Collider At(
            Ray aim, Vector3 reachingFrom, float reachMetres, float looksMetres, float coneDegrees,
            Func<Collider, bool> worthIt)
        {
            if (Physics.Raycast(aim, out var looked, looksMetres, ~0, QueryTriggerInteraction.Ignore)
                && worthIt(looked.collider))
            {
                return looked.collider;
            }

            Collider best = null;
            var narrowest = coneDegrees;

            Nearby.Clear();
            Nearby.AddRange(Physics.OverlapSphere(
                reachingFrom, reachMetres, ~0, QueryTriggerInteraction.Ignore));

            foreach (var candidate in Nearby)
            {
                if (!worthIt(candidate))
                {
                    continue;
                }

                var offTheAim = Vector3.Angle(
                    aim.direction, candidate.ClosestPoint(aim.origin) - aim.origin);

                if (offTheAim >= narrowest)
                {
                    continue;
                }

                narrowest = offTheAim;
                best = candidate;
            }

            return best;
        }

        public static T At<T>(Ray aim, float reachMetres, float coneDegrees) where T : Component
        {
            var hit = At(
                aim, aim.origin, reachMetres, reachMetres, coneDegrees,
                c => c.GetComponentInParent<T>() != null);

            return hit != null ? hit.GetComponentInParent<T>() : null;
        }
    }
}
