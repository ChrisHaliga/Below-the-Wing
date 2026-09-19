using System.Collections;
using BelowTheWing.Menu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace BelowTheWing.Tests.PlayMode
{
    public sealed class NameplateHoldsStillPlayTests
    {
        const string Called = "PLAYER 1";

        GameObject m_Document;
        GameObject m_Eye;
        MenuIcons m_Was;

        [SetUp]
        public void SetUp()
        {
            m_Was = MenuLook.Icons;

            MenuLook.Icons = new MenuIcons
            {
                Ready = ABadge(94, 97),
                Unready = ABadge(87, 87)
            };

            m_Eye = new GameObject("Eye");
            m_Eye.AddComponent<Camera>();
            m_Eye.transform.SetPositionAndRotation(new Vector3(0f, 1.5f, -6f), Quaternion.identity);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(MenuLook.Icons.Ready);
            Object.DestroyImmediate(MenuLook.Icons.Unready);
            MenuLook.Icons = m_Was;

            Object.DestroyImmediate(m_Eye);

            if (m_Document != null)
            {
                Object.DestroyImmediate(m_Document);
            }
        }

        static Texture2D ABadge(int width, int height)
        {
            var badge = new Texture2D(width, height);
            badge.SetPixels(new Color[width * height]);
            badge.Apply();
            return badge;
        }

        [UnityTest]
        public IEnumerator APlateDoesNotMoveTheFirstTimeItsCrewGoesReady()
        {
            var plates = new MenuNameplates();
            var root = APanelHolding(plates.Root);

            yield return null;
            yield return null;

            var eye = m_Eye.GetComponent<Camera>();
            var head = new Vector3(0f, 1.6f, 0f);

            plates.Show(new[] { new CrewOnStage(Called, false, head) }, eye);

            yield return null;
            yield return null;

            var unready = MiddleOfThePlate(root);

            plates.Show(new[] { new CrewOnStage(Called, true, head) }, eye);

            yield return null;
            yield return null;

            var ready = MiddleOfThePlate(root);

            Assert.That(ready.x, Is.EqualTo(unready.x).Within(0.5f),
                $"the plate's middle sits at x {unready.x:0.0} unready and {ready.x:0.0} ready, a " +
                $"jump of {ready.x - unready.x:0.0} px the first time somebody readies up. A badge " +
                "swap changes what is drawn in a box of a fixed size and must not move the box");

            Assert.That(ready.y, Is.EqualTo(unready.y).Within(0.5f),
                $"the plate's middle sits at y {unready.y:0.0} unready and {ready.y:0.0} ready");
        }

        [UnityTest]
        public IEnumerator APlateDoesNotMoveOnTheSecondReadyEither()
        {
            var plates = new MenuNameplates();
            var root = APanelHolding(plates.Root);

            var eye = m_Eye.GetComponent<Camera>();
            var head = new Vector3(0f, 1.6f, 0f);

            yield return null;
            yield return null;

            foreach (var ready in new[] { false, true, false })
            {
                plates.Show(new[] { new CrewOnStage(Called, ready, head) }, eye);
                yield return null;
                yield return null;
            }

            var settled = MiddleOfThePlate(root);

            plates.Show(new[] { new CrewOnStage(Called, true, head) }, eye);

            yield return null;
            yield return null;

            Assert.That(MiddleOfThePlate(root).x, Is.EqualTo(settled.x).Within(0.5f),
                "a plate that has already been readied once moves when readied again");
        }

        VisualElement APanelHolding(VisualElement plates)
        {
            m_Document = new GameObject("Menu document");

            var document = m_Document.AddComponent<UIDocument>();
            document.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            document.panelSettings.targetTexture = null;
            document.panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;

            document.rootVisualElement.Add(plates);

            return plates;
        }

        static Vector2 MiddleOfThePlate(VisualElement root)
        {
            var plate = root[0];

            Assert.That(plate.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex),
                "the plate is not displayed, so nothing about where it sits can be read");

            return plate[0].worldBound.center;
        }

        [UnityTest]
        public IEnumerator APlateDoesNotMoveWhenThePlaceholderNameIsReplacedByTheRealOne()
        {
            var plates = new MenuNameplates();
            var root = APanelHolding(plates.Root);

            var eye = m_Eye.GetComponent<Camera>();
            var head = new Vector3(0f, 1.6f, 0f);

            yield return null;
            yield return null;

            plates.Show(new[] { new CrewOnStage("You", false, head) }, eye);

            yield return null;
            yield return null;

            var placeholder = MiddleOfThePlate(root);

            plates.Show(new[] { new CrewOnStage(Called, false, head) }, eye);

            yield return null;
            yield return null;

            var real = MiddleOfThePlate(root);

            Assert.That(real.x, Is.EqualTo(placeholder.x).Within(0.5f),
                $"the badge sits at x {placeholder.x:0.0} over 'You' and {real.x:0.0} over " +
                $"'{Called}', a jump of {real.x - placeholder.x:0.0} px. The lobby shows 'You' " +
                "until the roster spawns and the seat's own name after, and a plate anchored to a " +
                "head has to stay over that head whatever the name under it says");
        }
    }
}
