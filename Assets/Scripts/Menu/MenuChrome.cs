using System;
using System.Collections.Generic;
using BelowTheWing.Wiring;
using UnityEngine;
using UnityEngine.UIElements;

namespace BelowTheWing.Menu
{
    public sealed class MenuChrome
    {
        readonly Dictionary<MenuScreen, VisualElement> m_Screens = new Dictionary<MenuScreen, VisualElement>();
        readonly Dictionary<MenuScreen, MenuList> m_Lists = new Dictionary<MenuScreen, MenuList>();

        readonly Label m_JoinCode;
        readonly Label m_Trouble;
        readonly TextField m_TypedCode;
        readonly VisualElement m_Slots;
        readonly SettingsPanel m_Settings;

        MenuScreen m_Showing = MenuScreen.Title;
        int m_ReadyOn;
        int m_StartOn;

        public MenuChrome(VisualElement root)
        {
            root.style.flexGrow = 1;

            Add(MenuScreen.Title, Title());
            Add(MenuScreen.Main, Main());
            Add(MenuScreen.Join, Join(out m_TypedCode, out m_Trouble));
            Add(MenuScreen.Lobby, Lobby(out m_JoinCode, out m_Slots));

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

        public bool AloneAndReady { get; private set; }

        public void ReadyOnYourOwn(bool ready) => AloneAndReady = ready;

        public void Moved(int by)
        {
            if (m_Lists.TryGetValue(m_Showing, out var list))
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

            m_JoinCode.text = waiting ? "OPENING" : code;
            m_JoinCode.style.color = waiting ? MenuLook.InkFaint : MenuLook.HiVis;
        }

        public void SayTheJoinFailed(string why)
        {
            m_Trouble.text = why;
            m_Trouble.style.color = why.EndsWith("...") ? MenuLook.InkSoft : MenuLook.Bad;
        }

        public void ShowOneSeatWaitingOnTheService()
            => PaintSeats(new[] { ("You", AloneAndReady) }, AloneAndReady, AloneAndReady);

        public void ShowTheSeats(IReadOnlyList<SeatedCrew> seated, bool amIReady, bool canStart)
        {
            var crew = new (string, bool)[seated.Count];

            for (var seat = 0; seat < seated.Count; seat++)
            {
                crew[seat] = (seated[seat].Called.ToString(), seated[seat].Ready);
            }

            PaintSeats(crew, amIReady, canStart);
        }

        void PaintSeats(IReadOnlyList<(string Called, bool Ready)> crew, bool amIReady, bool canStart)
        {
            m_Slots.Clear();

            for (var slot = 0; slot < Shift.MostCrew; slot++)
            {
                m_Slots.Add(slot < crew.Count
                    ? Seat(slot, crew[slot].Called, crew[slot].Ready)
                    : Seat(slot, null, false));
            }

            var list = m_Lists[MenuScreen.Lobby];
            list.Available(m_StartOn, canStart);

            m_ReadyLabel.text = amIReady ? "STAND DOWN" : "READY";
        }

        Label m_ReadyLabel;

        static VisualElement Seat(int slot, string called, bool ready)
        {
            var filled = called != null;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.height = 34;

            var pip = new VisualElement();
            pip.style.width = 7;
            pip.style.height = 7;
            pip.style.marginRight = 14;
            pip.style.backgroundColor = ready ? MenuLook.Good : filled ? MenuLook.InkSoft : Color.clear;
            MenuLook.Edges(pip, filled ? Color.clear : MenuLook.InkFaint, 1);

            var name = MenuLook.Text(
                filled ? called.ToUpperInvariant() : $"SLOT {slot + 1}",
                15,
                filled ? MenuLook.Ink : MenuLook.InkFaint,
                MenuLook.Typeface.Body);
            name.style.flexGrow = 1;
            name.style.letterSpacing = 1.4f;

            var state = MenuLook.Eyebrow(
                filled ? (ready ? "READY" : "STANDING BY") : "OPEN",
                ready ? MenuLook.Good : MenuLook.InkFaint);

            row.Add(pip);
            row.Add(name);
            row.Add(state);

            return row;
        }

        void Add(MenuScreen screen, VisualElement element) => m_Screens[screen] = element;

        static VisualElement Stage(VisualElement screen, float scrimAcross, float scrimDark)
        {
            screen.Add(MenuLook.Scrim(scrimAcross, scrimDark));
            screen.Add(MenuLook.FloorShadow());

            var column = new VisualElement();

            column.style.position = Position.Absolute;
            column.style.left = MenuLook.Gutter;
            column.style.bottom = MenuLook.Gutter;
            column.style.width = 520;

            screen.Add(column);

            return column;
        }

        static VisualElement Title()
        {
            var screen = MenuLook.Screen("title");
            var column = Stage(screen, 0.62f, 0.86f);

            var bar = MenuLook.Rule(64, MenuLook.HiVis, 4);
            bar.style.marginBottom = 26;

            var name = MenuLook.Display("BELOW THE WING", 86);
            name.style.marginBottom = 10;

            var what = MenuLook.Eyebrow("RAMP OPERATIONS", MenuLook.InkSoft);
            what.style.marginBottom = 52;

            var prompt = MenuLook.Eyebrow("PRESS ANY BUTTON", MenuLook.HiVis);

            column.Add(bar);
            column.Add(name);
            column.Add(what);
            column.Add(prompt);

            MenuLook.SettleIn(bar, 0.05f);
            MenuLook.SettleIn(name, 0.12f);
            MenuLook.SettleIn(what, 0.24f);
            MenuLook.SettleIn(prompt, 0.5f);

            return screen;
        }

        VisualElement Main()
        {
            var screen = MenuLook.Screen("main");
            var column = Stage(screen, 0.52f, 0.84f);

            column.Add(Header("BELOW THE WING", "MAIN MENU"));

            var list = new MenuList();
            list.Add("Host a shift", () => Hosted?.Invoke());
            list.Add("Join a shift", () => Joining?.Invoke());
            list.Add("Settings", () => SettingsOpened?.Invoke());
            list.Add("Quit", () => Quit?.Invoke());

            m_Lists[MenuScreen.Main] = list;
            column.Add(list.Root);

            return screen;
        }

        VisualElement Join(out TextField typed, out Label trouble)
        {
            var screen = MenuLook.Screen("join");
            var column = Stage(screen, 0.52f, 0.84f);

            column.Add(Header("JOIN A SHIFT", "ENTER THE HOST'S CODE"));

            typed = new TextField { maxLength = 12 };
            typed.style.marginTop = 14;
            typed.style.marginBottom = 6;
            typed.style.width = 300;
            typed.style.height = 52;
            typed.style.fontSize = 26;
            typed.style.letterSpacing = 8;
            typed.style.color = MenuLook.HiVis;
            typed.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
            typed.style.unityTextAlign = TextAnchor.MiddleCenter;
            typed.style.unityFont = MenuLook.Typeface.Data;
            typed.style.unityFontDefinition =
                new StyleFontDefinition(FontDefinition.FromFont(MenuLook.Typeface.Data));
            MenuLook.Edges(typed, MenuLook.InkFaint, 1);

            trouble = MenuLook.Quiet("", 13);
            trouble.style.minHeight = 26;

            var field = typed;

            var list = new MenuList();
            list.Add("Join", () => JoinedWith?.Invoke(field.value));
            list.Add("Back", () => Backed?.Invoke());

            m_Lists[MenuScreen.Join] = list;

            column.Add(typed);
            column.Add(trouble);
            column.Add(list.Root);

            return screen;
        }

        VisualElement Lobby(out Label code, out VisualElement slots)
        {
            var screen = MenuLook.Screen("lobby");
            var column = Stage(screen, 0.54f, 0.84f);

            column.Add(Header("CREW", "WHO IS ON THIS SHIFT"));

            slots = new VisualElement();
            slots.style.marginTop = 6;
            slots.style.marginBottom = 18;
            column.Add(slots);

            var codeRow = new VisualElement();
            codeRow.style.flexDirection = FlexDirection.Row;
            codeRow.style.alignItems = Align.Center;
            codeRow.style.marginBottom = 10;

            var label = MenuLook.Eyebrow("JOIN CODE", MenuLook.InkFaint);
            label.style.marginRight = 18;

            code = MenuLook.Data("OPENING", 24, MenuLook.InkFaint);

            codeRow.Add(label);
            codeRow.Add(code);
            column.Add(codeRow);
            column.Add(MenuLook.Rule(300, MenuLook.InkFaint));

            var list = new MenuList();
            m_ReadyOn = list.Count;
            list.Add("Ready", () => ReadyToggled?.Invoke());
            m_StartOn = list.Count;
            list.Add("Start shift", () => ShiftStarted?.Invoke());
            list.Add("Leave", () => Backed?.Invoke());
            list.Available(m_StartOn, false);

            m_Lists[MenuScreen.Lobby] = list;
            column.Add(list.Root);

            m_ReadyLabel = ReadyLabelOf(list);

            return screen;
        }

        static Label ReadyLabelOf(MenuList list) => list.Root[0].Q<Label>();

        static VisualElement Header(string heading, string eyebrow)
        {
            var stack = new VisualElement();

            var top = MenuLook.Eyebrow(eyebrow, MenuLook.HiVis);
            top.style.marginBottom = 10;

            var name = MenuLook.Display(heading, 40);
            name.style.marginBottom = 10;

            var rule = MenuLook.Rule(300, MenuLook.InkFaint);
            rule.style.marginBottom = 16;

            stack.Add(top);
            stack.Add(name);
            stack.Add(rule);

            MenuLook.SettleIn(top, 0.02f);
            MenuLook.SettleIn(name, 0.07f);

            return stack;
        }
    }
}
