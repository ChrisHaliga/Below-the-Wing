using UnityEngine;

namespace BelowTheWing.Vehicles
{
    public static class FrontAxle
    {
        public static float PointsAlongDegrees(
            Vector3 drawbarLocal, float mostItSwingsDegrees)
        {
            var flat = new Vector2(drawbarLocal.x, drawbarLocal.z);

            if (flat.sqrMagnitude < 1e-6f)
            {
                return 0f;
            }

            var off = Mathf.Atan2(flat.x, Mathf.Abs(flat.y)) * Mathf.Rad2Deg;

            return Mathf.Clamp(off, -mostItSwingsDegrees, mostItSwingsDegrees);
        }
    }
}
