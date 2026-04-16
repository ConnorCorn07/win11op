using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Win11Optimizer
{
    // ── Theme ─────────────────────────────────────────────────────────────
    public class AppTheme
    {
        public Color BG, SURFACE, CARD, BORDER, ACCENT, ACCENT2, WARN, DANGER, TEXT, TEXTDIM;
        public string Name;

        public static AppTheme Dark => new AppTheme
        {
            Name = "Dark", BG = Color.FromArgb(10, 10, 14), SURFACE = Color.FromArgb(18, 18, 24),
            CARD = Color.FromArgb(24, 24, 32), BORDER = Color.FromArgb(42, 42, 58),
            ACCENT = Color.FromArgb(0, 210, 140), ACCENT2 = Color.FromArgb(0, 160, 255),
            WARN = Color.FromArgb(255, 180, 0), DANGER = Color.FromArgb(255, 70, 70),
            TEXT = Color.FromArgb(230, 230, 240), TEXTDIM = Color.FromArgb(120, 120, 140),
        };
        public static AppTheme Light => new AppTheme
        {
            Name = "Light", BG = Color.FromArgb(242, 243, 247), SURFACE = Color.FromArgb(255, 255, 255),
            CARD = Color.FromArgb(250, 250, 253), BORDER = Color.FromArgb(210, 212, 225),
            ACCENT = Color.FromArgb(0, 170, 110), ACCENT2 = Color.FromArgb(0, 120, 210),
            WARN = Color.FromArgb(200, 130, 0), DANGER = Color.FromArgb(210, 50, 50),
            TEXT = Color.FromArgb(20, 22, 35), TEXTDIM = Color.FromArgb(110, 115, 140),
        };
    }

    // ── Windows version info (populated at startup) ───────────────────────
    public static class WinVersion
    {
        public static int    Build         { get; private set; }
        public static string DisplayName   { get; private set; } = "Unknown";
        public static bool   IsWin11       => Build >= 22000;
        public static bool   IsWin10       => Build >= 10240 && Build < 22000;

        public static void Detect()
        {
            try
            {
                string raw = Microsoft.Win32.Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                    "CurrentBuildNumber", "0")?.ToString() ?? "0";
                Build = int.TryParse(raw, out int b) ? b : 0;

                string dv = Microsoft.Win32.Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                    "DisplayVersion", "")?.ToString() ?? "";

                // ProductName still says "Windows 10" on Win11 — derive from build instead
                string winName = Build >= 22000 ? "Windows 11" : "Windows 10";
                DisplayName = string.IsNullOrWhiteSpace(dv)
                    ? $"{winName} (Build {Build})"
                    : $"{winName} {dv} (Build {Build})";
            }
            catch { Build = 0; DisplayName = "Unknown Windows"; }
        }
    }

    // ── Change log (persisted to disk) ────────────────────────────────────
    public static class ChangeLog
    {
        static readonly string LogFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "changelog.json");

        public class RunEntry
        {
            public string Timestamp   { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            public string WindowsVer  { get; set; } = WinVersion.DisplayName;
            public string Categories  { get; set; } = "";
            public int    Passed      { get; set; }
            public int    Failed      { get; set; }
            public bool   RestorePoint{ get; set; }
            public List<string> Details { get; set; } = new();
        }

        static List<RunEntry> _entries = new();
        public static IReadOnlyList<RunEntry> Entries => _entries.AsReadOnly();

        public static void Load()
        {
            try
            {
                if (!File.Exists(LogFile)) return;
                var loaded = System.Text.Json.JsonSerializer.Deserialize<List<RunEntry>>(
                    File.ReadAllText(LogFile));
                if (loaded != null) _entries = loaded;
            }
            catch { }
        }

        public static void AddEntry(RunEntry entry)
        {
            _entries.Insert(0, entry); // newest first
            Save();
        }

        static void Save()
        {
            try { File.WriteAllText(LogFile, System.Text.Json.JsonSerializer.Serialize(_entries, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })); }
            catch { }
        }

        public static void Clear()
        {
            _entries.Clear();
            try { if (File.Exists(LogFile)) File.Delete(LogFile); } catch { }
        }
    }

    public class MainForm : Form
    {
        AppTheme T = AppTheme.Dark;

        static readonly Font FONT_LABEL = new Font("Segoe UI", 12f, FontStyle.Bold);
        static readonly Font FONT_BODY  = new Font("Segoe UI", 11f, FontStyle.Regular);
        static readonly Font FONT_LOG   = new Font("Consolas", 11f, FontStyle.Regular);

#pragma warning disable CS8618
        CheckBox chkPerf, chkPrivacy, chkResponsive, chkGaming, chkNetwork, chkBloat, chkAdvanced;
        CheckBox chkRestorePoint;
        GlowButton btnRunSelected, btnRunAll;
        DarkRichTextBox logBox;
        Panel progressBar;
        Label lblStatus;
        Panel sideAccent;
        Label _passLabel, _failLabel;
        ThemeToggleButton _themeBtn;
        GlowButton _undoPerf, _undoPrivacy, _undoResponsive, _undoGaming, _undoNetwork, _undoAdvanced;
        Label _winVerLabel;
        TabControl _tabControl;
        Panel _changelogPanel;
#pragma warning restore CS8618

        HashSet<string> _selectedAdvancedTweaks = new();
        List<Action>    _themeApplicators        = new();
        int _totalTweaks, _doneTweaks;

        public MainForm() => InitUI();

        // ── Theme ─────────────────────────────────────────────────────────
        void ApplyTheme()
        {
            BackColor = T.BG; ForeColor = T.TEXT;
            sideAccent.BackColor = T.ACCENT;
            logBox.BackColor = T.BG; logBox.ForeColor = T.ACCENT;
            lblStatus.BackColor = T.BG; lblStatus.ForeColor = T.TEXTDIM;
            progressBar.BackColor = T.ACCENT;
            if (progressBar.Parent != null) progressBar.Parent.BackColor = T.BORDER;
            foreach (var a in _themeApplicators) a();
            foreach (Control c in GetAllControls(this))
            {
                if (c is GlowButton gb)       gb.ApplyTheme(T);
                if (c is ThemeToggleButton tb) tb.IsDark = T.Name == "Dark";
            }
            RefreshChangelogTab();
            Invalidate(true);
        }

        IEnumerable<Control> GetAllControls(Control root)
        {
            foreach (Control c in root.Controls) { yield return c; foreach (var ch in GetAllControls(c)) yield return ch; }
        }

        void InitUI()
        {
            Text = "Win11 Optimizer";
            Size = new Size(900, 780); MinimumSize = new Size(820, 740);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = T.BG; ForeColor = T.TEXT; Font = FONT_BODY;
            FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true;

            sideAccent = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = T.ACCENT };
            Controls.Add(sideAccent);

            // ── Outer layout: header / tab body / log ─────────────────────
            var outer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = T.BG };
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));  // header
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // tab body
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));  // log
            Controls.Add(outer);
            _themeApplicators.Add(() => outer.BackColor = T.BG);

            // ── Header ────────────────────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Fill, BackColor = T.SURFACE };
            _themeApplicators.Add(() => header.BackColor = T.SURFACE);
            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                using var br = new LinearGradientBrush(new Rectangle(0, header.Height - 2, header.Width, 2), T.ACCENT, T.ACCENT2, LinearGradientMode.Horizontal);
                g.FillRectangle(br, 0, header.Height - 2, header.Width, 2);
                float ts = Math.Max(14f, header.Height * 0.28f), ss = Math.Max(8f, header.Height * 0.13f);
                using var tf = new Font("Segoe UI", ts, FontStyle.Bold);
                using var sf = new Font("Segoe UI", ss, FontStyle.Regular);
                using var tb2 = new SolidBrush(T.TEXT); using var db = new SolidBrush(T.TEXTDIM);
                g.DrawString("WIN11  OPTIMIZER", tf, tb2, new PointF(20, header.Height * 0.10f));
                g.DrawString("Performance · Privacy · Gaming · Network", sf, db, new PointF(24, header.Height * 0.58f));
            };

            // Windows version badge
            _winVerLabel = new Label
            {
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                AutoSize = true, BackColor = T.SURFACE,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            header.Controls.Add(_winVerLabel);
            header.SizeChanged += (s, e) => _winVerLabel.Location = new Point(
                header.Width - _winVerLabel.PreferredWidth - 14, _themeBtn.Bottom + 4);
            Load += (s, e) => _winVerLabel.Location = new Point(
                header.Width - _winVerLabel.PreferredWidth - 14, _themeBtn.Bottom + 4);
            _themeApplicators.Add(() => { _winVerLabel.ForeColor = WinVersion.IsWin11 ? T.ACCENT : T.WARN; _winVerLabel.BackColor = T.SURFACE; });
            UpdateWinVerLabel();

            // Theme toggle
            _themeBtn = new ThemeToggleButton { IsDark = true, Size = new Size(80, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _themeBtn.Click += (s, e) => { T = T.Name == "Dark" ? AppTheme.Light : AppTheme.Dark; _themeBtn.IsDark = T.Name == "Dark"; ApplyTheme(); };
            header.SizeChanged += (s, e) => _themeBtn.Location = new Point(header.Width - _themeBtn.Width - 14, (header.Height - _themeBtn.Height) / 2);
            header.Controls.Add(_themeBtn);
            outer.Controls.Add(header, 0, 0);

            // ── Tab control ───────────────────────────────────────────────
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Point(14, 6)
            };
            _themeApplicators.Add(() =>
            {
                _tabControl.BackColor = T.BG;
                foreach (TabPage tp in _tabControl.TabPages) tp.BackColor = T.BG;
            });

            var tweaksPage   = new TabPage("  ⚡ Tweaks  ")   { BackColor = T.BG, BorderStyle = BorderStyle.None };
            var changelogPage = new TabPage("  📋 History  ") { BackColor = T.BG, BorderStyle = BorderStyle.None };
            _tabControl.TabPages.Add(tweaksPage);
            _tabControl.TabPages.Add(changelogPage);
            outer.Controls.Add(_tabControl, 0, 1);

            BuildTweaksTab(tweaksPage);
            BuildChangelogTab(changelogPage);

            // ── Log box ───────────────────────────────────────────────────
            var logCard  = MakeCardDock("OUTPUT LOG");
            outer.Controls.Add(logCard, 0, 2);
            var logInner = new Panel { Dock = DockStyle.Fill, BackColor = T.BG, Padding = new Padding(8, 36, 8, 8) };
            _themeApplicators.Add(() => logInner.BackColor = T.BG);
            logCard.Controls.Add(logInner);
            logBox = new DarkRichTextBox { Dock = DockStyle.Fill, BackColor = T.BG, ForeColor = T.ACCENT, Font = FONT_LOG, BorderStyle = BorderStyle.None, ReadOnly = true, ScrollBars = RichTextBoxScrollBars.Both };

            // ── Startup messages ──────────────────────────────────────────
            Log($"Win11 Optimizer ready  ·  {WinVersion.DisplayName}", T.TEXTDIM);
            if (!WinVersion.IsWin11 && !WinVersion.IsWin10)
                Log("⚠  Unrecognised Windows version — some tweaks may not apply.", T.WARN);
        }

        void UpdateWinVerLabel()
        {
            if (_winVerLabel == null) return;
            string icon = WinVersion.IsWin11 ? "✔" : WinVersion.IsWin10 ? "⚠" : "?";
            _winVerLabel.Text = $"{icon}  {WinVersion.DisplayName}";
            _winVerLabel.ForeColor = WinVersion.IsWin11 ? T.ACCENT : T.WARN;
        }

        // ── Tweaks tab ────────────────────────────────────────────────────
        void BuildTweaksTab(TabPage page)
        {
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = T.BG, Padding = new Padding(8) };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            page.Controls.Add(body);
            _themeApplicators.Add(() => body.BackColor = T.BG);

            // Left column
            var leftCol = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = T.BG, Padding = new Padding(0, 0, 4, 0) };
            leftCol.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftCol.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));  // restore point checkbox
            leftCol.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));  // run buttons
            leftCol.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // progress
            leftCol.RowCount = 4;
            body.Controls.Add(leftCol, 0, 0);
            _themeApplicators.Add(() => leftCol.BackColor = T.BG);

            // Tweaks card — 7 rows
            var selectCard = MakeCardDock("SELECT TWEAKS");
            leftCol.Controls.Add(selectCard, 0, 0);
            var checkContainer = new Panel { Dock = DockStyle.Fill, BackColor = T.CARD, Padding = new Padding(10, 34, 10, 0) };
            _themeApplicators.Add(() => checkContainer.BackColor = T.CARD);
            selectCard.Controls.Add(checkContainer);

            var checkLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, BackColor = T.CARD };
            _themeApplicators.Add(() => checkLayout.BackColor = T.CARD);
            for (int i = 0; i < 7; i++) checkLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 7));
            checkContainer.Controls.Add(checkLayout);

            chkPerf       = MakeCheckRowDock(checkLayout, 0, "⚡  Performance",        "Power plan, NTFS, visual effects, startup",   "Performance",    out _undoPerf);
            chkPrivacy    = MakeCheckRowDock(checkLayout, 1, "🔒  Privacy & Telemetry", "Disable tracking, ad ID, data collection",    "Privacy",         out _undoPrivacy);
            chkResponsive = MakeCheckRowDock(checkLayout, 2, "🖥  Responsiveness",      "Menu speed, shutdown timers, high-res clock",  "Responsiveness",  out _undoResponsive);
            chkGaming     = MakeCheckRowDock(checkLayout, 3, "🎮  Gaming",              "HAGS, Game Mode, priority, DVR off",           "Gaming",          out _undoGaming);
            chkNetwork    = MakeCheckRowDock(checkLayout, 4, "🌐  Network",             "Nagle off, TCP tuning, throttle index",        "Network",         out _undoNetwork);
            chkBloat      = MakeCheckRowDock(checkLayout, 5, "🗑  Remove Bloatware",    "Strips pre-installed junk & ads",              "Bloatware",       out _);
            chkAdvanced   = MakeCheckRowDock(checkLayout, 6, "⚠  Advanced Tweaks",     "CPU scheduling, timer res, TRIM, animations",  "Advanced",        out _undoAdvanced, isAdvanced: true);
            chkAdvanced.Checked = false;
            chkPerf.Checked = chkPrivacy.Checked = chkResponsive.Checked = true;

            chkAdvanced.CheckedChanged += (s, e) =>
            {
                if (!chkAdvanced.Checked) return;
                var dlg = new AdvancedTweakDialog(T);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedAdvancedTweaks = dlg.SelectedTweaks;
                    if (_selectedAdvancedTweaks.Count == 0) { chkAdvanced.Checked = false; Log("Advanced: no tweaks selected.", T.TEXTDIM); }
                    else Log($"Advanced queued: {string.Join(", ", _selectedAdvancedTweaks)}", T.WARN);
                }
                else { chkAdvanced.Checked = false; Log("Advanced tweaks cancelled.", T.TEXTDIM); }
            };

            _undoPerf.Click       += async (s, e) => await RunUndo("Performance",    TweakEngine.UndoPerformanceTweaks,    _undoPerf);
            _undoPrivacy.Click    += async (s, e) => await RunUndo("Privacy",        TweakEngine.UndoPrivacyTweaks,        _undoPrivacy);
            _undoResponsive.Click += async (s, e) => await RunUndo("Responsiveness", TweakEngine.UndoResponsivenessTweaks, _undoResponsive);
            _undoGaming.Click     += async (s, e) => await RunUndo("Gaming",         TweakEngine.UndoGamingTweaks,         _undoGaming);
            _undoNetwork.Click    += async (s, e) => await RunUndo("Network",        TweakEngine.UndoNetworkTweaks,        _undoNetwork);
            _undoAdvanced.Click   += async (s, e) => await RunUndo("Advanced",       TweakEngine.UndoAdvancedTweaks,       _undoAdvanced);

            TweakEngine.LoadBackups();
            RefreshUndoButtons();

            // ── Restore Point checkbox row ────────────────────────────────
            var rpRow = new Panel { Dock = DockStyle.Fill, BackColor = T.BG, Padding = new Padding(2, 4, 2, 0) };
            _themeApplicators.Add(() => rpRow.BackColor = T.BG);
            chkRestorePoint = new CheckBox
            {
                Text      = "🛡  Create Restore Point before running",
                Font      = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = T.ACCENT,
                AutoSize  = true, Checked = true,
                Location  = new Point(4, 8),
                BackColor = T.BG,
                FlatStyle = FlatStyle.Flat
            };
            chkRestorePoint.FlatAppearance.BorderColor        = T.ACCENT;
            chkRestorePoint.FlatAppearance.CheckedBackColor   = T.ACCENT;
            chkRestorePoint.FlatAppearance.MouseOverBackColor = T.BG;
            _themeApplicators.Add(() =>
            {
                chkRestorePoint.ForeColor = T.ACCENT;
                chkRestorePoint.FlatAppearance.BorderColor      = T.ACCENT;
                chkRestorePoint.FlatAppearance.CheckedBackColor = T.ACCENT;
            });
            rpRow.Controls.Add(chkRestorePoint);
            leftCol.Controls.Add(rpRow, 0, 1);

            // ── Run buttons ───────────────────────────────────────────────
            var btnPanel = new Panel { Dock = DockStyle.Fill, BackColor = T.BG, Padding = new Padding(0, 6, 0, 0) };
            _themeApplicators.Add(() => btnPanel.BackColor = T.BG);
            leftCol.Controls.Add(btnPanel, 0, 2);

            btnRunSelected = new GlowButton("RUN SELECTED", T.ACCENT,  new Rectangle(0, 0, 0, 42), T);
            btnRunAll      = new GlowButton("RUN ALL",      T.ACCENT2, new Rectangle(0, 0, 0, 42), T);
            btnRunSelected.Dock = DockStyle.Left; btnRunAll.Dock = DockStyle.Left;
            btnRunSelected.Width = 160; btnRunAll.Width = 130;
            btnRunSelected.Margin = new Padding(0, 0, 8, 0);
            btnRunSelected.Click += async (s, e) => await RunTweaks(selectedOnly: true);
            btnRunAll.Click      += async (s, e) => await RunTweaks(selectedOnly: false);
            btnPanel.Controls.Add(btnRunAll);
            btnPanel.Controls.Add(btnRunSelected);

            // ── Progress row ──────────────────────────────────────────────
            var progPanel = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = T.BG, ColumnCount = 1, RowCount = 2, Padding = new Padding(0, 4, 0, 0) };
            progPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
            progPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftCol.Controls.Add(progPanel, 0, 3);
            _themeApplicators.Add(() => progPanel.BackColor = T.BG);

            var progBg = new Panel { Dock = DockStyle.Fill, BackColor = T.BORDER };
            _themeApplicators.Add(() => progBg.BackColor = T.BORDER);
            progressBar = new Panel { Bounds = new Rectangle(0, 0, 0, 8), BackColor = T.ACCENT };
            progBg.Controls.Add(progressBar);
            progPanel.Controls.Add(progBg, 0, 0);

            lblStatus = new Label { Text = "Ready.", Font = FONT_BODY, ForeColor = T.TEXTDIM, AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, BackColor = T.BG };
            progPanel.Controls.Add(lblStatus, 0, 1);

            // ── Right column ──────────────────────────────────────────────
            var rightCol = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = T.BG, Padding = new Padding(4, 0, 0, 0) };
            rightCol.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            rightCol.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            rightCol.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            body.Controls.Add(rightCol, 1, 0);
            _themeApplicators.Add(() => rightCol.BackColor = T.BG);

            // System info
            var sysCard  = MakeCardDock("SYSTEM INFO"); rightCol.Controls.Add(sysCard, 0, 0);
            var sysInner = new Panel { Dock = DockStyle.Fill, BackColor = T.CARD, Padding = new Padding(10, 34, 10, 6) };
            _themeApplicators.Add(() => sysInner.BackColor = T.CARD);
            sysCard.Controls.Add(sysInner); BuildSystemInfoPanel(sysInner);

            // Notes
            var infoCard  = MakeCardDock("NOTES"); rightCol.Controls.Add(infoCard, 0, 1);
            var infoInner = new Panel { Dock = DockStyle.Fill, BackColor = T.CARD, Padding = new Padding(10, 34, 10, 10) };
            _themeApplicators.Add(() => infoInner.BackColor = T.CARD);
            infoCard.Controls.Add(infoInner);
            AddInfoLineDock(infoInner, T.ACCENT,  "• Run as Administrator for registry & service access.");
            AddInfoLineDock(infoInner, T.WARN,    "• A reboot is required for HAGS & timer tweaks.");
            AddInfoLineDock(infoInner, T.TEXTDIM, "• Bloatware removal strips provisioned packages.");
            AddGithubLink(infoInner);

            // Summary
            var sumCard  = MakeCardDock("LAST RUN SUMMARY"); rightCol.Controls.Add(sumCard, 0, 2);
            var sumInner = new Panel { Dock = DockStyle.Fill, BackColor = T.CARD, Padding = new Padding(10, 34, 10, 10) };
            _themeApplicators.Add(() => sumInner.BackColor = T.CARD);
            sumCard.Controls.Add(sumInner);
            var sumBoxes = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = T.CARD, ColumnCount = 2, RowCount = 1 };
            _themeApplicators.Add(() => sumBoxes.BackColor = T.CARD);
            sumBoxes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            sumBoxes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            sumBoxes.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            sumInner.Controls.Add(sumBoxes);
            _passLabel = AddSummaryBoxDock(sumBoxes, 0, T.ACCENT, "0", "Succeeded", "✔");
            _failLabel = AddSummaryBoxDock(sumBoxes, 1, T.DANGER, "0", "Failed",    "✘");
        }

        // ── Changelog tab ─────────────────────────────────────────────────
        void BuildChangelogTab(TabPage page)
        {
            _changelogPanel = new Panel { Dock = DockStyle.Fill, BackColor = T.BG, Padding = new Padding(10) };
            _themeApplicators.Add(() => _changelogPanel.BackColor = T.BG);
            page.Controls.Add(_changelogPanel);
            RefreshChangelogTab();
        }

        void RefreshChangelogTab()
        {
            if (_changelogPanel == null) return;
            _changelogPanel.Controls.Clear();

            // Header row
            var topRow = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = T.BG };
            _changelogPanel.Controls.Add(topRow);

            var titleLbl = new Label { Text = "RUN HISTORY", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = T.TEXTDIM, AutoSize = true, Location = new Point(4, 12), BackColor = T.BG };
            topRow.Controls.Add(titleLbl);

            var btnClear = new GlowButton("CLEAR HISTORY", T.DANGER, new Rectangle(0, 0, 140, 30), T)
            {
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            topRow.SizeChanged += (s, e) => btnClear.Location = new Point(topRow.Width - 150, 7);
            btnClear.Click += (s, e) =>
            {
                if (MessageBox.Show("Clear all run history?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                { ChangeLog.Clear(); RefreshChangelogTab(); }
            };
            topRow.Controls.Add(btnClear);

            // Scrollable entry list
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = T.BG };
            _changelogPanel.Controls.Add(scroll);

            if (ChangeLog.Entries.Count == 0)
            {
                scroll.Controls.Add(new Label
                {
                    Text = "No runs recorded yet. Run some tweaks to start tracking history.",
                    Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                    ForeColor = T.TEXTDIM, AutoSize = true,
                    Location = new Point(10, 20), BackColor = T.BG
                });
                return;
            }

            int y = 4;
            foreach (var entry in ChangeLog.Entries)
            {
                var card = new Panel
                {
                    Left = 0, Top = y, Width = scroll.ClientSize.Width - 4,
                    BackColor = T.CARD, Padding = new Padding(12, 8, 12, 10),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                card.Paint += (s, e) =>
                {
                    using var pen = new Pen(T.BORDER); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                    using var stripe = new SolidBrush(T.ACCENT2); e.Graphics.FillRectangle(stripe, 0, 0, 3, card.Height);
                };

                // Timestamp + OS
                var tsLbl = new Label { Text = entry.Timestamp, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = T.ACCENT2, AutoSize = true, Location = new Point(14, 8), BackColor = T.BG };
                var osLbl = new Label { Text = entry.WindowsVer, Font = new Font("Segoe UI", 8.5f, FontStyle.Regular), ForeColor = T.TEXTDIM, AutoSize = true, Location = new Point(14, 26), BackColor = T.BG };

                // Categories run
                var catLbl = new Label { Text = $"Categories: {entry.Categories}", Font = new Font("Segoe UI", 9f, FontStyle.Regular), ForeColor = T.TEXT, AutoSize = true, Location = new Point(14, 46), BackColor = T.BG };

                // Pass/fail counters
                Color passCol = entry.Failed == 0 ? T.ACCENT : T.WARN;
                var statLbl  = new Label
                {
                    Text = $"✔ {entry.Passed} succeeded   ✘ {entry.Failed} failed" +
                           (entry.RestorePoint ? "   🛡 Restore Point created" : ""),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = passCol, AutoSize = true,
                    Location = new Point(14, 64), BackColor = T.BG
                };

                // Details (collapsible — show on hover would be complex, so show inline truncated)
                string detailText = entry.Details.Count > 0
                    ? string.Join("  ·  ", entry.Details.Take(6)) + (entry.Details.Count > 6 ? $"  … +{entry.Details.Count - 6} more" : "")
                    : "";
                int cardH = 84;
                if (!string.IsNullOrEmpty(detailText))
                {
                    var detLbl = new Label { Text = detailText, Font = new Font("Consolas", 8f, FontStyle.Regular), ForeColor = T.TEXTDIM, AutoSize = false, Width = card.Width - 28, Location = new Point(14, 82), BackColor = T.BG };
                    int dh = MeasureTextHeight(detailText, detLbl.Font, detLbl.Width) + 4;
                    detLbl.Height = dh;
                    card.Controls.Add(detLbl);
                    cardH = 82 + dh + 10;
                }

                card.Height = cardH;
                card.Controls.Add(tsLbl); card.Controls.Add(osLbl);
                card.Controls.Add(catLbl); card.Controls.Add(statLbl);
                scroll.Controls.Add(card);
                y += card.Height + 8;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────
        Panel MakeCardDock(string title)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = T.CARD, Margin = new Padding(4) };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(T.BORDER); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                using var br = new LinearGradientBrush(new Rectangle(0, 0, card.Width, 2), T.ACCENT, T.ACCENT2, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(br, 0, 0, card.Width, 2);
            };
            var lbl = new Label { Text = title, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = T.TEXTDIM, AutoSize = true, Location = new Point(12, 10), BackColor = T.CARD };
            _themeApplicators.Add(() => { card.BackColor = T.CARD; lbl.ForeColor = T.TEXTDIM; lbl.BackColor = T.CARD; card.Invalidate(); });
            card.Controls.Add(lbl);
            return card;
        }

        CheckBox MakeCheckRowDock(TableLayoutPanel parent, int row, string title, string subtitle,
                                   string category, out GlowButton undoBtn, bool isAdvanced = false)
        {
            var cell = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = T.CARD, ColumnCount = 2, RowCount = 1 };
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            _themeApplicators.Add(() => cell.BackColor = T.CARD);

            if (isAdvanced)
            {
                // Make the cell itself clip properly and draw both bg + border
                cell.Paint += (s, e) =>
                {
                    using var bg = new SolidBrush(Color.FromArgb(35, T.WARN.R, T.WARN.G, T.WARN.B));
                    e.Graphics.FillRectangle(bg, 0, 0, cell.Width, cell.Height);
                };

                // Draw border AFTER children paint using the parent's Paint event
                // but offset using the cell's actual bounds within the parent
                cell.LocationChanged += (s, e) => parent.Invalidate();
                cell.SizeChanged     += (s, e) => parent.Invalidate();
                parent.Paint += (s, e) =>
                {
                    // Find the cell's bounds relative to parent
                    var bounds = cell.Bounds;
                    if (bounds.IsEmpty) return;
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var pen = new Pen(T.WARN, 2f);
                    e.Graphics.DrawRectangle(pen,
                        bounds.X + 1,
                        bounds.Y + 1,
                        bounds.Width  - 2,
                        bounds.Height - 2);
                };
            }

            var left = new Panel { Dock = DockStyle.Fill, BackColor = T.CARD };
            var chk  = new CheckBox { Text = title, Font = isAdvanced ? new Font("Segoe UI", 12f, FontStyle.Bold) : FONT_LABEL, ForeColor = isAdvanced ? T.WARN : T.TEXT, AutoSize = true, Location = new Point(6, 4), BackColor = T.CARD, FlatStyle = FlatStyle.Flat };
            chk.FlatAppearance.BorderColor = isAdvanced ? T.WARN : T.BORDER;
            chk.FlatAppearance.CheckedBackColor = isAdvanced ? T.WARN : T.ACCENT;
            chk.FlatAppearance.MouseOverBackColor = T.CARD;
            _themeApplicators.Add(() => { chk.BackColor = T.CARD; chk.FlatAppearance.MouseOverBackColor = T.CARD; chk.ForeColor = isAdvanced ? T.WARN : T.TEXT; chk.FlatAppearance.BorderColor = isAdvanced ? T.WARN : T.BORDER; chk.FlatAppearance.CheckedBackColor = isAdvanced ? T.WARN : T.ACCENT; });

            var desc = new Label { Text = subtitle, Font = FONT_BODY, ForeColor = isAdvanced ? Color.FromArgb(180, T.WARN.R, T.WARN.G, T.WARN.B) : T.TEXTDIM, AutoSize = true, Location = new Point(26, 24), BackColor = T.CARD };
            _themeApplicators.Add(() => { desc.ForeColor = isAdvanced ? Color.FromArgb(180, T.WARN.R, T.WARN.G, T.WARN.B) : T.TEXTDIM; desc.BackColor = T.CARD; });
            left.Controls.Add(chk); left.Controls.Add(desc);

            var btnWrapper = new Panel { Dock = DockStyle.Fill, BackColor = T.CARD, Padding = new Padding(4, 0, 4, 0) };
            _themeApplicators.Add(() => btnWrapper.BackColor = T.CARD);
            undoBtn = new GlowButton("↩ UNDO", T.DANGER, new Rectangle(0, 0, 72, 30), T) { Visible = TweakEngine.HasBackup(category) };
            var btn = undoBtn;
            btnWrapper.Resize += (s, e) => { btn.Width = btnWrapper.Width - 8; btn.Height = 30; btn.Location = new Point(4, (btnWrapper.Height - btn.Height) / 2); };
            btnWrapper.Controls.Add(btn);
            cell.Controls.Add(left, 0, 0); cell.Controls.Add(btnWrapper, 1, 0);
            parent.Controls.Add(cell, 0, row);
            return chk;
        }

        void BuildSystemInfoPanel(Panel parent)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = T.CARD };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            _themeApplicators.Add(() => layout.BackColor = T.CARD);
            var keys = new[] { "OS", "CPU", "RAM", "GPU" };
            var vals = new Dictionary<string, Label>();
            foreach (var key in keys)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); layout.RowCount++;
                var kl = new Label { Text = key, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = T.TEXTDIM, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, BackColor = T.CARD, Padding = new Padding(4, 0, 0, 0) };
                _themeApplicators.Add(() => { kl.ForeColor = T.TEXTDIM; kl.BackColor = T.CARD; });
                var vl = new Label { Text = "Loading…", Font = new Font("Segoe UI", 9f, FontStyle.Regular), ForeColor = T.TEXT, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, BackColor = T.CARD, AutoEllipsis = true };
                _themeApplicators.Add(() => { vl.ForeColor = T.TEXT; vl.BackColor = T.CARD; });
                layout.Controls.Add(kl); layout.Controls.Add(vl); vals[key] = vl;
            }
            parent.Controls.Add(layout);
            Task.Run(() =>
            {
                try
                {
                    string cpu = ReadRegistry(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "Unknown CPU").Trim();
                    string ram = RunAndCapture("powershell", "-NoProfile -Command \"[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1GB)\"");
                    ram = string.IsNullOrWhiteSpace(ram) ? "Unknown" : $"{ram.Trim()} GB";
                    string gpu = RunAndCapture("powershell", "-NoProfile -Command \"(Get-CimInstance Win32_VideoController | Select-Object -First 1).Name\"").Trim();
                    if (string.IsNullOrWhiteSpace(gpu)) gpu = "Unknown";
                    Invoke(new Action(() => { vals["OS"].Text = WinVersion.DisplayName; vals["CPU"].Text = cpu; vals["RAM"].Text = ram; vals["GPU"].Text = gpu; }));
                }
                catch { Invoke(new Action(() => { foreach (var l in vals.Values) l.Text = "Unavailable"; })); }
            });
        }

        static string ReadRegistry(string kp, string vn, string fb)
        {
            try { var v = Microsoft.Win32.Registry.GetValue(kp, vn, null); return v?.ToString() ?? fb; } catch { return fb; }
        }

        static string RunAndCapture(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args) { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true };
                using var p = Process.Start(psi); string o = p.StandardOutput.ReadToEnd().Trim(); p.WaitForExit(); return o;
            }
            catch { return ""; }
        }

        void AddInfoLineDock(Panel parent, Color col, string text)
        {
            var flow = parent.Controls.Count == 0
                ? new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, BackColor = T.CARD, WrapContents = false }
                : (FlowLayoutPanel)parent.Controls[0];
            if (parent.Controls.Count == 0) parent.Controls.Add(flow);
            _themeApplicators.Add(() => flow.BackColor = T.CARD);
            var lbl = new Label { Text = text, Font = new Font("Segoe UI", 11f, FontStyle.Regular), ForeColor = col, AutoSize = true, BackColor = T.CARD, Margin = new Padding(0, 4, 0, 4) };
            _themeApplicators.Add(() => lbl.BackColor = T.CARD);
            flow.Controls.Add(lbl);
        }

        void AddGithubLink(Panel parent)
        {
            var flow = parent.Controls.Count == 0
                ? new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, BackColor = T.CARD, WrapContents = false }
                : (FlowLayoutPanel)parent.Controls[0];
            if (parent.Controls.Count == 0) parent.Controls.Add(flow);
            var link = new LinkLabel { Text = "⭐  GitHub: github.com/ConnorCorn07/win11op", Font = new Font("Segoe UI", 11f, FontStyle.Regular), AutoSize = true, BackColor = T.CARD, Margin = new Padding(0, 8, 0, 4) };
            link.LinkColor = T.ACCENT2; link.ActiveLinkColor = T.ACCENT; link.VisitedLinkColor = T.ACCENT2; link.LinkBehavior = LinkBehavior.HoverUnderline;
            link.Click += (s, e) => Process.Start(new ProcessStartInfo { FileName = "https://github.com/ConnorCorn07/win11op", UseShellExecute = true });
            _themeApplicators.Add(() => { link.BackColor = T.CARD; link.LinkColor = T.ACCENT2; link.ActiveLinkColor = T.ACCENT; link.VisitedLinkColor = T.ACCENT2; });
            flow.Controls.Add(link);
        }

        Label AddSummaryBoxDock(TableLayoutPanel parent, int col, Color ac, string count, string title, string icon)
        {
            var box = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, ac.R, ac.G, ac.B), Margin = new Padding(4) };
            box.Paint += (s, e) =>
            {
                using var sb = new SolidBrush(ac); e.Graphics.FillRectangle(sb, 0, 0, 5, box.Height);
                using var pen = new Pen(Color.FromArgb(80, ac.R, ac.G, ac.B), 1.5f); e.Graphics.DrawRectangle(pen, 1, 1, box.Width - 3, box.Height - 3);
            };
            var sub = new Label { Text = title.ToUpper(), Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(200, ac.R, ac.G, ac.B), TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Bottom, Height = 30, BackColor = Color.FromArgb(30, ac.R, ac.G, ac.B), Padding = new Padding(14, 0, 0, 0) };
            var mid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.FromArgb(30, ac.R, ac.G, ac.B) };
            mid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60)); mid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            var num = new Label { Text = count, Font = new Font("Segoe UI", 42f, FontStyle.Bold), ForeColor = ac, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, ac.R, ac.G, ac.B), Padding = new Padding(14, 0, 0, 0) };
            var ico = new Label { Text = icon, Font = new Font("Segoe UI", 32f, FontStyle.Regular), ForeColor = Color.FromArgb(70, ac.R, ac.G, ac.B), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, ac.R, ac.G, ac.B) };
            mid.Controls.Add(num, 0, 0); mid.Controls.Add(ico, 1, 0);
            box.Controls.Add(sub); box.Controls.Add(mid);
            parent.Controls.Add(box, col, 0);
            return num;
        }

        static int MeasureTextHeight(string text, Font font, int width)
        {
            using var g = Graphics.FromHwnd(IntPtr.Zero);
            return (int)Math.Ceiling(g.MeasureString(text, font, width).Height) + 4;
        }

        // ── Undo / run ────────────────────────────────────────────────────
        void RefreshUndoButtons()
        {
            if (InvokeRequired) { Invoke(new Action(RefreshUndoButtons)); return; }
            _undoPerf.Visible       = TweakEngine.HasBackup("Performance");
            _undoPrivacy.Visible    = TweakEngine.HasBackup("Privacy");
            _undoResponsive.Visible = TweakEngine.HasBackup("Responsiveness");
            _undoGaming.Visible     = TweakEngine.HasBackup("Gaming");
            _undoNetwork.Visible    = TweakEngine.HasBackup("Network");
            _undoAdvanced.Visible   = TweakEngine.HasBackup("Advanced");
        }

        async Task RunUndo(string category, Func<List<TweakEngine.TweakResult>> undoAction, GlowButton btn)
        {
            btn.Enabled = false;
            Log($"↩ Undoing {category} tweaks…", T.WARN);
            var results = await Task.Run(undoAction);
            int ok = results.Count(r => r.Success), bad = results.Count(r => !r.Success);
            Log($"┌─ UNDO {category.ToUpper()}  ({ok} restored, {bad} failed)", T.TEXTDIM);
            foreach (var r in results) { if (r.Success) Log($"│  ✔  {r.Name}", T.ACCENT); else Log($"│  ✘  {r.Name}: {r.Error}", T.DANGER); }
            Log($"└─────────────────────────────────────", T.BORDER);
            Log($"↩ {category} undone. Reboot recommended.", T.ACCENT);
            SetStatus($"{category} tweaks undone.", T.ACCENT);
            RefreshUndoButtons(); btn.Enabled = true;
        }

        void Log(string msg, Color? col = null)
        {
            if (InvokeRequired) { Invoke(new Action(() => Log(msg, col))); return; }
            logBox.SelectionColor = col ?? T.ACCENT;
            logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            logBox.ScrollToCaret();
        }

        void SetProgress(int done, int total)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetProgress(done, total))); return; }
            int w = total == 0 ? 0 : (int)((double)progressBar.Parent.Width * done / total);
            progressBar.Width = w;
            if (total > 0 && done < total) lblStatus.Text = $"Running… {done}/{total}";
            else if (total == 0)           lblStatus.Text = "Ready.";
        }

        void SetStatus(string msg, Color? col = null)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetStatus(msg, col))); return; }
            lblStatus.Text = msg; lblStatus.ForeColor = col ?? T.TEXTDIM;
        }

        async Task RunTweaks(bool selectedOnly)
        {
            btnRunSelected.Enabled = btnRunAll.Enabled = false;
            TweakEngine.ClearResults(); logBox.Clear();
            progressBar.Width = 0; progressBar.BackColor = T.WARN;
            _passLabel.Text = "0"; _failLabel.Text = "0";

            bool doPerf     = !selectedOnly || chkPerf.Checked;
            bool doPrivacy  = !selectedOnly || chkPrivacy.Checked;
            bool doRespond  = !selectedOnly || chkResponsive.Checked;
            bool doGaming   = !selectedOnly || chkGaming.Checked;
            bool doNetwork  = !selectedOnly || chkNetwork.Checked;
            bool doBloat    = !selectedOnly || chkBloat.Checked;
            bool doAdvanced = (!selectedOnly || chkAdvanced.Checked) && _selectedAdvancedTweaks.Count > 0;

            // ── Restore Point ─────────────────────────────────────────────
            bool rpCreated = false;
            if (chkRestorePoint.Checked)
            {
                SetStatus("Creating System Restore Point…", T.WARN);
                Log("🛡  Creating System Restore Point…", T.WARN);
                bool ok = await Task.Run(() => TweakEngine.CreateRestorePoint("Win11Optimizer — before tweaks"));
                if (ok) { Log("🛡  Restore Point created successfully.", T.ACCENT); rpCreated = true; }
                else    Log("⚠  Restore Point creation failed or was skipped (may need System Protection enabled).", T.WARN);
            }

            // ── Windows version warnings ───────────────────────────────────
            if (!WinVersion.IsWin11 && doGaming)
                Log($"⚠  HAGS may behave differently on Windows 10 (Build {WinVersion.Build}).", T.WARN);
            if (WinVersion.Build > 0 && WinVersion.Build < 19041 && doNetwork)
                Log($"⚠  TCP auto-tuning flags differ on Build {WinVersion.Build}. Network tweaks may partially fail.", T.WARN);

            _totalTweaks = (doPerf ? 9 : 0) + (doPrivacy ? 25 : 0) + (doRespond ? 8 : 0) +
                           (doGaming ? 10 : 0) + (doNetwork ? 8 : 0) + (doBloat ? 40 : 0) +
                           (doAdvanced ? _selectedAdvancedTweaks.Count : 0);
            _doneTweaks = 0;
            SetProgress(0, _totalTweaks);

            // Collect category names for the log entry
            var cats = new List<string>();
            if (doPerf) cats.Add("Performance"); if (doPrivacy) cats.Add("Privacy");
            if (doRespond) cats.Add("Responsiveness"); if (doGaming) cats.Add("Gaming");
            if (doNetwork) cats.Add("Network"); if (doBloat) cats.Add("Bloatware");
            if (doAdvanced) cats.Add("Advanced");

            int prevCount = 0;
            var logDetails = new List<string>();

            void LogSection(string name)
            {
                var all = TweakEngine.GetResults(); var sec = all.Skip(prevCount).ToList(); prevCount = all.Count;
                int ok = sec.Count(r => r.Success), bad = sec.Count(r => !r.Success);
                Log($"┌─ {name}  ({ok} ok, {bad} failed)", T.TEXTDIM);
                foreach (var r in sec)
                {
                    if (r.Success) { Log($"│  ✔  {r.Name}", T.ACCENT);  logDetails.Add($"✔ {r.Name}"); }
                    else           { Log($"│  ✘  {r.Name}: {r.Error}", T.DANGER); logDetails.Add($"✘ {r.Name}"); }
                }
                Log($"└─────────────────────────────────────", T.BORDER);
            }

            await Task.Run(() =>
            {
                void Tick(string msg) { _doneTweaks++; SetProgress(_doneTweaks, _totalTweaks); Log(msg, T.TEXTDIM); }
                if (doPerf)     { Tick("→ Applying Performance tweaks…");         TweakEngine.ApplyPerformanceTweaks();                       Invoke(new Action(() => LogSection("PERFORMANCE"))); }
                if (doPrivacy)  { Tick("→ Applying Privacy & Telemetry tweaks…"); TweakEngine.ApplyPrivacyTweaks();                           Invoke(new Action(() => LogSection("PRIVACY & TELEMETRY"))); }
                if (doRespond)  { Tick("→ Applying Responsiveness tweaks…");      TweakEngine.ApplySystemResponsiveness();                    Invoke(new Action(() => LogSection("RESPONSIVENESS"))); }
                if (doGaming)   { Tick("→ Applying Gaming tweaks…");              TweakEngine.ApplyGamingTweaks();                            Invoke(new Action(() => LogSection("GAMING"))); }
                if (doNetwork)  { Tick("→ Applying Network tweaks…");             TweakEngine.ApplyNetworkTweaks();                           Invoke(new Action(() => LogSection("NETWORK"))); }
                if (doBloat)    { TweakEngine.RemoveBloatware(msg => Tick(msg));                                                              Invoke(new Action(() => LogSection("BLOATWARE REMOVAL"))); }
                if (doAdvanced) { Tick("→ Applying Advanced tweaks…");            TweakEngine.ApplyAdvancedTweaks(_selectedAdvancedTweaks);   Invoke(new Action(() => LogSection("ADVANCED"))); }
            });

            var results = TweakEngine.GetResults();
            int pass = results.Count(r => r.Success), fail = results.Count(r => !r.Success);
            _passLabel.Text = pass.ToString(); _failLabel.Text = fail.ToString();
            Log($"══ COMPLETE: {pass} succeeded, {fail} failed. Reboot recommended. ══", T.ACCENT);
            SetStatus($"Complete — {pass} ok, {fail} failed.", T.ACCENT);
            SetProgress(_totalTweaks, _totalTweaks); progressBar.BackColor = T.ACCENT;

            // ── Write changelog entry ─────────────────────────────────────
            ChangeLog.AddEntry(new ChangeLog.RunEntry
            {
                Categories   = string.Join(", ", cats),
                Passed       = pass, Failed = fail,
                RestorePoint = rpCreated,
                Details      = logDetails
            });
            RefreshChangelogTab();

            btnRunSelected.Enabled = btnRunAll.Enabled = true;
            RefreshUndoButtons();
            PromptReboot();
        }

        void PromptReboot()
        {
            var r = MessageBox.Show("Some tweaks require a reboot to take full effect.\n\nWould you like to reboot now?",
                "Reboot Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (r == DialogResult.Yes)
            {
                Log("Initiating reboot…", T.WARN);
                Process.Start(new ProcessStartInfo { FileName = "shutdown.exe", Arguments = "/r /t 10 /c \"Win11 Optimizer: Rebooting to apply tweaks.\"", UseShellExecute = false, CreateNoWindow = true });
                MessageBox.Show("Your PC will reboot in 10 seconds.\n\nTo cancel, open a command prompt and run:\n  shutdown /a", "Rebooting in 10 seconds", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    // ── Advanced tweak dialog ─────────────────────────────────────────────
    public class AdvancedTweakDialog : Form
    {
        public HashSet<string> SelectedTweaks { get; private set; } = new();

        static readonly (string Key, string Title, string Desc, string Risk)[] TWEAKS =
        {
            ("ProcessorScheduling", "Processor Scheduling → Programs",
             "Sets Win32PrioritySeparation to 38 (0x26), giving the foreground app a much larger CPU time slice. Best for gaming and interactive workloads.",
             "Low — standard Windows tuning. Revert by setting Win32PrioritySeparation back to 2."),
            ("DisableDynamicTick", "Disable Dynamic Tick (IRQ8 / High-Res Timer)",
             "Runs 'bcdedit /set disabledynamictick yes'. Forces a constant high-resolution timer, reducing micro-stutter in games and real-time audio.",
             "Low — boot config only. Undo: 'bcdedit /deletevalue disabledynamictick'. Reboot required."),
            ("DisableCpuThrottling", "Disable CPU Throttling for Background Processes",
             "Sets the THROTTLING_POLICY power setting to 0, preventing Windows from aggressively pulling CPU clocks from background tasks during gaming.",
             "Medium — increases CPU power draw and heat slightly. Not recommended on battery."),
            ("EnableTrim", "Ensure SSD TRIM is Enabled",
             "Runs 'fsutil behavior set disabledeletenotify 0' to guarantee TRIM notifications are sent to your SSD, keeping write speeds consistent over time.",
             "Very Low — this is the Windows default. Safe on any SSD."),
            ("AggressiveAnimations", "Aggressive Animation Disabling",
             "Disables UserPreferencesMask, TaskbarAnimations, MinAnimate, ListviewShadow. Goes beyond VisualFXSetting=2 to eliminate every remaining UI animation.",
             "Low — purely cosmetic. Undo restores all original values."),
        };

        readonly AppTheme _t;
        readonly Dictionary<string, CheckBox> _checks = new();

        public AdvancedTweakDialog(AppTheme theme) { _t = theme; BuildUI(); }

        void BuildUI()
        {
            Text = "Advanced Tweaks — Review & Confirm";
            Size = new Size(640, 640); MinimumSize = new Size(580, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = _t.BG; ForeColor = _t.TEXT;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;

            var banner = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(38, _t.WARN.R, _t.WARN.G, _t.WARN.B) };
            banner.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(70, _t.WARN.R, _t.WARN.G, _t.WARN.B)); e.Graphics.DrawLine(pen, 0, banner.Height - 1, banner.Width, banner.Height - 1);
                using var bar = new SolidBrush(_t.WARN); e.Graphics.FillRectangle(bar, 0, 0, 4, banner.Height);
                using var f = new Font("Segoe UI", 10f, FontStyle.Bold); using var b = new SolidBrush(_t.WARN);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawString("⚠  These tweaks are more aggressive. Read each description carefully before proceeding.", f, b, new PointF(14, (banner.Height - 18) / 2f));
            };
            Controls.Add(banner);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = _t.SURFACE };
            footer.Paint += (s, e) => { using var pen = new Pen(_t.BORDER); e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0); };
            var btnP = new GlowButton("PROCEED", _t.WARN,   new Rectangle(0, 0, 130, 36), _t);
            var btnC = new GlowButton("CANCEL",  _t.DANGER, new Rectangle(0, 0, 100, 36), _t);
            btnP.Anchor = btnC.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            footer.SizeChanged += (s, e) => { btnP.Location = new Point(footer.Width - 248, (footer.Height - 36) / 2); btnC.Location = new Point(footer.Width - 108, (footer.Height - 36) / 2); };
            btnP.Click += (s, e) => { SelectedTweaks = new HashSet<string>(_checks.Where(kv => kv.Value.Checked).Select(kv => kv.Key)); DialogResult = DialogResult.OK; Close(); };
            btnC.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            footer.Controls.Add(btnP); footer.Controls.Add(btnC);
            Controls.Add(footer);

            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = _t.BG, Padding = new Padding(12, 8, 12, 8) };
            Controls.Add(scroll);

            int y = 8;
            foreach (var (key, title, desc, risk) in TWEAKS)
            {
                var card = new Panel { Left = 0, Top = y, Width = scroll.ClientSize.Width - 24, BackColor = _t.CARD, Padding = new Padding(14, 10, 14, 12), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
                card.Paint += (s, e) => { using var pen = new Pen(_t.BORDER); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); using var stripe = new SolidBrush(_t.WARN); e.Graphics.FillRectangle(stripe, 0, 0, 3, card.Height); };

                var chk = new CheckBox { Text = title, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = _t.WARN, Checked = true, AutoSize = false, Width = card.Width - 30, Height = 28, Location = new Point(14, 10), BackColor = _t.CARD, FlatStyle = FlatStyle.Flat };
                chk.FlatAppearance.BorderColor = _t.WARN; chk.FlatAppearance.CheckedBackColor = _t.WARN; chk.FlatAppearance.MouseOverBackColor = _t.CARD;

                int dw = card.Width - 42;
                var dL = new Label { Text = desc, Font = new Font("Segoe UI", 9f, FontStyle.Regular), ForeColor = _t.TEXT, AutoSize = false, Width = dw, Location = new Point(28, 44), BackColor = _t.CARD };
                dL.Height = MeasureH(desc, dL.Font, dw);
                var rL = new Label { Text = $"⚠ Risk: {risk}", Font = new Font("Segoe UI", 8.5f, FontStyle.Italic), ForeColor = Color.FromArgb(160, _t.WARN.R, _t.WARN.G, _t.WARN.B), AutoSize = false, Width = dw, Location = new Point(28, dL.Bottom + 6), BackColor = _t.CARD };
                rL.Height = MeasureH(risk, rL.Font, dw) + 4;

                card.Controls.Add(chk); card.Controls.Add(dL); card.Controls.Add(rL);
                card.Height = rL.Bottom + 12;
                scroll.Controls.Add(card); _checks[key] = chk; y += card.Height + 10;
            }
        }

        static int MeasureH(string text, Font font, int width)
        {
            using var g = Graphics.FromHwnd(IntPtr.Zero);
            return (int)Math.Ceiling(g.MeasureString(text, font, width).Height) + 4;
        }
    }

    // ── Custom controls ───────────────────────────────────────────────────
    public class GlowButton : Control
    {
        Color _accent; bool _hover, _pressed; AppTheme _theme;
        public GlowButton(string text, Color accent, Rectangle bounds, AppTheme theme = null)
        {
            Text = text; _accent = accent; _theme = theme ?? AppTheme.Dark; Bounds = bounds;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold); ForeColor = Color.FromArgb(10, 10, 14); BackColor = _theme.BG; Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }
        public void ApplyTheme(AppTheme t) { _theme = t; BackColor = t.BG; Invalidate(); }
        protected override void OnMouseEnter(EventArgs e)     { _hover   = true;  Invalidate(); }
        protected override void OnMouseLeave(EventArgs e)     { _hover   = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true;  Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e)   { _pressed = false; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            var rc = new Rectangle(0, 0, Width, Height);
            Color fill = _pressed ? Color.FromArgb(180, _accent.R, _accent.G, _accent.B) : _hover ? _accent : Color.FromArgb(220, _accent.R, _accent.G, _accent.B);
            using var br = new SolidBrush(fill); g.FillRectangle(br, rc);
            if (_hover && !_pressed) { using var pen = new Pen(Color.FromArgb(160, _accent.R, _accent.G, _accent.B), 1.5f); g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3); }
            Color tc = _theme.Name == "Light" ? Color.FromArgb(20, 20, 30) : Color.FromArgb(10, 10, 14);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var tb = new SolidBrush(tc); g.DrawString(Text, Font, tb, rc, sf);
        }
    }

    public class ThemeToggleButton : Control
    {
        bool _isDark = true, _hover;
        public bool IsDark { get => _isDark; set { _isDark = value; Invalidate(); } }
        public ThemeToggleButton() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true); Cursor = Cursors.Hand; }
        protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Color pb = _isDark ? Color.FromArgb(_hover ? 60 : 40, 255, 255, 255) : Color.FromArgb(_hover ? 60 : 40, 0, 0, 0);
            Color pd = _isDark ? Color.FromArgb(80, 255, 255, 255) : Color.FromArgb(80, 0, 0, 0);
            using var bgb = new SolidBrush(pb); using var pp = new Pen(pd, 1.5f);
            g.FillRoundedRectangle(bgb, 0, 0, Width - 1, Height - 1, 8); g.DrawRoundedRectangle(pp, 0, 0, Width - 1, Height - 1, 8);
            string icon = _isDark ? "🌙" : "☀", label = _isDark ? " Dark" : " Light";
            Color tc = _isDark ? Color.FromArgb(200, 200, 220) : Color.FromArgb(40, 40, 60);
            using var iF = new Font("Segoe UI Emoji", 13f); using var lF = new Font("Segoe UI", 9f, FontStyle.Bold); using var tb = new SolidBrush(tc);
            int mid = Width / 2;
            g.DrawString(icon,  iF, tb, new RectangleF(0, 0, mid, Height),   new StringFormat { Alignment = StringAlignment.Far,  LineAlignment = StringAlignment.Center });
            g.DrawString(label, lF, tb, new RectangleF(mid, 0, mid, Height), new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
        }
    }

    static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush b, int x, int y, int w, int h, int r)
        { using var p = RP(x, y, w, h, r); g.FillPath(b, p); }
        public static void DrawRoundedRectangle(this Graphics g, Pen p, int x, int y, int w, int h, int r)
        { using var path = RP(x, y, w, h, r); g.DrawPath(p, path); }
        static GraphicsPath RP(int x, int y, int w, int h, int r)
        {
            var p = new GraphicsPath();
            p.AddArc(x, y, r * 2, r * 2, 180, 90); p.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            p.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90); p.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            p.CloseFigure(); return p;
        }
    }

    public class DarkRichTextBox : RichTextBox
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); SetWindowTheme(Handle, "DarkMode_Explorer", null); }
    }

    // ── Entry point ───────────────────────────────────────────────────────
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += (s, e) => LogCrash(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => LogCrash(e.ExceptionObject as Exception);

                // ── Detect Windows version ─────────────────────────────────
                WinVersion.Detect();

                // ── Load changelog ─────────────────────────────────────────
                ChangeLog.Load();

                // ── Admin check ────────────────────────────────────────────
                if (!IsRunningAsAdmin())
                {
                    var choice = MessageBox.Show(
                        "Win11 Optimizer needs Administrator privileges to apply registry and service tweaks.\n\n" +
                        "Would you like to relaunch as Administrator now?",
                        "Administrator Required",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button1);

                    if (choice == DialogResult.Yes)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName        = Application.ExecutablePath,
                                UseShellExecute = true,
                                Verb            = "runas"   // triggers UAC prompt
                            });
                        }
                        catch { /* user declined UAC — fall through to warning mode */ }
                        return; // exit this non-elevated instance
                    }
                    // User chose No — show a persistent warning banner inside the app
                    // by setting a flag the form can read
                    AdminWarning.Show = true;
                }

                Application.Run(new MainForm());
            }
            catch (Exception ex) { LogCrash(ex); }
        }

        static bool IsRunningAsAdmin()
        {
            try
            {
                using var id = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        static void LogCrash(Exception ex)
        {
            try
            {
                string lp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.AppendAllText(lp, $"[{DateTime.Now}]\n{ex}\n\n");
                MessageBox.Show($"Crash logged to:\n{lp}\n\n{ex?.Message}", "Win11Optimizer — Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }

    // ── Admin warning flag (read by MainForm if launched without elevation) ─
    public static class AdminWarning
    {
        public static bool Show { get; set; } = false;
    }
}