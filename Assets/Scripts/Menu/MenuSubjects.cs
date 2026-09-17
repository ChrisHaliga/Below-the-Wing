using System.Collections.Generic;
using BelowTheWing.Apron;
using UnityEngine;

namespace BelowTheWing.Menu
{
    public static class MenuSubjects
    {
        public const float WholeApronDegreesPerSecond = 2.5f;
        public const float AirlinerDegreesPerSecond = 4f;
        public const float CrewDegreesPerSecond = 6f;

        public static CameraShot WholeApron(ApronPlan plan)
        {
            var everything = new Bounds(plan.Aircraft.Position, plan.Aircraft.SizeMetres);

            foreach (var train in plan.Trains)
            {
                everything.Encapsulate(train.Tractor.Bounds);

                foreach (var cart in train.Carts)
                {
                    everything.Encapsulate(cart.Bounds);
                }
            }

            return Holding(everything, WholeApronDegreesPerSecond);
        }

        public static CameraShot Airliner(ApronPlan plan)
            => Holding(new Bounds(plan.Aircraft.Position, plan.Aircraft.SizeMetres), AirlinerDegreesPerSecond);

        public static CameraShot Crew(ApronPlan plan, IReadOnlyList<Transform> standing)
        {
            var atTheCart = FirstCart(plan);

            if (standing == null || standing.Count == 0)
            {
                return Holding(atTheCart, CrewDegreesPerSecond);
            }

            var group = new Bounds(standing[0].position, Vector3.one);

            foreach (var crew in standing)
            {
                if (crew != null)
                {
                    group.Encapsulate(new Bounds(crew.position, new Vector3(1f, 2f, 1f)));
                }
            }

            group.Encapsulate(atTheCart);

            return Holding(group, CrewDegreesPerSecond);
        }

        static Bounds FirstCart(ApronPlan plan)
        {
            foreach (var train in plan.Trains)
            {
                foreach (var cart in train.Carts)
                {
                    return cart.Bounds;
                }
            }

            return new Bounds(plan.Aircraft.Position, Vector3.one);
        }

        static CameraShot Holding(Bounds what, float degreesPerSecond)
            => CameraShot.Framing(
                what,
                Settings.CrewSettings.FieldOfViewDegrees,
                Mathf.Max(Screen.width / (float)Mathf.Max(Screen.height, 1), 0.5f),
                degreesPerSecond);
    }
}
