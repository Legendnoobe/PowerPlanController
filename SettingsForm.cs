using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace PowerPlanController;

public partial class SettingsForm : Form
{
    // ── P/Invoke for borderless window drag ───────────────────────
    [DllImport("user32.dll")] static extern int  SendMessage(IntPtr h, int msg, int wp, int lp);
    [DllImport("user32.dll")] static extern bool ReleaseCapture();

    // ── Theme (Windows 11 Fluent Dark Palette) ────────────────────
    static readonly Color BG       = Color.FromArgb(32,  32,  32);
    static readonly Color RowBG    = Color.FromArgb(43,  43,  43);
    static readonly Color RowHov   = Color.FromArgb(53,  53,  53);
    static readonly Color TitleBG  = Color.FromArgb(24,  24,  24);
    static readonly Color Accent   = Color.FromArgb(96,  205, 255); // Win11 Dark Theme Blue
    static readonly Color FG       = Color.FromArgb(255, 255, 255);
    static readonly Color FGDim    = Color.FromArgb(180, 180, 180);
    static readonly Color DropBG   = Color.FromArgb(50,  50,  50);
    static readonly Color DropBord = Color.FromArgb(75,  75,  75);
    static readonly Color BtnBG    = Color.FromArgb(55,  55,  55);
    static readonly Color BtnHov   = Color.FromArgb(70,  70,  70);
    static readonly Color SepColor = Color.FromArgb(55,  55,  55);

    static readonly Font FontTitle  = new("Segoe UI Variable Display", 9.5f, FontStyle.Bold);
    static readonly Font FontSec    = new("Segoe UI Variable Text", 8f, FontStyle.Bold);
    static readonly Font FontRow    = new("Segoe UI Variable Text", 9.5f);
    static readonly Font FontDrop   = new("Segoe UI Variable Text", 8.5f);
    static readonly Font FontBtn    = new("Segoe UI Variable Text", 9f);
    static readonly Font FontTbBtn  = new("Segoe UI", 11f);  // title-bar buttons

    // Fallback fonts if Segoe UI Variable is not available (e.g., Win10)
    static Font GetFont(Font preferred, Font fallback) => 
        new FontFamily(preferred.Name).Name == preferred.Name ? preferred : fallback;

    // ── Timeout options ───────────────────────────────────────────
    static readonly int[] Mins = [0, 1, 2, 3, 5, 10, 15, 20, 25, 30, 45, 60, 90, 120];
    (string Label, int Minutes)[] _options = [];

    // ── Controls ──────────────────────────────────────────────────
    ComboBox _batScreen = null!, _batSleep = null!,
             _plugScreen = null!, _plugSleep = null!;
    CheckBox _trayChk   = null!;
    Label    _statusLbl = null!;

    System.Windows.Forms.Timer _fadeTimer = new();

    // ── Config in %AppData% ───────────────────────────────────────
    static string ConfigDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PowerPlanController");
    static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    bool _trayEnabled = true;
    bool _startupEnabled = false;
    public Action<bool>? OnTrayToggle;
    public Action?       OnQuit;

    // ── Registry for Startup ──────────────────────────────────────
    void SetStartup(bool enable)
    {
        try
        {
            using var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (rk == null) return;
            if (enable) rk.SetValue("PowerPlanController", $"\"{Application.ExecutablePath}\"");
            else        rk.DeleteValue("PowerPlanController", false);
        }
        catch { }
    }
    
    bool CheckStartup()
    {
        try
        {
            using var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return rk?.GetValue("PowerPlanController") != null;
        }
        catch { return false; }
    }

