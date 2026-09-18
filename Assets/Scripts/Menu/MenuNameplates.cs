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
            public Label Called;
            public Label State;
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
            plate.State.text = CrewPlate.Says(crew.Ready);
            plate.State.style.color = CrewPlate.Colour(crew.Ready);
        }

        Plate Raise()
        {
            var plate = new Plate
            {
                Element = new VisualElement { pickingMode = PickingMode.Ignore },
                Called = MenuLook.Text("", 17, MenuLook.Ink, MenuLook.Typeface.Body),
                State = MenuLook.Eyebrow("", MenuLook.InkSoft)
            };

            plate.Element.style.position = Position.Absolute;
            plate.Element.style.alignItems = Align.Center;
            plate.Element.style.translate = new Translate(Length.Percent(-50), Length.Percent(-100));

            plate.Called.style.letterSpacing = 1.6f;
            plate.Called.style.marginBottom = 3;
            plate.State.style.marginBottom = 10;

            plate.Element.Add(plate.Called);
            plate.Element.Add(plate.State);

            Root.Add(plate.Element);

            return plate;
        }
    }
}
