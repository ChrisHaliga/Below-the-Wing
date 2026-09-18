using System;
using BelowTheWing.Wiring;
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

    public struct MenuIcons
    {
        public Texture2D Ready;
        public Texture2D Unready;
    }

    public static class MenuLook
    {
        public static MenuIcons Icons;

        public const int Gutter = 96;
        public const int ColumnWidth = 520;

        public const int TitleSize = 42;
        public const int SectionSize = 22;
        public const int RowSize = 20;
        public const int ButtonSize = 20;
        public const int HintSize = 15;

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

            dim.style.backgroundColor = new Color(Palette.Dark.r, Palette.Dark.g, Palette.Dark.b, darkest);

            return dim;
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

                wide.SetPixel(x, 0, new Color(Palette.Dark.r, Palette.Dark.g, Palette.Dark.b, darkest * falloff));
            }

            wide.Apply();

            return wide;
        }

        public static Label Display(string text, int size)
        {
            var label = Text(text, size, Palette.Ink, Typeface.Display);

            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.letterSpacing = size * 0.045f;

            return label;
        }

        public static VisualElement Panel()
        {
            var panel = new VisualElement();

            panel.style.backgroundColor = Palette.PanelInk;
            panel.style.paddingLeft = 26;
            panel.style.paddingRight = 26;
            panel.style.paddingTop = 18;
            panel.style.paddingBottom = 18;

            Edges(panel, Palette.PanelEdge, 1);

            return panel;
        }

        public static VisualElement SectionRule(string text)
        {
            var row = new VisualElement();

            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 16;
            row.style.marginBottom = 8;

            var name = Text(text, SectionSize, Palette.Ink, Typeface.Display);
            name.style.marginLeft = 20;
            name.style.marginRight = 20;

            row.Add(Reaching());
            row.Add(name);
            row.Add(Reaching());

            return row;
        }

        static VisualElement Reaching()
        {
            var line = Rule(0, Palette.PanelEdge);

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

            var cap = Text(key, HintSize - 1, Palette.Ink, Typeface.Data);
            cap.style.backgroundColor = Palette.KeyCap;
            cap.style.paddingLeft = 8;
            cap.style.paddingRight = 8;
            cap.style.paddingTop = 3;
            cap.style.paddingBottom = 3;
            cap.style.marginRight = 8;
            cap.style.unityTextAlign = TextAnchor.MiddleCenter;
            Edges(cap, Palette.PanelEdge, 1);

            row.Add(cap);
            row.Add(Text(what, HintSize, Palette.InkSoft, Typeface.Body));

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
            var label = Text(text, size, Palette.InkSoft, Typeface.Body);

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
