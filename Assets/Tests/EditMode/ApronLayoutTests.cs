using System.Collections.Generic;
using System.Linq;
using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Where everything stands before any of it is built.
    ///
    /// Two things are being checked: that the apron holds what it is supposed to hold, and that no
    /// two things have been put in the same place. The second matters more than it sounds. Two
    /// rigidbodies that begin a session overlapping do not settle into position, they fire apart,
    /// and the resulting mess looks like a physics bug rather than an arithmetic one.
    /// </summary>
    public sealed class ApronLayoutTests
    {
        VehicleProfile m_Tractor;
        VehicleProfile m_Cart;
        AircraftProfile m_Aircraft;
        CrewProfile m_Crew;

        [SetUp]
        public void SetUp()
        {
            m_Tractor = TestProfiles.Tractor();
            m_Cart = TestProfiles.Cart();
            m_Aircraft = TestProfiles.Aircraft();
            m_Crew = TestProfiles.CrewMember();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Tractor);
            Object.DestroyImmediate(m_Cart);
            Object.DestroyImmediate(m_Aircraft);
            Object.DestroyImmediate(m_Crew);
        }

        Vector3 CrewSize()
            => new Vector3(m_Crew.radiusMetres * 2f, m_Crew.heightMetres, m_Crew.radiusMetres * 2f);

        ApronPlan Plan(ApronLayoutSettings settings) => ApronLayout.Build(settings, m_Tractor, m_Cart, m_Aircraft, CrewSize());

        [Test]
        public void TheApronHoldsOneAircraftTwoTractorsAndEightCarts()
        {
            var plan = Plan(ApronLayoutSettings.Default);

            Assert.That(plan.Trains.Count, Is.EqualTo(2));
            Assert.That(plan.Trains.Sum(t => t.Carts.Count), Is.EqualTo(8));
            Assert.That(plan.Everything.Count, Is.EqualTo(11), "the aircraft, two tractors and eight carts");
        }

        [Test]
        public void EachTractorHasItsOwnFourCarts()
        {
            var plan = Plan(ApronLayoutSettings.Default);

            foreach (var train in plan.Trains)
            {
                Assert.That(train.Carts.Count, Is.EqualTo(4));
            }

            var everyCart = plan.Trains.SelectMany(t => t.Carts).Select(c => c.Name).ToList();
            Assert.That(everyCart.Distinct().Count(), Is.EqualTo(everyCart.Count),
                "no cart may appear in two trains at once");
        }

        [Test]
        public void EverythingOnTheApronHasAName()
        {
            var plan = Plan(ApronLayoutSettings.Default);

            foreach (var placement in plan.Everything)
            {
                Assert.That(placement.Name, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public void NothingIsPlacedInsideAnythingElse()
        {
            var plan = Plan(ApronLayoutSettings.Default);
            var all = plan.Everything;

            for (var i = 0; i < all.Count; i++)
            {
                for (var j = i + 1; j < all.Count; j++)
                {
                    Assert.That(all[i].Bounds.Intersects(all[j].Bounds), Is.False,
                        $"'{all[i].Name}' and '{all[j].Name}' are standing in the same place");
                }
            }
        }

        [Test]
        public void ALongerTrainSpreadsOutRatherThanStackingUp()
        {
            var settings = ApronLayoutSettings.Default;
            settings.cartsPerTrain = 8;

            var plan = Plan(settings);
            var carts = plan.Trains[0].Carts;

            Assert.That(carts.Count, Is.EqualTo(8));
            for (var i = 1; i < carts.Count; i++)
            {
                var gap = Vector3.Distance(carts[i - 1].Position, carts[i].Position);
                Assert.That(gap, Is.GreaterThanOrEqualTo(m_Cart.bodySizeMetres.z),
                    "consecutive carts must be at least a cart length apart");
            }
        }

        [Test]
        public void BiggerEquipmentTakesUpMoreRoomRatherThanOverlapping()
        {
            m_Cart.bodySizeMetres = new Vector3(2.5f, 2.5f, 6f);

            var plan = Plan(ApronLayoutSettings.Default);
            var carts = plan.Trains[0].Carts;

            var gap = Vector3.Distance(carts[0].Position, carts[1].Position);
            Assert.That(gap, Is.GreaterThanOrEqualTo(6f),
                "spacing must come from the size of the equipment, not from a hardcoded distance");
        }

        [Test]
        public void EveryPlayerGetsSomewhereOfTheirOwnToArrive()
        {
            var plan = Plan(ApronLayoutSettings.Default);

            Assert.That(plan.CrewSpawnPoints.Count, Is.GreaterThanOrEqualTo(5),
                "the design target is four players and five has to be checked, so there must be room " +
                "for at least five to arrive");

            for (var i = 0; i < plan.CrewSpawnPoints.Count; i++)
            {
                for (var j = i + 1; j < plan.CrewSpawnPoints.Count; j++)
                {
                    Assert.That(
                        plan.CrewSpawnPoints[i].Bounds.Intersects(plan.CrewSpawnPoints[j].Bounds),
                        Is.False,
                        $"players {i} and {j} arrive inside one another. Two capsules starting in the " +
                        "same place do not settle, they fire apart");
                }
            }
        }

        [Test]
        public void NobodyArrivesInsideTheEquipment()
        {
            var plan = Plan(ApronLayoutSettings.Default);

            foreach (var arrival in plan.CrewSpawnPoints)
            {
                foreach (var thing in plan.Everything)
                {
                    Assert.That(arrival.Bounds.Intersects(thing.Bounds), Is.False,
                        $"'{arrival.Name}' arrives inside '{thing.Name}'");
                }
            }
        }

        [Test]
        public void EveryPlacementIsTheRealSizeOfWhatItStandsFor()
        {
            var plan = Plan(ApronLayoutSettings.Default);

            Assert.That(plan.Aircraft.SizeMetres.z, Is.EqualTo(m_Aircraft.lengthMetres).Within(0.01f));
            Assert.That(plan.Trains[0].Tractor.SizeMetres, Is.EqualTo(m_Tractor.bodySizeMetres));
            Assert.That(plan.Trains[0].Carts[0].SizeMetres, Is.EqualTo(m_Cart.bodySizeMetres));
        }
    }
}
