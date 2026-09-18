using System;
using System.Collections.Generic;
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

            m_Code = waiting ? "" : code;
            m_JoinCode.text = waiting ? "OPENING" : code;
            m_JoinCode.style.color = waiting ? MenuLook.InkFaint : MenuLook.HiVis;
        }

        public void SayTheJoinFailed(string why)
        {
            m_Trouble.text = why;
            m_Trouble.style.color = why.EndsWith("...") ? MenuLook.InkSoft : MenuLook.Bad;
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
            screen.Add(MenuLook.FloorShadow());

            var column = new VisualElement();

            column.style.position = Position.Absolute;
            column.style.left = MenuLook.Gutter + MenuLook.Nudge;
            column.style.bottom = MenuLook.Gutter + MenuLook.Nudge;
            column.style.width = MenuLook.ColumnWidth;

            screen.Add(column);

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
            prompt.style.color = MenuLook.HiVis;
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
            var column = Stage(screen, 0.55f, 0.92f);

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
            var column = Stage(screen, 0.55f, 0.92f);

            column.Add(Heading("JOIN A SHIFT"));

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

        VisualElement Lobby(out Label code)
        {
            var screen = MenuLook.Screen("lobby");

            screen.Add(m_Plates.Root);

            var column = Stage(screen, 0.5f, 0.88f);

            column.Add(Heading("THE CREW"));

            var leaving = new MenuList();
            leaving.Add("Leave", () => Backed?.Invoke());
            column.Add(leaving.Root);

            var acting = new MenuList(acrossTheScreen: true);
            m_ReadyOn = acting.Count;
            acting.Add("Ready", () => ReadyToggled?.Invoke());
            m_StartOn = acting.Count;
            acting.Add("Start", () => ShiftStarted?.Invoke());
            acting.Available(m_StartOn, false);

            m_Lists[MenuScreen.Lobby] = acting;
            screen.Add(AlongTheBottom(acting.Root));

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

        VisualElement JoinCodeCard(out Label code)
        {
            var card = new VisualElement();

            card.style.position = Position.Absolute;
            card.style.right = MenuLook.Gutter;
            card.style.bottom = MenuLook.Gutter;
            card.style.paddingLeft = 22;
            card.style.paddingRight = 22;
            card.style.paddingTop = 16;
            card.style.paddingBottom = 16;
            card.style.backgroundColor = new Color(0f, 0f, 0f, 0.62f);
            MenuLook.Edges(card, MenuLook.InkFaint, 1);

            var label = MenuLook.Eyebrow("JOIN CODE", MenuLook.InkFaint);
            label.style.marginBottom = 8;

            code = MenuLook.Data("OPENING", 26, MenuLook.InkFaint);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var copy = new Button(CopyTheCode) { text = "COPY" };
            copy.style.marginLeft = 18;
            copy.style.marginRight = 0;
            copy.style.height = 30;
            copy.style.paddingLeft = 14;
            copy.style.paddingRight = 14;
            copy.style.fontSize = 12;
            copy.style.letterSpacing = 2f;
            copy.style.color = MenuLook.Ink;
            copy.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
            MenuLook.Edges(copy, MenuLook.InkSoft, 1);

            row.Add(code);
            row.Add(copy);

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

            var name = MenuLook.Display(heading, 40);
            name.style.marginBottom = 10;

            var rule = MenuLook.Rule(300, MenuLook.InkFaint);
            rule.style.marginBottom = 16;

            stack.Add(name);
            stack.Add(rule);

            MenuLook.SettleIn(name, 0.07f);

            return stack;
        }
    }
}
