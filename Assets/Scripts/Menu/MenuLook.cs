using UnityEngine;
using UnityEngine.UIElements;

namespace BelowTheWing.Menu
{
    public static class MenuLook
    {
        public static readonly Color Ink = new Color(0.94f, 0.95f, 0.94f);
        public static readonly Color InkSoft = new Color(0.72f, 0.76f, 0.76f);
        public static readonly Color Panel = new Color(0.06f, 0.08f, 0.09f, 0.82f);
        public static readonly Color Edge = new Color(0.30f, 0.36f, 0.37f, 0.9f);
        public static readonly Color HiVis = new Color(0.95f, 0.75f, 0.15f);
        public static readonly Color Deep = new Color(0.04f, 0.26f, 0.30f);

        public static VisualElement Screen(string name)
        {
            var screen = new VisualElement { name = name };

            screen.style.position = Position.Absolute;
            screen.style.left = 0;
            screen.style.right = 0;
            screen.style.top = 0;
            screen.style.bottom = 0;
            screen.style.paddingLeft = 48;
            screen.style.paddingRight = 48;
            screen.style.paddingTop = 48;
            screen.style.paddingBottom = 48;

            return screen;
        }

        public static VisualElement Card(float widthPixels)
        {
            var card = new VisualElement();

            card.style.width = widthPixels;
            card.style.paddingLeft = 28;
            card.style.paddingRight = 28;
            card.style.paddingTop = 24;
            card.style.paddingBottom = 24;
            card.style.backgroundColor = Panel;
            Border(card, Edge, 1);

            return card;
        }

        public static Label Heading(string text, int size)
        {
            var label = new Label(text);

            label.style.color = Ink;
            label.style.fontSize = size;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.letterSpacing = size * 0.06f;
            label.style.marginBottom = 4;

            return label;
        }

        public static Label Quiet(string text)
        {
            var label = new Label(text);

            label.style.color = InkSoft;
            label.style.fontSize = 13;
            label.style.whiteSpace = WhiteSpace.Normal;

            return label;
        }

        public static Button Press(string text, System.Action clicked)
        {
            var button = new Button(clicked) { text = text };

            button.style.height = 42;
            button.style.marginLeft = 0;
            button.style.marginRight = 0;
            button.style.marginTop = 4;
            button.style.marginBottom = 4;
            button.style.fontSize = 15;
            button.style.color = Ink;
            button.style.backgroundColor = Deep;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            Border(button, Edge, 1);

            button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = HiVis);
            button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = Deep);

            return button;
        }

        public static VisualElement Row()
        {
            var row = new VisualElement();

            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginTop = 6;
            row.style.marginBottom = 6;

            return row;
        }

        public static void Border(VisualElement element, Color colour, float width)
        {
            element.style.borderTopColor = colour;
            element.style.borderRightColor = colour;
            element.style.borderBottomColor = colour;
            element.style.borderLeftColor = colour;

            element.style.borderTopWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
        }
    }
}
