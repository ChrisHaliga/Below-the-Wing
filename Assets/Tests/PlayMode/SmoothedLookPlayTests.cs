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

        [UnityTest]
        public IEnumerator AShapeAuthoredLyingOnItsSideIsStillLyingOnItsSide()
        {
            var vehicle = SomebodyElsesTractor();
            var shape = ShapeOf(vehicle);

            // What an aircraft is: a capsule primitive, whose own axis runs up, turned a quarter
            // turn so that it lies along the length of the fuselage.
            shape.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shape.RememberHowItWasPlaced();
            shape.CatchUpNow();

            yield return Step(0.5f);

            var lyingDown = Quaternion.Angle(
                shape.transform.rotation, vehicle.transform.rotation * Quaternion.Euler(90f, 0f, 0f));

            Assert.That(lyingDown, Is.LessThan(1f),
                "writing the body's rotation straight onto the shape throws away the quarter turn " +
                "that lays a fuselage down, and the aircraft stands on its tail in the middle of " +
                "the apron");
        }

        [UnityTest]
        public IEnumerator AShapeAuthoredAwayFromItsOriginStaysThere()
        {
            var vehicle = SomebodyElsesTractor();
            var shape = ShapeOf(vehicle);

            // A vehicle's origin is on the ground between its wheels, so anything drawn for it sits
            // above that origin rather than on it.
            var placedAt = new Vector3(0f, 0.8f, 0f);
            shape.transform.localPosition = placedAt;
            shape.RememberHowItWasPlaced();
            shape.CatchUpNow();

            vehicle.Body.position = new Vector3(0f, 0f, 6f);
            vehicle.transform.position = vehicle.Body.position;

            yield return Step(1f);

            var offset = vehicle.transform.InverseTransformPoint(shape.transform.position);

            Assert.That(Vector3.Distance(offset, placedAt), Is.LessThan(0.05f),
                $"the shape settled {offset} from its body rather than {placedAt}. Catching up by " +
                "writing the body's own position collapses every offset a model was authored with");
        }

        [UnityTest]
        public IEnumerator SomethingThatAlreadyHasAModelIsNotMovedOffIt()
        {
            var vehicle = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);

            // A model, placed by whoever built the prefab, exactly where its measurements were
            // taken from.
            var model = new GameObject(ApronAppearance.LookName);
            model.transform.SetParent(vehicle.transform, worldPositionStays: false);

            var appearance = vehicle.gameObject.AddComponent<ApronAppearance>();
            appearance.DescribeAs(
                ApronAppearance.Shape.AlreadyModelled,
                Vector3.one,
                Color.grey,
                labelHeightMetres: 2f,
                drawnAtLocal: new Vector3(0f, 1.09f, 0.13f));

            appearance.Show("Tug 1");
            yield return null;

            Assert.That(model.transform.localPosition, Is.EqualTo(Vector3.zero).Using(Near),
                "a grey primitive is built at the object's origin and has to be lifted to where the " +
                "bodywork is. A model is already there, and lifting it too puts the cart you see a " +
                "metre above the cart you drive into");
        }

        static readonly System.Collections.Generic.IEqualityComparer<Vector3> Near = new Within(0.01f);

        sealed class Within : System.Collections.Generic.IEqualityComparer<Vector3>
        {
            readonly float m_Tolerance;

            public Within(float tolerance) => m_Tolerance = tolerance;

            public bool Equals(Vector3 a, Vector3 b) => Vector3.Distance(a, b) <= m_Tolerance;

            public int GetHashCode(Vector3 of) => of.GetHashCode();
        }

        static IEnumerator Step(float seconds)
        {
            var steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
