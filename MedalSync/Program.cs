namespace MedalSync;

static class Program
{
    /// <summary>
    /// MedalSync — Syncs NVIDIA ShadowPlay clips to Medal via hard links.
    /// Runs as a system tray application with no visible window.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Ensure only one instance runs at a time
        using var mutex = new Mutex(true, "MedalSync_SingleInstance", out bool isNew);
        if (!isNew)
        {
            // Load settings to get language for the error message
            var currentSettings = Settings.Load();
            Loc.SetLanguage(currentSettings.Language);

            MessageBox.Show(
                Loc.AlreadyRunning,
                "MedalSync",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        var settings = Settings.Load();
        Loc.SetLanguage(settings.Language);

        if (settings.IsFirstRun)
        {
            MessageBox.Show(
                Loc.FirstRunMessage,
                Loc.FirstRunTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            using var dialog = new SettingsDialog(settings);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                settings.IsFirstRun = false;
                settings.Save();
            }
            else
            {
                // User cancelled first run setup
                return;
            }
        }

        Application.Run(new TrayApplication(settings));
    }
}
