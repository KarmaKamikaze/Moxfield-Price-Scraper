namespace MoxfieldPriceScraper;

public interface ISettings
{
    decimal TargetPrice { get; set; }
    int UpdateFrequency { get; set; }
    string? MoxfieldUsername { get; set; }
    string? MoxfieldPassword { get; set; }
    bool SendEmailNotification { get; set; }
    string? SenderEmailAddress { get; set; }
    string? SenderEmailPassword { get; set; }
    string? ReceiverEmailAddress { get; set; }
    Dictionary<string, string>? DeckList { get; set; }

    /// <summary>
    /// Saves the current settings to file.
    /// </summary>
    Task SaveSettingsAsync();
}
