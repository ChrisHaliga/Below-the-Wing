using UnityEngine;

namespace BelowTheWing.Cargo
{
    /// <summary>
    /// What a piece of baggage is physically.
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

        [Header("Grip")]
        [Tooltip("How much grip it has on whatever it lies on, as a coefficient of friction. A " +
                 "hard-shell case on a steel deck is about 0.3. This is the dial that decides how " +
                 "hard a corner has to be taken to throw a load, because a cart's tyres will not " +
                 "let it corner harder than about eight metres per second squared and a bag that " +
                 "grips better than that never comes off.")]
        public float frictionCoefficient = 0.3f;

    }
}
