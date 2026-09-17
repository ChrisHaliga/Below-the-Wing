using System;
using System.Collections.Generic;
using BelowTheWing.Settings;
using BelowTheWing.Wiring;
using UnityEngine;
using UnityEngine.UIElements;

namespace BelowTheWing.Menu
{
    public sealed class MenuChrome
    {
        readonly Dictionary<MenuScreen, VisualElement> m_Screens = new Dictionary<MenuScreen, VisualElement>();

        readonly Label m_JoinCode;
        readonly Label m_Trouble;
        readonly TextField m_TypedCode;
        readonly Button m_Start;
        readonly Button m_Ready;
        readonly VisualElement m_Slots;
        readonly SettingsPanel m_Settings;

        public MenuChrome(VisualElement root)
        {
            root.style.flexGrow = 1;

            Add(MenuScreen.Title, Title());
            Add(MenuScreen.Main, Main());

            var join = Join(out m_TypedCode, out m_Trouble);
            Add(MenuScreen.Join, join);

            Add(MenuScreen.Lobby, Lobby(out m_JoinCode, out m_Slots, out m_Ready, out m_Start));

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

        public void Show(MenuScreen screen)
        {
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
            }
        }

        public void SayTheCodeIs(string code) => m_JoinCode.text = string.IsNullOrEmpty(code) ? "--" : code;

        public void SayTheJoinFailed(string why) => m_Trouble.text = why;

        public void ShowTheSeats(IReadOnlyList<SeatedCrew> seated, bool amIReady, bool canStart)
        {
            m_Slots.Clear();

            for (var slot = 0; slot < Shift.MostCrew; slot++)
            {
                m_Slots.Add(Seat(slot, slot < seated.Count ? seated[slot] : default, slot < seated.Count));
            }

            m_Ready.text = amIReady ? "Not ready" : "Ready";
            m_Start.SetEnabled(canStart);
        }

        static VisualElement Seat(int slot, SeatedCrew who, bool filled)
        {
            var row = MenuLook.Row();

            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.paddingTop = 8;
            row.style.paddingBottom = 8;
            row.style.marginTop = 3;
            row.style.marginBottom = 3;
            row.style.backgroundColor = filled ? MenuLook.Deep : new Color(0f, 0f, 0f, 0.25f);
            MenuLook.Border(row, MenuLook.Edge, 1);

            var name = new Label(filled ? who.Called.ToString() : $"Slot {slot + 1} -- open");
            name.style.color = filled ? MenuLook.Ink : MenuLook.InkSoft;
            name.style.fontSize = 14;

            var state = new Label(filled ? (who.Ready ? "READY" : "waiting") : "");
            state.style.color = who.Ready ? MenuLook.HiVis : MenuLook.InkSoft;
            state.style.fontSize = 12;
            state.style.unityFontStyleAndWeight = FontStyle.Bold;

            row.Add(name);
            row.Add(state);

            return row;
        }

        void Add(MenuScreen screen, VisualElement element) => m_Screens[screen] = element;

        static VisualElement Title()
        {
            var screen = MenuLook.Screen("title");

            screen.style.justifyContent = Justify.Center;
            screen.style.alignItems = Align.Center;

            var name = MenuLook.Heading("BELOW THE WING", 56);
            name.style.marginBottom = 18;

            var prompt = MenuLook.Quiet("Press any button to start");
            prompt.style.fontSize = 17;
            prompt.style.color = MenuLook.HiVis;

            screen.Add(name);
            screen.Add(prompt);

            return screen;
        }

        VisualElement Main()
        {
            var screen = MenuLook.Screen("main");

            screen.style.justifyContent = Justify.Center;
            screen.style.alignItems = Align.FlexStart;

            var card = MenuLook.Card(320);
            card.Add(MenuLook.Heading("BELOW THE WING", 26));
            card.Add(MenuLook.Quiet("Ramp operations"));

            var buttons = new VisualElement();
            buttons.style.marginTop = 18;

            buttons.Add(MenuLook.Press("Host", () => Hosted?.Invoke()));
            buttons.Add(MenuLook.Press("Join", () => Joining?.Invoke()));
            buttons.Add(MenuLook.Press("Settings", () => SettingsOpened?.Invoke()));
            buttons.Add(MenuLook.Press("Quit", () => Quit?.Invoke()));

            card.Add(buttons);
            screen.Add(card);

            return screen;
        }

        VisualElement Join(out TextField typed, out Label trouble)
        {
            var screen = MenuLook.Screen("join");

            screen.style.justifyContent = Justify.Center;
            screen.style.alignItems = Align.FlexStart;

            var card = MenuLook.Card(320);
            card.Add(MenuLook.Heading("JOIN A SHIFT", 22));
            card.Add(MenuLook.Quiet("Enter the code the host gave you."));

            typed = new TextField { maxLength = 12 };
            typed.style.marginTop = 14;
            typed.style.height = 38;
            typed.style.fontSize = 18;

            trouble = MenuLook.Quiet("");
            trouble.style.color = new Color(0.90f, 0.45f, 0.38f);
            trouble.style.marginTop = 8;

            var field = typed;

            card.Add(typed);
            card.Add(trouble);
            card.Add(MenuLook.Press("Join", () => JoinedWith?.Invoke(field.value)));
            card.Add(MenuLook.Press("Back", () => Backed?.Invoke()));

            screen.Add(card);

            return screen;
        }

        VisualElement Lobby(out Label code, out VisualElement slots, out Button ready, out Button start)
        {
            var screen = MenuLook.Screen("lobby");

            screen.style.justifyContent = Justify.Center;
            screen.style.alignItems = Align.FlexStart;

            var card = MenuLook.Card(400);
            card.Add(MenuLook.Heading("CREW", 22));

            slots = new VisualElement();
            slots.style.marginTop = 10;
            slots.style.marginBottom = 14;
            card.Add(slots);

            var codeRow = MenuLook.Row();
            codeRow.Add(MenuLook.Quiet("Join code"));

            code = new Label("--");
            code.style.color = MenuLook.HiVis;
            code.style.fontSize = 20;
            code.style.unityFontStyleAndWeight = FontStyle.Bold;
            code.style.letterSpacing = 3;
            codeRow.Add(code);

            card.Add(codeRow);

            ready = MenuLook.Press("Ready", () => ReadyToggled?.Invoke());
            start = MenuLook.Press("Start shift", () => ShiftStarted?.Invoke());

            card.Add(ready);
            card.Add(start);
            card.Add(MenuLook.Press("Back", () => Backed?.Invoke()));

            screen.Add(card);

            return screen;
        }
    }
}
