using BelowTheWing.Menu;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class MenuCameraShotTests
    {
        [Test]
        public void AShotOrbitsItsSubjectAtAConstantRate()
        {
            var shot = new CameraShot(Vector3.zero, radiusMetres: 20f, heightMetres: 8f, degreesPerSecond: 6f);

            Assert.That(shot.AngleAfter(0f, 0f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(shot.AngleAfter(0f, 10f), Is.EqualTo(60f).Within(1e-4f));
            Assert.That(shot.AngleAfter(30f, 10f), Is.EqualTo(90f).Within(1e-4f));
        }

        [Test]
        public void AnOrbitWrapsRatherThanCountingUpForever()
        {
            var shot = new CameraShot(Vector3.zero, 20f, 8f, degreesPerSecond: 90f);

            Assert.That(shot.AngleAfter(0f, 5f), Is.EqualTo(90f).Within(1e-3f),
                "450 degrees is 90 degrees, and a number that climbs all session loses its " +
                "precision by the time anybody has sat on the title screen for a while");
        }

        [Test]
        public void AShotStandsOffItsSubjectByItsOwnRadiusAndHeight()
        {
            var shot = new CameraShot(new Vector3(10f, 0f, 4f), radiusMetres: 20f, heightMetres: 8f, degreesPerSecond: 0f);

            var at = shot.PlacedAt(0f);

            Assert.That(Vector3.Distance(new Vector3(at.x, 0f, at.z), new Vector3(10f, 0f, 4f)),
                Is.EqualTo(20f).Within(1e-3f));
            Assert.That(at.y, Is.EqualTo(8f).Within(1e-3f));
        }

        [Test]
        public void AShotAlwaysFacesWhatItIsOrbiting()
        {
            var subject = new Vector3(-6f, 2f, 3f);
            var shot = new CameraShot(subject, 15f, 9f, 0f);

            foreach (var angle in new[] { 0f, 90f, 217f })
            {
                var looking = shot.FacingFrom(angle);
                var toSubject = (subject - shot.PlacedAt(angle)).normalized;

                Assert.That(Vector3.Dot(looking * Vector3.forward, toSubject),
                    Is.EqualTo(1f).Within(1e-3f), $"at {angle} degrees");
            }
        }

        [Test]
        public void TravellingBetweenTwoShotsStartsAtOneAndEndsAtTheOther()
        {
            var from = new CameraShot(Vector3.zero, 80f, 40f, 4f);
            var to = new CameraShot(new Vector3(30f, 0f, 0f), 18f, 7f, 8f);

            var atTheStart = CameraShot.Between(from, to, 0f);
            var atTheEnd = CameraShot.Between(from, to, 1f);

            Assert.That(atTheStart.RadiusMetres, Is.EqualTo(80f).Within(1e-3f));
            Assert.That(atTheEnd.RadiusMetres, Is.EqualTo(18f).Within(1e-3f));
            Assert.That(atTheEnd.Subject.x, Is.EqualTo(30f).Within(1e-3f));
            Assert.That(atTheEnd.DegreesPerSecond, Is.EqualTo(8f).Within(1e-3f));
        }

        [Test]
        public void ATravelEasesRatherThanStartingAndStoppingDead()
        {
            var from = new CameraShot(Vector3.zero, 100f, 10f, 0f);
            var to = new CameraShot(Vector3.zero, 0f, 10f, 0f);

            var earlyOn = 100f - CameraShot.Between(from, to, 0.1f).RadiusMetres;
            var halfWay = 100f - CameraShot.Between(from, to, 0.5f).RadiusMetres;

            Assert.That(halfWay, Is.EqualTo(50f).Within(1f), "half the time is half the distance");
            Assert.That(earlyOn, Is.LessThan(10f),
                "a linear move covers a tenth of the ground in a tenth of the time, and reads as " +
                "the camera being yanked rather than flown");
        }

        [Test]
        public void ATravelPastItsEndStaysAtItsEnd()
        {
            var from = new CameraShot(Vector3.zero, 80f, 40f, 4f);
            var to = new CameraShot(Vector3.one, 18f, 7f, 8f);

            Assert.That(CameraShot.Between(from, to, 4f).RadiusMetres, Is.EqualTo(18f).Within(1e-3f));
            Assert.That(CameraShot.Between(from, to, -2f).RadiusMetres, Is.EqualTo(80f).Within(1e-3f));
        }

        [Test]
        public void AShotFramingAGroupStandsFarEnoughBackToHoldAllOfThem()
        {
            var crew = new Bounds(new Vector3(4f, 1f, 2f), new Vector3(6f, 2f, 3f));

            var close = CameraShot.Framing(crew, fieldOfViewDegrees: 60f, aspect: 16f / 9f, degreesPerSecond: 3f);
            var wide = CameraShot.Framing(crew, fieldOfViewDegrees: 30f, aspect: 16f / 9f, degreesPerSecond: 3f);

            Assert.That(close.Subject, Is.EqualTo(crew.center));
            Assert.That(wide.RadiusMetres, Is.GreaterThan(close.RadiusMetres),
                "a narrower lens has to stand further back to hold the same group of people");
        }

        [Test]
        public void AShotFramingAGroupHoldsEveryOneOfThemAllTheWayRound()
        {
            const float fieldOfView = 60f;
            const float aspect = 16f / 9f;

            var crew = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(12f, 2f, 2f));
            var shot = CameraShot.Framing(crew, fieldOfView, aspect, 3f);

            var upright = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            var across = upright * aspect;

            for (var angle = 0f; angle < 360f; angle += 45f)
            {
                var looking = Quaternion.Inverse(shot.FacingFrom(angle));
                var from = shot.PlacedAt(angle);

                foreach (var corner in Corners(crew))
                {
                    var inView = looking * (corner - from);

                    Assert.That(inView.z, Is.GreaterThan(0f),
                        $"{corner} is behind the camera at {angle} degrees");
                    Assert.That(Mathf.Abs(inView.x) / inView.z, Is.LessThanOrEqualTo(across + 1e-3f),
                        $"{corner} is off the side of the frame at {angle} degrees");
                    Assert.That(Mathf.Abs(inView.y) / inView.z, Is.LessThanOrEqualTo(upright + 1e-3f),
                        $"{corner} is off the top or bottom of the frame at {angle} degrees");
                }
            }
        }

        static Vector3[] Corners(Bounds box)
        {
            var e = box.extents;

            return new[]
            {
                box.center + new Vector3(-e.x, -e.y, -e.z), box.center + new Vector3(e.x, -e.y, -e.z),
                box.center + new Vector3(-e.x, e.y, -e.z), box.center + new Vector3(e.x, e.y, -e.z),
                box.center + new Vector3(-e.x, -e.y, e.z), box.center + new Vector3(e.x, -e.y, e.z),
                box.center + new Vector3(-e.x, e.y, e.z), box.center + new Vector3(e.x, e.y, e.z)
            };
        }
    }
}
