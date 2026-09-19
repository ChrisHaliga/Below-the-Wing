using System.Collections.Generic;
using BelowTheWing.Menu;
using BelowTheWing.Wiring;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

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
        public void TheMenuSceneRendersThroughOneCameraUnderOneSun()
        {
            var cameras = 0;
            var lights = 0;

            foreach (var thing in Everything())
            {
                foreach (var camera in thing.GetComponents<Camera>())
                {
                    cameras += camera.enabled ? 1 : 0;
                }

                foreach (var light in thing.GetComponents<Light>())
                {
                    lights += light.enabled && light.type == LightType.Directional ? 1 : 0;
                }
            }

            Assert.That(cameras, Is.EqualTo(1),
                $"{cameras} cameras are enabled. A model exported from Blender carries the camera " +
                "and light it was authored with, and a raw model parked in the scene brings them along");
            Assert.That(lights, Is.EqualTo(1), $"{lights} directional lights are enabled");
        }

        [Test]
        public void TheBackdropHasAStationForEveryPlaceTheCameraStands()
        {
            var backdrop = Only<MenuBackdrop>();

            Assert.That(backdrop.CartShot, Is.Not.Null);
            Assert.That(backdrop.InsideShot, Is.Not.Null);
        }

        [Test]
        public void TheMenuHasBothReadinessIconsWiredIntoIt()
        {
            var driver = new UnityEditor.SerializedObject(Only<MenuDriver>());

            foreach (var field in new[] { "m_ReadyIcon", "m_UnreadyIcon" })
            {
                var wired = driver.FindProperty(field);

                Assert.That(wired, Is.Not.Null, $"MenuDriver has no {field}");
                Assert.That(wired.objectReferenceValue, Is.Not.Null,
                    $"{field} is empty, so a nameplate draws no badge at all");
            }
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
        public void TheBackdropStillNamesEveryPieceOfDressingTheMenuExpects()
        {
            var backdrop = Only<MenuBackdrop>();

            Assert.That(backdrop.BeltLoader, Is.Not.Null, "no belt loader");
            Assert.That(backdrop.BeltLoader.GetComponentsInChildren<Renderer>(true).Length,
                Is.GreaterThan(0), "the belt loader draws nothing");
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
