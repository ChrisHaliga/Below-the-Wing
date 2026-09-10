using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    /// <summary>
    /// A vehicle a player might be able to take control of.
    ///
    /// Whether a particular vehicle can be taken right now is the vehicle's own business rather
    /// than the asking player's: a baggage cart is towed and never driven, and a tractor somebody
    /// else is already sitting in has no room for a second driver. Both answer
    /// <see cref="AcceptsDriver"/> with false, and neither is offered to anyone.
    /// </summary>
    public interface IDriveable
    {
        /// <summary>The name a player sees when offered this vehicle, such as "Tug 1".</summary>
        string DisplayName { get; }

        /// <summary>Where the vehicle is, for working out which one a player is standing nearest.</summary>
        Vector3 Position { get; }

        /// <summary>Whether this vehicle would take a driver if one asked right now.</summary>
        bool AcceptsDriver { get; }
    }

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
        public static IDriveable Nearest(Vector3 from, float radiusMetres, IReadOnlyList<IDriveable> candidates)
        {
            IDriveable nearest = null;
            var nearestDistance = radiusMetres;

            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.AcceptsDriver)
                {
                    continue;
                }

                var distance = Vector3.Distance(from, candidate.Position);
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
