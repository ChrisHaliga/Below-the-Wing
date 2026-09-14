using System.Collections.Generic;
using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class DriverPrompt
    {
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
