namespace MedalSync;

/// <summary>
/// Simple localization system supporting German and English.
/// All UI strings are centralized here.
/// </summary>
public static class Loc
{
    public static string CurrentLanguage { get; private set; } = "de";

    public static void SetLanguage(string lang)
    {
        CurrentLanguage = lang == "en" ? "en" : "de";
    }

    // Helper: returns German or English string based on current language
    private static string S(string de, string en) => CurrentLanguage == "en" ? en : de;

    // ── Tray Menu ───────────────────────────────────────────────────────

    public static string TrayStarting => S("Wird gestartet...", "Starting...");
    public static string TrayTooltip(string status) => $"MedalSync — {status}";
    public static string ClipsSynced(int n) => S($"{n} Clips gesynct", $"{n} clips synced");

    public static string MenuPause => S("⏸  Pausieren", "⏸  Pause");
    public static string MenuResume => S("▶  Fortsetzen", "▶  Resume");
    public static string MenuResync => S("🔄  Neu synchronisieren", "🔄  Resync");
    public static string MenuOpenSync => S("📂  Sync-Ordner öffnen", "📂  Open sync folder");
    public static string MenuOpenSource => S("📂  Clips-Ordner öffnen", "📂  Open clips folder");
    public static string MenuSettings => S("⚙  Einstellungen...", "⚙  Settings...");
    public static string MenuLanguage => S("🌐  Sprache", "🌐  Language");
    public static string MenuAutoStartOn => S("☑  Mit Windows starten", "☑  Start with Windows");
    public static string MenuAutoStartOff => S("☐  Mit Windows starten", "☐  Start with Windows");
    public static string MenuExit => S("❌  Beenden", "❌  Exit");

    public static string LangGerman => "Deutsch";
    public static string LangEnglish => "English";

    // ── Status Messages ─────────────────────────────────────────────────

    public static string StatusActive => S("Aktiv – überwacht Clips...", "Active – watching clips...");
    public static string StatusPaused => S("Pausiert", "Paused");
    public static string StatusStopped => S("Gestoppt", "Stopped");
    public static string StatusSyncing => S("Synchronisiere bestehende Clips...", "Syncing existing clips...");
    public static string StatusErrorRestart => S("Fehler – versuche Neustart...", "Error – attempting restart...");
    public static string StatusErrorFailed => S(
        "Fehler – Watcher konnte nicht neu gestartet werden",
        "Error – watcher could not be restarted");
    public static string StatusSourceNotFound(string path) => S(
        $"Quellordner nicht gefunden: {path}",
        $"Source folder not found: {path}");

    // ── Log Messages ────────────────────────────────────────────────────

    public static string LogLinkCreated(string name) => S($"Link erstellt: {name}", $"Link created: {name}");
    public static string LogLinkError(string name, string? err) => S($"Fehler bei {name}: {err}", $"Error for {name}: {err}");
    public static string LogOrphanRemoved(string name) => S($"Verwaisten Link entfernt: {name}", $"Orphan removed: {name}");
    public static string LogSyncComplete(int created, int skipped, int cleaned) => S(
        $"Initial-Sync abgeschlossen: {created} erstellt, {skipped} übersprungen, {cleaned} aufgeräumt",
        $"Initial sync complete: {created} created, {skipped} skipped, {cleaned} cleaned");
    public static string LogLinkExists(string name) => S($"Link existiert bereits: {name}", $"Link already exists: {name}");
    public static string LogNewClip(string name) => S($"Neuer Clip gesynct: {name}", $"New clip synced: {name}");
    public static string LogLinkRemoved(string name) => S($"Link entfernt: {name}", $"Link removed: {name}");
    public static string LogRemoveError(string msg) => S($"Fehler beim Entfernen: {msg}", $"Error removing: {msg}");
    public static string LogWatcherError(string msg) => S($"Watcher-Fehler: {msg}", $"Watcher error: {msg}");
    public static string LogRestartFailed(string msg) => S($"Neustart fehlgeschlagen: {msg}", $"Restart failed: {msg}");

    // ── Settings Dialog ─────────────────────────────────────────────────

    public static string SettingsTitle => S("MedalSync — Einstellungen", "MedalSync — Settings");
    public static string SettingsHeader => S("⚙  Einstellungen", "⚙  Settings");
    public static string SettingsSourceLabel => S("NVIDIA Clips-Ordner:", "NVIDIA Clips Folder:");
    public static string SettingsSyncLabel => S("Medal Sync-Ordner:", "Medal Sync Folder:");
    public static string SettingsSave => S("Speichern", "Save");
    public static string SettingsCancel => S("Abbrechen", "Cancel");
    public static string SettingsBrowse => S("Ordner auswählen", "Select folder");
    public static string SettingsErrorTitle => S("MedalSync — Fehler", "MedalSync — Error");
    public static string SettingsErrorNoSource => S(
        "Bitte gib den NVIDIA Clips-Ordner an.",
        "Please specify the NVIDIA clips folder.");
    public static string SettingsErrorNoSync => S(
        "Bitte gib den Sync-Ordner an.",
        "Please specify the sync folder.");
    public static string SettingsErrorSourceNotExist(string path) => S(
        $"Der Clips-Ordner existiert nicht:\n{path}",
        $"The clips folder does not exist:\n{path}");
    public static string SettingsErrorDiffDrive => S(
        "Clips-Ordner und Sync-Ordner müssen auf dem selben Laufwerk liegen!\n\n(Hardlinks funktionieren nur auf dem gleichen NTFS-Volume)",
        "Clips folder and sync folder must be on the same drive!\n\n(Hardlinks only work on the same NTFS volume)");
    public static string SettingsCustomSync => S(
        "Benutzerdefinierten Sync-Ordner verwenden",
        "Use custom sync folder");

    // ── First Run ───────────────────────────────────────────────────────

    public static string FirstRunTitle => S("MedalSync — Ersteinrichtung", "MedalSync — First Time Setup");
    public static string FirstRunMessage => S(
        "Willkommen bei MedalSync!\n\nBitte wähle im nächsten Schritt deinen NVIDIA ShadowPlay Clips-Ordner aus.",
        "Welcome to MedalSync!\n\nIn the next step, please select your NVIDIA ShadowPlay clips folder.");

    // ── Other ───────────────────────────────────────────────────────────

    public static string AlreadyRunning => S(
        "MedalSync läuft bereits im System Tray!",
        "MedalSync is already running in the system tray!");
}
