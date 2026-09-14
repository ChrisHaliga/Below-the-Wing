using BelowTheWing.Crew;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class CrewLocomotionTests
    {
        static readonly Vector2 Forward = new Vector2(0f, 1f);

        CrewProfile m_Profile;

        [SetUp]
        public void SetUp() => m_Profile = TestProfiles.CrewMember();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Profile);

        [Test]
        public void PressingForwardWalksAwayFromACameraLookingNorth()
        {
            var velocity = CrewLocomotion.DesiredVelocity(Forward, cameraYawDegrees: 0f, sprinting: false, m_Profile);

            Assert.That(velocity.normalized.z, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(velocity.x, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void SwingingTheCameraSwingsWhereForwardIs()
        {
            var facingNorth = CrewLocomotion.DesiredVelocity(Forward, 0f, false, m_Profile);
            var facingEast = CrewLocomotion.DesiredVelocity(Forward, 90f, false, m_Profile);

            Assert.That(facingEast.normalized.x, Is.EqualTo(1f).Within(1e-3f),
                "forward is away from the camera, not a fixed direction in the world");
            Assert.That(facingEast.normalized.z, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(facingEast.magnitude, Is.EqualTo(facingNorth.magnitude).Within(1e-3f));
        }

        [Test]
        public void CharactersWalkAcrossTheApronRatherThanUpIntoTheAir()
        {
            var velocity = CrewLocomotion.DesiredVelocity(Forward, 45f, false, m_Profile);

            Assert.That(velocity.y, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void SprintingIsFasterThanWalkingAndReleasingItSlowsBackDown()
        {
            var walking = CrewLocomotion.DesiredVelocity(Forward, 0f, sprinting: false, m_Profile);
            var sprinting = CrewLocomotion.DesiredVelocity(Forward, 0f, sprinting: true, m_Profile);

            Assert.That(walking.magnitude, Is.EqualTo(m_Profile.walkSpeedMetresPerSecond).Within(1e-3f));
            Assert.That(sprinting.magnitude, Is.EqualTo(m_Profile.sprintSpeedMetresPerSecond).Within(1e-3f));
            Assert.That(sprinting.magnitude, Is.GreaterThan(walking.magnitude));
        }

        [Test]
        public void PressingNothingAsksToGoNowhere()
        {
            var velocity = CrewLocomotion.DesiredVelocity(Vector2.zero, 0f, true, m_Profile);

            Assert.That(velocity.magnitude, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void PushingTheStickHalfwayAsksForHalfSpeed()
        {
            var half = CrewLocomotion.DesiredVelocity(new Vector2(0f, 0.5f), 0f, false, m_Profile);

            Assert.That(half.magnitude, Is.EqualTo(m_Profile.walkSpeedMetresPerSecond * 0.5f).Within(1e-3f));
        }

        [Test]
        public void DiagonalInputIsNoFasterThanStraightAhead()
        {
            var straight = CrewLocomotion.DesiredVelocity(Forward, 0f, false, m_Profile);
            var diagonal = CrewLocomotion.DesiredVelocity(new Vector2(1f, 1f), 0f, false, m_Profile);

            Assert.That(diagonal.magnitude, Is.EqualTo(straight.magnitude).Within(1e-3f),
                "walking north-east must not be faster than walking north");
        }
    }
}
