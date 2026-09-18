using System.Collections.Generic;
using System;
using BelowTheWing.Wiring;
using UnityEngine.UIElements;
using UnityEngine;

namespace BelowTheWing.Menu
{
    public sealed class MenuChrome
    {
        readonly Dictionary<MenuScreen, VisualElement> m_Screens = new Dictionary<MenuScreen, VisualElement>();
        readonly Dictionary<MenuScreen, MenuList> m_Lists = new Dictionary<MenuScreen, MenuList>();

        readonly Label m_JoinCode;
        readonly Label m_Trouble;
        readonly TextField m_TypedCode;
        readonly MenuNameplates m_Plates = new MenuNameplates();
        readonly SettingsPanel m_Settings;

        MenuScreen m_Showing = MenuScreen.Title;
        string m_Code = "";
        int m_ReadyOn;
        int m_StartOn;

        public MenuChrome(VisualElement root)
        {
            root.style.flexGrow = 1;

            Add(MenuScreen.Title, Title());
            Add(MenuScreen.Main, Main());
            Add(MenuScreen.Join, Join(out m_TypedCode, out m_Trouble));
            Add(MenuScreen.Lobby, Lobby(out m_JoinCode));

            m_Settings = new SettingsPanel(() => Backed?.Invoke());
            Add(MenuScreen.Settings, m_Settings.Root);

            foreach (var screen in m_Screens.Values)
            {
                root.Add(screen);
            }

            Show(MenuScreen.Title);
        }

        public event Action Hosted;
        public event Action Joining;
        public event Action<string> JoinedWith;
        public event Action SettingsOpened;
        public event Action Quit;
        public event Action Backed;
        public event Action ReadyToggled;
        public event Action ShiftStarted;

        public bool TypingACode
            => m_TypedCode.panel != null
               && ReferenceEquals(m_TypedCode.panel.focusController?.focusedElement, m_TypedCode);

        public void Moved(int by)
        {
            if (m_Showing == MenuScreen.Settings)
            {
                m_Settings.Move(by);
                return;
            }

            if (m_Lists.TryGetValue(m_Showing, out var list) && !list.AcrossTheScreen)
            {
                list.Move(by);
            }
        }

        public void Nudged(int by)
        {
            if (m_Showing == MenuScreen.Settings)
            {
                m_Settings.Nudge(by);
                return;
            }

            if (m_Lists.TryGetValue(m_Showing, out var list) && list.AcrossTheScreen)
            {
                list.Move(by);
            }
        }

        public void Chose()
        {
            if (m_Lists.TryGetValue(m_Showing, out var list))
            {
                list.ChooseWhatIsOn();
            }
        }

