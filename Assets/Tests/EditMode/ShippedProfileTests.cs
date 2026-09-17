using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class ShippedProfileTests
    {
        const string TractorPath = ShippedContent.TractorProfilePath;
        const string CartPath = ShippedContent.CartProfilePath;
        const string AircraftPath = ShippedContent.AircraftProfilePath;
        const string CrewPath = ShippedContent.CrewProfilePath;
        const string TractorPrefabPath = ShippedContent.TractorPrefabPath;
        const string CartPrefabPath = ShippedContent.CartPrefabPath;

        static VehicleShape Shape(string prefabPath)
            => Load<GameObject>(prefabPath).GetComponent<VehicleShape>();

        static float SmallestWheelMetres(VehicleShape shape)
        {
            var smallest = float.MaxValue;
            foreach (var wheel in shape.Wheels)
            {
                smallest = Mathf.Min(smallest, wheel.RadiusMetres);
            }

            return smallest;
        }

        static T Load<T>(string path) where T : UnityEngine.Object => ShippedContent.Load<T>(path);

        [Test]
        public void ABaggageTractorWeighsWhatABaggageTractorWeighs()
        {
            var tractor = Load<VehicleProfile>(TractorPath);

            Assert.That(tractor.massKg, Is.InRange(2000f, 4500f), "a baggage tug is a few tonnes");
            Assert.That(Shape(TractorPrefabPath).EnvelopeSizeMetres.z, Is.InRange(2f, 4f),
                "and about the length of a car");
            Assert.That(tractor.equipmentNote, Is.Not.Empty,
                "a mass with no note saying what it stands for is a number somebody will change on a whim");
        }

        [Test]
        public void ABaggageCartWeighsWhatAnEmptyBaggageCartWeighs()
        {
            var cart = Load<VehicleProfile>(CartPath);

            Assert.That(cart.massKg, Is.InRange(350f, 800f), "tare weight, before a single bag goes on");
            Assert.That(Shape(CartPrefabPath).EnvelopeSizeMetres.z, Is.InRange(2f, 4f));
            Assert.That(cart.equipmentNote, Is.Not.Empty);
        }

        [Test]
        public void EveryVehicleHasLessSuspensionTravelThanItsSmallestWheel()
        {
            foreach (var (profilePath, prefabPath) in new[]
                     {
                         (TractorPath, TractorPrefabPath),
                         (CartPath, CartPrefabPath)
                     })
            {
                var profile = Load<VehicleProfile>(profilePath);
                var smallest = SmallestWheelMetres(Shape(prefabPath));

                Assert.That(profile.suspensionRestLengthMetres, Is.LessThan(smallest),
                    $"{profilePath} has {profile.suspensionRestLengthMetres:F2} m of travel on a " +
                    $"{smallest:F3} m wheel. More travel than the wheel has radius and the vehicle " +
                    "visibly floats above its own axles. This asset is what the game runs on, so it " +
                    "is the copy of the figure that has to be right");
            }
        }

        [Test]
        public void TheShippedTractorCarriesTheFiguresItWasTunedTo()
        {
            var tractor = Load<VehicleProfile>(TractorPath);

            Assert.That(tractor.massKg, Is.EqualTo(2500f).Within(1f),
                "a 2.8 m tug, which is the class this model is");
            Assert.That(tractor.suspensionRestLengthMetres, Is.EqualTo(0.10f).Within(0.005f),
                "under half the smaller of its two wheels");
            Assert.That(tractor.centerOfMassOffset.y, Is.EqualTo(0.55f).Within(0.05f),
                "low in bodywork whose own middle is a metre up, or it rolls over in the first corner");
            Assert.That(tractor.maxDriveForceNewtons, Is.EqualTo(30000f).Within(1f),
                "1.2 g of thrust, which no real tug has. Along with top speed this is a figure " +
                "chosen for how the apron feels to cross rather than measured off a machine, and " +
                "the launch multiplier is stacked on top of it");
            Assert.That(tractor.maxSteerAngleDegrees, Is.GreaterThanOrEqualTo(40f),
                "a tug turns tightly; it spends its life reversing carts into stands");
        }

        [Test]
        public void TheTractorSettlesWithRoomToSquashAndRoomToExtend()
        {
            var tractor = Load<VehicleProfile>(TractorPath);
            var atRest = VehicleController.SuspensionCompressionAtRest(tractor);

            Assert.That(atRest, Is.InRange(0.05f, 0.30f),
                $"the tractor's springs sit {atRest:P0} compressed carrying nothing but itself. " +
                "Resting fully extended it has nothing to absorb a bump with; resting bottomed out " +
                "it has nothing left to give under a load");
        }

        [Test]
        public void EveryVehicleCarriesItsWeightAboveTheGroundItStandsOn()
        {
            foreach (var path in new[] { TractorPath, CartPath })
            {
                var profile = Load<VehicleProfile>(path);

                Assert.That(profile.centerOfMassOffset.y, Is.GreaterThan(0f),
                    $"{path} puts its centre of mass at or below its own origin. A vehicle's origin " +
                    "is on the tarmac between its wheels, so that is mass underneath every contact " +
                    "patch: braking pitches the nose up, accelerating dives it, and nothing can tip " +
                    "the vehicle over");

                var bodywork = Shape(path == TractorPath ? TractorPrefabPath : CartPrefabPath);

                Assert.That(profile.centerOfMassOffset.y,
                    Is.LessThan(bodywork.EnvelopeCentreLocal.y + (bodywork.EnvelopeSizeMetres.y * 0.5f)),
                    $"{path} carries its weight above its own roof");
            }
        }

        [Test]
        public void ATractorOutweighsAnEmptyCartAboutAsMuchAsARealOneDoes()
        {
            var tractor = Load<VehicleProfile>(TractorPath);
            var cart = Load<VehicleProfile>(CartPath);

            Assert.That(tractor.massKg / cart.massKg, Is.InRange(4f, 8f));
        }

        [Test]
        public void ACartIsTowedAndNeverDriven()
        {
            Assert.That(Load<VehicleProfile>(CartPath).driveable, Is.False);
            Assert.That(Load<VehicleProfile>(TractorPath).driveable, Is.True);
        }

        [Test]
        public void TheAircraftIsANarrowbodyAtItsRealEmptyWeightAndLength()
        {
            var aircraft = Load<AircraftProfile>(AircraftPath);

            Assert.That(aircraft.massKg, Is.InRange(35000f, 50000f), "operating empty weight, roughly forty tonnes");
            Assert.That(aircraft.lengthMetres, Is.InRange(30f, 45f));
            Assert.That(aircraft.fuselageDiameterMetres, Is.InRange(3f, 4.5f));
        }

        [Test]
        public void ARampWorkerIsTheSizeAndWeightOfAPerson()
        {
            var crew = Load<CrewProfile>(CrewPath);

            Assert.That(crew.massKg, Is.InRange(60f, 110f));
            Assert.That(crew.heightMetres, Is.InRange(1.5f, 2.1f));
            Assert.That(crew.sprintSpeedMetresPerSecond, Is.GreaterThan(crew.walkSpeedMetresPerSecond));
        }

        [Test]
        public void ARampWorkerFitsInsideABaggageCartCrouched()
        {
            var crew = Load<CrewProfile>(CrewPath);
            var clearInside = Shape(CartPrefabPath).InteriorLocal.size.y;

            Assert.That(crew.crouchedHeightMetres, Is.LessThan(clearInside),
                $"crouched they are {crew.crouchedHeightMetres} m and a cart has {clearInside:F2} m " +
                "clear above its deck. Taller than that and nobody can get into a cart at all: the " +
                "crouch is checked against what is overhead, so they simply refuse to stand up and " +
                "then refuse to fit. This is a relationship between a person and a cart, and the two " +
                "figures live in different assets with nothing but this holding them together");
        }

        [Test]
        public void ARampWorkerCanJumpOntoABaggageCartsDeck()
        {
            var crew = Load<CrewProfile>(CrewPath);
            var deckTop = Shape(CartPrefabPath).InteriorLocal.min.y;

            Assert.That(crew.jumpHeightMetres, Is.GreaterThan(deckTop),
                $"a jump clears {crew.jumpHeightMetres} m and a cart's deck is {deckTop:F2} m up. " +
                "Reaching a deck is the one thing jumping is for on an apron, and a jump that cannot " +
                "is a control that does nothing anybody wants");
        }

        [Test]
        public void AGripTearsOffBeforeARampWorkersOwnLegsCanTearIt()
        {
            var crew = Load<CrewProfile>(CrewPath);

            var whatTheirLegsPush = crew.massKg * crew.gaitResponseMetresPerSecondSquared;

            Assert.That(crew.hands.gripBreakForceNewtons, Is.GreaterThan(whatTheirLegsPush * 2f),
                $"their legs push {whatTheirLegsPush:F0} N and a grip lets go at " +
                $"{crew.hands.gripBreakForceNewtons:F0} N. Set below what they can push with, a player " +
                "holding a rail and walking forward tears their own hand off it, and the harder the " +
                "controls are made to answer the more certain that becomes");
        }

        [Test]
        public void ASpringHoldsUpTheMassItIsGiven()
        {
            var tractor = Load<VehicleProfile>(TractorPath);
            const int corners = 4;

            var weightPerCorner = tractor.massKg * Mathf.Abs(Physics.gravity.y) / corners;
            var springAtFullCompression = tractor.springStrengthNewtons;

            Assert.That(springAtFullCompression, Is.GreaterThan(weightPerCorner),
                "a spring weaker than the weight on it cannot hold the vehicle off the ground at all");
            Assert.That(springAtFullCompression, Is.LessThan(weightPerCorner * 20f),
                "and one far stronger than the weight on it barely compresses, so the vehicle rides on stilts");
        }

        [Test]
        public void TheTractorIsQuickAndTheCartRollsFree()
        {
            var tractor = Load<VehicleProfile>(TractorPath);
            var cart = Load<VehicleProfile>(CartPath);

            Assert.That(tractor.topSpeedMetresPerSecond, Is.InRange(15f, 40f), "BaggageTractor: a top speed worth having");
            Assert.That(tractor.maxDriveForceNewtons / tractor.massKg, Is.GreaterThan(6f),
                "BaggageTractor: pulls away at more than six metres per second squared");
            Assert.That(tractor.bounciness, Is.GreaterThan(0.2f), "BaggageTractor: bumps have to come back");
            Assert.That(cart.bounciness, Is.GreaterThan(0.2f), "BaggageCart: bumps have to come back");
            Assert.That(cart.coastingDragPerSecond, Is.InRange(0.5f, 1.2f),
                "BaggageCart: a cart shoved with 2000 Ns rolls 3.7 m at 0.8 and 15.8 m at 0.1. The " +
                "second reads as a cart with no weight in it, which is what this figure is set by. " +
                "It still has no driveline dragging it down: nothing here brakes it, only drag");
        }
    }
}
