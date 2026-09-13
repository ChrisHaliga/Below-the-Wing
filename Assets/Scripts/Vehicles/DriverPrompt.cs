using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// Choosing which vehicle to offer a player who is standing on the apron.
    ///
    /// Kept apart from the component that draws the prompt so that the choice can be checked
    /// directly: near nothing, near one thing, near two things at once.
    /// </summary>
    public static class DriverPrompt
    {
        /// <summary>
        /// The vehicle to offer, or null if there is nothing worth offering.
        ///
        /// Only vehicles within <paramref name="radiusMetres"/> that would accept a driver are
        /// considered, and of those the nearest wins, so that standing between a tractor and its
        /// cart offers the one you are actually next to.
        /// </summary>
        public static VehicleController Nearest(Vector3 from, float radiusMetres, IReadOnlyList<VehicleController> candidates)
        {
            VehicleController nearest = null;
            var nearestDistance = radiusMetres;

            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.AcceptsDriver)
                {
                    continue;
                }

                var distance = Vector3.Distance(from, candidate.transform.position);
                if (distance > nearestDistance)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = distance;
            }

            return nearest;
        }
    }
}
