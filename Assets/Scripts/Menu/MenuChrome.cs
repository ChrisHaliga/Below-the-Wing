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
            Add(MenuScreen.Join, Join(out m_TypedCode, out m_Trouble));
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

        public bool AloneAndReady { get; private set; }

        public void ReadyOnYourOwn(bool ready) => AloneAndReady = ready;

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
                m_TypedCode.Focus();
            }
        }

        public void SayTheCodeIs(string code)
        {
            m_JoinCode.text = string.IsNullOrEmpty(code) ? "opening..." : code;
            m_JoinCode.style.color = string.IsNullOrEmpty(code) ? MenuLook.InkFaint : MenuLook.HiVis;
        }

        public void SayTheJoinFailed(string why)
        {
            m_Trouble.text = why;
            m_Trouble.style.color = why.EndsWith("...") ? MenuLook.InkSoft : MenuLook.Bad;
        }

        public void ShowOneSeatWaitingOnTheService()
        {
            m_Slots.Clear();
            m_Slots.Add(Seat(0, "You", AloneAndReady, filled: true));

            for (var slot = 1; slot < Shift.MostCrew; slot++)
            {
                m_Slots.Add(Seat(slot, null, false, filled: false));
            }

            m_Ready.text = AloneAndReady ? "Stand down" : "Ready";
            m_Start.SetEnabled(AloneAndReady);
        }

        public void ShowTheSeats(IReadOnlyList<SeatedCrew> seated, bool amIReady, bool canStart)
        {
            m_Slots.Clear();

            for (var slot = 0; slot < Shift.MostCrew; slot++)
            {
                var filled = slot < seated.Count;

                m_Slots.Add(Seat(
                    slot,
                    filled ? seated[slot].Called.ToString() : null,
                    filled && seated[slot].Ready,
                    filled));
            }

            m_Ready.text = amIReady ? "Stand down" : "Ready";
            m_Start.SetEnabled(canStart);
        }

        static VisualElement Seat(int slot, string called, bool ready, bool filled)
        {
            var row = MenuLook.Row();

            row.style.height = 36;
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.marginBottom = 4;
            row.style.backgroundColor = filled ? MenuLook.Rest : MenuLook.Sunk;

            MenuLook.Edges(row, MenuLook.Edge, 1);
            row.style.borderLeftWidth = 2;
            row.style.borderLeftColor = ready ? MenuLook.Good : filled ? MenuLook.Hairline : MenuLook.Edge;

            var name = MenuLook.Text(
                filled ? called : $"{slot + 1}", 13, filled ? MenuLook.Ink : MenuLook.InkFaint);

            var state = MenuLook.Text(
                filled ? (ready ? "READY" : "waiting") : "open",
                10,
                ready ? MenuLook.Good : MenuLook.InkFaint);

            state.style.letterSpacing = 1.2f;
            state.style.unityFontStyleAndWeight = FontStyle.Bold;

            row.Add(name);
            row.Add(state);

            return row;
        }

        void Add(MenuScreen screen, VisualElement element) => m_Screens[screen] = element;

        static VisualElement Title()
        {
            var screen = MenuLook.Screen("title");

            screen.Add(MenuLook.Shade(0.45f));

            var stack = new VisualElement();
            stack.style.marginLeft = MenuLook.Gutter;

            var bar = new VisualElement();
            bar.style.width = 54;
            bar.style.height = 3;
            bar.style.backgroundColor = MenuLook.HiVis;
            bar.style.marginBottom = 18;

            var name = MenuLook.Heading("BELOW THE WING", 62);
            name.style.marginBottom = 6;

            var what = MenuLook.Quiet("Ramp operations, one shift at a time", 16);
            what.style.marginBottom = 34;

            var prompt = MenuLook.Text("PRESS ANY BUTTON", 13, MenuLook.HiVis);
            prompt.style.letterSpacing = 3.4f;
            prompt.style.unityFontStyleAndWeight = FontStyle.Bold;

            stack.Add(bar);
            stack.Add(name);
            stack.Add(what);
            stack.Add(prompt);

            screen.Add(stack);

            return screen;
        }

        VisualElement Main()
        {
            var screen = MenuLook.Screen("main");

            screen.Add(MenuLook.Shade(0.35f));

            var card = MenuLook.Card(300);
            card.Add(MenuLook.Eyebrow("BELOW THE WING"));

            card.Add(MenuLook.Press("Host a shift", () => Hosted?.Invoke(), leading: true));
            card.Add(MenuLook.Press("Join a shift", () => Joining?.Invoke()));
            card.Add(MenuLook.Rule());
            card.Add(MenuLook.Press("Settings", () => SettingsOpened?.Invoke()));
            card.Add(MenuLook.Press("Quit", () => Quit?.Invoke()));

            screen.Add(card);

            return screen;
        }

        VisualElement Join(out TextField typed, out Label trouble)
        {
            var screen = MenuLook.Screen("join");

            screen.Add(MenuLook.Shade(0.35f));

            var card = MenuLook.Card(300);
            card.Add(MenuLook.Eyebrow("JOIN A SHIFT"));
            card.Add(MenuLook.Quiet("Enter the code the host gave you."));

            typed = new TextField { maxLength = 12 };
            typed.style.marginTop = 12;
            typed.style.marginBottom = 4;
            typed.style.height = 40;
            typed.style.fontSize = 20;
            typed.style.letterSpacing = 4;
            typed.style.unityTextAlign = TextAnchor.MiddleCenter;

            trouble = MenuLook.Quiet("", 12);
            trouble.style.minHeight = 30;

            var field = typed;

            card.Add(typed);
            card.Add(trouble);
            card.Add(MenuLook.Press("Join", () => JoinedWith?.Invoke(field.value), leading: true));
            card.Add(MenuLook.Press("Back", () => Backed?.Invoke()));

            screen.Add(card);

            return screen;
        }

        VisualElement Lobby(out Label code, out VisualElement slots, out Button ready, out Button start)
        {
            var screen = MenuLook.Screen("lobby");

            screen.Add(MenuLook.Shade(0.3f));

            var card = MenuLook.Card(330);
            card.Add(MenuLook.Eyebrow("CREW"));

            slots = new VisualElement();
            slots.style.marginBottom = 12;
            card.Add(slots);

            var codeRow = MenuLook.Row();
            codeRow.style.height = 34;
            codeRow.style.paddingLeft = 12;
            codeRow.style.paddingRight = 12;
            codeRow.style.marginBottom = 12;
            codeRow.style.backgroundColor = MenuLook.Sunk;
            MenuLook.Edges(codeRow, MenuLook.Edge, 1);

            var codeLabel = MenuLook.Text("JOIN CODE", 10, MenuLook.InkFaint);
            codeLabel.style.letterSpacing = 1.6f;
            codeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            code = MenuLook.Text("opening...", 16, MenuLook.InkFaint);
            code.style.unityFontStyleAndWeight = FontStyle.Bold;
            code.style.letterSpacing = 3;

            codeRow.Add(codeLabel);
            codeRow.Add(code);
            card.Add(codeRow);

            ready = MenuLook.Press("Ready", () => ReadyToggled?.Invoke());
            start = MenuLook.Press("Start shift", () => ShiftStarted?.Invoke(), leading: true);

            card.Add(ready);
            card.Add(start);
            card.Add(MenuLook.Press("Leave", () => Backed?.Invoke()));

            screen.Add(card);

            return screen;
        }
    }
}
