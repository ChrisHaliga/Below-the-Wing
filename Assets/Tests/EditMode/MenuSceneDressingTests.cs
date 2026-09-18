using System.Collections.Generic;
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
        public void TheStagedCartCanBeOpenedWithoutItsPhysics()
        {
            var doors = Only<MenuCartDoors>();

            Assert.That(doors.LeafCount, Is.GreaterThan(0),
                "the menu cart's own door driver is switched off with the rest of its behaviours, " +
                "so the reveal has nothing to move unless the leaves are wired at build time");

            Assert.That(Only<MenuBackdrop>().CartDoors, Is.SameAs(doors),
                "a MenuCartDoors standing in the scene proves nothing on its own; the backdrop is " +
                "what the menu asks for it through");
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
