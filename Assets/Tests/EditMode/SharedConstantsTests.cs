using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace BelowTheWing.Tests.EditMode
{
    /// <summary>
    /// Arithmetic that has to agree with the simulation it is describing.
    ///
    /// Resting height is what decides where a vehicle's coupling point sits, and two vehicles whose
    /// coupling points do not meet lean into the difference until a parked train wanders off across
    /// the apron. That calculation reading a different gravity from the one physics is using is
    /// exactly the sort of quiet disagreement that produced it in the first place.
    /// </summary>
    public sealed class SharedConstantsTests
    {
        VehicleProfile m_Tractor;
        Vector3 m_RealGravity;

        [SetUp]
        public void SetUp()
        {
            m_Tractor = TestProfiles.Tractor();
            m_RealGravity = Physics.gravity;
        }

        [TearDown]
        public void TearDown()
        {
            Physics.gravity = m_RealGravity;
            Object.DestroyImmediate(m_Tractor);
        }

        [Test]
        public void RestingHeightFollowsTheGravityPhysicsIsActuallyUsing()
        {
            var onEarth = VehicleController.RestingHeightMetres(m_Tractor);

            Physics.gravity = m_RealGravity * 2f;
            var underTwiceTheWeight = VehicleController.RestingHeightMetres(m_Tractor);

            Assert.That(underTwiceTheWeight, Is.LessThan(onEarth),
                "twice the weight compresses the springs further, so the vehicle sits lower. A hard-coded " +
                "9.81 here would put the coupling point somewhere physics disagrees with");
        }

        [Test]
        public void RollingResistanceFollowsTheGravityPhysicsIsActuallyUsing()
        {
            const float load = 750f;
            var onEarth = Mathf.Abs(WheelPhysics.RollingResistance(4f, load, 0.02f, m_Tractor));

            Physics.gravity = m_RealGravity * 2f;
            var underTwiceTheWeight = Mathf.Abs(WheelPhysics.RollingResistance(4f, load, 0.02f, m_Tractor));

            Assert.That(underTwiceTheWeight, Is.GreaterThan(onEarth),
                "a tire carrying twice the weight costs more to roll");
        }
    }
}
