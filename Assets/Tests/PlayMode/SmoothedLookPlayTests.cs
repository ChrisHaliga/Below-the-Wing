using System.Collections;
using BelowTheWing.Apron;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    /// <summary>
    /// What you see, against where the physics actually is.
    ///
    /// Hard corrections are going to happen: a vehicle too far from where its owner says it is gets
    /// moved outright, because easing it twenty metres across the apron is worse than arriving. The
    /// jump is right and watching it is horrible, so the shape trails the body by about a tenth of a
    /// second and turns the teleport into a fast slide.
    /// </summary>
    public sealed class SmoothedLookPlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_TractorProfile;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_TractorProfile = TestProfiles.Tractor();
        }

        [TearDown]
        public void TearDown()
        {
            m_Apron.TearDown();
            Object.DestroyImmediate(m_TractorProfile);
        }

        SmoothedLook ShapeOf(VehicleController vehicle)
        {
            var found = vehicle.GetComponentInChildren<SmoothedLook>();
            Assert.That(found, Is.Not.Null, "the vehicle was drawn without anything smoothing it");
            return found;
        }

        VehicleController SomebodyElsesTractor()
        {
            var vehicle = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);
            vehicle.gameObject.AddComponent<ApronAppearance>().Show("Tug 1");
            vehicle.OursToMove = false;
            return vehicle;
        }

        [UnityTest]
        public IEnumerator AVehicleMovedOutrightDoesNotTakeItsShapeWithItInOneFrame()
        {
            var vehicle = SomebodyElsesTractor();
            var shape = ShapeOf(vehicle);
            shape.CatchUpNow();

            // Snapped four metres, which is the kind of jump correction makes when it gives up
            // blending.
            vehicle.transform.position += new Vector3(0f, 0f, 4f);
            shape.Follow(1f / 60f);

            Assert.That(shape.TrailingByMetres, Is.GreaterThan(1f),
                "the shape arrived with the body, so the jump is drawn exactly as it happened and the " +
                "vehicle is seen to cease being in one place and start being in another");

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheShapeCatchesUpQuicklyEnoughNotToBeNoticed()
        {
            var vehicle = SomebodyElsesTractor();
            var shape = ShapeOf(vehicle);
            shape.CatchUpNow();

            vehicle.transform.position += new Vector3(0f, 0f, 4f);

            // A third of a second, at sixty frames a second.
            for (var i = 0; i < 20; i++)
            {
                shape.Follow(1f / 60f);
            }

            Assert.That(shape.TrailingByMetres, Is.LessThan(0.2f),
                "a shape that never quite arrives is a vehicle permanently drawn somewhere it is not, " +
                "which is worse than the snap it was hiding");

            yield return null;
        }

        [UnityTest]
        public IEnumerator AVehicleThisMachineDrivesIsDrawnExactlyWhereItIs()
        {
            var vehicle = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);
            vehicle.gameObject.AddComponent<ApronAppearance>().Show("Tug 1");
            var shape = ShapeOf(vehicle);
            shape.CatchUpNow();

            vehicle.transform.position += new Vector3(0f, 0f, 4f);
            shape.Follow(1f / 60f);

            Assert.That(shape.TrailingByMetres, Is.LessThan(0.01f),
                "your own tractor never gets snapped, so there is nothing to hide -- and a tenth of a " +
                "second between the wheel and what you see is felt at once as the controls going soft");

            yield return null;
        }

        [UnityTest]
        public IEnumerator AShapeLeftFarBehindStopsPretendingAndCatchesUpAtOnce()
        {
            var vehicle = SomebodyElsesTractor();
            var shape = ShapeOf(vehicle);
            shape.CatchUpNow();

            // Right across the apron, not a correction: a respawn, or a reclaim after a stall.
            vehicle.transform.position += new Vector3(0f, 0f, 60f);
            shape.Follow(1f / 60f);

            Assert.That(shape.TrailingByMetres, Is.LessThan(0.01f),
                "drawn sliding sixty metres under its own power, which is a worse lie than the jump " +
                "smoothing exists to hide");

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheShapeFollowsAtTheSameRateWhateverTheFramerate()
        {
            var slow = SomebodyElsesTractor();
            var slowShape = ShapeOf(slow);
            slowShape.CatchUpNow();
            slow.transform.position += new Vector3(0f, 0f, 4f);

            var fast = m_Apron.AddVehicle(m_TractorProfile, "Tug 2", new Vector3(40f, 0f, 0f), Quaternion.identity);
            fast.gameObject.AddComponent<ApronAppearance>().Show("Tug 2");
            fast.OursToMove = false;
            var fastShape = ShapeOf(fast);
            fastShape.CatchUpNow();
            fast.transform.position += new Vector3(0f, 0f, 4f);

            // A tenth of a second of catching up, spent in three chunks or in twelve.
            for (var i = 0; i < 3; i++)
            {
                slowShape.Follow(1f / 30f);
            }

            for (var i = 0; i < 12; i++)
            {
                fastShape.Follow(1f / 120f);
            }

            Assert.That(slowShape.TrailingByMetres, Is.EqualTo(fastShape.TrailingByMetres).Within(0.15f),
                "closing a fixed fraction per frame rather than per second leaves the shape further " +
                "behind on a fast machine than a slow one, which is backwards");

            yield return null;
        }
    }
}
