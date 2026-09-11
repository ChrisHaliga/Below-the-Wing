using UnityEngine;

namespace BelowTheWing.Cargo
{
    /// <summary>
    /// What a piece of baggage is physically, and what it takes to shake one loose.
    ///
    /// A bag is always a single box. Not a compound collider, not a convex mesh, not now and not
    /// later when they have proper shapes: forty of them in a cart is forty contact pairs against
    /// the deck and each other, and every one of those is work the solver does on every machine
    /// whether or not anybody is looking at that cart.
    ///
    /// The weights are real. Airline checked baggage runs from about fifteen kilograms to a limit
    /// of twenty-three, and the ratio between a bag and the things it touches is what decides
    /// whether contact between them is stable at all -- a fifteen kilogram box resting on a three
    /// tonne cart is already at the edge of what a solver resolves cleanly, and anything heavier
    /// touching anything lighter than that is kept apart in the layer matrix rather than tuned
    /// around.
    /// </summary>
    [CreateAssetMenu(menuName = "Below the Wing/Bag Profile", fileName = "BagProfile")]
    public sealed class BagProfile : ScriptableObject
    {
        [Header("Mass and scale (real-world)")]
        [Tooltip("What it weighs in kilograms. Airline checked baggage is 15 to 23.")]
        public float massKg = 20f;

        [Tooltip("Width, height and length in metres.")]
        public Vector3 sizeMetres = new Vector3(0.4f, 0.25f, 0.6f);

        [Header("What it takes to shake one loose")]
        [Tooltip("Sideways acceleration at the bag's own position that throws it off its carrier, " +
                 "in metres per second squared. Lower for a bag standing on end than a flat case.")]
        public float wakeAtLateralAcceleration = 6f;

        [Tooltip("How far a carrier may lean before its cargo lets go, in degrees.")]
        public float wakeAtTiltDegrees = 25f;

        [Tooltip("How hard a carrier has to be hit for its cargo to let go, in newton seconds.")]
        public float wakeAtImpactImpulse = 400f;

        [Tooltip("Seconds after being thrown off before a bag may settle onto a carrier again. " +
                 "Without it a bag flung on a corner sticks straight back down during the same corner.")]
        public float cannotSettleForSeconds = 1f;

        [Header("Damage")]
        [Tooltip("Impact in newton seconds that marks a bag as damaged. No consequence yet beyond " +
                 "being able to see which ones it happened to.")]
        public float damagedAtImpulse = 250f;
    }
}
