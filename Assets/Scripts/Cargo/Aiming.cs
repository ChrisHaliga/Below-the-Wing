using System;
using UnityEngine;

namespace BelowTheWing.Cargo
{
    public static class Aiming
    {
        const int MostWithinReach = 32;

        static readonly Collider[] Nearby = new Collider[MostWithinReach];

        public static Collider At(
            Ray aim, Vector3 reachingFrom, float reachMetres, float looksMetres, float coneDegrees,
            Func<Collider, bool> worthIt)
        {
            if (Physics.Raycast(aim, out var looked, looksMetres, Physics.AllLayers, QueryTriggerInteraction.Ignore)
                && worthIt(looked.collider))
            {
                return looked.collider;
            }

            Collider best = null;
            var narrowest = coneDegrees;

            var found = Physics.OverlapSphereNonAlloc(
                reachingFrom, reachMetres, Nearby, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            for (var i = 0; i < found; i++)
            {
                var candidate = Nearby[i];

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