    // ── Constructor ───────────────────────────────────────────────
    public SettingsForm()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint  |
                 ControlStyles.UserPaint, true);
        UpdateStyles();
        _options = Mins.Select(m => (I18n.LoadMin(m), m)).ToArray();
        InitializeComponent();
        LoadConfig();
        BuildUI();

        _fadeTimer.Interval = 10;
        _fadeTimer.Tick += FadeTimer_Tick;
    }

    protected override void OnLoad(EventArgs e)   { base.OnLoad(e); LoadCurrentSettings(); }

    // Rounded corners via Region
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (Width <= 0 || Height <= 0) return;
        int r = 14; // Slightly more rounded for modern feel
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(0,         0,          r, r, 180, 90);
        path.AddArc(Width - r, 0,          r, r, 270, 90);
        path.AddArc(Width - r, Height - r, r, r, 0,   90);
        path.AddArc(0,         Height - r, r, r, 90,  90);
        path.CloseAllFigures();
        Region = new Region(path);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return cp;
        }
    }

    // ── Config ────────────────────────────────────────────────────
    void LoadConfig()
    {
        try
        {
            _startupEnabled = CheckStartup();
            Directory.CreateDirectory(ConfigDir);
            if (!File.Exists(ConfigPath)) return;
            var doc = JsonDocument.Parse(File.ReadAllText(ConfigPath));
            _trayEnabled = doc.RootElement.GetProperty("tray").GetBoolean();
        }
        catch { _trayEnabled = true; }
    }

    void SaveConfig() =>
        File.WriteAllText(ConfigPath, $"{{\"tray\":{(_trayEnabled ? "true" : "false")}}}");

    // ── Embedded icon ─────────────────────────────────────────────
    public static Icon? LoadEmbeddedIcon()
    {
        var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("icon.ico");
        return s != null ? new Icon(s) : null;
    }

    // ── Double-buffered Panel ─────────────────────────────────────
    class DBPanel : Panel
    {
        public DBPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint  |
                     ControlStyles.UserPaint, true);
            DoubleBuffered = true;
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var b = new SolidBrush(BackColor);
            e.Graphics.FillRectangle(b, e.ClipRectangle);
        }
    }

    // ── UI ────────────────────────────────────────────────────────
    void BuildUI()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition   = FormStartPosition.Manual;
        BackColor       = BG;
        ClientSize      = new Size(360, 345);
        TopMost         = true;
        Icon            = LoadEmbeddedIcon();
        Opacity         = 0;

        SuspendLayout();

        // Use fallback fonts if Segoe UI Variable isn't present
        var fTitle = GetFont(FontTitle, new Font("Segoe UI", 9.5f, FontStyle.Bold));
        var fSec   = GetFont(FontSec,   new Font("Segoe UI", 8f, FontStyle.Bold));
        var fRow   = GetFont(FontRow,   new Font("Segoe UI", 9.5f));
        var fDrop  = GetFont(FontDrop,  new Font("Segoe UI", 8.5f));
        var fBtn   = GetFont(FontBtn,   new Font("Segoe UI", 9f));

        // ── Title bar ─────────────────────────────────────────────
        var titleBar = new DBPanel
        {
            BackColor = TitleBG, Dock = DockStyle.Top, Height = 40,
        };

        var ico = LoadEmbeddedIcon();
        if (ico != null)
        {
            var icoBox = new PictureBox
            {
                Image    = ico.ToBitmap(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size     = new Size(18, 18),
                Location = new Point(12, 11),
                BackColor = Color.Transparent,
            };
            titleBar.Controls.Add(icoBox);
        }

        var titleLbl = new Label
        {
            Text      = I18n.AppName,
            UseMnemonic = false,
            Font      = fTitle,
            ForeColor = FG,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(38, 10),
        };
        titleBar.Controls.Add(titleLbl);

        // Status label will be added to the bottom instead of titleBar

        // Close button
        var closeBtn = MakeTitleBtn("✕", Color.FromArgb(196, 43, 28));
        closeBtn.Location = new Point(ClientSize.Width - 46, 0);
        closeBtn.Size     = new Size(46, 40);
        closeBtn.Click   += (_, _) => { if (_trayEnabled) HideFade(); else OnQuit?.Invoke(); };
        titleBar.Controls.Add(closeBtn);

        // Minimize button
        var minBtn = MakeTitleBtn("─", Color.FromArgb(60, 60, 60));
        minBtn.Location = new Point(ClientSize.Width - 92, 0);
        minBtn.Size     = new Size(46, 40);
        minBtn.Click   += (_, _) => {
            if (_trayEnabled) HideFade(); 
            else WindowState = FormWindowState.Minimized;
        };
        titleBar.Controls.Add(minBtn);

        // Drag to move
        void StartDrag(object? s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); }
        }
        titleBar.MouseDown += StartDrag;
        titleLbl.MouseDown += StartDrag;

        Controls.Add(titleBar);

        int y = 50;

        // ── Battery section ───────────────────────────────────────
        Controls.Add(SectionLabel(I18n.OnBattery, y, fSec)); y += 24;
        _batScreen = AddRow("🖥️", I18n.ScreenOff, y, fRow, fDrop); y += 42;
        _batSleep  = AddRow("💤", I18n.Sleep,     y, fRow, fDrop); y += 42;
        y += 6;

        Controls.Add(HSep(y)); y += 2;
        y += 10;

        // ── Plugged section ───────────────────────────────────────
        Controls.Add(SectionLabel(I18n.PluggedIn, y, fSec)); y += 24;
        _plugScreen = AddRow("🖥️", I18n.ScreenOff, y, fRow, fDrop); y += 42;
        _plugSleep  = AddRow("💤", I18n.Sleep,     y, fRow, fDrop); y += 42;
        y += 6;

        Controls.Add(HSep(y)); y += 2;
        y += 12;

        // ── Bottom row: checkbox left, button right ───────────────
        _trayChk = new CheckBox
        {
            Text      = I18n.TrayToggle,
            Font      = fDrop,
            ForeColor = FG,
            BackColor = BG,
            Checked   = _trayEnabled,
            AutoSize  = true,
            Location  = new Point(14, y + 6),
            Cursor    = Cursors.Default,
            FlatStyle = FlatStyle.Flat,
        };
        _trayChk.FlatAppearance.BorderColor        = DropBord;
        _trayChk.FlatAppearance.CheckedBackColor   = Accent;
        _trayChk.FlatAppearance.MouseOverBackColor = Color.Transparent;
        _trayChk.FlatAppearance.MouseDownBackColor = Color.Transparent;
        _trayChk.CheckedChanged += (_, _) =>
        {
            _trayEnabled = _trayChk.Checked;
            SaveConfig();
            OnTrayToggle?.Invoke(_trayEnabled);
        };
        Controls.Add(_trayChk);
        
        var startupChk = new CheckBox
        {
            Text      = I18n.Startup,
            Font      = fDrop,
            ForeColor = FG,
            BackColor = BG,
            Checked   = _startupEnabled,
            AutoSize  = true,
            Location  = new Point(14, y + 26),
            Cursor    = Cursors.Default,
            FlatStyle = FlatStyle.Flat,
        };
        startupChk.FlatAppearance.BorderColor        = DropBord;
        startupChk.FlatAppearance.CheckedBackColor   = Accent;
        startupChk.FlatAppearance.MouseOverBackColor = Color.Transparent;
        startupChk.FlatAppearance.MouseDownBackColor = Color.Transparent;
        startupChk.CheckedChanged += (_, _) =>
        {
            _startupEnabled = startupChk.Checked;
            SetStartup(_startupEnabled);
        };
        Controls.Add(startupChk);

        // Status label below the buttons to prevent any overlap
        _statusLbl = new Label
        {
            Text      = "",
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = FGDim,
            BackColor = Color.Transparent,
            AutoSize  = false,
            Size      = new Size(ClientSize.Width - 28, 20),
            Location  = new Point(14, y + 54),
            TextAlign = ContentAlignment.MiddleRight,
        };
        Controls.Add(_statusLbl);

        var applyBtn = new Button
        {
            Text      = I18n.Apply,
            Font      = fBtn,
            FlatStyle = FlatStyle.Flat,
            BackColor = BtnBG,
            ForeColor = FG,
            Size      = new Size(110, 32),
            Location  = new Point(ClientSize.Width - 124, y),
            Cursor    = Cursors.Default,
        };
        applyBtn.FlatAppearance.BorderSize        = 1;
        applyBtn.FlatAppearance.BorderColor        = DropBord;
        applyBtn.FlatAppearance.MouseOverBackColor = BtnHov;
        applyBtn.FlatAppearance.MouseDownBackColor = BtnBG;
        
        applyBtn.Paint += (s, e) => {
            // Give button slightly rounded corners via Paint
            var btn = (Button)s!;
            int radius = 6;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(btn.Width - radius - 1, 0, radius, radius, 270, 90);
            path.AddArc(btn.Width - radius - 1, btn.Height - radius - 1, radius, radius, 0, 90);
            path.AddArc(0, btn.Height - radius - 1, radius, radius, 90, 90);
            path.CloseAllFigures();
            btn.Region = new Region(path);
        };

        applyBtn.Click += (_, _) => ApplySettings();
        Controls.Add(applyBtn);

        // Resize form to fit everything including the new status label row
        ClientSize = new Size(360, y + 80);

        ResumeLayout(false);

        FormClosing += (_, e) => 
        { 
            if (Opacity > 0)
            {
                e.Cancel = true; 
                if (_trayEnabled) HideFade(); else OnQuit?.Invoke(); 
            }
        };
    }

    // ── Animations ────────────────────────────────────────────────
    bool _isFadingIn = false;
    void FadeTimer_Tick(object? sender, EventArgs e)
    {
        if (_isFadingIn)
        {
            if (Opacity < 1) Opacity += 0.15;
            else { Opacity = 1; _fadeTimer.Stop(); }
        }
        else
        {
            if (Opacity > 0) Opacity -= 0.15;
            else { Opacity = 0; _fadeTimer.Stop(); Hide(); }
        }
    }

    public void ShowForm()
    {
        if (Visible && Opacity == 1) return;
        LoadCurrentSettings();
        
        // Position at bottom right (above system tray)
        var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location = new Point(wa.Right - Width - 12, wa.Bottom - Height - 12);
        
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        
        _isFadingIn = true;
        _fadeTimer.Start();
    }
    
    public void ToggleForm()
    {
        if (Visible && Opacity > 0)
            HideFade();
        else
            ShowForm();
    }

    void HideFade()
    {
        _isFadingIn = false;
        _fadeTimer.Start();
    }

    // ── Helper: title-bar button ──────────────────────────────────
    Button MakeTitleBtn(string text, Color hoverBg)
    {
        var btn = new Button
        {
            Text      = text,
            Font      = FontTbBtn,
            FlatStyle = FlatStyle.Flat,
            BackColor = TitleBG,
            ForeColor = FGDim,
            Cursor    = Cursors.Default,
            TabStop   = false,
        };
        btn.FlatAppearance.BorderSize        = 0;
        btn.FlatAppearance.MouseOverBackColor = hoverBg;
        btn.FlatAppearance.MouseDownBackColor = hoverBg;
        btn.MouseEnter += (_, _) => btn.ForeColor = Color.White;
        btn.MouseLeave += (_, _) => btn.ForeColor = FGDim;
        return btn;
    }

    // ── Helper: section label  ────────────────────────────────────
    Label SectionLabel(string text, int y, Font f) => new()
    {
        Text      = text,
        Font      = f,
        ForeColor = Accent,
        BackColor = BG,
        AutoSize  = true,
        Location  = new Point(14, y),
    };

    // ── Helper: setting row ───────────────────────────────────────
    ComboBox AddRow(string icon, string label, int y, Font fRow, Font fDrop)
    {
        const int ROW_H = 40;
        const int PAD   = 12;

        // Row background panel
        var row = new DBPanel
        {
            BackColor = RowBG,
            Location  = new Point(PAD, y),
            Size      = new Size(ClientSize.Width - PAD * 2, ROW_H),
        };

        // Give row rounded corners
        row.Paint += (s, e) => {
            var r = (Panel)s!;
            int rad = 8;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, rad, rad, 180, 90);
            path.AddArc(r.Width - rad, 0, rad, rad, 270, 90);
            path.AddArc(r.Width - rad, r.Height - rad, rad, rad, 0, 90);
            path.AddArc(0, r.Height - rad, rad, rad, 90, 90);
            path.CloseAllFigures();
            r.Region = new Region(path);
        };

        // Icon — fixed width so it never overlaps the text
        var icoLbl = new Label
        {
            Text      = icon,
            Font      = new Font("Segoe UI Emoji", 10.5f),
            ForeColor = FGDim,
            BackColor = Color.Transparent,
            AutoSize  = false,
            Size      = new Size(28, 22),
            Location  = new Point(8, 10),
            TextAlign = ContentAlignment.MiddleCenter,
        };
        row.Controls.Add(icoLbl);

        // Label — starts after the fixed-width icon area
        var nameLbl = new Label
        {
            Text      = label,
            Font      = fRow,
            ForeColor = FG,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(42, 10),
        };
        row.Controls.Add(nameLbl);

        // Container for ComboBox to hide its native border
        var cbBg = new DBPanel
        {
            BackColor = DropBord,
            Size      = new Size(114, 24),
            Location  = new Point(row.Width - 114 - 10, (ROW_H - 24) / 2),
            Anchor    = AnchorStyles.Right | AnchorStyles.Top,
        };
        // Give the border container slight rounding
        cbBg.Paint += (s, e) => {
            var p = (Panel)s!;
            int rad = 4;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, rad, rad, 180, 90);
            path.AddArc(p.Width - rad, 0, rad, rad, 270, 90);
            path.AddArc(p.Width - rad, p.Height - rad, rad, rad, 0, 90);
            path.AddArc(0, p.Height - rad, rad, rad, 90, 90);
            path.CloseAllFigures();
            p.Region = new Region(path);
        };

        // Combobox (owner-draw) inside the container
        var cb = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle     = FlatStyle.Flat,
            BackColor     = DropBG,
            ForeColor     = FG,
            Font          = fDrop,
            DrawMode      = DrawMode.OwnerDrawFixed,
            ItemHeight    = 18,
            Width         = 116,
            DropDownWidth = 112,
            Location      = new Point(-1, -1), // Shift up/left to hide white border
        };
        cb.DrawItem += (s, e) => ComboDrawItem(s, e, fDrop);
        cb.Items.AddRange(_options.Select(o => (object)o.Label).ToArray());
        
        cbBg.Controls.Add(cb);
        row.Controls.Add(cbBg);

        // Hover effect for responsiveness
        row.MouseEnter += (_, _) => row.BackColor = RowHov;
        row.MouseLeave += (_, _) => { if (!row.ClientRectangle.Contains(row.PointToClient(Cursor.Position))) row.BackColor = RowBG; };
        
        foreach (Control c in row.Controls)
        {
            c.MouseEnter += (_, _) => row.BackColor = RowHov;
            // Only revert if mouse has actually left the row bounds
            c.MouseLeave += (_, _) => { if (!row.ClientRectangle.Contains(row.PointToClient(Cursor.Position))) row.BackColor = RowBG; };
        }

        Controls.Add(row);
        return cb;
    }

    void ComboDrawItem(object? sender, DrawItemEventArgs e, Font fDrop)
    {
        if (e.Index < 0) return;
        
        var cb = (ComboBox)sender!;
        
        // ComboBoxEdit = the closed combo display area
        bool isEdit   = (e.State & DrawItemState.ComboBoxEdit) != 0;
        bool selected = (e.State & DrawItemState.Selected) != 0;
        
        Color bgColor = DropBG;
        if (selected && !isEdit) bgColor = Accent;
        else if (isEdit) bgColor = cb.BackColor;

        using var bg = new SolidBrush(bgColor);
        e.Graphics.FillRectangle(bg, e.Bounds);
        
        // Darken text if Accent background is used to keep readability
        Color fgColor = (selected && !isEdit) ? Color.Black : FG;
        using var fg = new SolidBrush(fgColor);
        
        e.Graphics.DrawString(_options[e.Index].Label, fDrop, fg,
                              e.Bounds.X + 4, e.Bounds.Y + 1);
    }

    Panel HSep(int y) => new()
    {
        Location  = new Point(14, y),
        Size      = new Size(332, 1),
        BackColor = SepColor,
    };

    // ── Logic ─────────────────────────────────────────────────────
    void LoadCurrentSettings()
    {
        try
        {
            var s = PowerManager.GetCurrent();
            SetCombo(_batScreen,  s.BatteryScreen);
            SetCombo(_batSleep,   s.BatterySleep);
            SetCombo(_plugScreen, s.PlugScreen);
            SetCombo(_plugSleep,  s.PlugSleep);
            ShowStatus(I18n.Loaded, Accent);
        }
        catch
        {
            foreach (var cb in new[] { _batScreen, _batSleep, _plugScreen, _plugSleep })
                cb.SelectedIndex = 0;
        }
    }

    void SetCombo(ComboBox cb, int minutes)
    {
        int idx = Array.FindIndex(_options, o => o.Minutes == minutes);
        cb.SelectedIndex = idx >= 0 ? idx : 0;
    }

    int GetMinutes(ComboBox cb) =>
        cb.SelectedIndex >= 0 ? _options[cb.SelectedIndex].Minutes : 0;

    void ApplySettings()
    {
        int bs  = GetMinutes(_batScreen);
        int bsl = GetMinutes(_batSleep);
        int ps  = GetMinutes(_plugScreen);
        int psl = GetMinutes(_plugSleep);

        foreach (var (sv, slv, name) in new[] { (bs, bsl, I18n.ModeBattery), (ps, psl, I18n.ModePlugged) })
        {
            if (sv > 0 && slv > 0 && slv < sv)
            {
                MessageBox.Show(I18n.WarnSize(name), I18n.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        PowerManager.Apply(bs, bsl, ps, psl);
        ShowStatus(I18n.Applied, Color.FromArgb(0, 220, 100));
    }

    void ShowStatus(string text, Color color)
    {
        if (IsDisposed) return;
        _statusLbl.ForeColor = color;
        _statusLbl.Text      = text;
        Task.Delay(4000).ContinueWith(_ =>
            Invoke(() => { if (!IsDisposed) _statusLbl.Text = ""; }));
    }

    private void InitializeComponent() { }
}
