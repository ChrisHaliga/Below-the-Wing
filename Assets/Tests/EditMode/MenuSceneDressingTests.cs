using System.Collections.Generic;
using BelowTheWing.Crew;
using BelowTheWing.Menu;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BelowTheWing.Tests.EditMode
{
    public sealed class MenuSceneDressingTests
    {
        Scene m_Menu;

        [SetUp]
        public void OpenTheMenuScene()
            => m_Menu = EditorSceneManager.OpenScene(ShippedContent.MenuScenePath, OpenSceneMode.Additive);

        [TearDown]
        public void CloseTheMenuScene() => EditorSceneManager.CloseScene(m_Menu, removeScene: true);

        [Test]
        public void OnlyTheFirstTrainIsDressedIntoTheBackdrop()
        {
            var named = new List<string>();

            foreach (var thing in Everything())
            {
                if (thing.name.StartsWith("Tug ") || thing.name.StartsWith("Cart "))
                {
                    named.Add(thing.name);
                }
            }

            Assert.That(named, Has.Member("Tug 1"));
            Assert.That(named, Has.No.Member("Tug 2"),
                "a second tractor stands between the camera and the crew when the shot pulls back");

            foreach (var one in named)
            {
                Assert.That(one.StartsWith("Cart 2-"), Is.False, $"{one} belongs to the second train");
            }
        }

        [Test]
        public void TheBackdropHasAStationForEveryPlaceTheCameraStands()
        {
            var backdrop = Only<MenuBackdrop>();

            Assert.That(backdrop.WideShot, Is.Not.Null);
            Assert.That(backdrop.CartShot, Is.Not.Null);
            Assert.That(backdrop.InsideShot, Is.Not.Null);
        }

        [Test]
        public void TheDoorDriverKnowsWhichCartItOpens()
        {
            var doors = Only<MenuCartDoors>();

            Assert.That(doors.Cart, Is.Not.Null,
                "the cart's own VehicleController is switched off with the rest of its behaviours, " +
                "so the door rig is never raised unless the driver raises it");

            Assert.That(Only<MenuBackdrop>().CartDoors, Is.SameAs(doors),
                "a MenuCartDoors standing in the scene proves nothing on its own; the backdrop is " +
                "what the menu asks for it through");
        }

        [Test]
        public void ABeltLoaderStandsBehindTheCrew()
        {
            var backdrop = Only<MenuBackdrop>();

            Assert.That(backdrop.BeltLoader, Is.Not.Null);
            Assert.That(backdrop.BeltLoader.GetComponentsInChildren<Renderer>(true).Length,
                Is.GreaterThan(0));

            var toTheLoader = backdrop.BeltLoader.position - backdrop.FigureFor(2).transform.position;
            var toTheCamera = backdrop.InsideShot.position - backdrop.FigureFor(2).transform.position;

            Assert.That(Vector3.Dot(toTheLoader.normalized, toTheCamera.normalized), Is.LessThan(0f),
                "the loader is what the crew line up in front of, so it sits on the far side of " +
                "them from the camera");
        }

        [Test]
        public void EveryCrewFigureCarriesItsOwnGeometry()
        {
            var backdrop = Only<MenuBackdrop>();

            for (var crew = 0; crew < backdrop.CrewCount; crew++)
            {
                Assert.That(backdrop.FigureFor(crew).GetComponentsInChildren<Renderer>(true).Length,
                    Is.GreaterThan(0),
                    $"crew figure {crew} draws nothing. RampWorker.prefab carries no renderer of its " +
                    "own; ApronAppearance builds one at run time, and every behaviour on a dressed " +
                    "figure is switched off");
            }
        }

        [Test]
        public void TheBeltLoaderStandsOnItsWheelsRatherThanOnItsNose()
        {
            var box = DrawnBounds(Only<MenuBackdrop>().BeltLoader.gameObject);

            Assert.That(box.min.y, Is.EqualTo(0f).Within(0.05f),
                $"the loader's lowest point is at {box.min.y:0.00} m, so it is sunk into the apron " +
                "or hanging over it");

            Assert.That(box.size.y, Is.LessThan(2f),
                $"the loader stands {box.size.y:0.00} m tall. belt_loader.fbx is 0.79 m tall and " +
                "4.68 m long, so a tall box means it has been tipped onto an end. Its root carries " +
                "the importer's own rotation, and overwriting that rotation is what tips it");
        }

        [Test]
        public void ANameplateHangsJustClearOfTheTopOfACrewMembersHead()
        {
            var backdrop = Only<MenuBackdrop>();
            var tall = ShippedContent.Load<CrewProfile>(ShippedContent.CrewProfilePath).heightMetres;

            var feet = backdrop.FigureFor(0).transform.position.y - (tall * 0.5f);
            var plate = backdrop.PlateOver(0).y - feet;

            Assert.That(plate, Is.InRange(tall, tall * 1.15f),
                $"the plate floats {plate:0.00} m up a {tall:0.00} m figure. Below the crown it " +
                "lands on the body, and far above it stops reading as that person's plate");
        }

        static Bounds DrawnBounds(GameObject thing)
        {
            var drawn = thing.GetComponentsInChildren<Renderer>(true);

            Assert.That(drawn.Length, Is.GreaterThan(0), $"{thing.name} draws nothing");

            var box = drawn[0].bounds;

            foreach (var one in drawn)
            {
                box.Encapsulate(one.bounds);
            }

            return box;
        }

        [Test]
        public void TheCrewStandInEvenlySpacedSlots()
        {
            var backdrop = Only<MenuBackdrop>();
            var gaps = new List<float>();

            for (var crew = 1; crew < backdrop.CrewCount; crew++)
            {
                gaps.Add(Vector3.Distance(
                    backdrop.FigureFor(crew).transform.position,
                    backdrop.FigureFor(crew - 1).transform.position));
            }

            foreach (var gap in gaps)
            {
                Assert.That(gap, Is.EqualTo(gaps[0]).Within(1e-3f));
            }
        }

        [Test]
        public void TheDoorDriverDoesNotRideOnACartPrefabInstance()
        {
            var doors = Only<MenuCartDoors>();

            Assert.That(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(doors.gameObject), Is.False,
                "a component added to a prefab instance is an override, and reimporting the prefab " +
                "rebuilds the instance and drops it. The rebuild writes BaggageCart.prefab and " +
                "Menu.unity in the same pass, so that reimport happens every time");
        }

        [Test]
        public void TheBackdropHoldsAFigureForEveryCrewSlot()
            => Assert.That(Only<MenuBackdrop>().CrewCount, Is.EqualTo(Wiring.Shift.MostCrew));

        T Only<T>() where T : Component
        {
            var found = new List<T>();

            foreach (var thing in Everything())
            {
                found.AddRange(thing.GetComponents<T>());
            }

            Assert.That(found.Count, Is.EqualTo(1), $"the menu scene holds {found.Count} of {typeof(T).Name}");

            return found[0];
        }

        IEnumerable<GameObject> Everything()
        {
            foreach (var root in m_Menu.GetRootGameObjects())
            {
                foreach (var thing in root.GetComponentsInChildren<Transform>(true))
                {
                    yield return thing.gameObject;
                }
            }
        }
    }
}
