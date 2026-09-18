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
        public const int Nudge = Gutter * 2;
        public const int ColumnWidth = 520;
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

        const float ScrimHoldsUntil = 0.66f;

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

        public static Label Eyebrow(string text, Color colour)
        {
            var label = Text(text, 11, colour, Typeface.Data);

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
