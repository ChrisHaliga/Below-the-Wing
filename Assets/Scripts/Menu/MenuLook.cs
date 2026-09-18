using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace BelowTheWing.Menu
{
    public static class MenuLook
    {
        public static readonly Color Ink = new Color(0.93f, 0.95f, 0.95f);
        public static readonly Color InkSoft = new Color(0.62f, 0.68f, 0.69f);
        public static readonly Color InkFaint = new Color(0.44f, 0.50f, 0.51f);

        public static readonly Color Panel = new Color(0.055f, 0.070f, 0.078f, 0.96f);
        public static readonly Color Sunk = new Color(0.02f, 0.03f, 0.035f, 0.85f);
        public static readonly Color Edge = new Color(1f, 1f, 1f, 0.09f);
        public static readonly Color Hairline = new Color(1f, 1f, 1f, 0.16f);

        public static readonly Color HiVis = new Color(0.98f, 0.76f, 0.16f);
        public static readonly Color Rest = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color Hover = new Color(1f, 1f, 1f, 0.12f);
        public static readonly Color Bad = new Color(0.91f, 0.48f, 0.40f);
        public static readonly Color Good = new Color(0.47f, 0.80f, 0.55f);

        public const int Gutter = 64;

        public static VisualElement Screen(string name)
        {
            var screen = new VisualElement { name = name };

            Fill(screen);
            screen.style.flexDirection = FlexDirection.Row;
            screen.style.alignItems = Align.Center;

            return screen;
        }

        public static void Fill(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.right = 0;
            element.style.top = 0;
            element.style.bottom = 0;
        }

        public static VisualElement Shade(float opacity)
        {
            var shade = new VisualElement { pickingMode = PickingMode.Ignore };

            Fill(shade);
            shade.style.backgroundColor = new Color(0.02f, 0.03f, 0.04f, opacity);

            return shade;
        }

        public static VisualElement Card(float widthPixels)
        {
            var card = new VisualElement();

            card.style.width = widthPixels;
            card.style.marginLeft = Gutter;
            card.style.paddingLeft = 26;
            card.style.paddingRight = 26;
            card.style.paddingTop = 22;
            card.style.paddingBottom = 22;
            card.style.backgroundColor = Panel;

            Edges(card, Edge, 1);
            card.style.borderTopWidth = 2;
            card.style.borderTopColor = HiVis;

            return card;
        }

        public static Label Eyebrow(string text)
        {
            var label = Text(text, 10, InkFaint);

            label.style.letterSpacing = 2.4f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 10;

            return label;
        }

        public static Label Heading(string text, int size)
        {
            var label = Text(text, size, Ink);

            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.letterSpacing = size * 0.04f;

            return label;
        }

        public static Label Quiet(string text, int size = 13)
        {
            var label = Text(text, size, InkSoft);

            label.style.whiteSpace = WhiteSpace.Normal;

            return label;
        }

        public static Label Text(string text, int size, Color colour)
        {
            var label = new Label(text);

            label.style.color = colour;
            label.style.fontSize = size;

            return label;
        }

        public static VisualElement Rule()
        {
            var rule = new VisualElement();

            rule.style.height = 1;
            rule.style.backgroundColor = Edge;
            rule.style.marginTop = 14;
            rule.style.marginBottom = 14;

            return rule;
        }

        public static Button Press(string text, Action clicked, bool leading = false)
        {
            var button = new Button(clicked) { text = text };

            button.style.height = 40;
            button.style.marginLeft = 0;
            button.style.marginRight = 0;
            button.style.marginTop = 0;
            button.style.marginBottom = 6;
            button.style.paddingLeft = 14;
            button.style.fontSize = 14;
            button.style.color = leading ? HiVis : Ink;
            button.style.backgroundColor = Rest;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.style.unityFontStyleAndWeight = leading ? FontStyle.Bold : FontStyle.Normal;
            button.style.letterSpacing = 0.6f;

            Edges(button, Edge, 1);
            button.style.borderLeftWidth = 2;
            button.style.borderLeftColor = leading ? HiVis : Edge;

            button.RegisterCallback<MouseEnterEvent>(_ =>
            {
                button.style.backgroundColor = Hover;
                button.style.borderLeftColor = HiVis;
            });

            button.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                button.style.backgroundColor = Rest;
                button.style.borderLeftColor = leading ? HiVis : Edge;
            });

            return button;
        }

        public static VisualElement Row()
        {
            var row = new VisualElement();

            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;

            return row;
        }

        public static void Edges(VisualElement element, Color colour, float width)
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
