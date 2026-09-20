using System.Collections;
using System.Collections.Generic;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class DoorsSettlePlayTests
    {
        TestApron m_Apron;
        VehicleProfile m_CartProfile;
        GameObject m_Cart;

        [SetUp]
        public void SetUp()
        {
            m_Apron = new TestApron();
            m_CartProfile = TestProfiles.Cart();
            m_CartProfile.doorRail = DoorRailSettings.Default;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Cart != null)
            {
                Object.DestroyImmediate(m_Cart);
            }

            m_Apron.TearDown();
            Object.DestroyImmediate(m_CartProfile);
        }

        IReadOnlyList<SlidingDoorPole> ACartWithDoors()
        {
            m_Cart = TestDoorCart.Build(m_CartProfile);

            return m_Cart.GetComponentsInChildren<SlidingDoorPole>(true);
        }

        static void Put(SlidingDoorPole pole, float openness)
        {
            var body = pole.GetComponent<Rigidbody>();

            pole.transform.localPosition += pole.AlongTheRail * (pole.TravelMetres * openness);
            body.linearVelocity = Vector3.zero;
        }

        [UnityTest]
        public IEnumerator ADoorLetGoBelowHalfwayEndsShut()
        {
            var doors = ACartWithDoors();

            Put(doors[0], 0.4f);

            yield return Steps.Seconds(3f);

            Assert.That(doors[0].Openness, Is.LessThan(0.05f),
                $"left at 0.40 open it settled at {doors[0].Openness:F2}. A door below halfway " +
                "belongs shut, and one that stops wherever its drag ran out is the intermediate " +
                "spot this exists to remove");
        }

        [UnityTest]
        public IEnumerator ADoorLetGoAboveHalfwayEndsOpen()
        {
            var doors = ACartWithDoors();

            Put(doors[0], 0.6f);

            yield return Steps.Seconds(3f);

            Assert.That(doors[0].Openness, Is.GreaterThan(0.95f),
                $"left at 0.60 open it settled at {doors[0].Openness:F2}");
        }

        [UnityTest]
        public IEnumerator ASeatedDoorIsNotMovedByABagDrivenIntoIt()
        {
            var doors = ACartWithDoors();

            yield return Steps.Seconds(1.5f);

            Assert.That(doors[0].Latched, Is.True, "the door never latched, so nothing is under test");

            var bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bag.transform.localScale = new Vector3(0.4f, 0.25f, 0.6f);
            bag.transform.position = doors[0].transform.position - (doors[0].OpensToward * 0.6f);

            var body = bag.AddComponent<Rigidbody>();
            body.mass = 20f;
            body.useGravity = false;
            body.linearVelocity = doors[0].OpensToward * 4f;

            yield return Steps.Seconds(1.5f);

            Object.DestroyImmediate(bag);

            Assert.That(doors[0].Openness, Is.LessThan(0.05f),
                $"a 20 kg bag at 4 m/s left the door {doors[0].Openness:F2} open. A player who shut " +
                "a door should find it shut");
        }

        [UnityTest]
        public IEnumerator SlammingOneDoorLeavesTheOthersWhereTheyWere()
        {
            var doors = ACartWithDoors();

            yield return Steps.Seconds(1.5f);

            var slammed = doors[0];
            var before = new float[doors.Count];

            for (var door = 0; door < doors.Count; door++)
            {
                before[door] = doors[door].Openness;
            }

            slammed.GetComponent<Rigidbody>().AddForce(
                slammed.OpensToward * 120f, ForceMode.Impulse);

            yield return Steps.Seconds(2f);

            for (var door = 0; door < doors.Count; door++)
            {
                if (ReferenceEquals(doors[door], slammed))
                {
                    continue;
                }

                var moved = Mathf.Abs(doors[door].Openness - before[door]);

                Assert.That(moved, Is.LessThan(0.05f),
                    $"slamming one door moved door {door + 1} by {moved:F2} of its travel. Every " +
                    "door is jointed to the same cart body, so a shove drives the cart the other " +
                    "way and anything loose slides with it");
            }
        }

        [UnityTest]
        public IEnumerator AHandThrowsTheHookAndOpensAShutDoor()
        {
            var doors = ACartWithDoors();

            yield return Steps.Seconds(1.5f);

            Assert.That(doors[0].Hooked, Is.True, "the door never hooked, so nothing is under test");

            var body = doors[0].GetComponent<Rigidbody>();

            doors[0].TakeHold();

            for (var step = 0; step < 120; step++)
            {
                body.AddForce(doors[0].OpensToward * 200f);
                yield return new WaitForFixedUpdate();
            }

            doors[0].LetGo();

            Assert.That(doors[0].Openness, Is.GreaterThan(0.5f),
                $"a hand pulling a door it has hold of reached {doors[0].Openness:F2} open. A hook " +
                "a hand cannot throw is a door that never opens");
        }

        [UnityTest]
        public IEnumerator NothingShortOfAHandOrAKnockOpensAShutDoor()
        {
            var doors = ACartWithDoors();

            yield return Steps.Seconds(1.5f);

            Assert.That(doors[0].Hooked, Is.True, "the door never hooked, so nothing is under test");

            var body = doors[0].GetComponent<Rigidbody>();

            for (var step = 0; step < 120; step++)
            {
                body.AddForce(doors[0].OpensToward * 400f);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(doors[0].Openness, Is.LessThan(0.05f),
                $"400 N of steady shove left the door {doors[0].Openness:F2} open. A cart door is " +
                "hooked shut, and a load shifting against it is not a hand");
        }

        [UnityTest]
        public IEnumerator AKnockHardEnoughThrowsTheHook()
        {
            var doors = ACartWithDoors();

            yield return Steps.Seconds(1.5f);

            Assert.That(doors[0].Hooked, Is.True, "the door never hooked, so nothing is under test");

            var cart = m_Cart.GetComponent<Rigidbody>();
            var shake = m_CartProfile.doorRail.unhooksAboveMetresPerSecondSquared;

            cart.linearVelocity = doors[0].OpensToward * (shake * 4f * Time.fixedDeltaTime);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(doors[0].Hooked, Is.False,
                "the cart was knocked harder than the rail says throws a hook and the door stayed " +
                "hooked, so nothing a crash does can ever spill a load");
        }
    }
}
