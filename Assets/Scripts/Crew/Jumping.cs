using UnityEngine;

namespace BelowTheWing.Crew
{
    /// <summary>
    /// Getting off the ground.
    ///
    /// Crew are rigidbodies rather than character controllers, because being run over is part of the
    /// game and a controller cannot be sent flying. So a jump is an impulse and a check that there
    /// is something underneath, rather than a call into a controller that knows what the ground is.
    ///
    /// What is underneath matters more than it sounds. A player standing on a cart has to be able to
    /// jump off it, which means the check cannot ask about the apron -- it has to ask whether there
    /// is anything at all below, moving or not.
    /// </summary>
    public static class Jumping
    {
        /// <summary>
        /// How fast to leave the ground to reach a given height, in metres per second.
        ///
        /// Worked out from gravity rather than written down, so that changing how high a player can
        /// jump is a question about height -- which anybody can picture against a cart deck -- and
        /// not about an impulse nobody can.
        /// </summary>
        public static float TakeOffSpeed(float heightMetres, float gravity)
            => Mathf.Sqrt(2f * Mathf.Max(0f, gravity) * Mathf.Max(0f, heightMetres));

        /// <summary>
        /// Whether there is something close enough below to push off.
        ///
        /// The distance allowed is small and deliberate: too generous and a player can jump again
        /// part way up, which reads as a double jump nobody asked for.
        /// </summary>
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
