using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Win11Optimizer
{
    public class MainForm : Form
    {
        // ── Palette ──────────────────────────────────────────────────────
        static readonly Color BG        = Color.FromArgb(10, 10, 14);
        static readonly Color SURFACE   = Color.FromArgb(18, 18, 24);
        static readonly Color CARD      = Color.FromArgb(24, 24, 32);
        static readonly Color BORDER    = Color.FromArgb(42, 42, 58);
        static readonly Color ACCENT    = Color.FromArgb(0, 210, 140);
        static readonly Color ACCENT2   = Color.FromArgb(0, 160, 255);
        static readonly Color WARN      = Color.FromArgb(255, 180, 0);
        static readonly Color DANGER    = Color.FromArgb(255, 70, 70);
        static readonly Color TEXT      = Color.FromArgb(230, 230, 240);
        static readonly Color TEXTDIM   = Color.FromArgb(120, 120, 140);

        // ── Fonts ─────────────────────────────────────────────────────────
        static readonly Font FONT_HEAD  = new Font("Segoe UI", 22f, FontStyle.Bold);
        static readonly Font FONT_SUB   = new Font("Segoe UI", 9f,  FontStyle.Regular);
        static readonly Font FONT_LABEL = new Font("Segoe UI", 10f, FontStyle.Bold);
        static readonly Font FONT_BODY  = new Font("Segoe UI", 9f,  FontStyle.Regular);
        static readonly Font FONT_LOG   = new Font("Consolas", 8.5f, FontStyle.Regular);
        static readonly Font FONT_BTN   = new Font("Segoe UI", 9.5f, FontStyle.Bold);

        // ── Controls ─────────────────────────────────────────────────────
#pragma warning disable CS8618
        CheckBox chkPerf, chkPrivacy, chkResponsive, chkGaming, chkNetwork, chkBloat;
        GlowButton btnRunSelected, btnRunAll;
        RichTextBox logBox;
        Panel progressBar;
        Label lblStatus;
        Panel sideAccent;
        Label _passLabel, _failLabel;
#pragma warning restore CS8618
        int _totalTweaks, _doneTweaks;

        public MainForm()
        {
            InitUI();
        }

        void InitUI()
        {
            Text            = "Win11 Optimizer";
            Size            = new Size(820, 680);
            MinimumSize     = new Size(820, 680);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = BG;
            ForeColor       = TEXT;
            Font            = FONT_BODY;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;

            // ── Left accent bar ───────────────────────────────────────────
            sideAccent = new Panel
            {
                Bounds    = new Rectangle(0, 0, 4, Height),
                BackColor = ACCENT,
                Anchor    = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom
            };
            Controls.Add(sideAccent);

            // ── Header ────────────────────────────────────────────────────
            var header = new Panel
            {
                Bounds    = new Rectangle(4, 0, Width - 4, 80),
                BackColor = SURFACE,
                Anchor    = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                // gradient stripe at bottom
                using var br = new LinearGradientBrush(
                    new Rectangle(0, header.Height - 2, header.Width, 2),
                    ACCENT, ACCENT2, LinearGradientMode.Horizontal);
                g.FillRectangle(br, 0, header.Height - 2, header.Width, 2);
            };
            Controls.Add(header);

            var lblTitle = new Label
            {
                Text      = "WIN11  OPTIMIZER",
                Font      = FONT_HEAD,
                ForeColor = TEXT,
                AutoSize  = true,
                Location  = new Point(20, 14),
                BackColor = Color.FromArgb(18, 18, 24)  // SURFACE
            };
            header.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text      = "Performance · Privacy · Gaming · Network",
                Font      = FONT_SUB,
                ForeColor = TEXTDIM,
                AutoSize  = true,
                Location  = new Point(23, 50),
                BackColor = Color.FromArgb(18, 18, 24)  // SURFACE
            };
            header.Controls.Add(lblSub);

            // ── Tweak selection panel ─────────────────────────────────────
            var selectPanel = MakeCard(new Rectangle(12, 90, 380, 310), "SELECT TWEAKS");

            (chkPerf,      _) = MakeCheckRow(selectPanel,  54, "⚡  Performance",       "Power plan, NTFS, visual effects, startup");
            (chkPrivacy,   _) = MakeCheckRow(selectPanel,  98, "🔒  Privacy & Telemetry","Disable tracking, ad ID, data collection");
            (chkResponsive,_) = MakeCheckRow(selectPanel, 142, "🖥  Responsiveness",     "Menu speed, shutdown timers, high-res clock");
            (chkGaming,    _) = MakeCheckRow(selectPanel, 186, "🎮  Gaming",             "HAGS, Game Mode, priority, DVR off");
            (chkNetwork,   _) = MakeCheckRow(selectPanel, 230, "🌐  Network",            "Nagle off, TCP tuning, throttle index");
            (chkBloat,     _) = MakeCheckRow(selectPanel, 274, "🗑  Remove Bloatware",   "Strips pre-installed junk & ads");

            chkPerf.Checked = chkPrivacy.Checked = chkResponsive.Checked = true;

            // ── Info / warning card ───────────────────────────────────────
            var infoCard = MakeCard(new Rectangle(402, 90, 400, 130), "NOTES");
            AddInfoLine(infoCard, 48,  ACCENT,  "• Run as Administrator for registry & service access.");
            AddInfoLine(infoCard, 70,  WARN,    "• A reboot is required for HAGS & timer tweaks.");
            AddInfoLine(infoCard, 92,  TEXTDIM, "• Bloatware removal also strips provisioned packages.");
            AddInfoLine(infoCard, 114, TEXTDIM, "• Camera/mic policy applies to all Windows apps.");

            // ── Result summary boxes ──────────────────────────────────────
            var sumCard = MakeCard(new Rectangle(402, 232, 400, 170), "LAST RUN SUMMARY");
            // placeholders; populated after run
            _passLabel = AddSummaryBox(sumCard, 54,  ACCENT, "0", "Succeeded");
            _failLabel = AddSummaryBox(sumCard, 54 + 130, DANGER, "0", "Failed");

            // ── Buttons ───────────────────────────────────────────────────
            btnRunSelected = new GlowButton("RUN SELECTED", ACCENT,  new Rectangle(12,  412, 186, 46));
            btnRunAll      = new GlowButton("RUN ALL",      ACCENT2, new Rectangle(206, 412, 186, 46));
            btnRunSelected.Click += async (s, e) => await RunTweaks(selectedOnly: true);
            btnRunAll.Click      += async (s, e) => await RunTweaks(selectedOnly: false);
            Controls.Add(btnRunSelected);
            Controls.Add(btnRunAll);

            // ── Progress bar ──────────────────────────────────────────────
            var progBg = new Panel
            {
                Bounds    = new Rectangle(12, 468, 380, 6),
                BackColor = BORDER
            };
            progressBar = new Panel
            {
                Bounds    = new Rectangle(0, 0, 0, 6),
                BackColor = ACCENT
            };
            progBg.Controls.Add(progressBar);
            Controls.Add(progBg);

            lblStatus = new Label
            {
                Text      = "Ready.",
                Font      = FONT_BODY,
                ForeColor = TEXTDIM,
                AutoSize  = true,
                Location  = new Point(12, 482)
            };
            Controls.Add(lblStatus);

            // ── Log box ───────────────────────────────────────────────────
            var logCard = MakeCard(new Rectangle(12, 504, 790, 148), "OUTPUT LOG");
            logBox = new RichTextBox
            {
                Bounds          = new Rectangle(10, 38, 770, 98),
                BackColor       = BG,
                ForeColor       = ACCENT,
                Font            = FONT_LOG,
                BorderStyle     = BorderStyle.None,
                ReadOnly        = true,
                ScrollBars      = RichTextBoxScrollBars.Vertical,
                WordWrap        = false
            };
            logCard.Controls.Add(logBox);

            Log("Win11 Optimizer ready. Select tweaks and click Run.", TEXTDIM);
        }

        // ── Helper: card panel ────────────────────────────────────────────
        Panel MakeCard(Rectangle bounds, string title)
        {
            var card = new Panel
            {
                Bounds    = bounds,
                BackColor = CARD
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var pen = new Pen(BORDER);
                g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                // top accent line
                using var br = new LinearGradientBrush(
                    new Rectangle(0, 0, card.Width, 2), ACCENT, ACCENT2,
                    LinearGradientMode.Horizontal);
                g.FillRectangle(br, 0, 0, card.Width, 2);
            };
            var lbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = TEXTDIM,
                AutoSize  = true,
                Location  = new Point(12, 18),
                BackColor = Color.FromArgb(24, 24, 32)  // CARD
            };
            card.Controls.Add(lbl);
            Controls.Add(card);
            return card;
        }

        // ── Helper: checkbox row ──────────────────────────────────────────
        (CheckBox chk, Label desc) MakeCheckRow(Panel parent, int y, string title, string subtitle)
        {
            var chk = new CheckBox
            {
                Text      = title,
                Font      = FONT_LABEL,
                ForeColor = TEXT,
                Location  = new Point(14, y),
                AutoSize  = true,
                BackColor = Color.FromArgb(24, 24, 32),  // CARD
                Checked   = false
            };
            chk.FlatStyle = FlatStyle.Flat;
            StyleCheckbox(chk);

            var desc = new Label
            {
                Text      = subtitle,
                Font      = FONT_BODY,
                ForeColor = TEXTDIM,
                Location  = new Point(36, y + 20),
                AutoSize  = true,
                BackColor = Color.FromArgb(24, 24, 32)  // CARD
            };
            parent.Controls.Add(chk);
            parent.Controls.Add(desc);
            return (chk, desc);
        }

        void StyleCheckbox(CheckBox chk)
        {
            chk.FlatAppearance.BorderColor        = BORDER;
            chk.FlatAppearance.CheckedBackColor   = ACCENT;
            chk.FlatAppearance.MouseOverBackColor = Color.FromArgb(24, 24, 32);  // CARD
        }

        // ── Helper: info line ─────────────────────────────────────────────
        void AddInfoLine(Panel parent, int y, Color col, string text)
        {
            parent.Controls.Add(new Label
            {
                Text      = text,
                Font      = FONT_BODY,
                ForeColor = col,
                Location  = new Point(14, y),
                Size      = new Size(parent.Width - 28, 18),
                BackColor = Color.FromArgb(24, 24, 32)  // CARD
            });
        }

        // ── Helper: summary box ───────────────────────────────────────────
        Label AddSummaryBox(Panel parent, int x, Color col, string count, string title)
        {
            var box = new Panel
            {
                Bounds    = new Rectangle(x, 44, 110, 70),
                BackColor = BG
            };
            box.Paint += (s, e) =>
            {
                using var pen = new Pen(col) { Width = 1.5f };
                e.Graphics.DrawRectangle(pen, 0, 0, box.Width - 1, box.Height - 1);
            };

            var num = new Label
            {
                Text      = count,
                Font      = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = col,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds    = new Rectangle(0, 6, 110, 34),
                BackColor = Color.FromArgb(10, 10, 14)
            };
            var sub = new Label
            {
                Text      = title,
                Font      = FONT_BODY,
                ForeColor = TEXTDIM,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds    = new Rectangle(0, 42, 110, 20),
                BackColor = Color.FromArgb(10, 10, 14)
            };
            box.Controls.Add(num);
            box.Controls.Add(sub);
            parent.Controls.Add(box);
            return num;   // return number label so we can update it
        }

        // ── Logging ───────────────────────────────────────────────────────
        void Log(string msg, Color? col = null)
        {
            if (InvokeRequired) { Invoke(new Action(() => Log(msg, col))); return; }
            logBox.SelectionColor = col ?? ACCENT;
            logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            logBox.ScrollToCaret();
        }

        void SetProgress(int done, int total)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetProgress(done, total))); return; }
            int w = total == 0 ? 0 : (int)(380.0 * done / total);
            progressBar.Width = w;
            lblStatus.Text = total == 0 ? "Ready." : $"Running… {done}/{total}";
        }

        void SetStatus(string msg, Color? col = null)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetStatus(msg, col))); return; }
            lblStatus.Text      = msg;
            lblStatus.ForeColor = col ?? TEXTDIM;
        }

        // ── Run tweaks ────────────────────────────────────────────────────
        async Task RunTweaks(bool selectedOnly)
        {
            btnRunSelected.Enabled = btnRunAll.Enabled = false;
            TweakEngine.ClearResults();
            logBox.Clear();
            progressBar.Width = 0;
            _passLabel.Text   = "0";
            _failLabel.Text   = "0";

            bool doPerf      = !selectedOnly || chkPerf.Checked;
            bool doPrivacy   = !selectedOnly || chkPrivacy.Checked;
            bool doRespond   = !selectedOnly || chkResponsive.Checked;
            bool doGaming    = !selectedOnly || chkGaming.Checked;
            bool doNetwork   = !selectedOnly || chkNetwork.Checked;
            bool doBloat     = !selectedOnly || chkBloat.Checked;

            // Count approximate steps for progress
            _totalTweaks = (doPerf ? 8 : 0) + (doPrivacy ? 25 : 0) + (doRespond ? 8 : 0) +
                           (doGaming ? 10 : 0) + (doNetwork ? 8 : 0) + (doBloat ? 40 : 0);
            _doneTweaks = 0;
            SetProgress(0, _totalTweaks);

            await Task.Run(() =>
            {
                void Tick(string msg) { _doneTweaks++; SetProgress(_doneTweaks, _totalTweaks); Log(msg); }

                if (doPerf)    { Tick("→ Performance tweaks…");    TweakEngine.ApplyPerformanceTweaks(); }
                if (doPrivacy) { Tick("→ Privacy tweaks…");        TweakEngine.ApplyPrivacyTweaks(); }
                if (doRespond) { Tick("→ Responsiveness tweaks…"); TweakEngine.ApplySystemResponsiveness(); }
                if (doGaming)  { Tick("→ Gaming tweaks…");         TweakEngine.ApplyGamingTweaks(); }
                if (doNetwork) { Tick("→ Network tweaks…");        TweakEngine.ApplyNetworkTweaks(); }
                if (doBloat)   { TweakEngine.RemoveBloatware(msg => { Tick(msg); }); }
            });

            // ── Results ───────────────────────────────────────────────────
            var results = TweakEngine.GetResults();
            int pass = results.Count(r => r.Success);
            int fail = results.Count(r => !r.Success);

            _passLabel.Text = pass.ToString();
            _failLabel.Text = fail.ToString();

            foreach (var r in results.Where(r => !r.Success))
                Log($"  ✗ {r.Name}: {r.Error}", DANGER);

            Log($"── Done: {pass} succeeded, {fail} failed. Reboot recommended. ──", WARN);
            SetStatus("Complete — reboot recommended.", WARN);
            SetProgress(_totalTweaks, _totalTweaks);
            progressBar.BackColor = fail == 0 ? ACCENT : WARN;

            btnRunSelected.Enabled = btnRunAll.Enabled = true;
        }
    }

    // ── Custom glow button ────────────────────────────────────────────────
    public class GlowButton : Control
    {
        readonly Color _accent;
        bool _hover, _pressed;

        public GlowButton(string text, Color accent, Rectangle bounds)
        {
            Text        = text;
            _accent     = accent;
            Bounds      = bounds;
            Font        = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            ForeColor   = Color.FromArgb(10, 10, 14);
            BackColor   = Color.Transparent;
            Cursor      = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover   = true;  Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _hover   = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true;  Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e)   { _pressed = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g  = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rc = new Rectangle(0, 0, Width, Height);

            Color fill = _pressed ? Color.FromArgb(180, _accent.R, _accent.G, _accent.B)
                       : _hover   ? _accent
                       : Color.FromArgb(220, _accent.R, _accent.G, _accent.B);

            using var br = new SolidBrush(fill);
            g.FillRectangle(br, rc);

            if (_hover && !_pressed)
            {
                // glow border
                using var pen = new Pen(Color.FromArgb(160, _accent.R, _accent.G, _accent.B), 1.5f);
                g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
            }

            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var tf = new SolidBrush(ForeColor);
            g.DrawString(Text, Font, tf, rc, sf);
        }
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
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                    LogCrash(e.ExceptionObject as Exception);

                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                LogCrash(ex);
            }
        }

        static void LogCrash(Exception? ex)
        {
            try
            {
                string logPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                string msg = $"[{DateTime.Now}]\n{ex}\n\n";
                System.IO.File.AppendAllText(logPath, msg);
                MessageBox.Show(
                    $"Crash logged to:\n{logPath}\n\n{ex?.Message}",
                    "Win11Optimizer — Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}