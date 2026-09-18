namespace BelowTheWing.Menu
{
    public enum MenuStation
    {
        Wide,

        Cart,

        Inside
    }

    public readonly struct MenuStaging
    {
        public const float TitleSeconds = 1.4f;
        public const float PanelSeconds = 1.0f;
        public const float RevealSeconds = 3.4f;

        MenuStaging(MenuStation station, bool doorsOpen, float travelSeconds)
        {
            Station = station;
            DoorsOpen = doorsOpen;
            TravelSeconds = travelSeconds;
            Staged = true;
        }

        public MenuStation Station { get; }

        public bool DoorsOpen { get; }

        public float TravelSeconds { get; }

        public bool Staged { get; }

        public static MenuStaging For(MenuScreen screen)
            => screen switch
            {
                MenuScreen.None => default,
                MenuScreen.Title => new MenuStaging(MenuStation.Wide, false, TitleSeconds),
                MenuScreen.Lobby => new MenuStaging(MenuStation.Inside, true, RevealSeconds),
                _ => new MenuStaging(MenuStation.Cart, false, PanelSeconds)
            };
    }
}
