using System.Collections;
using BelowTheWing.Apron;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class SmoothedLookPlayTests
    {
        sealed class Mover : MonoBehaviour, IMovedFromHere
        {
            public bool OursToMove { get; set; } = true;
        }

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

        (Transform Body, SmoothedLook Shape) SomethingDrawn(bool sayItIsOurs, bool sayAnythingAtAll)
        {
            var body = m_Apron.Track(new GameObject("Body").transform);
            if (sayAnythingAtAll)
            {
                body.gameObject.AddComponent<Mover>().OursToMove = sayItIsOurs;
            }

            var look = new GameObject(ApronAppearance.LookName).transform;
            look.SetParent(body, worldPositionStays: false);

            return (body, look.gameObject.AddComponent<SmoothedLook>());
        }

        [UnityTest]
        public IEnumerator WhatThisMachineMovesIsDrawnExactlyWhereItIs()
        {
            var (body, shape) = SomethingDrawn(sayItIsOurs: true, sayAnythingAtAll: true);

            body.position = new Vector3(0f, 0f, 5f);
            yield return null;

            Assert.That(shape.TrailingByMetres, Is.LessThan(0.01f),
                $"it is drawn {shape.TrailingByMetres:F2} m from where it is. A tenth of a second of " +
                "trailing is two metres at the speed a tractor does, which is a bag drawn hanging " +
                "out the back of the cart it is sitting in");
        }

        [UnityTest]
        public IEnumerator SomethingThatSaysNothingAboutWhoMovesItIsDrawnWhereItIs()
        {
            var (body, shape) = SomethingDrawn(sayItIsOurs: false, sayAnythingAtAll: false);

            body.position = new Vector3(0f, 0f, 5f);
            yield return null;

            Assert.That(shape.TrailingByMetres, Is.LessThan(0.01f),
                $"it is drawn {shape.TrailingByMetres:F2} m behind itself because nothing on it says " +
                "whether this machine moves it. Trailing hides a correction arriving from somewhere " +
                "else; something that never said it was a copy of anything has no corrections to hide");
        }

        [UnityTest]
        public IEnumerator AShapeTrailsByTheSameFractionHoweverFarTheBodyHasGone()
        {
            var vehicle = SomebodyElsesTractor();
            var shape = ShapeOf(vehicle);

            shape.CatchUpNow();
            vehicle.transform.position += new Vector3(0f, 0f, 4f);
            shape.Follow(1f / 60f);
            var afterFour = shape.TrailingByMetres / 4f;

            shape.CatchUpNow();
            vehicle.transform.position += new Vector3(0f, 0f, 60f);
            shape.Follow(1f / 60f);
            var afterSixty = shape.TrailingByMetres / 60f;

            Assert.That(afterSixty, Is.EqualTo(afterFour).Within(0.01f),
                "smoothing is one rate and it applies whatever the body did. A cutoff above which " +
                $"the shape gives up and arrives with the body -- {afterFour:P0} of the way behind " +
                $"over four metres, {afterSixty:P0} over sixty -- is a snap being hidden, and there " +
                "are no snaps left to hide");

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheShapeCatchesUpQuicklyEnoughNotToBeNoticed()
        {
            var vehicle = SomebodyElsesTractor();
            var shape = ShapeOf(vehicle);
            shape.CatchUpNow();

            vehicle.transform.position += new Vector3(0f, 0f, 4f);

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
        public IEnumerator YourOwnCharacterIsDrawnExactlyWhereItIs()
        {
            var crew = m_Apron.AddCrew(TestProfiles.CrewMember(), Vector3.zero);
            crew.gameObject.AddComponent<ApronAppearance>().Show("Player 1");
            var shape = crew.GetComponentInChildren<SmoothedLook>();
            shape.CatchUpNow();

            crew.transform.position += new Vector3(0f, 0f, 4f);
            shape.Follow(1f / 60f);

            Assert.That(shape.TrailingByMetres, Is.LessThan(0.01f),
                "the body you walk is the one body that must never trail. Smoothing only knew how to " +
                "ask a vehicle whether it was yours, so every player's own capsule was drawn a tenth " +
                "of a second behind their feet");

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

            shape.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shape.RememberHowItWasPlaced();
            shape.CatchUpNow();

            yield return Steps.Seconds(0.5f);

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

            var placedAt = new Vector3(0f, 0.8f, 0f);
            shape.transform.localPosition = placedAt;
            shape.RememberHowItWasPlaced();
            shape.CatchUpNow();

            vehicle.Body.position = new Vector3(0f, 0f, 6f);
            vehicle.transform.position = vehicle.Body.position;

            yield return Steps.Seconds(1f);

            var offset = vehicle.transform.InverseTransformPoint(shape.transform.position);

            Assert.That(Vector3.Distance(offset, placedAt), Is.LessThan(0.05f),
                $"the shape settled {offset} from its body rather than {placedAt}. Catching up by " +
                "writing the body's own position collapses every offset a model was authored with");
        }

        [UnityTest]
        public IEnumerator SomethingThatAlreadyHasAModelIsNotMovedOffIt()
        {
            var vehicle = m_Apron.AddVehicle(m_TractorProfile, "Tug 1", Vector3.zero, Quaternion.identity);

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

            Assert.That(model.transform.localPosition, Is.EqualTo(Vector3.zero).Using(Nearly.Within(0.01f)),
                "a grey primitive is built at the object's origin and has to be lifted to where the " +
                "bodywork is. A model is already there, and lifting it too puts the cart you see a " +
                "metre above the cart you drive into");
        }
    }
}
