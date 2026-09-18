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

        const int RailWidth = 230;
        const int LabelWidth = 340;
        const int ValueWidth = 320;
        const int RowHeight = 46;
        const int DialSteps = 20;

        readonly List<Category> m_Categories = new List<Category>();
        readonly List<Row> m_Rows = new List<Row>();
        readonly VisualElement m_Sheet;
        readonly Label m_Note;

        List<Vector2Int> m_Sizes = new List<Vector2Int>();
        int m_OnCategory;
        int m_OnRow;

        public SettingsPanel(Action back)
        {
            Root = MenuLook.Screen("settings");
            Root.Add(MenuLook.Dim(0.72f));

            var rail = new VisualElement();
            rail.style.position = Position.Absolute;
            rail.style.left = MenuLook.Gutter;
            rail.style.top = MenuLook.Gutter + 60;
            rail.style.width = RailWidth;

            var panel = MenuLook.Panel();
            panel.style.position = Position.Absolute;
            panel.style.left = MenuLook.Gutter + RailWidth + 28;
            panel.style.right = MenuLook.Gutter;
            panel.style.top = MenuLook.Gutter + 60;
            panel.style.bottom = MenuLook.Gutter + 20;

            m_Sheet = new VisualElement();
            m_Sheet.style.flexGrow = 1;

            m_Note = MenuLook.Quiet("", MenuLook.HintSize);
            m_Note.style.marginTop = 10;
            m_Note.style.marginLeft = LabelWidth;

            panel.Add(m_Sheet);
            panel.Add(m_Note);

            var heading = MenuLook.Display("SETTINGS", MenuLook.TitleSize);
            heading.style.position = Position.Absolute;
            heading.style.left = MenuLook.Gutter;
            heading.style.top = MenuLook.Gutter - 10;

            Root.Add(heading);
            Root.Add(rail);
            Root.Add(panel);

            AddCategory(rail, "Video");
            AddCategory(rail, "Audio");
            AddCategory(rail, "Control");

            var leaving = new VisualElement();
            leaving.style.position = Position.Absolute;
            leaving.style.left = MenuLook.Gutter;
            leaving.style.bottom = MenuLook.Gutter;
            leaving.Add(Leaving(back));

            Root.Add(leaving);

            Root.Add(MenuLook.Hints(
                ("W / S", "Navigate"),
                ("A / D", "Change"),
                ("Enter", "Select"),
                ("Esc", "Back")));

            OpenCategory(0);
        }

        public VisualElement Root { get; }

        static Button Leaving(Action back)
        {
            var chip = new Button(() => back?.Invoke()) { text = "BACK" };

            chip.style.marginLeft = 0;
            chip.style.height = 42;
            chip.style.paddingLeft = 24;
            chip.style.paddingRight = 24;
            chip.style.fontSize = MenuLook.HintSize;
            chip.style.letterSpacing = 2f;
            chip.style.color = MenuLook.Ink;
            chip.style.backgroundColor = MenuLook.ButtonFill;
            MenuLook.Edges(chip, MenuLook.InkFaint, 1);

            return chip;
        }

        sealed class Category
        {
            public VisualElement Element;
            public Label Name;
            public VisualElement Marker;
        }

        sealed class Row
        {
            public VisualElement Element;
            public Label Value;
            public Action<int> Change;
            public Func<string> Reads;
        }

        public void Move(int by)
        {
            if (m_Rows.Count == 0)
            {
                return;
            }

            m_OnRow = Stepping.Next(m_OnRow, m_Rows.Count, by);

            PaintRows();
        }

        public void Nudge(int by)
        {
            if (m_OnRow < 0 || m_OnRow >= m_Rows.Count)
            {
                return;
            }

            m_Rows[m_OnRow].Change(by);

            DisplayOptions.Apply();
            Refresh();
        }

        public void Refresh()
        {
            m_Sizes = DisplayOptions.Resolutions();

            foreach (var row in m_Rows)
            {
                row.Value.text = row.Reads();
            }

            m_Note.text = DisplayOptions.CanApply
                ? ""
                : "The editor ignores window settings. They are stored and apply to a build.";

            PaintRows();
        }

        void AddCategory(VisualElement rail, string name)
        {
            var mine = m_Categories.Count;

            var category = new Category
            {
                Element = new VisualElement(),
                Marker = new VisualElement(),
                Name = MenuLook.Text(name, MenuLook.SectionSize, MenuLook.InkSoft, MenuLook.Typeface.Body)
            };

            category.Element.style.flexDirection = FlexDirection.Row;
            category.Element.style.alignItems = Align.Center;
            category.Element.style.height = 54;
            category.Element.style.marginBottom = 8;
            category.Element.style.backgroundColor = new Color(1f, 1f, 1f, 0.06f);

            category.Marker.style.width = 5;
            category.Marker.style.height = Length.Percent(100);
            category.Marker.style.marginRight = 20;
            category.Marker.style.backgroundColor = Color.clear;

            category.Name.style.letterSpacing = 1.2f;

            category.Element.Add(category.Marker);
            category.Element.Add(category.Name);
            category.Element.RegisterCallback<MouseDownEvent>(_ => OpenCategory(mine));

            m_Categories.Add(category);
            rail.Add(category.Element);
        }

        void OpenCategory(int which)
        {
            m_OnCategory = which;
            m_OnRow = 0;

            for (var category = 0; category < m_Categories.Count; category++)
            {
                var on = category == which;

                m_Categories[category].Marker.style.backgroundColor = on ? MenuLook.HiVis : Color.clear;
                m_Categories[category].Name.style.color = on ? MenuLook.Ink : MenuLook.InkSoft;
                m_Categories[category].Element.style.backgroundColor =
                    new Color(1f, 1f, 1f, on ? 0.12f : 0.06f);
            }

            m_Rows.Clear();
            m_Sheet.Clear();

            switch (which)
            {
                case 0:
                    BuildVideo();
                    break;

                case 1:
                    BuildAudio();
                    break;

                default:
                    BuildControl();
                    break;
            }

            Refresh();
        }

        void BuildVideo()
        {
            m_Sheet.Add(MenuLook.SectionRule("Basic Settings"));

            AddRow("Windowed Mode",
                by => CrewSettings.Mode = Modes[Stepping.Next(Array.IndexOf(Modes, CrewSettings.Mode), Modes.Length, by)],
                () => ModeNames[Mathf.Max(Array.IndexOf(Modes, CrewSettings.Mode), 0)]);

            AddRow("Resolution", StepResolution, ReadResolution);

            AddRow("Monitor",
                by => CrewSettings.Monitor = Stepping.Next(CrewSettings.Monitor, Mathf.Max(DisplayOptions.Monitors().Count, 1), by),
                () => $"Display {CrewSettings.Monitor + 1}");

            m_Sheet.Add(MenuLook.SectionRule("Advanced Settings"));

            AddRow("Field of View",
                by => CrewSettings.FieldOfViewDegrees = Stepping.Nudge(
                    CrewSettings.FieldOfViewDegrees,
                    CrewSettings.NarrowestFieldOfView,
                    CrewSettings.WidestFieldOfView,
                    by,
                    DialSteps),
                () => $"{CrewSettings.FieldOfViewDegrees:0}");
        }

        void BuildAudio()
        {
            m_Sheet.Add(MenuLook.SectionRule("Levels"));

            AddRow("Master",
                by => CrewSettings.MasterVolume = Stepping.Nudge(CrewSettings.MasterVolume, 0f, 1f, by, DialSteps),
                () => $"{CrewSettings.MasterVolume * 100f:0}%");

            AddRow("Effects",
                by => CrewSettings.EffectsVolume = Stepping.Nudge(CrewSettings.EffectsVolume, 0f, 1f, by, DialSteps),
                () => $"{CrewSettings.EffectsVolume * 100f:0}%");
        }

        void BuildControl()
        {
            m_Sheet.Add(MenuLook.SectionRule("Looking Around"));

            AddRow("Sensitivity",
                by => CrewSettings.LookSensitivity = Stepping.Nudge(
                    CrewSettings.LookSensitivity,
                    CrewSettings.LeastSensitive,
                    CrewSettings.MostSensitive,
                    by,
                    DialSteps),
                () => $"{CrewSettings.LookSensitivity:0.00}");

            AddRow("Invert Y",
                _ => CrewSettings.InvertLookY = !CrewSettings.InvertLookY,
                () => CrewSettings.InvertLookY ? "On" : "Off");

            m_Sheet.Add(MenuLook.SectionRule("Everything"));

            AddRow("Reset to Defaults", _ => CrewSettings.ResetToDefaults(), () => "Press A or D");
        }

        void StepResolution(int by)
        {
            var at = m_Sizes.IndexOf(CrewSettings.Resolution) + 1;

            at = Stepping.Next(at, m_Sizes.Count + 1, by);

            CrewSettings.Resolution = at == 0 ? Vector2Int.zero : m_Sizes[at - 1];
        }

        string ReadResolution()
            => CrewSettings.Resolution == Vector2Int.zero
                ? "As the display is"
                : $"{CrewSettings.Resolution.x} x {CrewSettings.Resolution.y}";

        void AddRow(string label, Action<int> change, Func<string> reads)
        {
            var mine = m_Rows.Count;

            var row = new Row { Change = change, Reads = reads };

            row.Element = new VisualElement();
            row.Element.style.flexDirection = FlexDirection.Row;
            row.Element.style.alignItems = Align.Center;
            row.Element.style.height = RowHeight;

            var name = MenuLook.Text(label, MenuLook.RowSize, MenuLook.Ink, MenuLook.Typeface.Body);
            name.style.width = LabelWidth;
            name.style.flexGrow = 1;

            row.Value = MenuLook.Text("", MenuLook.RowSize, MenuLook.Ink, MenuLook.Typeface.Body);
            row.Value.style.width = ValueWidth;
            row.Value.style.unityTextAlign = TextAnchor.MiddleCenter;
            row.Value.style.paddingTop = 4;
            row.Value.style.paddingBottom = 4;

            row.Element.Add(name);
            row.Element.Add(Arrow("<", () => Pick(mine, -1)));
            row.Element.Add(row.Value);
            row.Element.Add(Arrow(">", () => Pick(mine, 1)));

            row.Element.RegisterCallback<MouseEnterEvent>(_ =>
            {
                m_OnRow = mine;
                PaintRows();
            });

            m_Rows.Add(row);
            m_Sheet.Add(row.Element);
        }

        void Pick(int row, int by)
        {
            m_OnRow = row;

            Nudge(by);
        }

        static VisualElement Arrow(string glyph, Action pressed)
        {
            var arrow = new Button(pressed) { text = glyph };

            arrow.style.width = 44;
            arrow.style.height = 34;
            arrow.style.marginLeft = 0;
            arrow.style.marginRight = 0;
            arrow.style.fontSize = MenuLook.RowSize;
            arrow.style.color = MenuLook.Ink;
            arrow.style.backgroundColor = MenuLook.KeyCap;
            MenuLook.Edges(arrow, MenuLook.PanelEdge, 1);

            return arrow;
        }

        void PaintRows()
        {
            for (var row = 0; row < m_Rows.Count; row++)
            {
                var on = row == m_OnRow;

                m_Rows[row].Value.style.backgroundColor = on ? MenuLook.HiVis : Color.clear;
                m_Rows[row].Value.style.color = on ? MenuLook.Dark : MenuLook.Ink;
            }
        }
    }
}
