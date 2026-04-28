namespace MedalSync;

/// <summary>
/// Simple settings dialog for configuring source and sync folders.
/// Dark-themed to match the tray menu aesthetic. Fully localized.
/// </summary>
public sealed class SettingsDialog : Form
{
    private readonly Settings _settings;
    private readonly TextBox _sourceBox;
    private readonly TextBox _syncBox;

    public SettingsDialog(Settings settings)
    {
        _settings = settings;

        // ── Form Setup ──────────────────────────────────────────────────
        Text = Loc.SettingsTitle;
        Size = new Size(520, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(25, 25, 28);
        ForeColor = Color.FromArgb(220, 220, 220);
        Font = new Font("Segoe UI", 9.5f);

        // ── Title ───────────────────────────────────────────────────────
        var titleLabel = new Label
        {
            Text = Loc.SettingsHeader,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 180, 50),
            Location = new Point(20, 15),
            AutoSize = true
        };

        // ── Source Folder ───────────────────────────────────────────────
        var sourceLabel = new Label
        {
            Text = Loc.SettingsSourceLabel,
            Location = new Point(20, 60),
            AutoSize = true
        };

        _sourceBox = new TextBox
        {
            Text = settings.SourceFolder,
            Location = new Point(20, 82),
            Size = new Size(380, 28),
            BackColor = Color.FromArgb(40, 40, 44),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f)
        };

        var sourceBrowse = CreateBrowseButton(400, 80);
        sourceBrowse.Click += (s, e) => BrowseFolder(_sourceBox);

        // ── Sync Folder ─────────────────────────────────────────────────
        var syncLabel = new Label
        {
            Text = Loc.SettingsSyncLabel,
            Location = new Point(20, 118),
            AutoSize = true
        };

        _syncBox = new TextBox
        {
            Text = settings.SyncFolder,
            Location = new Point(20, 140),
            Size = new Size(380, 28),
            BackColor = Color.FromArgb(40, 40, 44),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f)
        };

        var syncBrowse = CreateBrowseButton(400, 138);
        syncBrowse.Click += (s, e) => BrowseFolder(_syncBox);

        // ── Buttons ─────────────────────────────────────────────────────
        var saveButton = new Button
        {
            Text = Loc.SettingsSave,
            Size = new Size(100, 35),
            Location = new Point(285, 190),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(255, 180, 50),
            ForeColor = Color.FromArgb(25, 25, 28),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.OK
        };
        saveButton.FlatAppearance.BorderSize = 0;
        saveButton.Click += OnSave;

        var cancelButton = new Button
        {
            Text = Loc.SettingsCancel,
            Size = new Size(100, 35),
            Location = new Point(395, 190),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 50, 55),
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        cancelButton.FlatAppearance.BorderSize = 0;

        // ── Add Controls ────────────────────────────────────────────────
        Controls.AddRange(new Control[]
        {
            titleLabel,
            sourceLabel, _sourceBox, sourceBrowse,
            syncLabel, _syncBox, syncBrowse,
            saveButton, cancelButton
        });

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        string source = _sourceBox.Text.Trim();
        string sync = _syncBox.Text.Trim();

        if (string.IsNullOrEmpty(source))
        {
            ShowError(Loc.SettingsErrorNoSource);
            return;
        }

        if (string.IsNullOrEmpty(sync))
        {
            ShowError(Loc.SettingsErrorNoSync);
            return;
        }

        if (!Directory.Exists(source))
        {
            ShowError(Loc.SettingsErrorSourceNotExist(source));
            return;
        }

        // Check both paths are on the same drive (required for hard links)
        if (!string.Equals(Path.GetPathRoot(source), Path.GetPathRoot(sync),
            StringComparison.OrdinalIgnoreCase))
        {
            ShowError(Loc.SettingsErrorDiffDrive);
            return;
        }

        _settings.SourceFolder = source;
        _settings.SyncFolder = sync;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static void BrowseFolder(TextBox target)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = Loc.SettingsBrowse,
            UseDescriptionForTitle = true,
            SelectedPath = target.Text
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            target.Text = dialog.SelectedPath;
        }
    }

    private Button CreateBrowseButton(int x, int y)
    {
        var btn = new Button
        {
            Text = "📁",
            Size = new Size(35, 28),
            Location = new Point(x, y),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 50, 55),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 65);
        btn.FlatAppearance.BorderSize = 1;
        return btn;
    }

    private static void ShowError(string message)
    {
        MessageBox.Show(message, Loc.SettingsErrorTitle,
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
