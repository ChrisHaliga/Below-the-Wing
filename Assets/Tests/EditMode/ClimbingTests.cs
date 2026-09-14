using BelowTheWing.Crew;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class ClimbingTests
    {
        const float Gravity = 9.81f;

        [Test]
        public void AnArcThatReachesTheTopOfTheEdgeAndNoHigher()
        {
            var launch = Climbing.Launch(riseMetres: 0.8f, inwardMetres: 1f, gravity: Gravity);

            var reached = launch.Up * launch.Up / (2f * Gravity);

            Assert.That(reached, Is.EqualTo(0.8f).Within(0.001f),
                "the arc has to peak at the height of the edge. Short and they climb into the side " +
                $"of the cart; this one peaks at {reached:F2} m");
        }

        [Test]
        public void PastTheEdgeByTheTimeTheyAreOverIt()
        {
            var launch = Climbing.Launch(riseMetres: 0.8f, inwardMetres: 1.2f, gravity: Gravity);

            var toTheTop = launch.Up / Gravity;
            var travelled = launch.Inward * toTheTop;

            Assert.That(travelled, Is.EqualTo(1.2f).Within(0.001f),
                "they have to be over the edge at the top of the arc, not still outside it. This " +
                $"one is {travelled:F2} m in when it starts to fall, and the edge is 1.2 m away");
        }

        [Test]
        public void AnEdgeNoHigherThanTheirFeetIsStillAHopRatherThanADivisionByZero()
        {
            var launch = Climbing.Launch(riseMetres: 0f, inwardMetres: 1f, gravity: Gravity);

            Assert.That(launch.Up, Is.GreaterThan(0f),
                "a flat edge asks for no rise at all, and an arc with no height takes no time, so " +
                "the speed across the ground is a division by zero. A hop is the honest answer");
            Assert.That(launch.Inward, Is.GreaterThan(0f).And.LessThan(100f),
                "and the speed across the ground has to stay a speed");
        }

        [Test]
        public void AHigherEdgeIsLeftSlower()
        {
            var low = Climbing.Launch(riseMetres: 0.4f, inwardMetres: 1f, gravity: Gravity);
            var high = Climbing.Launch(riseMetres: 1.2f, inwardMetres: 1f, gravity: Gravity);

            Assert.That(high.Up, Is.GreaterThan(low.Up),
                "a taller edge needs more of a launch to clear");
            Assert.That(high.Inward, Is.LessThan(low.Inward),
                "and a taller edge takes longer to get over, so the same distance is covered more " +
                "slowly. A climb that keeps the same speed across the ground overshoots a tall " +
                "edge and lands on the far lip");
        }
    }
}
