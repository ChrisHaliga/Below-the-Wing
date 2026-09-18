using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CrewProfileTests
    {
        CrewProfile m_Profile;

        [SetUp]
        public void SetUp() => m_Profile = TestProfiles.CrewMember();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Profile);

        [Test]
        public void AWorkersSizeIsTheirCapsuleAsABox()
        {
            m_Profile.radiusMetres = 0.3f;
            m_Profile.heightMetres = 1.8f;

            Assert.That(m_Profile.SizeMetres, Is.EqualTo(new Vector3(0.6f, 1.8f, 0.6f)).Using(Nearly.Within(1e-4f)),
                "the layout, the builder and the generator each spelt this out for themselves");
        }

        [Test]
        public void HandAnchorsHangInFrontOfTheBodyEitherSideOfItsMiddle()
        {
            var hands = HandSettings.Default;
            hands.anchorsForwardOfTheBodyMetres = 0.45f;
            hands.anchorHalfSpanMetres = 0.3f;
            hands.anchorHeightMetres = 0.2f;

            var left = hands.AnchorLocal(bodyRadiusMetres: 0.3f, left: true);
            var right = hands.AnchorLocal(bodyRadiusMetres: 0.3f, left: false);

            Assert.That(left, Is.EqualTo(new Vector3(-0.3f, 0.2f, 0.75f)).Using(Nearly.Within(1e-4f)));
            Assert.That(right, Is.EqualTo(new Vector3(0.3f, 0.2f, 0.75f)).Using(Nearly.Within(1e-4f)));
        }

        [Test]
        public void AWiderWorkerCarriesTheirHandsFurtherOut()
        {
            var hands = HandSettings.Default;

            Assert.That(hands.AnchorLocal(0.5f, left: false).z, Is.GreaterThan(hands.AnchorLocal(0.3f, left: false).z),
                "the anchors sit past the body's own radius, so a broader body reaches further forward");
        }
    }
}
