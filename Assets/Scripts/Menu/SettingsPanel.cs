using System;
using System.Collections.Generic;
using BelowTheWing.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace BelowTheWing.Menu
{
    public sealed class SettingsPanel
    {
        static readonly FullScreenMode[] Modes =
        {
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.FullScreenWindow,
            FullScreenMode.Windowed
        };

        static readonly string[] ModeNames = { "Fullscreen", "Borderless", "Windowed" };

        const int LabelWidth = 190;
        const int ControlWidth = 300;

        readonly DropdownField m_Mode;
        readonly DropdownField m_Monitor;
        readonly DropdownField m_Resolution;
        readonly Label m_Note;

        readonly Slider m_Look;
        readonly Toggle m_Invert;
        readonly Slider m_Master;
        readonly Slider m_Effects;
        readonly Slider m_FieldOfView;

        readonly Label m_LookReads;
        readonly Label m_FieldOfViewReads;
        readonly Label m_MasterReads;
        readonly Label m_EffectsReads;

        List<Vector2Int> m_Sizes = new List<Vector2Int>();
        bool m_Painting;

        public SettingsPanel(Action back)
        {
            Root = MenuLook.Screen("settings");
            Root.Add(MenuLook.Scrim(0.9f, 0.9f));

            var column = new VisualElement();
            column.style.position = Position.Absolute;
            column.style.left = MenuLook.Gutter + MenuLook.Nudge;
            column.style.top = MenuLook.Gutter;
            column.style.bottom = MenuLook.Gutter;
            column.style.width = 560;

            var eyebrow = MenuLook.Eyebrow("SETTINGS", MenuLook.HiVis);
            eyebrow.style.marginBottom = 10;

            var heading = MenuLook.Display("SETTINGS", 40);
            heading.style.marginBottom = 10;

            column.Add(eyebrow);
            column.Add(heading);
            column.Add(MenuLook.Rule(ControlWidth + LabelWidth, MenuLook.InkFaint));

            column.Add(Section("DISPLAY"));

            m_Mode = Choice(column, "Window");
            m_Monitor = Choice(column, "Monitor");
            m_Resolution = Choice(column, "Resolution");

            m_Note = MenuLook.Quiet("", 12);
            m_Note.style.marginTop = 6;
            m_Note.style.marginLeft = LabelWidth;
            m_Note.style.maxWidth = ControlWidth;
            column.Add(m_Note);

            column.Add(Section("LOOKING AROUND"));

            m_Look = Dial(column, "Sensitivity", CrewSettings.LeastSensitive, CrewSettings.MostSensitive, out m_LookReads);
            m_FieldOfView = Dial(column, "Field of view", CrewSettings.NarrowestFieldOfView, CrewSettings.WidestFieldOfView, out m_FieldOfViewReads);
            m_Invert = Switch(column, "Invert Y");

            column.Add(Section("SOUND"));

            m_Master = Dial(column, "Master", 0f, 1f, out m_MasterReads);
            m_Effects = Dial(column, "Effects", 0f, 1f, out m_EffectsReads);

            m_Mode.RegisterValueChangedCallback(_ => Apply(() => CrewSettings.Mode = Modes[Mathf.Clamp(m_Mode.index, 0, Modes.Length - 1)]));
            m_Monitor.RegisterValueChangedCallback(_ => Apply(() => CrewSettings.Monitor = m_Monitor.index));
            m_Resolution.RegisterValueChangedCallback(_ => Apply(() => CrewSettings.Resolution = Chosen()));

            m_Look.RegisterValueChangedCallback(e => Write(() =>
            {
                CrewSettings.LookSensitivity = e.newValue;
                m_LookReads.text = $"{CrewSettings.LookSensitivity:0.00}";
            }));
            m_FieldOfView.RegisterValueChangedCallback(e => Write(() =>
            {
                CrewSettings.FieldOfViewDegrees = e.newValue;
                m_FieldOfViewReads.text = $"{CrewSettings.FieldOfViewDegrees:0} DEG";
            }));
            m_Master.RegisterValueChangedCallback(e => Write(() =>
            {
                CrewSettings.MasterVolume = e.newValue;
                m_MasterReads.text = $"{CrewSettings.MasterVolume * 100f:0}%";
            }));
            m_Effects.RegisterValueChangedCallback(e => Write(() =>
            {
                CrewSettings.EffectsVolume = e.newValue;
                m_EffectsReads.text = $"{CrewSettings.EffectsVolume * 100f:0}%";
            }));
            m_Invert.RegisterValueChangedCallback(e => Write(() => CrewSettings.InvertLookY = e.newValue));

            var list = new MenuList();
            list.Add("Reset to defaults", () =>
            {
                CrewSettings.ResetToDefaults();
                Refresh();
            });
            list.Add("Back", () => back?.Invoke());

            column.Add(list.Root);
            Root.Add(column);
        }

        public VisualElement Root { get; }

        public void Refresh()
        {
            m_Painting = true;

            m_Mode.choices = new List<string>(ModeNames);
            m_Mode.index = Mathf.Max(0, Array.IndexOf(Modes, CrewSettings.Mode));

            var monitors = DisplayOptions.Monitors();
            var names = new List<string>(monitors.Count);

            for (var i = 0; i < monitors.Count; i++)
            {
                names.Add(DisplayOptions.Describe(i, monitors[i]));
            }

            if (names.Count == 0)
            {
                names.Add("Display 1");
            }

            m_Monitor.choices = names;
            m_Monitor.index = Mathf.Clamp(CrewSettings.Monitor, 0, names.Count - 1);

            m_Sizes = DisplayOptions.Resolutions();
            var sizes = new List<string>(m_Sizes.Count + 1) { "As the display is" };

            foreach (var size in m_Sizes)
            {
                sizes.Add($"{size.x} x {size.y}");
            }

            m_Resolution.choices = sizes;
            m_Resolution.index = Mathf.Max(0, m_Sizes.IndexOf(CrewSettings.Resolution) + 1);

            m_Note.text = DisplayOptions.CanApply
                ? ""
                : "The editor ignores window settings. They are stored and apply to a build.";

            m_Look.value = CrewSettings.LookSensitivity;
            m_Invert.value = CrewSettings.InvertLookY;
            m_FieldOfView.value = CrewSettings.FieldOfViewDegrees;
            m_Master.value = CrewSettings.MasterVolume;
            m_Effects.value = CrewSettings.EffectsVolume;

            m_LookReads.text = $"{CrewSettings.LookSensitivity:0.00}";
            m_FieldOfViewReads.text = $"{CrewSettings.FieldOfViewDegrees:0} DEG";
            m_MasterReads.text = $"{CrewSettings.MasterVolume * 100f:0}%";
            m_EffectsReads.text = $"{CrewSettings.EffectsVolume * 100f:0}%";

            m_Painting = false;
        }

        Vector2Int Chosen()
            => m_Resolution.index <= 0 || m_Resolution.index > m_Sizes.Count
                ? Vector2Int.zero
                : m_Sizes[m_Resolution.index - 1];

        void Write(Action change)
        {
            if (!m_Painting)
            {
                change();
            }
        }

        void Apply(Action change)
        {
            if (m_Painting)
            {
                return;
            }

            change();
            DisplayOptions.Apply();
        }

        static VisualElement Section(string name)
        {
            var eyebrow = MenuLook.Eyebrow(name, MenuLook.HiVis);

            eyebrow.style.marginTop = 26;
            eyebrow.style.marginBottom = 8;

            return eyebrow;
        }

        static VisualElement Line(VisualElement column, string label)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = 34;

            var name = MenuLook.Text(label, 14, MenuLook.InkSoft, MenuLook.Typeface.Body);
            name.style.width = LabelWidth;

            row.Add(name);
            column.Add(row);

            return row;
        }

        static DropdownField Choice(VisualElement column, string label)
        {
            var field = new DropdownField();

            field.style.width = ControlWidth;
            field.style.height = 28;
            field.style.fontSize = 13;
            field.style.marginLeft = 0;

            Line(column, label).Add(field);

            return field;
        }

        static Toggle Switch(VisualElement column, string label)
        {
            var toggle = new Toggle();

            toggle.style.marginLeft = 0;

            Line(column, label).Add(toggle);

            return toggle;
        }

        static Slider Dial(VisualElement column, string label, float least, float most, out Label reads)
        {
            var slider = new Slider(least, most);

            slider.style.width = ControlWidth - 76;
            slider.style.marginLeft = 0;
            slider.style.marginRight = 14;

            reads = MenuLook.Data("", 13, MenuLook.HiVis);
            reads.style.width = 62;
            reads.style.unityTextAlign = TextAnchor.MiddleRight;

            var row = Line(column, label);
            row.Add(slider);
            row.Add(reads);

            return slider;
        }
    }
}
