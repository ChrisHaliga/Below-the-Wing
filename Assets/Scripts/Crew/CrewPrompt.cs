namespace BelowTheWing.Crew
{
    public enum CrewPrompt
    {
        None,

        Offer,

        NoAnswer,

        BeingDriven
    }

    public interface IOfferSomething
    {
        CrewPrompt Prompt { get; }

        string Subject { get; }
    }
}
