using Microsoft.Win32;

namespace MedalSync;

/// <summary>
/// System tray application — no visible window, only a tray icon with context menu.
/// </summary>
public sealed class TrayApplication : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly SyncEngine _engine;
    private readonly Settings _settings;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _autoStartItem;
    private readonly ToolStripLabel _statusLabel;
    private readonly ToolStripLabel _countLabel;

    private const string AutoStartRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MedalSync";

    public TrayApplication()
    {
        _settings = Settings.Load();
        _engine = new SyncEngine(_settings);

        // ── Build Context Menu ──────────────────────────────────────────

        _statusLabel = new ToolStripLabel("Wird gestartet...")
        {
            ForeColor = Color.FromArgb(140, 140, 140),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
        };

        _countLabel = new ToolStripLabel("0 Clips gesynct")
        {
            ForeColor = Color.FromArgb(100, 180, 255),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };

        _pauseItem = new ToolStripMenuItem("⏸  Pausieren", null, OnPauseResume);

        _autoStartItem = new ToolStripMenuItem("Autostart mit Windows")
        {
            CheckOnClick = true,
            Checked = _settings.AutoStart
        };
        _autoStartItem.CheckedChanged += OnAutoStartChanged;

        var menu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f),
            ShowImageMargin = false,
            Renderer = new DarkMenuRenderer()
        };

        menu.Items.AddRange(new ToolStripItem[]
        {
            _statusLabel,
            _countLabel,
            new ToolStripSeparator(),
            _pauseItem,
            new ToolStripMenuItem("🔄  Neu synchronisieren", null, OnResync),
            new ToolStripSeparator(),
            new ToolStripMenuItem("📂  Sync-Ordner öffnen", null, OnOpenSyncFolder),
            new ToolStripMenuItem("📂  Clips-Ordner öffnen", null, OnOpenSourceFolder),
            new ToolStripSeparator(),
            new ToolStripMenuItem("⚙  Einstellungen...", null, OnShowSettings),
            _autoStartItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem("❌  Beenden", null, OnExit)
        });

        // ── Tray Icon ───────────────────────────────────────────────────

        _trayIcon = new NotifyIcon
        {
            Icon = CreateAppIcon(),
            ContextMenuStrip = menu,
            Text = "MedalSync – Wird gestartet...",
            Visible = true
        };

        _trayIcon.DoubleClick += OnOpenSyncFolder;

        // ── Wire up engine events ───────────────────────────────────────

        _engine.StatusChanged += status =>
        {
            _statusLabel.Text = status;
            _trayIcon.Text = $"MedalSync – {status}";
        };

        _engine.SyncCountChanged += count =>
        {
            _countLabel.Text = $"{count} Clips gesynct";
        };

        // ── Start engine ────────────────────────────────────────────────

        _engine.Start();
    }

    // ── Event Handlers ──────────────────────────────────────────────────

    private void OnPauseResume(object? sender, EventArgs e)
    {
        if (_engine.IsPaused)
        {
            _engine.Resume();
            _pauseItem.Text = "⏸  Pausieren";
        }
        else
        {
            _engine.Pause();
            _pauseItem.Text = "▶  Fortsetzen";
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

    private void OnAutoStartChanged(object? sender, EventArgs e)
    {
        _settings.AutoStart = _autoStartItem.Checked;
        _settings.Save();
        SetAutoStart(_settings.AutoStart);
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _engine.Stop();
        _engine.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    // ── Auto-Start Management ───────────────────────────────────────────

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

    // ── Icon Generation ─────────────────────────────────────────────────

    /// <summary>
    /// Creates a simple sync-style icon programmatically.
    /// Two curved arrows forming a circle with an "M" in the center.
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

        // Background circle — Medal-ish gold/amber gradient
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
        g.DrawString("M", font,  textBrush,
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

// ── Dark Theme Menu Renderer ────────────────────────────────────────────

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
}
