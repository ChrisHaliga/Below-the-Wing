using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CartFittingsOnTheRealCartTests
    {
        const string CartPrefabPath = "Assets/Content/Prefabs/BaggageCart.prefab";
        const string CartProfilePath = "Assets/Content/Vehicles/BaggageCart.asset";

        GameObject m_Cart;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CartPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"there is no prefab at {CartPrefabPath}");

            var profile = AssetDatabase.LoadAssetAtPath<VehicleProfile>(CartProfilePath);
            Assert.That(profile, Is.Not.Null, $"there is no profile at {CartProfilePath}");

            m_Cart = Object.Instantiate(prefab);
            m_Cart.GetComponent<VehicleController>().Configure(profile, "Cart 1");
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
        public void EveryDoorPanelAndFabricOnTheCartCarriesTheBlendShapeItIsDrivenBy()
        {
            foreach (var skin in m_Cart.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skin.name.StartsWith("Door_Fabric"))
                {
                    Assert.That(skin.sharedMesh.GetBlendShapeIndex("Closed"), Is.GreaterThanOrEqualTo(0),
                        $"{skin.name} is driven by a Closed shape that its mesh does not have");
                }
                else if (skin.name.StartsWith("Door"))
                {
                    Assert.That(skin.sharedMesh.GetBlendShapeIndex("Open"), Is.GreaterThanOrEqualTo(0),
                        $"{skin.name} is driven by an Open shape that its mesh does not have");
                }
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
        public void ADoorPoleSlidesFreelyAndOnlyBouncesWhenItReachesAStop()
        {
            foreach (var pole in m_Cart.GetComponentsInChildren<SlidingDoorPole>(true))
            {
                var rail = pole.GetComponent<ConfigurableJoint>();

                Assert.That(rail.zMotion, Is.EqualTo(ConfigurableJointMotion.Limited));
                Assert.That(rail.linearLimitSpring.spring, Is.EqualTo(0f).Within(1e-4f),
                    $"{pole.name} has a spring pulling it back to the middle of its track, so it " +
                    "sits half open and jiggles instead of sliding where it is pushed");
                Assert.That(rail.linearLimit.bounciness, Is.GreaterThan(0f),
                    $"{pole.name} has to bounce off its stop when it is slammed");
            }
        }

        [Test]
        public void ADoorPoleDoesNotFightTheCartItRunsAlong()
        {
            var poles = m_Cart.GetComponentsInChildren<SlidingDoorPole>(true);
            Assert.That(poles, Is.Not.Empty);

            foreach (var pole in poles)
            {
                Assert.That(pole.GetComponent<Collider>().excludeLayers.value, Is.Not.EqualTo(0),
                    $"{pole.name} sits inside the cart's own bodywork, so unless it is excluded " +
                    "from it the pole is jammed in a collision and cannot slide at all");
            }
        }

        [Test]
        public void NothingDrivesTheVinylAwayFromTheWeightTheModelShipsWith()
        {
            foreach (var skin in m_Cart.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!skin.name.StartsWith("Door_Fabric"))
                {
                    continue;
                }

                Assert.That(skin.GetBlendShapeWeight(0), Is.EqualTo(100f).Within(1e-3f),
                    $"{skin.name} was authored at 100 and nothing should be moving it");
            }
        }
    }
}
