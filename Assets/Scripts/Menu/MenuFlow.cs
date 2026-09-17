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

        public event Action LeftTheSession;

        public MenuScreen Showing { get; private set; } = MenuScreen.Title;

        public bool Open => Showing != MenuScreen.None;

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

            var leaving = Showing == MenuScreen.Lobby;

            Show(MenuScreen.Main);

            if (leaving)
            {
                LeftTheSession?.Invoke();
            }
        }

        public void ShiftStarted() => Show(MenuScreen.None);
    }
}
