using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class SolidAsModelledTests
    {
        const string TractorPrefabPath = "Assets/Content/Prefabs/BaggageTractor.prefab";
        const string CartPrefabPath = "Assets/Content/Prefabs/BaggageCart.prefab";

        static VehicleShape Shape(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, $"there is no prefab at {path}");

            var shape = prefab.GetComponent<VehicleShape>();
            Assert.That(shape, Is.Not.Null, $"{path} has no shape");
            return shape;
        }

        [Test]
        public void ATractorIsSolidInTheShapeOfItsModelRatherThanOneBox()
        {
            var parts = Shape(TractorPrefabPath).SolidParts;

            Assert.That(parts.Count, Is.GreaterThan(1),
                "a tractor described as a single box is solid from the tarmac to above head height " +
                "across its whole length: there is nothing to stand on, no gap between its axles to " +
                "walk through, and the open sides of it are a wall");

            foreach (var part in parts)
            {
                Assert.That(part.IsAPieceOfTheModel, Is.True,
                    $"'{part.Name}' is a box standing in for a piece of a machine that was modelled. " +
                    "What a tractor is solid in is what it looks like, or the two drift apart the " +
                    "first time somebody moves anything");
            }
        }

        [Test]
        public void ATractorHasSomethingToStandOnOverItsRearWheels()
        {
            var shape = Shape(TractorPrefabPath);

            var rearWheelTop = 0f;
            foreach (var wheel in shape.Wheels)
            {
                if (wheel.CentreLocal.z < 0f)
                {
                    rearWheelTop = Mathf.Max(rearWheelTop, wheel.CentreLocal.y + wheel.RadiusMetres);
                }
            }

            var platform = default(VehicleShape.SolidPart);
            var found = false;
            foreach (var part in shape.SolidParts)
            {
                if (part.CentreLocal.z < -0.4f && part.CentreLocal.y > rearWheelTop * 0.8f
                    && part.CentreLocal.y < 1f)
                {
                    platform = part;
                    found = true;
                }
            }

            Assert.That(found, Is.True,
                "these tractors carry a platform over their back wheels and it is how somebody gets " +
                "on. Described as part of one big box, it is the top of a solid block a person can " +
                $"neither step onto nor stand beside. Rear wheels reach {rearWheelTop:F2} m");

            Assert.That(platform.IsAPieceOfTheModel, Is.True,
                $"'{platform.Name}' has to be solid at the shape it was modelled, because the useful " +
                "part of a step is its surface and a box around it fills the space underneath");
        }

        [Test]
        public void ACartIsStillSolidInBoxesBecauseADeckAndALipAreBoxes()
        {
            var parts = Shape(CartPrefabPath).SolidParts;

            Assert.That(parts, Is.Not.Empty);

            foreach (var part in parts)
            {
                Assert.That(part.IsAPieceOfTheModel, Is.False,
                    $"'{part.Name}' is a slab of a cart: a deck, a lip, an end or a roof. Taking " +
                    "these off the model instead would hand the load space over to whatever shape " +
                    "the bodywork mesh happens to be, and a cart's interior is the one thing about " +
                    "it that is described rather than drawn");
            }
        }
    }
}
