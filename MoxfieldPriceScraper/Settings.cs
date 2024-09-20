using Newtonsoft.Json;
using Serilog;

namespace MoxfieldPriceScraper;

public class Settings : ISettings
{
    private const string SettingsPath = "./settings.json";

    public decimal TargetPrice { get; set; }
    public int UpdateFrequency { get; set; }
    public string? MoxfieldUsername { get; set; }
    public string? MoxfieldPassword { get; set; }
    public bool SendEmailNotification { get; set; }
    public string? SenderEmailAddress { get; set; }
    public string? SenderEmailPassword { get; set; }
    public string? ReceiverEmailAddress { get; set; }
    public Dictionary<string, string>? DeckList { get; set; }

    // Private constructor to enforce use of factory method
    private Settings()
    {
    }

    /// <summary>
    /// Creates a new instance of the Settings class, propagated with loaded settings values.
    /// </summary>
    /// <returns>A new instance of the Settings class</returns>
    public static async Task<Settings> CreateAsync()
    {
        Log.Debug("Loading settings from file");
        return await LoadSettingsAsync();
    }

    /// <inheritdoc/>
    /// <exception cref="Exception">JSON serialization failed.</exception>
    public async Task SaveSettingsAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Log.Debug("Creating directory {Directory}", directory);
                Directory.CreateDirectory(directory);
            }

            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            await File.WriteAllTextAsync(SettingsPath, json);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Failed to serialize settings.json");
            throw new Exception("Failed to serialize settings.json", e);
        }
    }

    /// <summary>
    /// Loads settings from file.
    /// </summary>
    /// <returns>A deserialized settings object.</returns>
    /// <exception cref="Exception">JSON deserialization failed.</exception>
    private static async Task<Settings> LoadSettingsAsync()
    {
        if (!File.Exists(SettingsPath))
        {
            Log.Fatal("settings.json not found");
            throw new Exception("settings.json not found");
        }

        try
        {
            var json = await File.ReadAllTextAsync(SettingsPath);
            return JsonConvert.DeserializeObject<Settings>(json) ??
                   throw new Exception("Failed to deserialize settings.json");
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Failed to deserialize settings.json");
            throw new Exception("Failed to deserialize settings.json", e);
        }
    }
}
