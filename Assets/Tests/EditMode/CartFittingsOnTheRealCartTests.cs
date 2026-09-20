using BelowTheWing.Cargo;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CartFittingsOnTheRealCartTests
    {
        GameObject m_Cart;

        [SetUp]
        public void SetUp()
        {
            m_Cart = Object.Instantiate(ShippedContent.Prefab(ShippedContent.CartPrefabPath));
            m_Cart.GetComponent<VehicleController>().Configure(
                ShippedContent.Load<VehicleProfile>(ShippedContent.CartProfilePath), "Cart 1");
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Cart);

        [Test]
        public void TheCartTheGameShipsRaisesAPoleForEveryDoorOnItsModel()
        {
            var poles = m_Cart.GetComponentsInChildren<SlidingDoorPole>(true);

            Assert.That(poles.Length, Is.EqualTo(4),
                "the model carries Door1 to Door4, and a door with no pole cannot be opened at all");

            foreach (var pole in poles)
            {
                Assert.That(pole.GetComponent<Rigidbody>(), Is.Not.Null,
                    $"{pole.name} has to be a body, or a hand has nothing to tether to");
                Assert.That(pole.GetComponent<ConfigurableJoint>(), Is.Not.Null,
                    $"{pole.name} has to be on its track, or it floats off the cart");
            }
        }

        [Test]
        public void EveryDoorPanelAndVinylCarriesExactlyOneShapeToBeDrivenBy()
        {
            foreach (var skin in m_Cart.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!skin.name.StartsWith("Door"))
                {
                    continue;
                }

                Assert.That(skin.sharedMesh.blendShapeCount, Is.EqualTo(1),
                    $"{skin.name} is driven by shape 0 and nothing else, so a second shape means " +
                    "the model changed and the door no longer knows what it is driving");
            }
        }

        [Test]
        public void TheCartTheGameShipsHasABrakeAndABarToPullItBy()
        {
            Assert.That(m_Cart.GetComponent<CartBrake>(), Is.Not.Null,
                "without a brake there is nothing for E to park");

            var bar = m_Cart.transform.Find(Drawbar.BarName);
            Assert.That(bar, Is.Not.Null, "an unhitched cart with no drawbar cannot be moved by hand");
            Assert.That(bar.GetComponentInChildren<Collider>(), Is.Not.Null,
                "the bar needs something to grab");
        }
    
        [Test]
        public void ADoorPoleSlidesFreelyWithNoSpringAndNoBounce()
        {
            foreach (var pole in m_Cart.GetComponentsInChildren<SlidingDoorPole>(true))
            {
                var rail = pole.GetComponent<ConfigurableJoint>();

                Assert.That(rail.zMotion, Is.EqualTo(ConfigurableJointMotion.Limited));
                Assert.That(rail.xMotion, Is.EqualTo(ConfigurableJointMotion.Locked));
                Assert.That(rail.linearLimitSpring.spring, Is.EqualTo(0f).Within(1e-4f),
                    $"{pole.name} has a spring on its track, and a door is meant to slide and stop");
                Assert.That(rail.linearLimit.bounciness, Is.EqualTo(0f).Within(1e-4f),
                    $"{pole.name} bounces off its stop, and a door is meant to slide and stop");
            }
        }

        [Test]
        public void TheGrabbablePoleSitsWhereTheModelPutsItShutAndOpen()
        {
            var pole = APoleCalled("Door1 pole");
            var grab = pole.GetComponent<CapsuleCollider>();

            Assert.That(grab.radius, Is.EqualTo(0.02171f).Within(5e-4f),
                "the pole a player grabs is 0.02171 m across in the model");
            Assert.That(pole.transform.localPosition.z, Is.EqualTo(0.02919f).Within(5e-4f),
                "a shut door puts its moving pole at 0.02919 m, near the middle of the cart");

            Assert.That(pole.TravelMetres, Is.EqualTo(1.01162f).Within(1e-3f),
                "the pole travels 1.01162 m, from 0.02919 m shut out to 1.04081 m open");

            var rail = pole.GetComponent<ConfigurableJoint>();

            pole.TakeHold();

            Assert.That(rail.linearLimit.limit * 2f, Is.EqualTo(1.01162f).Within(1e-3f),
                "off its hook the rail gives the pole the whole travel the model asks for");
            Assert.That(rail.connectedAnchor.z, Is.EqualTo(0.535f).Within(1e-3f),
                "the rail is anchored half way along that travel");

            pole.LetGo();
        }

        [Test]
        public void TheDoorsOnTheBackOfTheCartRunTheOtherWay()
        {
            var pole = APoleCalled("Door3 pole");

            Assert.That(pole.transform.localPosition.z, Is.EqualTo(-0.02919f).Within(5e-4f),
                "a door on the back half opens toward the back, so every figure is mirrored");
        }

        SlidingDoorPole APoleCalled(string called)
        {
            foreach (var pole in m_Cart.GetComponentsInChildren<SlidingDoorPole>(true))
            {
                if (pole.name == called)
                {
                    return pole;
                }
            }

            Assert.Fail($"no pole called {called} was raised");
            return null;
        }

        [Test]
        public void ADoorPoleDoesNotFightTheCartItRunsAlong()
        {
            var poles = m_Cart.GetComponentsInChildren<SlidingDoorPole>(true);
            Assert.That(poles, Is.Not.Empty);

            var deck = FindCartCollider("Deck");

            foreach (var pole in poles)
            {
                Assert.That(Physics.GetIgnoreCollision(pole.GetComponent<Collider>(), deck), Is.True,
                    $"{pole.name} is raised inside the cart's own bodywork, so unless that pair is " +
                    "ignored the pole is jammed in a collision and cannot slide at all");
            }
        }

        [Test]
        public void ADoorPoleStillCollidesWithWhateverIsNotTheCart()
        {
            var pole = m_Cart.GetComponentInChildren<SlidingDoorPole>(true).GetComponent<Collider>();

            Assert.That(pole.excludeLayers.value, Is.EqualTo(0),
                "excluding a whole layer takes the pole out of every collision there is, bags and " +
                "crew included, which is far more than keeping it off its own cart");
        }

        Collider FindCartCollider(string called)
        {
            foreach (var part in m_Cart.GetComponentsInChildren<Collider>(true))
            {
                if (part.name == called)
                {
                    return part;
                }
            }

            Assert.Fail($"the cart has no collider called {called}");
            return null;
        }
    
        [Test]
        public void EveryDoorPoleIsSomethingAHandCanTakeHoldOf()
        {
            var poles = m_Cart.GetComponentsInChildren<SlidingDoorPole>(true);
            Assert.That(poles.Length, Is.EqualTo(4));

            foreach (var pole in poles)
            {
                Assert.That(HandUse.TryFind(pole.GetComponent<Collider>(), out var use), Is.True,
                    $"{pole.name} is the part of a door a player works it by");
                Assert.That(use.As, Is.EqualTo(HandUse.Category.HoldOnto));
            }
        }

        [Test]
        public void ADoorsCoverBlocksBagsWithoutBeingSomethingAHandCanTake()
        {
            foreach (var part in m_Cart.GetComponentsInChildren<Collider>(true))
            {
                if (!part.name.EndsWith("cover"))
                {
                    continue;
                }

                Assert.That(part.isTrigger, Is.False, "a cover has to stop a bag going through it");
                Assert.That(HandUse.TryFind(part, out _), Is.False,
                    $"{part.name} is the door's wall, and grabbing the wall instead of the pole is " +
                    "what makes a door feel like it cannot be worked");
            }
        }
    }
}
