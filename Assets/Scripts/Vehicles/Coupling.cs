using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Hooking a cart onto the back of a train, and unhooking one, while somebody is standing there.
    ///
    /// The rules only. Who owns what, and what a player pressed, live elsewhere -- this says where a
    /// cart has to be put before it can be hitched, and which cart is close enough to be worth
    /// offering.
    /// </summary>
    public static class Coupling
    {
        /// <summary>
        /// How far from the back of a train a cart may be and still be hitched, in metres.
        ///
        /// Generous on purpose. Reversing a three tonne tractor to within a few centimetres of a
        /// cart is not a test of skill, it is a test of patience, and the cart is moved into place
        /// on hitching anyway.
        /// </summary>
        public const float ReachMetres = 6f;

        /// <summary>
        /// Where a cart has to stand to be hitched behind this vehicle, so that the two hitch points
        /// land on the same spot.
        ///
        /// This matters more than it looks. The joint anchors each end at its own vehicle's hitch,
        /// and at this distance those two points coincide, so the coupling begins life already
        /// satisfied with nothing to pull against. Created while the cart is somewhere else, the
        /// joint starts out violated and never stops trying to close -- which drags the whole train
        /// sideways across the apron for the rest of the session. That bug has been fixed here once
        /// already, for parked trains; hitching at runtime is the second way in.
        /// </summary>
        public static (Vector3 Position, Quaternion Rotation) WhereToStand(
            VehicleController behind, VehicleController cart)
        {
            var facing = behind.transform.rotation;

            // Worked back from where the two hitches have to meet rather than by measuring along the
            // ground. A tractor and a cart settle at different heights on their own suspension, so
            // their hitch points sit at different offsets within their own bodies -- placing the
            // cart at the tractor's height leaves the two hitches a few centimetres apart
            // vertically, which is enough for the joint to start out violated.
            var meetHere = behind.transform.TransformPoint(behind.RearHitchLocal);

            return (meetHere - (facing * cart.FrontHitchLocal), facing);
        }

        /// <summary>
        /// The nearest vehicle that could be hitched to the back of this train, or null.
        ///
        /// A candidate has to be a vehicle nobody is driving, not already part of a train of more
        /// than itself, not this train, and within reach of the back of it.
        /// </summary>
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
                if (away < nearestDistance && away <= ReachMetres)
                {
                    nearest = candidate;
                    nearestDistance = away;
                }
            }

            return nearest;
        }

        /// <summary>Whether this vehicle is free to be hooked onto the back of that train.</summary>
        public static bool CanBeHitched(VehicleController candidate, CartChain train)
        {
            if (candidate == null || train == null || train.Contains(candidate))
            {
                return false;
            }

            if (candidate.Occupied)
            {
                // Hitching a tractor somebody is sitting in would leave two drivers on one train
                // pulling against each other.
                return false;
            }

            // A cart in the middle of another train cannot be taken without breaking that one.
            return candidate.Chain == null || candidate.Chain.Members.Count == 1;
        }

        /// <summary>
        /// Whether this train can be unhooked behind the given member.
        ///
        /// Not behind the last one, because there is nothing there, and not in front of the first,
        /// because a train with no tractor at the head of it is not a train anybody can move.
        /// </summary>
        public static bool CanBeSplitAfter(CartChain train, int memberIndex)
            => train != null && memberIndex >= 0 && memberIndex < train.Members.Count - 1;
    }
}
