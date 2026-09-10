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

        [Test]
        public void NothingIsOfferedWhenEverythingIsOutOfReach()
        {
            var candidates = new List<IDriveable>
            {
                new StubDriveable("Tug 1", new Vector3(0f, 0f, 12f))
            };

            Assert.That(DriverPrompt.Nearest(Standing, Reach, candidates), Is.Null);
        }

        [Test]
        public void AVehicleWithinReachIsOffered()
        {
            var tug = new StubDriveable("Tug 1", new Vector3(0f, 0f, 2f));

            var offered = DriverPrompt.Nearest(Standing, Reach, new List<IDriveable> { tug });

            Assert.That(offered, Is.SameAs(tug));
        }

        [Test]
        public void StandingBetweenTwoVehiclesOffersTheNearer()
        {
            var further = new StubDriveable("Tug 1", new Vector3(0f, 0f, 2.5f));
            var nearer = new StubDriveable("Tug 2", new Vector3(0f, 0f, -1f));

            var offered = DriverPrompt.Nearest(Standing, Reach, new List<IDriveable> { further, nearer });

            Assert.That(offered, Is.SameAs(nearer), "the offer must follow where the player actually is");
        }

        [Test]
        public void AVehicleThatTakesNoDriverIsNeverOffered()
        {
            var cart = new StubDriveable("Cart 1-1", new Vector3(0f, 0f, 0.5f), acceptsDriver: false);
            var tug = new StubDriveable("Tug 1", new Vector3(0f, 0f, 2.5f));

            var offered = DriverPrompt.Nearest(Standing, Reach, new List<IDriveable> { cart, tug });

            Assert.That(offered, Is.SameAs(tug),
                "a cart is towed, never driven, even when it is the closest thing to hand");
        }

        [Test]
        public void NothingIsOfferedWhenTheOnlyVehicleInReachTakesNoDriver()
        {
            var cart = new StubDriveable("Cart 1-1", new Vector3(0f, 0f, 0.5f), acceptsDriver: false);

            Assert.That(DriverPrompt.Nearest(Standing, Reach, new List<IDriveable> { cart }), Is.Null);
        }

        [Test]
        public void NothingIsOfferedWhenThereAreNoVehiclesAtAll()
        {
            Assert.That(DriverPrompt.Nearest(Standing, Reach, new List<IDriveable>()), Is.Null);
        }
    }
}
