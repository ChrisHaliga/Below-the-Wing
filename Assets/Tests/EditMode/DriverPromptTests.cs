using System.Collections.Generic;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Which vehicle, if any, a player standing on the apron gets offered.
    /// </summary>
    public sealed class DriverPromptTests
    {
        const float Reach = 3f;

        static readonly Vector3 Standing = Vector3.zero;

        TestApron m_Apron;
        VehicleProfile m_Profile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_Profile = TestProfiles.Tractor();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_Profile);
        }

        VehicleController At(string called, float metresAway, bool driveable = true)
            => m_Apron.AddMarker(m_Profile, called, new Vector3(0f, 0f, metresAway), driveable);

        [Test]
        public void NothingIsOfferedWhenEverythingIsOutOfReach()
        {
            var candidates = new List<VehicleController>
            {
                At("Tug 1", 12f)
            };

            Assert.That(DriverPrompt.Nearest(Standing, Reach, candidates), Is.Null);
        }

        [Test]
        public void AVehicleWithinReachIsOffered()
        {
            var tug = At("Tug 1", 2f);

            var offered = DriverPrompt.Nearest(Standing, Reach, new List<VehicleController> { tug });

            Assert.That(offered, Is.SameAs(tug));
        }

        [Test]
        public void StandingBetweenTwoVehiclesOffersTheNearer()
        {
            var further = At("Tug 1", 2.5f);
            var nearer = At("Tug 2", -1f);

            var offered = DriverPrompt.Nearest(Standing, Reach, new List<VehicleController> { further, nearer });

            Assert.That(offered, Is.SameAs(nearer), "the offer must follow where the player actually is");
        }

        [Test]
        public void AVehicleThatTakesNoDriverIsNeverOffered()
        {
            var cart = At("Cart 1-1", 0.5f, driveable: false);
            var tug = At("Tug 1", 2.5f);

            var offered = DriverPrompt.Nearest(Standing, Reach, new List<VehicleController> { cart, tug });

            Assert.That(offered, Is.SameAs(tug),
                "a cart is towed, never driven, even when it is the closest thing to hand");
        }

        [Test]
        public void NothingIsOfferedWhenTheOnlyVehicleInReachTakesNoDriver()
        {
            var cart = At("Cart 1-1", 0.5f, driveable: false);

            Assert.That(DriverPrompt.Nearest(Standing, Reach, new List<VehicleController> { cart }), Is.Null);
        }

        [Test]
        public void NothingIsOfferedWhenThereAreNoVehiclesAtAll()
        {
            Assert.That(DriverPrompt.Nearest(Standing, Reach, new List<VehicleController>()), Is.Null);
        }
    }
}
