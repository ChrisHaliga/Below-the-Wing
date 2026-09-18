namespace BelowTheWing.Wiring
{
    public static class Shift
    {
        public const int MostCrew = 5;

        public const ulong Nobody = ulong.MaxValue;

        public const string Title = "Below the Wing";

        public static string NameFor(ulong client) => $"Player {client}";
    }
}
