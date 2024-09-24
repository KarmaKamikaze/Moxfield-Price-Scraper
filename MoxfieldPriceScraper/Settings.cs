using DotNetEnv;
using Newtonsoft.Json;
using Serilog;

namespace MoxfieldPriceScraper;

public class Settings : ISettings
{
    public decimal TargetPrice { get; }
    public int UpdateFrequency { get; }
    public string? MoxfieldUsername { get; }
    public string? MoxfieldPassword { get; }
    public bool SendEmailNotification { get; }
    public string? SenderEmailAddress { get; }
    public string? SenderEmailPassword { get; }
    public string? ReceiverEmailAddress { get; }
    public Dictionary<string, string>? DeckList { get; }

    public Settings()
    {
#if DEBUG
        Log.Debug("Loading settings from .env file");
        Env.Load(".env");
#endif
        TargetPrice = GetDecimal("TARGET_PRICE", 25.00m);
        UpdateFrequency = GetInt("UPDATE_FREQUENCY", 300);
        MoxfieldUsername = GetString("MOXFIELD_USERNAME");
        MoxfieldPassword = GetString("MOXFIELD_PASSWORD");
        SendEmailNotification = GetBool("SEND_EMAIL_NOTIFICATION", false);
        SenderEmailAddress = GetString("SENDER_EMAIL_ADDRESS");
        SenderEmailPassword = GetString("SENDER_EMAIL_PASSWORD");
        ReceiverEmailAddress = GetString("RECEIVER_EMAIL_ADDRESS");
        DeckList = GetDeckList("DECK_LIST"); // Expecting a JSON-formatted string in env variable
    }

    /// <summary>
    /// Gets a decimal value from the environment variables, or returns a default value if not found.
    /// </summary>
    /// <param name="key">The key from the environment containing a decimal value.</param>
    /// <param name="defaultValue">The default fallback value.</param>
    /// <returns>A decimal value associated with the given key.</returns>
    private static decimal GetDecimal(string key, decimal defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets an integer value from the environment variables, or returns a default value if not found.
    /// </summary>
    /// <param name="key">The key from the environment containing an integer value.</param>
    /// <param name="defaultValue">The default fallback value.</param>
    /// <returns>An integer value associated with the given key.</returns>
    private static int GetInt(string key, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets a string value from the environment variables.
    /// </summary>
    /// <param name="key">The key from the environment containing a string value.</param>
    /// <returns>A string value associated with the given key.</returns>
    private static string? GetString(string key)
    {
        return Environment.GetEnvironmentVariable(key);
    }

    /// <summary>
    /// Gets a boolean value from the environment variables, or returns a default value if not found.
    /// </summary>
    /// <param name="key">The key from the environment containing a boolean value.</param>
    /// <param name="defaultValue">The default fallback value.</param>
    /// <returns>A boolean value associated with the given key.</returns>
    private static bool GetBool(string key, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets a dictionary of deck names and URLs from the environment variables.
    /// </summary>
    /// <param name="key">The key from the environment containing a dictionary of deck names and URLs in json format.</param>
    /// <returns>A dictionary of deck names and URLs associated with the given key.</returns>
    private static Dictionary<string, string>? GetDeckList(string key)
    {
        var json = Environment.GetEnvironmentVariable(key);
        return json != null
            ? JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
            : null;
    }
}
