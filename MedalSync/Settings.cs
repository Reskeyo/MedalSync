using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedalSync;

/// <summary>
/// Application settings — persisted as JSON next to the executable.
/// </summary>
public sealed class Settings
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MedalSync");
    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");
    private static readonly string LegacySettingsPath =
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

    /// <summary>
    /// UI language: "de" (Deutsch) or "en" (English).
    /// </summary>
    public string Language { get; set; } = "de";

    /// <summary>
    /// Whether this is the first time the app is running.
    /// </summary>
    public bool IsFirstRun { get; set; } = true;

    /// <summary>
    /// Whether the user has opted to use a custom sync folder.
    /// </summary>
    public bool CustomSyncFolder { get; set; } = false;

    /// <summary>
    /// Whether automatic update checks are enabled.
    /// </summary>
    public bool AutoUpdateEnabled { get; set; } = true;

    /// <summary>
    /// Last successful update check time (UTC).
    /// </summary>
    public DateTime LastUpdateCheckUtc { get; set; } = DateTime.MinValue;

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

            if (File.Exists(LegacySettingsPath))
            {
                string json = File.ReadAllText(LegacySettingsPath);
                var settings = JsonSerializer.Deserialize<Settings>(json, JsonOptions) ?? new Settings();
                settings.Save();
                return settings;
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
            Directory.CreateDirectory(SettingsDir);
            string json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Best effort — settings file is not critical
        }
    }
}