        public void Show(MenuScreen screen)
        {
            m_Showing = screen;

            foreach (var pair in m_Screens)
            {
                pair.Value.style.display = pair.Key == screen ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (screen == MenuScreen.Settings)
            {
                m_Settings.Refresh();
            }

            if (screen == MenuScreen.Join)
            {
                m_TypedCode.value = "";
                m_Trouble.text = "";
                m_TypedCode.schedule.Execute(() => m_TypedCode.Focus()).StartingIn(60);
            }
        }

        public void SayTheCodeIs(string code)
        {
            var waiting = string.IsNullOrEmpty(code);

            m_Code = waiting ? "" : code;
            m_JoinCode.text = waiting ? "OPENING" : code;
            m_JoinCode.style.color = waiting ? Palette.InkFaint : Palette.HiVis;
        }

        public void SayTheJoinFailed(string why)
        {
            m_Trouble.text = why;
            m_Trouble.style.color = why.EndsWith("...") ? Palette.InkSoft : Palette.Bad;
        }

        public void ShowTheLobby(bool amIReady, bool canStart)
        {
            var list = m_Lists[MenuScreen.Lobby];

            list.Available(m_StartOn, canStart);
            list.Rename(m_ReadyOn, amIReady ? "Unready" : "Ready");
        }

        public void ShowCrewOnStage(IReadOnlyList<CrewOnStage> crew, Camera eye)
            => m_Plates.Show(crew, eye);

        void Add(MenuScreen screen, VisualElement element) => m_Screens[screen] = element;

        static VisualElement Stage(VisualElement screen, float scrimWide, float scrimDark)
        {
            screen.Add(MenuLook.Scrim(scrimWide, scrimDark));

            var holder = new VisualElement();

            holder.style.position = Position.Absolute;
            holder.style.left = MenuLook.Gutter;
            holder.style.top = 0;
            holder.style.bottom = 0;
            holder.style.width = MenuLook.ColumnWidth;
            holder.style.justifyContent = Justify.Center;

            var column = new VisualElement();

            holder.Add(column);
            screen.Add(holder);

            return column;
        }

        static VisualElement Title()
        {
            var screen = MenuLook.Screen("title");

            screen.Add(MenuLook.Dim(0.55f));

            var middle = new VisualElement();
            MenuLook.Fill(middle);
            middle.pickingMode = PickingMode.Ignore;
            middle.style.alignItems = Align.Center;
            middle.style.justifyContent = Justify.Center;

            var name = MenuLook.Display("BELOW THE WING", 94);
            name.style.unityTextAlign = TextAnchor.MiddleCenter;
            name.style.marginBottom = 46;

            var prompt = MenuLook.Display("PRESS ANY BUTTON", 34);
            prompt.style.color = Palette.HiVis;
            prompt.style.unityTextAlign = TextAnchor.MiddleCenter;

            middle.Add(name);
            middle.Add(prompt);
            screen.Add(middle);

            MenuLook.SettleIn(name, 0.1f);
            MenuLook.SettleIn(prompt, 0.45f);

            return screen;
        }

        VisualElement Main()
        {
            var screen = MenuLook.Screen("main");
            var column = Stage(screen, 0.3f, 0.9f);

            column.Add(Heading("BELOW THE WING"));

            var list = new MenuList();
            list.Add("Host", () => Hosted?.Invoke());
            list.Add("Join", () => Joining?.Invoke());
            list.Add("Settings", () => SettingsOpened?.Invoke());
            list.Add("Quit", () => Quit?.Invoke());

            m_Lists[MenuScreen.Main] = list;
            column.Add(list.Root);

            return screen;
        }

        VisualElement Join(out TextField typed, out Label trouble)
        {
            var screen = MenuLook.Screen("join");

            screen.Add(MenuLook.Dim(0.62f));

            var middle = new VisualElement();
            MenuLook.Fill(middle);
            middle.style.alignItems = Align.Center;
            middle.style.justifyContent = Justify.Center;

            var modal = MenuLook.Panel();
            modal.style.width = 620;

            var name = MenuLook.Display("JOIN BY CODE", 34);
            name.style.marginBottom = 6;
            name.style.unityTextAlign = TextAnchor.MiddleCenter;

            var rule = MenuLook.Rule(0, Palette.PanelEdge);
            rule.style.width = Length.Percent(100);
            rule.style.marginBottom = 24;

            typed = new TextField { maxLength = 12 };
            typed.style.flexGrow = 1;
            typed.style.height = 58;
            typed.style.fontSize = 28;
            typed.style.letterSpacing = 8;
            typed.style.color = Palette.HiVis;
            typed.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
            typed.style.unityTextAlign = TextAnchor.MiddleCenter;
            typed.style.unityFont = MenuLook.Typeface.Data;
            typed.style.unityFontDefinition =
                new StyleFontDefinition(FontDefinition.FromFont(MenuLook.Typeface.Data));
            MenuLook.Edges(typed, Palette.PanelEdge, 1);

            var field = typed;

            var entry = new VisualElement();
            entry.style.flexDirection = FlexDirection.Row;
            entry.style.alignItems = Align.Center;
            entry.Add(typed);
            entry.Add(Chip("Paste", () => field.value = GUIUtility.systemCopyBuffer ?? "", high: 58));

            trouble = MenuLook.Quiet("", 15);
            trouble.style.minHeight = 30;
            trouble.style.marginTop = 10;
            trouble.style.unityTextAlign = TextAnchor.MiddleCenter;

            var list = new MenuList(acrossTheScreen: true);
            list.Add("Back", () => Backed?.Invoke());
            list.Add("Confirm", () => JoinedWith?.Invoke(field.value));
            list.Root.style.justifyContent = Justify.SpaceBetween;
            list.Root.style.marginTop = 10;

            m_Lists[MenuScreen.Join] = list;

            modal.Add(name);
            modal.Add(rule);
            modal.Add(entry);
            modal.Add(trouble);
            modal.Add(list.Root);

            middle.Add(modal);
            screen.Add(middle);

            screen.Add(MenuLook.Hints(
                ("A / D", "Back or Confirm"),
                ("Enter", "Select"),
                ("Esc", "Back")));

            return screen;
        }

        static Button Chip(string text, Action pressed, int high = 42)
        {
            var chip = new Button(pressed) { text = text.ToUpperInvariant() };

            chip.style.marginLeft = 14;
            chip.style.marginRight = 0;
            chip.style.height = high;
            chip.style.paddingLeft = 20;
            chip.style.paddingRight = 20;
            chip.style.fontSize = MenuLook.HintSize;
            chip.style.letterSpacing = 2f;
            chip.style.color = Palette.Ink;
            chip.style.backgroundColor = Palette.ButtonFill;
            MenuLook.Edges(chip, Palette.InkFaint, 1);

            return chip;
        }

        VisualElement Lobby(out Label code)
        {
            var screen = MenuLook.Screen("lobby");

            screen.Add(m_Plates.Root);

            var acting = new MenuList(acrossTheScreen: true);
            m_ReadyOn = acting.Count;
            acting.Add("Ready", () => ReadyToggled?.Invoke());
            m_StartOn = acting.Count;
            acting.Add("Start", () => ShiftStarted?.Invoke());
            acting.Available(m_StartOn, false);

            m_Lists[MenuScreen.Lobby] = acting;

            screen.Add(AlongTheBottom(acting.Root));
            screen.Add(Corner(Chip("Leave", () => Backed?.Invoke()), left: true));
            screen.Add(JoinCodeCard(out code));

            return screen;
        }

        static VisualElement AlongTheBottom(VisualElement row)
        {
            var strip = new VisualElement();

            strip.style.position = Position.Absolute;
            strip.style.left = 0;
            strip.style.right = 0;
            strip.style.bottom = MenuLook.Gutter;
            strip.style.alignItems = Align.Center;

            strip.Add(row);

            return strip;
        }

        static VisualElement Corner(VisualElement held, bool left)
        {
            var spot = new VisualElement();

            spot.style.position = Position.Absolute;
            spot.style.bottom = MenuLook.Gutter;

            if (left)
            {
                spot.style.left = MenuLook.Gutter;
            }
            else
            {
                spot.style.right = MenuLook.Gutter;
            }

            spot.Add(held);

            return spot;
        }

        VisualElement JoinCodeCard(out Label code)
        {
            var card = MenuLook.Panel();

            card.style.position = Position.Absolute;
            card.style.right = MenuLook.Gutter;
            card.style.top = MenuLook.Gutter;

            var label = MenuLook.Eyebrow("JOIN CODE", Palette.InkFaint);
            label.style.marginBottom = 8;

            code = MenuLook.Data("OPENING", 30, Palette.InkFaint);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            row.Add(code);
            row.Add(Chip("Copy", CopyTheCode));

            card.Add(label);
            card.Add(row);

            return card;
        }

        void CopyTheCode()
        {
            if (!string.IsNullOrEmpty(m_Code))
            {
                GUIUtility.systemCopyBuffer = m_Code;
            }
        }

        static VisualElement Heading(string heading)
        {
            var stack = new VisualElement();

            var name = MenuLook.Display(heading, MenuLook.TitleSize);
            name.style.marginBottom = 10;

            var rule = MenuLook.Rule(300, Palette.PanelEdge);
            rule.style.marginBottom = 16;

            stack.Add(name);
            stack.Add(rule);

            MenuLook.SettleIn(name, 0.07f);

            return stack;
        }
    }
}
