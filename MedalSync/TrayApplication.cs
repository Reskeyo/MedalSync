using Microsoft.Win32;

namespace MedalSync;

/// <summary>
/// System tray application â€” no visible window, only a tray icon with context menu.
/// Supports German and English localization with dynamic menu rebuilding.
/// </summary>
public sealed class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly SyncEngine _engine;
    private readonly Settings _settings;
    private readonly SynchronizationContext? _syncContext;
    private ContextMenuStrip _menu = null!;

    // Menu items that need dynamic updates
    private ToolStripLabel _statusLabel = null!;
    private ToolStripLabel _countLabel = null!;
    private ToolStripMenuItem _pauseItem = null!;
    private ToolStripMenuItem _autoStartItem = null!;
    private ToolStripMenuItem _langDeItem = null!;
    private ToolStripMenuItem _langEnItem = null!;

    private const string AutoStartRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MedalSync";

    public TrayApplication(Settings settings)
    {
        _settings = settings;
        _syncContext = SynchronizationContext.Current;
        _settings.AutoStart = CheckAutoStart();
        _settings.Save();

        Loc.SetLanguage(_settings.Language);

        _engine = new SyncEngine(_settings);

        // â”€â”€ Tray Icon â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        _trayIcon = new NotifyIcon
        {
            Icon = CreateAppIcon(),
            Text = Loc.TrayTooltip(Loc.TrayStarting),
            Visible = true
        };

        _trayIcon.DoubleClick += OnOpenSyncFolder;

        // â”€â”€ Build Menu & Wire Events â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        BuildMenu();

        _engine.StatusChanged += status =>
        {
            _statusLabel.Text = status;
            _trayIcon.Text = Loc.TrayTooltip(status);
        };

        _engine.SyncCountChanged += count =>
        {
            _countLabel.Text = Loc.ClipsSynced(count);
        };

        // â”€â”€ Start engine â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        _engine.Start();

        StartUpdateCheck();
    }

    // â”€â”€ Menu Building â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Builds (or rebuilds) the entire context menu with current language strings.
    /// </summary>
    private void BuildMenu()
    {
        _menu?.Dispose();

        _statusLabel = new ToolStripLabel(Loc.TrayStarting)
        {
            ForeColor = Color.FromArgb(140, 140, 140),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
        };

        _countLabel = new ToolStripLabel(Loc.ClipsSynced(_engine.SyncedCount))
        {
            ForeColor = Color.FromArgb(100, 180, 255),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };

        _pauseItem = new ToolStripMenuItem(
            _engine.IsPaused ? Loc.MenuResume : Loc.MenuPause, null, OnPauseResume);

        // Autostart â€” show checked/unchecked state in the label itself
        _autoStartItem = new ToolStripMenuItem(
            _settings.AutoStart ? Loc.MenuAutoStartOn : Loc.MenuAutoStartOff,
            null, OnAutoStartToggle);

        // Language submenu
        _langDeItem = new ToolStripMenuItem(Loc.LangGerman, null, OnSetGerman)
        {
            Checked = _settings.Language == "de",
            ForeColor = _settings.Language == "de" ? Color.FromArgb(255, 180, 50) : Color.FromArgb(200, 200, 200)
        };
        _langEnItem = new ToolStripMenuItem(Loc.LangEnglish, null, OnSetEnglish)
        {
            Checked = _settings.Language == "en",
            ForeColor = _settings.Language == "en" ? Color.FromArgb(255, 180, 50) : Color.FromArgb(200, 200, 200)
        };

        var langMenu = new ToolStripMenuItem(Loc.MenuLanguage);
        langMenu.DropDownItems.AddRange(new ToolStripItem[] { _langDeItem, _langEnItem });

        // Style the language dropdown
        langMenu.DropDown.BackColor = Color.FromArgb(30, 30, 30);
        langMenu.DropDown.ForeColor = Color.White;
        langMenu.DropDown.Renderer = new DarkMenuRenderer();

        _menu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f),
            ShowImageMargin = false,
            Renderer = new DarkMenuRenderer()
        };

        _menu.Items.AddRange(new ToolStripItem[]
        {
            _statusLabel,
            _countLabel,
            new ToolStripSeparator(),
            _pauseItem,
            new ToolStripMenuItem(Loc.MenuResync, null, OnResync),
            new ToolStripSeparator(),
            new ToolStripMenuItem(Loc.MenuOpenSync, null, OnOpenSyncFolder),
            new ToolStripMenuItem(Loc.MenuOpenSource, null, OnOpenSourceFolder),
            new ToolStripSeparator(),
            new ToolStripMenuItem(Loc.MenuSettings, null, OnShowSettings),
            _autoStartItem,
            langMenu,
            new ToolStripSeparator(),
            new ToolStripMenuItem(Loc.MenuExit, null, OnExit)
        });

        _trayIcon.ContextMenuStrip = _menu;
    }

    // â”€â”€ Event Handlers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void OnPauseResume(object? sender, EventArgs e)
    {
        if (_engine.IsPaused)
        {
            _engine.Resume();
            _pauseItem.Text = Loc.MenuPause;
        }
        else
        {
            _engine.Pause();
            _pauseItem.Text = Loc.MenuResume;
        }
    }

    private void OnResync(object? sender, EventArgs e)
    {
        _engine.Resync();
    }

    private void OnOpenSyncFolder(object? sender, EventArgs e)
    {
        if (Directory.Exists(_settings.SyncFolder))
            System.Diagnostics.Process.Start("explorer.exe", _settings.SyncFolder);
    }

    private void OnOpenSourceFolder(object? sender, EventArgs e)
    {
        if (Directory.Exists(_settings.SourceFolder))
            System.Diagnostics.Process.Start("explorer.exe", _settings.SourceFolder);
    }

    private void OnShowSettings(object? sender, EventArgs e)
    {
        using var dialog = new SettingsDialog(_settings);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _settings.Save();
            _engine.Stop();
            _engine.Start();
        }
    }

    private void OnAutoStartToggle(object? sender, EventArgs e)
    {
        _settings.AutoStart = !_settings.AutoStart;
        _settings.Save();
        SetAutoStart(_settings.AutoStart);

        // Update the label to reflect new state
        _autoStartItem.Text = _settings.AutoStart
            ? Loc.MenuAutoStartOn
            : Loc.MenuAutoStartOff;
    }

    private void OnSetGerman(object? sender, EventArgs e) => ChangeLanguage("de");
    private void OnSetEnglish(object? sender, EventArgs e) => ChangeLanguage("en");

    private void ChangeLanguage(string lang)
    {
        if (_settings.Language == lang) return;

        _settings.Language = lang;
        _settings.Save();
        Loc.SetLanguage(lang);

        // Rebuild entire menu with new language
        string currentStatus = _statusLabel.Text ?? Loc.StatusActive;
        int currentCount = _engine.SyncedCount;

        BuildMenu();

        // Restore current state
        _statusLabel.Text = _engine.IsRunning
            ? (_engine.IsPaused ? Loc.StatusPaused : Loc.StatusActive)
            : Loc.StatusStopped;
        _countLabel.Text = Loc.ClipsSynced(currentCount);
        _trayIcon.Text = Loc.TrayTooltip(_statusLabel.Text);
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _engine.Stop();
        _engine.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    private void StartUpdateCheck()
    {
        Task.Run(async () =>
        {
            try
            {
                var update = await UpdateService.CheckForUpdateAsync(_settings);
                if (update == null)
                    return;

                RunOnUiThread(() => PromptUpdate(update));
            }
            catch
            {
                // Best effort only for background updates.
            }
        });
    }

    private void PromptUpdate(UpdateInfo update)
    {
        var result = MessageBox.Show(
            Loc.UpdateAvailable(update.Version.ToString()),
            Loc.UpdateTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (result != DialogResult.Yes)
            return;

        Task.Run(async () =>
        {
            try
            {
                var installerPath = await UpdateService.DownloadUpdateAsync(update);
                try
                {
                    UpdateService.RunInstaller(installerPath);
                    RunOnUiThread(Application.Exit);
                }
                catch (Exception ex)
                {
                    RunOnUiThread(() => MessageBox.Show(
                        Loc.UpdateLaunchFailed(ex.Message),
                        Loc.UpdateTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning));
                }
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => MessageBox.Show(
                    Loc.UpdateDownloadFailed(ex.Message),
                    Loc.UpdateTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning));
            }
        });
    }

    private void RunOnUiThread(Action action)
    {
        if (_syncContext != null)
            _syncContext.Post(_ => action(), null);
        else
            action();
    }

    // â”€â”€ Auto-Start Management â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AutoStartRegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                string exePath = Application.ExecutablePath;
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch
        {
            // Registry access might fail — silently ignore
        }
    }

    private static bool CheckAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AutoStartRegistryKey, false);
            if (key == null) return false;

            var value = key.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch { return false; }
    }

    // ── Icon Generation ─────────────────────────────────────────────────

    /// <summary>
    /// Creates a simple sync-style icon programmatically.
    /// Gold circle with "M" in the center.
    /// </summary>
    private static Icon CreateAppIcon()
    {
        // Try to load custom icon first
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "icon.ico");
        if (File.Exists(iconPath))
        {
            try { return new Icon(iconPath); }
            catch { /* fall through to generated icon */ }
        }

        // Generate a simple icon programmatically
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Background circle â€” Medal-ish gold/amber gradient
        using var bgBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(0, 0, 32, 32),
            Color.FromArgb(255, 180, 50),   // Gold
            Color.FromArgb(230, 120, 20),   // Darker amber
            45f);
        g.FillEllipse(bgBrush, 1, 1, 30, 30);

        // "M" letter in center
        using var textBrush = new SolidBrush(Color.White);
        using var font = new Font("Segoe UI", 16, FontStyle.Bold, GraphicsUnit.Pixel);
        var textSize = g.MeasureString("M", font);
        g.DrawString("M", font, textBrush,
            (32 - textSize.Width) / 2,
            (32 - textSize.Height) / 2);

        // Small sync arrows indicator (bottom-right corner)
        using var arrowPen = new Pen(Color.White, 1.5f);
        g.DrawArc(arrowPen, 20, 20, 10, 10, 0, 270);

        // Convert Bitmap to Icon
        IntPtr hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }
}

