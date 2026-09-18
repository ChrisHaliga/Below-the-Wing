using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

        sealed class Plate
        {
            public VisualElement Element;
            public VisualElement Ring;
            public VisualElement Tick;
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
            plate.Ring.style.backgroundColor = CrewPlate.Fill(crew.Ready);
            plate.Tick.style.display = CrewPlate.TickShows(crew.Ready) ? DisplayStyle.Flex : DisplayStyle.None;

            MenuLook.Edges(plate.Ring, CrewPlate.Ring(crew.Ready), 3);
        }

        Plate Raise()
        {
            var plate = new Plate
            {
                Element = new VisualElement { pickingMode = PickingMode.Ignore },
                Ring = new VisualElement { pickingMode = PickingMode.Ignore },
                Called = MenuLook.Text("", 18, MenuLook.Ink, MenuLook.Typeface.Body)
            };

            plate.Element.style.position = Position.Absolute;
            plate.Element.style.alignItems = Align.Center;
            plate.Element.style.translate = new Translate(Length.Percent(-50), Length.Percent(-100));

            plate.Ring.style.width = CrewPlate.RingPixels;
            plate.Ring.style.height = CrewPlate.RingPixels;
            plate.Ring.style.marginBottom = 8;
            plate.Ring.style.alignItems = Align.Center;
            plate.Ring.style.justifyContent = Justify.Center;
            Round(plate.Ring, CrewPlate.RingPixels * 0.5f);

            plate.Tick = Tick();
            plate.Ring.Add(plate.Tick);

            plate.Called.style.letterSpacing = 1.6f;
            plate.Called.style.marginBottom = 4;

            plate.Element.Add(plate.Ring);
            plate.Element.Add(plate.Called);

            Root.Add(plate.Element);

            return plate;
        }

        // Two bars at right angles rather than a glyph, so the mark does not depend on the font
        // carrying U+2713.
        static VisualElement Tick()
        {
            var tick = new VisualElement { pickingMode = PickingMode.Ignore };

            tick.style.width = CrewPlate.RingPixels;
            tick.style.height = CrewPlate.RingPixels;

            tick.Add(Bar(13f, 19f, 15f, -45f, hangsFromItsTop: true));
            tick.Add(Bar(23.6f, 9.6f, 20f, 45f, hangsFromItsTop: false));

            return tick;
        }

        const float TickThickness = 4f;

        // A bar runs down the screen from its origin. Pinned at its top and turned anticlockwise it
        // draws the short stroke down and to the right; pinned at its foot and turned clockwise it
        // draws the long stroke up and to the right from the same point.
        static VisualElement Bar(float left, float top, float length, float degrees, bool hangsFromItsTop)
        {
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };

            bar.style.position = Position.Absolute;
            bar.style.left = left;
            bar.style.top = top;
            bar.style.width = TickThickness;
            bar.style.height = length;
            bar.style.backgroundColor = MenuLook.Dark;
            bar.style.rotate = new Rotate(degrees);
            bar.style.transformOrigin = new TransformOrigin(
                Length.Percent(50),
                Length.Percent(hangsFromItsTop ? 0f : 100f));

            return bar;
        }

        static void Round(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
    }
}
