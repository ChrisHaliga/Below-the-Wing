using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class Coupling
    {
        public const float OffersWithinMetres = 6f;

        public static (Vector3 Position, Quaternion Rotation)? WhereToStand(
            VehicleController behind, VehicleController cart)
        {
            var theirEnd = behind.RearHitchLocal;
            var ourEnd = cart.FrontHitchLocal;

            if (theirEnd == null || ourEnd == null)
            {
                return null;
            }

            var facing = behind.transform.rotation;

            var meetHere = behind.transform.TransformPoint(theirEnd.Value);

            return (meetHere - (facing * ourEnd.Value), facing);
        }

        public static VehicleController WorthHitching(
            CartChain train, IReadOnlyList<VehicleController> nearby)
        {
            if (train == null)
            {
                return null;
            }

            var back = train.Members[train.Members.Count - 1];
            var from = back.transform.position - (back.transform.rotation * Vector3.forward * back.RearReach);

            VehicleController nearest = null;
            var nearestDistance = float.MaxValue;

            foreach (var candidate in nearby)
            {
                if (!CanBeHitched(candidate, train))
                {
                    continue;
                }

                var away = Vector3.Distance(candidate.transform.position, from);
                if (away < nearestDistance && away <= OffersWithinMetres)
                {
                    nearest = candidate;
                    nearestDistance = away;
                }
            }

            return nearest;
        }

        public static bool CanBeHitched(VehicleController candidate, CartChain train)
        {
            if (candidate == null || train == null || train.Contains(candidate))
            {
                return false;
            }

            if (!candidate.CanBeTowed)
            {
                return false;
            }

            if (candidate.Occupied)
            {
                return false;
            }

            return candidate.Chain == null || candidate.Chain.Members.Count == 1;
        }

        public static bool CanBeSplitAfter(CartChain train, int memberIndex)
            => train != null && memberIndex >= 0 && memberIndex < train.Members.Count - 1;
    }
}
