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

        readonly DropdownField m_Mode;
        readonly DropdownField m_Monitor;
        readonly DropdownField m_Resolution;
        readonly Label m_Note;

        readonly Slider m_Look;
        readonly Toggle m_Invert;
        readonly Slider m_Master;
        readonly Slider m_Effects;
        readonly Slider m_FieldOfView;

        List<Vector2Int> m_Sizes = new List<Vector2Int>();
        bool m_Painting;

        public SettingsPanel(Action back)
        {
            Root = MenuLook.Screen("settings");
            Root.style.justifyContent = Justify.Center;
            Root.style.alignItems = Align.FlexStart;

            var card = MenuLook.Card(430);
            card.Add(MenuLook.Heading("SETTINGS", 22));

            m_Mode = Choice(card, "Window");
            m_Monitor = Choice(card, "Monitor");
            m_Resolution = Choice(card, "Resolution");

            m_Note = MenuLook.Quiet("");
            m_Note.style.marginTop = 2;
            m_Note.style.marginBottom = 10;
            card.Add(m_Note);

            m_Look = Dial(card, "Look sensitivity", CrewSettings.LeastSensitive, CrewSettings.MostSensitive);
            m_Invert = Switch(card, "Invert look Y");
            m_FieldOfView = Dial(card, "Field of view", CrewSettings.NarrowestFieldOfView, CrewSettings.WidestFieldOfView);
            m_Master = Dial(card, "Master volume", 0f, 1f);
            m_Effects = Dial(card, "Effects volume", 0f, 1f);

            m_Mode.RegisterValueChangedCallback(_ => Apply(() => CrewSettings.Mode = Modes[Mathf.Clamp(m_Mode.index, 0, Modes.Length - 1)]));
            m_Monitor.RegisterValueChangedCallback(_ => Apply(() => CrewSettings.Monitor = m_Monitor.index));
            m_Resolution.RegisterValueChangedCallback(_ => Apply(() => CrewSettings.Resolution = Chosen()));

            m_Look.RegisterValueChangedCallback(e => Write(() => CrewSettings.LookSensitivity = e.newValue));
            m_Invert.RegisterValueChangedCallback(e => Write(() => CrewSettings.InvertLookY = e.newValue));
            m_FieldOfView.RegisterValueChangedCallback(e => Write(() => CrewSettings.FieldOfViewDegrees = e.newValue));
            m_Master.RegisterValueChangedCallback(e => Write(() => CrewSettings.MasterVolume = e.newValue));
            m_Effects.RegisterValueChangedCallback(e => Write(() => CrewSettings.EffectsVolume = e.newValue));

            card.Add(MenuLook.Press("Reset to defaults", () =>
            {
                CrewSettings.ResetToDefaults();
                Refresh();
            }));

            card.Add(MenuLook.Press("Back", () => back?.Invoke()));

            Root.Add(card);
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

        static DropdownField Choice(VisualElement card, string label)
        {
            var field = new DropdownField(label);

            field.style.marginTop = 6;
            field.labelElement.style.color = MenuLook.InkSoft;
            field.labelElement.style.minWidth = 140;
            card.Add(field);

            return field;
        }

        static Toggle Switch(VisualElement card, string label)
        {
            var toggle = new Toggle(label);

            toggle.style.marginTop = 6;
            toggle.labelElement.style.color = MenuLook.InkSoft;
            toggle.labelElement.style.minWidth = 140;
            card.Add(toggle);

            return toggle;
        }

        static Slider Dial(VisualElement card, string label, float least, float most)
        {
            var slider = new Slider(label, least, most) { showInputField = true };

            slider.style.marginTop = 6;
            slider.labelElement.style.color = MenuLook.InkSoft;
            slider.labelElement.style.minWidth = 140;
            card.Add(slider);

            return slider;
        }
    }
}
