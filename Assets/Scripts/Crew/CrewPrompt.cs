namespace BelowTheWing.Crew
{
    public enum CrewPrompt
    {
        None,

        Offer,

        Refused
    }

    public interface IOfferSomething
    {
        CrewPrompt Prompt { get; }

        string Message { get; }
    }
}
