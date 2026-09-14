using UnityEngine;

namespace BelowTheWing.Crew
{
    public static class Jumping
    {
        public static float TakeOffSpeed(float heightMetres, float gravity)
            => Mathf.Sqrt(2f * Mathf.Max(0f, gravity) * Mathf.Max(0f, heightMetres));

        public static bool StandingOnSomething(
            Vector3 feet, float reachMetres, int groundMask, out Collider what)
        {
            if (Physics.Raycast(feet, Vector3.down, out var hit, reachMetres, groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                what = hit.collider;
                return true;
            }

            what = null;
            return false;
        }
    }
}
