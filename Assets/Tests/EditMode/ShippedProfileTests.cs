using BelowTheWing.Apron;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEditor;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// The equipment the game actually ships with weighs and measures what the real thing does.
    ///
    /// Every other test builds its own profiles so that retuning a vehicle for feel cannot turn a
    /// test red. These are the exception, and they are here because the figures being real is
    /// itself a requirement: a spring rate is only possible to reason about if the mass it is
    /// holding up is a mass something actually has.
    ///
    /// The ranges are wide on purpose. They are there to catch a profile drifting into fantasy,
    /// not to pin a number nobody should be free to adjust.
    /// </summary>
    public sealed class ShippedProfileTests
    {
        const string TractorPath = "Assets/Content/Vehicles/BaggageTractor.asset";
        const string CartPath = "Assets/Content/Vehicles/BaggageCart.asset";
        const string AircraftPath = "Assets/Content/Aircraft/NarrowbodyAirliner.asset";
        const string CrewPath = "Assets/Content/Crew/RampWorker.asset";

        static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"expected a {typeof(T).Name} at {path}");
            return asset;
        }

        [Test]
        public void ABaggageTractorWeighsWhatABaggageTractorWeighs()
        {
            var tractor = Load<VehicleProfile>(TractorPath);

            Assert.That(tractor.massKg, Is.InRange(2000f, 4500f), "a baggage tug is a few tonnes");
            Assert.That(tractor.bodySizeMetres.z, Is.InRange(2f, 4f), "and about the length of a car");
            Assert.That(tractor.equipmentNote, Is.Not.Empty,
                "a mass with no note saying what it stands for is a number somebody will change on a whim");
        }

        [Test]
        public void ABaggageCartWeighsWhatAnEmptyBaggageCartWeighs()
        {
            var cart = Load<VehicleProfile>(CartPath);

            Assert.That(cart.massKg, Is.InRange(350f, 800f), "tare weight, before a single bag goes on");
            Assert.That(cart.bodySizeMetres.z, Is.InRange(2f, 4f));
            Assert.That(cart.equipmentNote, Is.Not.Empty);
        }

        [Test]
        public void TheShippedCartRunsOnTheNumbersItsModelWasMeasuredAt()
        {
            var cart = Load<VehicleProfile>(CartPath);

            // Off the model. The cart prefab hangs its suspension from wheel positions read out of
            // the mesh, and this is the one wheel figure that stays in the profile because it is a
            // physics quantity rather than a position. If the two disagree, the invisible wheels
            // holding the cart up are a different size from the visible ones turning on it.
            Assert.That(cart.wheelRadiusMetres, Is.EqualTo(0.157f).Within(0.005f),
                "the modelled cart runs on 0.157 m wheels. This asset is written once, when it does " +
                "not exist, and never again -- so retuning the cart in the bootstrap and assuming " +
                "the game picked it up is exactly how the shipped cart ends up running on numbers " +
                "nothing else in the project uses");

            Assert.That(cart.suspensionRestLengthMetres, Is.LessThan(cart.wheelRadiusMetres),
                $"{cart.suspensionRestLengthMetres:F2} m of travel on a {cart.wheelRadiusMetres:F3} m " +
                "wheel. More travel than the wheel has radius and the cart visibly floats above its " +
                "own axles");
        }

        [Test]
        public void EveryVehicleCarriesItsWeightAboveTheGroundItStandsOn()
        {
            foreach (var path in new[] { TractorPath, CartPath })
            {
                var profile = Load<VehicleProfile>(path);

                Assert.That(profile.centerOfMassOffset.y, Is.GreaterThan(0f),
                    $"{path} puts its centre of mass at or below its own origin. A vehicle's origin " +
                    "is on the tarmac between its wheels now, so that is mass underneath every " +
                    "contact patch: braking pitches the nose up, accelerating dives it, and nothing " +
                    "can tip the vehicle over");

                Assert.That(profile.centerOfMassOffset.y,
                    Is.LessThan(profile.bodySizeMetres.y),
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
        public void ASpringHoldsUpTheMassItIsGiven()
        {
            var tractor = Load<VehicleProfile>(TractorPath);
            const int corners = 4;
            const float gravity = 9.81f;

            var weightPerCorner = tractor.massKg * gravity / corners;
            var springAtFullCompression = tractor.springStrengthNewtons;

            Assert.That(springAtFullCompression, Is.GreaterThan(weightPerCorner),
                "a spring weaker than the weight on it cannot hold the vehicle off the ground at all");
            Assert.That(springAtFullCompression, Is.LessThan(weightPerCorner * 20f),
                "and one far stronger than the weight on it barely compresses, so the vehicle rides on stilts");
        }
    }
}
