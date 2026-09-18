using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BelowTheWing.Menu
{
    public struct MenuTypeface
    {
        public Font Display;
        public Font Body;
        public Font Data;
    }

    public static class MenuLook
    {
        public static readonly Color Ink = new Color(0.96f, 0.97f, 0.97f);
        public static readonly Color InkSoft = new Color(0.66f, 0.71f, 0.72f);
        public static readonly Color InkFaint = new Color(0.40f, 0.46f, 0.47f);

        public static readonly Color HiVis = new Color(0.99f, 0.78f, 0.13f);
        public static readonly Color Good = new Color(0.44f, 0.82f, 0.53f);
        public static readonly Color Bad = new Color(0.93f, 0.44f, 0.36f);
        public static readonly Color Dark = new Color(0.035f, 0.045f, 0.052f);

        public const int Gutter = 96;
        public const int ColumnWidth = 520;

        public const int TitleSize = 42;
        public const int SectionSize = 22;
        public const int RowSize = 20;
        public const int ButtonSize = 20;
        public const int HintSize = 15;

        public static readonly Color PanelInk = new Color(0.10f, 0.11f, 0.12f, 0.88f);
        public static readonly Color PanelEdge = new Color(0.42f, 0.46f, 0.48f, 0.65f);
        public static readonly Color KeyCap = new Color(1f, 1f, 1f, 0.14f);
        public const float TypeSettleSeconds = 0.35f;

        public static MenuTypeface Typeface;

        public static VisualElement Screen(string name)
        {
            var screen = new VisualElement { name = name };

            Fill(screen);

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

        // A panel standing from the top of the screen to the bottom, holding whatever the screen
        // puts on it, with its trailing edge faded out so it does not cut the scene in half.
        public static VisualElement Scrim(float widthFraction, float darkest)
        {
            var scrim = new VisualElement { pickingMode = PickingMode.Ignore };

            scrim.style.position = Position.Absolute;
            scrim.style.left = 0;
            scrim.style.top = 0;
            scrim.style.bottom = 0;
            scrim.style.width = Length.Percent(Mathf.Clamp01(widthFraction) * 100f);

            scrim.style.backgroundImage = Background.FromTexture2D(SideToSide(darkest));
            scrim.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            scrim.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);

            return scrim;
        }

        public static VisualElement Dim(float darkest)
        {
            var dim = new VisualElement { pickingMode = PickingMode.Ignore };

            Fill(dim);

            dim.style.backgroundColor = new Color(Dark.r, Dark.g, Dark.b, darkest);

            return dim;
        }

        public static VisualElement FloorShadow()
        {
            var shadow = new VisualElement { pickingMode = PickingMode.Ignore };

            shadow.style.position = Position.Absolute;
            shadow.style.left = 0;
            shadow.style.right = 0;
            shadow.style.bottom = 0;
            shadow.style.height = Length.Percent(38);

            shadow.style.backgroundImage = Background.FromTexture2D(TopToBottom());
            shadow.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);

            return shadow;
        }

        const float ScrimHoldsUntil = 0.55f;

        static Texture2D SideToSide(float darkest)
        {
            var wide = new Texture2D(256, 1, TextureFormat.RGBA32, mipChain: false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (var x = 0; x < wide.width; x++)
            {
                var across = x / (wide.width - 1f);
                var falloff = across <= ScrimHoldsUntil
                    ? 1f
                    : 1f - Mathf.SmoothStep(0f, 1f, (across - ScrimHoldsUntil) / (1f - ScrimHoldsUntil));

                wide.SetPixel(x, 0, new Color(Dark.r, Dark.g, Dark.b, darkest * falloff));
            }

            wide.Apply();

            return wide;
        }

        static Texture2D TopToBottom()
        {
            var tall = new Texture2D(1, 128, TextureFormat.RGBA32, mipChain: false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (var y = 0; y < tall.height; y++)
            {
                var up = y / (tall.height - 1f);

                tall.SetPixel(0, y, new Color(Dark.r, Dark.g, Dark.b, Mathf.SmoothStep(0.72f, 0f, up)));
            }

            tall.Apply();

            return tall;
        }

        public static Label Display(string text, int size)
        {
            var label = Text(text, size, Ink, Typeface.Display);

            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.letterSpacing = size * 0.045f;

            return label;
        }

        public static VisualElement Panel()
        {
            var panel = new VisualElement();

            panel.style.backgroundColor = PanelInk;
            panel.style.paddingLeft = 26;
            panel.style.paddingRight = 26;
            panel.style.paddingTop = 18;
            panel.style.paddingBottom = 18;

            Edges(panel, PanelEdge, 1);

            return panel;
        }

        public static VisualElement SectionRule(string text)
        {
            var row = new VisualElement();

            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 16;
            row.style.marginBottom = 8;

            var name = Text(text, SectionSize, Ink, Typeface.Display);
            name.style.marginLeft = 20;
            name.style.marginRight = 20;

            row.Add(Reaching());
            row.Add(name);
            row.Add(Reaching());

            return row;
        }

        static VisualElement Reaching()
        {
            var line = Rule(0, PanelEdge);

            line.style.flexGrow = 1;

            return line;
        }

        public static VisualElement Hints(params (string Key, string What)[] hints)
        {
            var strip = new VisualElement { pickingMode = PickingMode.Ignore };

            strip.style.position = Position.Absolute;
            strip.style.left = 0;
            strip.style.right = 0;
            strip.style.bottom = 26;
            strip.style.flexDirection = FlexDirection.Row;
            strip.style.justifyContent = Justify.Center;
            strip.style.alignItems = Align.Center;

            foreach (var hint in hints)
            {
                strip.Add(Hint(hint.Key, hint.What));
            }

            return strip;
        }

        static VisualElement Hint(string key, string what)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };

            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginLeft = 14;
            row.style.marginRight = 14;

            var cap = Text(key, HintSize - 1, Ink, Typeface.Data);
            cap.style.backgroundColor = KeyCap;
            cap.style.paddingLeft = 8;
            cap.style.paddingRight = 8;
            cap.style.paddingTop = 3;
            cap.style.paddingBottom = 3;
            cap.style.marginRight = 8;
            cap.style.unityTextAlign = TextAnchor.MiddleCenter;
            Edges(cap, PanelEdge, 1);

            row.Add(cap);
            row.Add(Text(what, HintSize, InkSoft, Typeface.Body));

            return row;
        }

        public static Label Eyebrow(string text, Color colour)
        {
            var label = Text(text, 14, colour, Typeface.Data);

            label.style.letterSpacing = 4.5f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;

            return label;
        }

        public static Label Quiet(string text, int size = 14)
        {
            var label = Text(text, size, InkSoft, Typeface.Body);

            label.style.whiteSpace = WhiteSpace.Normal;

            return label;
        }

        public static Label Data(string text, int size, Color colour)
        {
            var label = Text(text, size, colour, Typeface.Data);

            label.style.letterSpacing = 2f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;

            return label;
        }

        public static Label Text(string text, int size, Color colour, Font face)
        {
            var label = new Label(text);

            label.style.color = colour;
            label.style.fontSize = size;
            label.style.textShadow = new TextShadow
            {
                offset = new Vector2(0f, 2f),
                blurRadius = 12f,
                color = new Color(0f, 0f, 0f, 0.9f)
            };

            if (face != null)
            {
                label.style.unityFont = face;
                label.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(face));
            }

            return label;
        }

        public static VisualElement Rule(float widthPixels, Color colour, float thickness = 1f)
        {
            var rule = new VisualElement { pickingMode = PickingMode.Ignore };

            rule.style.width = widthPixels;
            rule.style.height = thickness;
            rule.style.backgroundColor = colour;

            return rule;
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

        public static void SettleIn(VisualElement element, float delaySeconds)
        {
            element.style.opacity = 0f;
            element.style.translate = new Translate(-14, 0);

            element.schedule.Execute(() =>
            {
                element.style.transitionProperty = new StyleList<StylePropertyName>(
                    new List<StylePropertyName> { "opacity", "translate" });
                element.style.transitionDuration = new StyleList<TimeValue>(
                    new List<TimeValue> { TypeSettleSeconds, TypeSettleSeconds });
                element.style.transitionTimingFunction = new StyleList<EasingFunction>(
                    new List<EasingFunction> { EasingMode.EaseOutCubic });

                element.style.opacity = 1f;
                element.style.translate = new Translate(0, 0);
            }).StartingIn((long)(delaySeconds * 1000f));
        }
    }
}
