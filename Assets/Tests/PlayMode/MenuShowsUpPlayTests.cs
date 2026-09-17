using System.Collections;
using BelowTheWing.Menu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class MenuShowsUpPlayTests
    {
        [UnityTest]
        public IEnumerator TheApronSceneOpensOnATitleCardWithSomethingOnIt()
        {
            SceneManager.LoadScene("Apron", LoadSceneMode.Single);

            yield return null;
            yield return null;
            yield return null;

            var driver = Object.FindAnyObjectByType<MenuDriver>();
            Assert.That(driver, Is.Not.Null, "the scene has no MenuDriver in it at all");

            var document = driver.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null, "the menu object has no UIDocument");
            Assert.That(document.panelSettings, Is.Not.Null,
                "the document has no panel settings, so nothing it holds is ever drawn");
            Assert.That(document.panelSettings.themeStyleSheet, Is.Not.Null,
                "the panel has no theme, and every control in an unthemed runtime panel renders " +
                "as nothing at all");

            var root = document.rootVisualElement;
            Assert.That(root, Is.Not.Null, "the document built no root element");

            Debug.Log($"MENUDUMP children={root.childCount} display={root.resolvedStyle.display} " +
                      $"width={root.resolvedStyle.width} height={root.resolvedStyle.height}");

            foreach (var child in root.Children())
            {
                Debug.Log($"MENUDUMP  screen '{child.name}' display={child.resolvedStyle.display} " +
                          $"children={child.childCount} width={child.resolvedStyle.width} " +
                          $"height={child.resolvedStyle.height}");
            }

            Assert.That(root.childCount, Is.GreaterThan(0),
                "the document's root is empty, so the panels were never built");

            var title = root.Q<VisualElement>("title");
            Assert.That(title, Is.Not.Null, "there is no title screen in the tree");
            Assert.That(title.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex),
                "the title screen is in the tree but not displayed");
            Assert.That(title.resolvedStyle.width, Is.GreaterThan(1f),
                "the title screen has no width, so nothing in it can be seen");
        }

        [UnityTest]
        public IEnumerator TheApronSceneHasACameraLookingAtSomething()
        {
            SceneManager.LoadScene("Apron", LoadSceneMode.Single);

            yield return null;
            yield return null;
            yield return null;

            var on = 0;

            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                Debug.Log($"MENUDUMP camera '{camera.name}' enabled={camera.enabled} " +
                          $"active={camera.gameObject.activeInHierarchy} at {camera.transform.position}");

                if (camera.enabled && camera.gameObject.activeInHierarchy)
                {
                    on++;
                }
            }

            Assert.That(on, Is.EqualTo(1),
                $"{on} cameras are rendering. None and the screen is black; two and they fight");
        }
    }
}
