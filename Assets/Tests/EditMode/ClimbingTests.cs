using BelowTheWing.Crew;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class ClimbingTests
    {
        const float Gravity = 9.81f;

        static float RisesTo(float speed) => speed * speed / (2f * Gravity);

        [Test]
        public void ThePushOffReachesTheTopOfTheEdgeAndNoHigher()
        {
            var up = Climbing.StraightUpFor(riseMetres: 0.8f, Gravity);

            Assert.That(RisesTo(up), Is.EqualTo(0.8f).Within(0.001f),
                $"they push off hard enough to reach {RisesTo(up):F2} m and the edge stands 0.8 m " +
                "above their feet. Short of it they rise against the side of the cart and slide " +
                "back down it; far over it they clear the far lip as well and land on the tarmac " +
                "on the other side");
        }

        [Test]
        public void AnEdgeLevelWithTheirFeetIsStillAPushOff()
        {
            var up = Climbing.StraightUpFor(riseMetres: 0f, Gravity);

            Assert.That(up, Is.GreaterThan(0f),
                "an edge no higher than their feet asks for no rise at all. Taken literally that " +
                "is a climb that never leaves the ground, and the pull inward never comes because " +
                "it waits for them to be above the edge");
        }

        [Test]
        public void AHigherEdgeTakesAHarderPushOff()
        {
            Assert.That(
                Climbing.StraightUpFor(1.2f, Gravity),
                Is.GreaterThan(Climbing.StraightUpFor(0.4f, Gravity)),
                "a taller edge takes more of a push to get over");
        }

        static float PushOffTargetFor(float edgeTop, float ceiling, float ducked)
            => Mathf.Min(edgeTop + Climbing.OverTheTopMetres, ceiling - ducked);

        [Test]
        public void ALowRoofDecidesThePushOffRatherThanTheEdge()
        {
            const float edgeTop = 0.65f;
            const float ducked = 1.2f;

            var underACartsRoof = PushOffTargetFor(edgeTop, ceiling: 1.7f, ducked);

            Assert.That(underACartsRoof, Is.LessThan(edgeTop + Climbing.OverTheTopMetres),
                "a cart is a box with a roof on it, and the way in is the gap between the lip and " +
                "that roof. Where the roof is the tighter of the two, rising to clear the lip puts " +
                "a ducked head into the underside of it, which throws them back out and shoves the " +
                "cart sideways");
            Assert.That(underACartsRoof + ducked, Is.LessThanOrEqualTo(1.7f + 0.001f));
        }

        [Test]
        public void AHighRoofLeavesTheEdgeToDecideIt()
        {
            const float edgeTop = 0.65f;

            var target = PushOffTargetFor(edgeTop, ceiling: 4f, ducked: 1.2f);

            Assert.That(target, Is.EqualTo(edgeTop + Climbing.OverTheTopMetres).Within(0.001f),
                "with room to spare overhead there is nothing to duck under, and a push-off held " +
                "down to a roof that is nowhere near leaves them short of the lip they are trying " +
                "to get over");
        }

        [Test]
        public void ABaggageCartsOwnFiguresLeaveADuckedHeadUnderItsRoof()
        {
            const float edgeTop = 0.6527f;
            const float ceiling = 2.0987f;
            const float ducked = 1.2f;
            const float feet = 0.05f;

            var target = PushOffTargetFor(edgeTop, ceiling, ducked);
            var head = RisesTo(Climbing.StraightUpFor(target - feet, Gravity)) + feet + ducked;

            Assert.That(head, Is.LessThanOrEqualTo(ceiling),
                $"on the cart as measured, a ducked head reaches {head:F2} m and the roof is at " +
                $"{ceiling} m. This is the case the whole climb exists for, so it is the one worth " +
                "pinning against the real figures rather than made-up ones");
            Assert.That(target, Is.GreaterThan(edgeTop),
                "and they still get over the lip");
        }

        [Test]
        public void NothingIsHauledUntilSomethingIsTakenHoldOf()
        {
            var climbing = new Climbing();

            Assert.That(climbing.Hauling, Is.Null);
            Assert.That(
                climbing.Haul(Vector3.zero, Vector3.zero, 0.3f, 0.02f, Gravity), Is.EqualTo(Vector3.zero),
                "a person who never started a climb must not be pulled sideways in mid-air by one");
        }
    }
}