// â”€â”€ Dark Theme Menu Renderer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Custom renderer for a dark-themed context menu matching modern Windows aesthetics.
/// </summary>
internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkColorTable()) { }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected ? Color.White : Color.FromArgb(220, 220, 220);
        base.OnRenderItemText(e);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected && e.Item.Enabled)
        {
            using var brush = new SolidBrush(Color.FromArgb(50, 50, 55));
            e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
        }
        else
        {
            base.OnRenderMenuItemBackground(e);
        }
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(Color.FromArgb(55, 55, 60));
        int y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 0, y, e.Item.Width, y);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = Color.FromArgb(180, 180, 180);
        base.OnRenderArrow(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        // Draw a custom gold checkmark for checked language items
        using var pen = new Pen(Color.FromArgb(255, 180, 50), 2f);
        var rect = e.ImageRectangle;
        int x = rect.X + 4;
        int y = rect.Y + rect.Height / 2;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.DrawLines(pen, new Point[]
        {
            new(x, y),
            new(x + 4, y + 4),
            new(x + 10, y - 4)
        });
    }
}

internal sealed class DarkColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => Color.FromArgb(50, 50, 55);
    public override Color MenuItemBorder => Color.FromArgb(60, 60, 65);
    public override Color MenuItemSelected => Color.FromArgb(50, 50, 55);
    public override Color MenuStripGradientBegin => Color.FromArgb(30, 30, 30);
    public override Color MenuStripGradientEnd => Color.FromArgb(30, 30, 30);
    public override Color ToolStripDropDownBackground => Color.FromArgb(30, 30, 30);
    public override Color ImageMarginGradientBegin => Color.FromArgb(30, 30, 30);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(30, 30, 30);
    public override Color ImageMarginGradientEnd => Color.FromArgb(30, 30, 30);
    public override Color SeparatorDark => Color.FromArgb(55, 55, 60);
    public override Color SeparatorLight => Color.FromArgb(55, 55, 60);
    public override Color CheckBackground => Color.FromArgb(50, 50, 55);
    public override Color CheckSelectedBackground => Color.FromArgb(60, 60, 65);
    public override Color CheckPressedBackground => Color.FromArgb(60, 60, 65);
}
