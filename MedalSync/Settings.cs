using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedalSync;

/// <summary>
/// Application settings — persisted as JSON next to the executable.
/// </summary>
public sealed class Settings
{
    private static readonly string SettingsPath =
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── Properties ──────────────────────────────────────────────────────

    /// <summary>
    /// Root folder where NVIDIA ShadowPlay saves clips (with game sub-folders).
    /// </summary>
    public string SourceFolder { get; set; } = @"D:\Clips";

    /// <summary>
    /// Flat folder where hard links are created for Medal to read.
    /// </summary>
    public string SyncFolder { get; set; } = @"D:\MedalSync";

    /// <summary>
    /// File extensions to watch (case-insensitive).
    /// </summary>
    public string[] Extensions { get; set; } = [".mp4"];

    /// <summary>
    /// Whether the application should start with Windows.
    /// </summary>
    public bool AutoStart { get; set; } = false;

    // ── Persistence ─────────────────────────────────────────────────────

    public static Settings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<Settings>(json, JsonOptions) ?? new Settings();
            }
        }
        catch
        {
            // Corrupted file — fall back to defaults
        }

        var settings = new Settings();
        settings.Save(); // Create default settings file
        return settings;
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Best effort — settings file is not critical
        }
    }
}
