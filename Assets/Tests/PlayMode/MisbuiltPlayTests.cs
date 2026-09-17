using System.Collections;
using System.Text.RegularExpressions;
using BelowTheWing.Cargo;
using BelowTheWing.Crew;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class MisbuiltPlayTests
    {
        GameObject m_Object;

        [SetUp]
        public void SetUp() => m_Object = new GameObject("Unwired");

        [TearDown]
        public void TearDown() => Object.Destroy(m_Object);

        [UnityTest]
        public IEnumerator ABagWithNoProfileRefusesToRunRatherThanWeighingAKilogram()
        {
            var bag = m_Object.AddComponent<Bag>();
            LogAssert.Expect(LogType.Exception, new Regex("Unwired.*has no bag profile"));

            yield return null;

            Assert.That(bag.enabled, Is.False,
                "a bag with no profile weighs whatever its prefab said, and one that keeps running " +
                "looks like a physics bug for the rest of the game rather than the wiring fault it is");
        }

        [UnityTest]
        public IEnumerator ACrewMemberWithNoProfileRefusesToRun()
        {
            var crew = m_Object.AddComponent<CrewCharacter>();
            LogAssert.Expect(LogType.Exception, new Regex("Unwired.*has no crew profile"));

            yield return null;

            Assert.That(crew.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator AVehicleWithNoProfileRefusesToRun()
        {
            var vehicle = m_Object.AddComponent<VehicleController>();
            LogAssert.Expect(LogType.Exception, new Regex("Unwired.*has no vehicle profile"));

            yield return null;

            Assert.That(vehicle.enabled, Is.False);
        }
    }
}
