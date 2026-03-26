namespace PowerPlanController;

/// <summary>
/// Manages the system tray icon lifecycle.
/// </summary>
public sealed class TrayApp : IDisposable
{
    NotifyIcon?    _trayIcon;
    SettingsForm?  _form;
    bool           _trayEnabled;

    public TrayApp()
    {
        // Read initial tray preference from config
        _trayEnabled = ReadTrayConfig();

        _form = new SettingsForm
        {
            OnTrayToggle = enabled =>
            {
                _trayEnabled = enabled;
                if (enabled) StartTray();
                else         StopTray();
            },
            OnQuit = Quit,
        };

        if (_trayEnabled) StartTray();

        // Show window on first launch
        _form.ShowForm();
    }

    // ── Tray ─────────────────────────────────────────────────────
    void StartTray()
    {
        if (_trayIcon != null) return;

        _trayIcon = new NotifyIcon
        {
            Text    = I18n.AppName,
            Icon    = LoadIcon(),
            Visible = true,
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add($"⚡ {I18n.Settings}", null, (_, _) => _form?.ShowForm());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add($"✖ {I18n.Exit}", null, (_, _) => Quit());

        _trayIcon.ContextMenuStrip  = menu;
        _trayIcon.MouseClick += (_, e) => 
        {
            if (e.Button == MouseButtons.Left)
                _form?.ToggleForm();
        };
    }

    void StopTray()
    {
        if (_trayIcon == null) return;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayIcon = null;
    }

    void Quit()
    {
        StopTray();
        Application.ExitThread();
    }

    // ── Helpers ──────────────────────────────────────────────────
    static bool ReadTrayConfig()
    {
        try
        {
            var dir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PowerPlanController");
            var path = Path.Combine(dir, "config.json");
            if (!File.Exists(path)) return true;
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.GetProperty("tray").GetBoolean();
        }
        catch { return true; }
    }

    static Icon LoadIcon() =>
        SettingsForm.LoadEmbeddedIcon() ?? SystemIcons.Application;

    public void Dispose() => StopTray();
}
