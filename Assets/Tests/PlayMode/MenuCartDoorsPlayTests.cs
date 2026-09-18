using System.Collections;
using BelowTheWing.Menu;
using BelowTheWing.Tests.Support;
using BelowTheWing.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class MenuCartDoorsPlayTests
    {
        [UnityTearDown]
        public IEnumerator TearDown() => LoadedScene.Close();

        [UnityTest]
        public IEnumerator TheMenuRaisesADoorRigOnACartWhoseOwnBehavioursAreSwitchedOff()
        {
            yield return LoadedScene.Open("Menu");

            var doors = Object.FindAnyObjectByType<MenuCartDoors>();
            Assert.That(doors, Is.Not.Null);

            var poles = doors.Cart.GetComponentsInChildren<SlidingDoorPole>(true);

            Assert.That(poles.Length, Is.EqualTo(4),
                "the cart carries four door leaves, and its own VehicleController never runs in " +
                "the menu to raise them");

            foreach (var pole in poles)
            {
                Assert.That(pole.enabled, Is.True, $"{pole.name} is switched off");
                Assert.That(pole.GetComponent<Rigidbody>().isKinematic, Is.False,
                    $"{pole.name} cannot be shoved while it is kinematic");
            }
        }

        [UnityTest]
        public IEnumerator TheDoorsStayShutWhileNobodyHasAskedForTheLobby()
        {
            yield return LoadedScene.Open("Menu");

            var doors = Object.FindAnyObjectByType<MenuCartDoors>();

            yield return Settle(1.5f);

            Assert.That(doors.Openness, Is.LessThan(0.05f),
                $"the doors drifted to {doors.Openness:0.00} open with nothing asking them to. The " +
                "settle pull holds a pole at whatever the rail is aimed at, and an unaimed rail " +
                "aims at its own zero, which is halfway between shut and open");
        }

        [UnityTest]
        public IEnumerator AShovedDoorSlidesOpenAndStaysOpen()
        {
            yield return LoadedScene.Open("Menu");

            var doors = Object.FindAnyObjectByType<MenuCartDoors>();

            Assert.That(doors.Openness, Is.EqualTo(0f).Within(0.02f), "the doors do not start shut");

            doors.Open(true);

            yield return Settle(3f);

            Assert.That(doors.Openness, Is.GreaterThan(0.9f),
                $"a shove of the configured size left the doors at {doors.Openness:0.00} open. " +
                "Below about 40 newton-seconds the rail drag stops the pole short of the end");
        }

        [UnityTest]
        public IEnumerator ADoorShovedShutComesAllTheWayBack()
        {
            yield return LoadedScene.Open("Menu");

            var doors = Object.FindAnyObjectByType<MenuCartDoors>();

            doors.Open(true);

            yield return Settle(5f);

            doors.Open(false);

            yield return Settle(5f);

            Assert.That(Widest(doors, near: true), Is.LessThan(0.08f),
                $"the near doors settled at {Widest(doors, near: true):0.00} open");

            Assert.That(Widest(doors, near: false), Is.LessThan(0.08f),
                $"the far doors settled at {Widest(doors, near: false):0.00} open");
        }

        [UnityTest]
        public IEnumerator TheDoorsBehindTheCameraStandStillUntilTheirTurn()
        {
            yield return LoadedScene.Open("Menu");

            var doors = Object.FindAnyObjectByType<MenuCartDoors>();

            doors.Open(true);

            yield return Settle(1.5f);

            Assert.That(Widest(doors, near: true), Is.GreaterThan(0.85f),
                "the near doors should be thrown open well inside a second and a half");

            Assert.That(Widest(doors, near: false), Is.LessThan(0.05f),
                $"the far doors are {Widest(doors, near: false):0.00} open with four seconds still " +
                "to wait. A pole aimed at its far end before it is shoved is handed to the settle " +
                "pull early, and creeps rather than waiting");
        }

        static float Widest(MenuCartDoors doors, bool near)
        {
            var most = 0f;

            foreach (var pole in doors.Cart.GetComponentsInChildren<SlidingDoorPole>(true))
            {
                if (MenuCartDoors.NearTheCamera(pole.transform.localPosition.x) == near)
                {
                    most = Mathf.Max(most, pole.Openness);
                }
            }

            return most;
        }

        static IEnumerator Settle(float seconds)
        {
            for (var waited = 0f; waited < seconds; waited += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
