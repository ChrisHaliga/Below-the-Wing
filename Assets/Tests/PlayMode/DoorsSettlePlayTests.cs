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

            if (m_Hand != null)
            {
                Object.DestroyImmediate(m_Hand);
            }

            m_Apron.TearDown();
            Object.DestroyImmediate(m_CartProfile);
        }

        IReadOnlyList<SlidingDoorPole> ACartWithDoors()
        {
            m_Cart = TestDoorCart.Build(m_CartProfile);

            return m_Cart.GetComponentsInChildren<SlidingDoorPole>(true);
        }

        GameObject m_Hand;

        Transform AHand()
        {
            m_Hand = m_Hand != null ? m_Hand : new GameObject("Hand");
            return m_Hand.transform;
        }

        void LeftPartOpen(SlidingDoorPole pole, float openness)
        {
            pole.TakeHold(AHand());

            pole.transform.localPosition += pole.AlongTheRail * (pole.TravelMetres * openness);
            pole.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

            pole.LetGo();
        }

        IEnumerator AllTheWayOpen(SlidingDoorPole pole)
        {
            var hand = AHand();

            pole.TakeHold(hand);

            for (var step = 0; step < 120; step++)
            {
                hand.position = pole.transform.position + (pole.OpensToward * 0.5f);
                yield return new WaitForFixedUpdate();
            }

            pole.LetGo();

            yield return Steps.Seconds(2.5f);
        }

        [UnityTest]
        public IEnumerator ADoorLetGoBelowHalfwayEndsShut()
        {
            var doors = ACartWithDoors();

            LeftPartOpen(doors[0], 0.4f);

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

            LeftPartOpen(doors[0], 0.6f);

            yield return Steps.Seconds(3f);

            Assert.That(doors[0].Openness, Is.GreaterThan(0.95f),
                $"left at 0.60 open it settled at {doors[0].Openness:F2}");
        }

        [UnityTest]
        public IEnumerator ASeatedDoorIsNotMovedByABagDrivenIntoIt()
        {
            var doors = ACartWithDoors();

            yield return Steps.Seconds(1.5f);

            Assert.That(doors[0].Hooked, Is.True, "the door never hooked, so nothing is under test");

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
        public IEnumerator AHandHalfAMetreAlongTheRailOpensADoorWithoutBeingFlung()
        {
            var doors = ACartWithDoors();

            yield return Steps.Seconds(1.5f);

            Assert.That(doors[0].Hooked, Is.True, "the door never hooked, so nothing is under test");

            yield return AllTheWayOpen(doors[0]);

            Assert.That(doors[0].Openness, Is.GreaterThan(0.95f),
                $"a hand held half a metre along the rail got the door to {doors[0].Openness:F2}. " +
                "A grip that only bites once the player has sprinted away from the cart is a door " +
                "nobody can work from where they are standing");
        }

        [UnityTest]
        public IEnumerator AnOpenDoorIsNotKnockedShutByItsNeighbourBeingWorked()
        {
            var doors = ACartWithDoors();

            yield return Steps.Seconds(1.5f);
            yield return AllTheWayOpen(doors[0]);

            Assert.That(doors[0].Hooked, Is.True,
                "a door left at its open end is caught there, or nothing holds it against a knock");

            var was = doors[0].Openness;

            doors[1].GetComponent<Rigidbody>().AddForce(
                doors[1].OpensToward * 120f, ForceMode.Impulse);

            yield return Steps.Seconds(2f);

            Assert.That(Mathf.Abs(doors[0].Openness - was), Is.LessThan(0.05f),
                $"working the next door moved the open one from {was:F2} to " +
                $"{doors[0].Openness:F2}. An open door is held at its end the same way a shut one is");
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
            var wanted = shake * 4f * Time.fixedDeltaTime;

            cart.AddForce(doors[0].OpensToward * (wanted * cart.mass), ForceMode.Impulse);

            for (var step = 0; step < 6; step++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(doors[0].Hooked, Is.False,
                "the cart was knocked harder than the rail says throws a hook and the door stayed " +
                "hooked, so nothing a crash does can ever spill a load");
        }
    }
}
