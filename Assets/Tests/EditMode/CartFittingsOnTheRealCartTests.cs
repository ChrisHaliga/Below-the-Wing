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
    }
}
