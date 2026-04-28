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
            MessageBox.Show(
                "MedalSync läuft bereits im System Tray!",
                "MedalSync",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplication());
    }
}