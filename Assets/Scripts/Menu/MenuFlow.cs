using System;

namespace BelowTheWing.Menu
{
    public enum MenuScreen
    {
        None,

        Title,

        Main,

        Join,

        Lobby,

        Settings
    }

    public sealed class MenuFlow
    {
        public event Action<MenuScreen> Changed;

        public MenuScreen Showing { get; private set; } = MenuScreen.Title;

        public bool Open => Showing != MenuScreen.None;

        public bool LeavingTheSession { get; private set; }

        public void AnyButtonPressed()
        {
            if (Showing == MenuScreen.Title)
            {
                Show(MenuScreen.Main);
            }
        }

        public void Show(MenuScreen screen)
        {
            if (Showing == screen)
            {
                return;
            }

            Showing = screen;
            Changed?.Invoke(screen);
        }

        public void Back()
        {
            if (Showing is MenuScreen.Title or MenuScreen.Main or MenuScreen.None)
            {
                return;
            }

            LeavingTheSession = Showing == MenuScreen.Lobby;

            Show(MenuScreen.Main);
        }

        public void ShiftStarted() => Show(MenuScreen.None);
    }
}
