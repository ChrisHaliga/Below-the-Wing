using System.Collections;
using System.Reflection;
using BelowTheWing.Menu;
using BelowTheWing.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class MenuWiresItselfPlayTests
    {
        [UnityTearDown]
        public IEnumerator TearDown() => LoadedScene.Close();

        static readonly string[] SceneReferences =
        {
            "m_Gateway", "m_Camera", "m_Backdrop"
        };

        [UnityTest]
        public IEnumerator TheMenuFindsWhatItDrivesEvenWithNothingWiredInTheInspector()
        {
            yield return LoadedScene.Open("Menu");

            var driver = Object.FindAnyObjectByType<MenuDriver>();
            Assert.That(driver, Is.Not.Null, "precondition: the scene has a MenuDriver");

            var fresh = new GameObject("Unwired menu");
            fresh.SetActive(false);

            var document = fresh.AddComponent<UIDocument>();
            document.panelSettings = driver.GetComponent<UIDocument>().panelSettings;

            var unwired = fresh.AddComponent<MenuDriver>();

            foreach (var name in SceneReferences)
            {
                Assert.That(Field(name).GetValue(unwired), Is.Null,
                    $"precondition: {name} starts unset on a component nobody wired");
            }

            Object.Destroy(driver.gameObject);

            yield return null;

            fresh.SetActive(true);

            yield return null;

            foreach (var name in SceneReferences)
            {
                Assert.That(Field(name).GetValue(unwired), Is.Not.Null,
                    $"{name} is still unset. A serialized reference to something already in the " +
                    "scene is lost whenever the component round-trips through a missing script, " +
                    "and the menu then refuses to start with nothing a player can do about it");
            }

            Assert.That(unwired.GetComponent<UIDocument>().rootVisualElement.childCount,
                Is.GreaterThan(0), "the panels were never built");

            Object.Destroy(fresh);
        }

        static FieldInfo Field(string name)
        {
            var field = typeof(MenuDriver).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"MenuDriver has no field called {name}");

            return field;
        }
    }
}
