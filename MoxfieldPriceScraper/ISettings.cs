namespace MoxfieldPriceScraper;

public interface ISettings
{
    decimal TargetPrice { get; }
    int UpdateFrequency { get; }
    string? MoxfieldUsername { get; }
    string? MoxfieldPassword { get; }
    bool SendEmailNotification { get; }
    string? SenderEmailAddress { get; }
    string? SenderEmailPassword { get; }
    string? ReceiverEmailAddress { get; }
    Dictionary<string, string>? DeckList { get; }
}
