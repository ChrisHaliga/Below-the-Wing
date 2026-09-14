using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class GripCurvePeakTests
    {
        VehicleProfile m_Profile;

        [SetUp]
        public void MakeAProfile() => m_Profile = ScriptableObject.CreateInstance<VehicleProfile>();

        [TearDown]
        public void PutItAway() => Object.DestroyImmediate(m_Profile);

        [Test]
        public void ThePeakOfASmoothCurveIsFoundEvenWhenItFallsBetweenTwoKeys()
        {
            m_Profile.lateralGripCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(1f, 6f, 6f, 0f),
                new Keyframe(3f, 6f, 0f, -6f),
                new Keyframe(8f, 4f));

            var most = WheelPhysics.MostGripPerKilogram(m_Profile);
            var betweenTheKeys = m_Profile.lateralGripCurve.Evaluate(2f);

            Assert.That(most, Is.GreaterThanOrEqualTo(betweenTheKeys - 0.001f),
                $"the curve reaches {betweenTheKeys:F2} N/kg between two of its keys and the most " +
                $"grip this reports is {most:F2}. Read off the keys alone, a smoothly shaped tyre " +
                "under-reports what it can hold with, the below-a-crawl hold weakens by the same " +
                "fraction, and parked trains start creeping across the apron again");
        }

        [Test]
        public void ReshapingTheCurveChangesWhatItReports()
        {
            m_Profile.lateralGripCurve = AnimationCurve.Linear(0f, 0f, 10f, 5f);
            var weak = WheelPhysics.MostGripPerKilogram(m_Profile);

            m_Profile.lateralGripCurve = AnimationCurve.Linear(0f, 0f, 10f, 20f);
            var strong = WheelPhysics.MostGripPerKilogram(m_Profile);

            Assert.That(strong, Is.GreaterThan(weak),
                "the peak is worked out once and kept, so a curve swapped afterwards has to be " +
                "noticed. Kept from the first curve it ever saw, a retuned tyre holds a parked " +
                "cart with a figure nobody can find in the asset");
        }
    }
}
