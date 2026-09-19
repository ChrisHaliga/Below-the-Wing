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
    public sealed class ApronLayoutTests
    {
        VehicleFootprint m_Tractor;
        VehicleFootprint m_Cart;
        CrewProfile m_Crew;

        [SetUp]
        public void SetUp()
        {
            m_Tractor = TestShapes.Tractor().Footprint;
            m_Cart = TestShapes.Cart().Footprint;
            m_Crew = TestProfiles.CrewMember();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Crew);
        }

        ApronPlan Plan(ApronLayoutSettings settings)
            => ApronLayout.Build(
                settings,
                new ApronEquipment(
                    m_Tractor, m_Cart, TestShapes.BeltLoaderFootprint,
                    TestShapes.AircraftSizeMetres, TestShapes.AircraftCentreLocal, m_Crew.SizeMetres));

        [Test]
        public void TheApronHoldsAnAircraftABeltLoaderTwoTractorsAndEightCarts()
        {
            var plan = Plan(ApronLayoutSettings.Default);

            Assert.That(plan.Trains.Count, Is.EqualTo(2));
            Assert.That(plan.Trains.Sum(t => t.Carts.Count), Is.EqualTo(8));
            Assert.That(plan.Everything.Count, Is.EqualTo(12),
                "the aircraft, the belt loader, two tractors and eight carts");
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
                Assert.That(gap, Is.GreaterThanOrEqualTo(m_Cart.EnvelopeSizeMetres.z),
                    "consecutive carts must be at least a cart length apart");
            }
        }

        [Test]
        public void BiggerEquipmentTakesUpMoreRoomRatherThanOverlapping()
        {
            m_Cart = TestShapes.BoxVehicle(new Vector3(2.5f, 2.5f, 6f)).Footprint;

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
        public void TheCrewLineStandsWhereTheSettingsSayRelativeToTheTractors()
        {
            var settings = ApronLayoutSettings.Default;
            settings.firstTractorPosition = new Vector3(-18f, 0f, -22f);
            settings.crewArriveAheadOfTheTractorsMetres = 9f;
            settings.crewLineStartsLeftOfTheTractorsMetres = 4f;

            var plan = Plan(settings);

            Assert.That(plan.CrewSpawnPoints[0].Position.z, Is.EqualTo(-13f).Within(1e-3f),
                "the arrival line stands where the settings put it relative to the tractors");
            Assert.That(plan.CrewSpawnPoints[0].Position.x, Is.EqualTo(-22f).Within(1e-3f));
        }

        [Test]
        public void BagsArePlacedClearOfTheCartTheyBelongTo()
        {
            var settings = ApronLayoutSettings.Default;
            settings.bagsPerTrain = 4;

            var plan = Plan(settings);
            var train = plan.Trains[0];
            var bags = ApronLayout.BagsBeside(train, m_Cart, new Vector3(0.4f, 0.25f, 0.6f), settings);

            Assert.That(bags.Count, Is.EqualTo(4));

            foreach (var bag in bags)
            {
                Assert.That(bag.Bounds.Intersects(train.Carts[0].Bounds), Is.False,
                    $"'{bag.Name}' is inside the cart; bags stand off the cart's own side, whatever its width");
                Assert.That(bag.Position.y, Is.GreaterThan(0.125f), "a bag starts above the ground, not in it");
            }

            var pitch = Vector3.Distance(bags[1].Position, bags[0].Position);
            Assert.That(Vector3.Distance(bags[2].Position, bags[1].Position), Is.EqualTo(pitch).Within(1e-3f));
        }

        [Test]
        public void AWiderCartPushesItsBagsFurtherOut()
        {
            var settings = ApronLayoutSettings.Default;
            var plan = Plan(settings);
            var train = plan.Trains[0];

            var narrow = new VehicleFootprint(new Vector3(1.6f, 2f, 3.8f), m_Cart.FrontReachMetres, m_Cart.RearReachMetres);
            var wide = new VehicleFootprint(new Vector3(3.2f, 2f, 3.8f), m_Cart.FrontReachMetres, m_Cart.RearReachMetres);

            var besideNarrow = ApronLayout.BagsBeside(train, narrow, Vector3.one * 0.3f, settings)[0].Position.x;
            var besideWide = ApronLayout.BagsBeside(train, wide, Vector3.one * 0.3f, settings)[0].Position.x;

            Assert.That(besideWide - besideNarrow, Is.EqualTo(0.8f).Within(1e-3f),
                "half the extra width, because the bags stand off the cart's side, not its centre");
        }

        [Test]
        public void ThePlannedAircraftIsAsWideAsTheArtRatherThanAsWideAsItsFuselage()
        {
            var plan = Plan(ApronLayoutSettings.Default);

            Assert.That(plan.Aircraft.SizeMetres.x, Is.EqualTo(21.21f).Within(0.01f),
                "the aircraft used to be planned as a capsule the width of its fuselage, so a wing " +
                "reaching ten metres out counted for nothing and anything could be parked under it");

            foreach (var thing in plan.Everything)
            {
                if (ReferenceEquals(thing.Name, plan.Aircraft.Name))
                {
                    continue;
                }

                Assert.That(plan.Aircraft.Bounds.Intersects(thing.Bounds), Is.False,
                    $"'{thing.Name}' stands inside the aircraft once its wings are counted");
            }
        }

        [Test]
        public void TheBeltLoaderStandsOffTheAircraftsLeftSideTurnedTowardsIt()
        {
            var settings = ApronLayoutSettings.Default;
            var loader = Plan(settings).BeltLoader;

            Assert.That(loader.Position.x,
                Is.EqualTo(-settings.beltLoaderStandsLeftOfTheCentrelineMetres).Within(1e-3f),
                "the loader parks on the aircraft's left, where its hold door is");
            Assert.That(loader.Position.z,
                Is.EqualTo(-settings.beltLoaderStandsAftOfTheCentreMetres).Within(1e-3f));
            Assert.That(loader.Position.y, Is.EqualTo(0f).Within(1e-4f),
                "the loader stands on the apron, not above it");

            Assert.That((loader.Rotation * Vector3.forward).x, Is.GreaterThan(0.99f),
                "the loader is turned to face the fuselage, so its belt runs at the hold rather " +
                "than along the apron");
        }

        [Test]
        public void TheBeltLoaderIsPartOfWhatAnArrivalIsCheckedAgainst()
        {
            var settings = ApronLayoutSettings.Default;
            settings.beltLoaderStandsLeftOfTheCentrelineMetres = 14f;
            settings.crewSpawnPoints = 0;
            settings.crewLineStartsLeftOfTheTractorsMetres = -4f;
            settings.crewArriveAheadOfTheTractorsMetres = 15f;

            Assert.That((Plan(settings).BeltLoader.Position - new Vector3(-14f, 0f, -7f)).magnitude,
                Is.LessThan(1e-3f),
                "the fixture only means anything if the arrival and the loader are put in one place");

            settings.crewSpawnPoints = 1;

            Assert.That(() => Plan(settings), Throws.InvalidOperationException.With.Message.Contains("Belt loader"),
                "a player arriving on top of the belt loader is two bodies in one place, which is " +
                "exactly what the arrival check exists to catch");
        }

        [Test]
        public void APlacementTurnedSidewaysFillsASidewaysBox()
        {
            var loader = Plan(ApronLayoutSettings.Default).BeltLoader;

            Assert.That(loader.Bounds.size.x, Is.EqualTo(TestShapes.BeltLoaderSizeMetres.z).Within(1e-3f),
                "the loader is turned across the apron, so the 4.68 m it is long reaches along x. " +
                "Reporting its 1.62 m width there says nothing is parked where its belt is");
            Assert.That(loader.Bounds.size.z, Is.EqualTo(TestShapes.BeltLoaderSizeMetres.x).Within(1e-3f));
        }

        [Test]
        public void EveryPlacementIsTheRealSizeOfWhatItStandsFor()
        {
            var plan = Plan(ApronLayoutSettings.Default);

            Assert.That(plan.Aircraft.SizeMetres, Is.EqualTo(TestShapes.AircraftSizeMetres));
            Assert.That(plan.Trains[0].Tractor.SizeMetres, Is.EqualTo(m_Tractor.EnvelopeSizeMetres));
            Assert.That(plan.Trains[0].Carts[0].SizeMetres, Is.EqualTo(m_Cart.EnvelopeSizeMetres));
        }
    }
}
