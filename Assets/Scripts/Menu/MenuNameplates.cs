using System.Collections.Generic;
using BelowTheWing.Wiring;
using UnityEngine.UIElements;
using UnityEngine;

namespace BelowTheWing.Menu
{
    public sealed class MenuNameplates
    {
        readonly List<Plate> m_Plates = new List<Plate>();

        public MenuNameplates()
        {
            Root = MenuLook.Screen("nameplates");
            Root.pickingMode = PickingMode.Ignore;
        }

        public VisualElement Root { get; }

        const float PlateWidthPixels = 220f;

        sealed class Plate
        {
            public VisualElement Element;
            public VisualElement Badge;
            public Label Called;
        }

        public void Show(IReadOnlyList<CrewOnStage> crew, Camera eye)
        {
            var panel = new Vector2(Root.resolvedStyle.width, Root.resolvedStyle.height);

            while (m_Plates.Count < crew.Count)
            {
                m_Plates.Add(Raise());
            }

            for (var plate = 0; plate < m_Plates.Count; plate++)
            {
                Place(m_Plates[plate], plate < crew.Count ? crew[plate] : default, plate < crew.Count, eye, panel);
            }
        }

        static void Place(Plate plate, CrewOnStage crew, bool standing, Camera eye, Vector2 panel)
        {
            var viewport = standing && eye != null ? eye.WorldToViewportPoint(crew.Head) : Vector3.back;

            if (!standing || !CrewPlate.Visible(viewport) || panel.x <= 0f)
            {
                plate.Element.style.display = DisplayStyle.None;
                return;
            }

            var at = CrewPlate.OnPanel(viewport, panel);

            plate.Element.style.display = DisplayStyle.Flex;
            plate.Element.style.left = at.x;
            plate.Element.style.top = at.y;

            plate.Called.text = crew.Called.ToUpperInvariant();

            var badge = CrewPlate.Badge(crew.Ready);

            plate.Badge.style.backgroundImage = badge != null
                ? Background.FromTexture2D(badge)
                : new StyleBackground(StyleKeyword.None);
        }

        Plate Raise()
        {
            var plate = new Plate
            {
                Element = new VisualElement { pickingMode = PickingMode.Ignore },
                Badge = new VisualElement { pickingMode = PickingMode.Ignore },
                Called = MenuLook.Text("", 18, Palette.Ink, MenuLook.Typeface.Body)
            };

            plate.Element.style.position = Position.Absolute;
            plate.Element.style.alignItems = Align.Center;
            plate.Element.style.width = PlateWidthPixels;
            plate.Element.style.translate = new Translate(-PlateWidthPixels * 0.5f, Length.Percent(-100));

            plate.Badge.style.width = CrewPlate.BadgePixels;
            plate.Badge.style.height = CrewPlate.BadgePixels;
            plate.Badge.style.marginBottom = 8;
            plate.Badge.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);

            plate.Called.style.letterSpacing = 1.6f;
            plate.Called.style.marginBottom = 4;
            plate.Called.style.whiteSpace = WhiteSpace.NoWrap;
            plate.Called.style.unityTextAlign = TextAnchor.MiddleCenter;
            plate.Called.style.overflow = Overflow.Visible;

            plate.Element.Add(plate.Badge);
            plate.Element.Add(plate.Called);

            Root.Add(plate.Element);

            return plate;
        }

    }
}
