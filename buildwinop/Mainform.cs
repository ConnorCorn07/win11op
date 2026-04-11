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
        static readonly Font FONT_LABEL = new Font("Segoe UI", 12f, FontStyle.Bold);
        static readonly Font FONT_BODY  = new Font("Segoe UI", 11f, FontStyle.Regular);
        static readonly Font FONT_LOG   = new Font("Consolas", 11f, FontStyle.Regular);
        static readonly Font FONT_BTN   = new Font("Segoe UI", 11f, FontStyle.Bold);

        // ── Controls ─────────────────────────────────────────────────────
#pragma warning disable CS8618
        CheckBox chkPerf, chkPrivacy, chkResponsive, chkGaming, chkNetwork, chkBloat;
        GlowButton btnRunSelected, btnRunAll;
        DarkRichTextBox logBox;
        Panel progressBar;
        Label lblStatus;
        Panel sideAccent;
        Label _passLabel, _failLabel;
#pragma warning restore CS8618
        int _totalTweaks, _doneTweaks;

        // Undo buttons — one per category
        GlowButton _undoPerf, _undoPrivacy, _undoResponsive, _undoGaming, _undoNetwork;

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
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox     = true;

            // ── Left accent bar ───────────────────────────────────────────
            sideAccent = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 4,
                BackColor = ACCENT
            };
            Controls.Add(sideAccent);

            // ── Outer layout: header on top, body in middle, log at bottom ─
            var outer = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 3,
                BackColor   = BG,
                Padding     = new Padding(0)
            };
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));  // header
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // body
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));  // log
            Controls.Add(outer);

            // ── Header ────────────────────────────────────────────────────
            var header = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = SURFACE
            };
            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode    = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // gradient stripe at bottom
                using var br = new LinearGradientBrush(
                    new Rectangle(0, header.Height - 2, header.Width, 2),
                    ACCENT, ACCENT2, LinearGradientMode.Horizontal);
                g.FillRectangle(br, 0, header.Height - 2, header.Width, 2);

                // scale title font to header height
                float titleSize = Math.Max(14f, header.Height * 0.30f);
                float subSize   = Math.Max(8f,  header.Height * 0.13f);

                using var titleFont = new Font("Segoe UI", titleSize, FontStyle.Bold);
                using var subFont   = new Font("Segoe UI", subSize,   FontStyle.Regular);
                using var textBrush = new SolidBrush(TEXT);
                using var dimBrush  = new SolidBrush(TEXTDIM);

                g.DrawString("WIN11  OPTIMIZER", titleFont, textBrush, new PointF(20, header.Height * 0.12f));
                g.DrawString("Performance · Privacy · Gaming · Network", subFont, dimBrush, new PointF(24, header.Height * 0.62f));
            };
            outer.Controls.Add(header, 0, 0);

            // ── Body: left column (select + buttons) | right column (info + summary) ─
            var body = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = BG,
                Padding     = new Padding(8)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            outer.Controls.Add(body, 0, 1);

            // ── Left column ───────────────────────────────────────────────
            var leftCol = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 3,
                BackColor   = BG,
                Padding     = new Padding(0, 0, 4, 0)
            };
            leftCol.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // select card
            leftCol.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); // buttons
            leftCol.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); // progress+status
            body.Controls.Add(leftCol, 0, 0);

            // Select tweaks card
            var selectCard = MakeCardDock("SELECT TWEAKS");
            leftCol.Controls.Add(selectCard, 0, 0);

            var checkContainer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = CARD,
                Padding   = new Padding(10, 34, 10, 10)
            };
            selectCard.Controls.Add(checkContainer);

            var checkLayout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 6,
                BackColor   = CARD
            };
            for (int i = 0; i < 6; i++)
                checkLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6));
            checkContainer.Controls.Add(checkLayout);

            chkPerf       = MakeCheckRowDock(checkLayout, 0, "⚡  Performance",       "Power plan, NTFS, visual effects, startup",  "Performance",    out _undoPerf);
            chkPrivacy    = MakeCheckRowDock(checkLayout, 1, "🔒  Privacy & Telemetry","Disable tracking, ad ID, data collection",   "Privacy",         out _undoPrivacy);
            chkResponsive = MakeCheckRowDock(checkLayout, 2, "🖥  Responsiveness",     "Menu speed, shutdown timers, high-res clock", "Responsiveness",  out _undoResponsive);
            chkGaming     = MakeCheckRowDock(checkLayout, 3, "🎮  Gaming",             "HAGS, Game Mode, priority, DVR off",          "Gaming",          out _undoGaming);
            chkNetwork    = MakeCheckRowDock(checkLayout, 4, "🌐  Network",            "Nagle off, TCP tuning, throttle index",       "Network",         out _undoNetwork);
            chkBloat      = MakeCheckRowDock(checkLayout, 5, "🗑  Remove Bloatware",   "Strips pre-installed junk & ads",             "Bloatware",       out _);
            chkPerf.Checked = chkPrivacy.Checked = chkResponsive.Checked = true;

            // Wire undo buttons
            _undoPerf.Click       += async (s, e) => await RunUndo("Performance",    TweakEngine.UndoPerformanceTweaks,    _undoPerf);
            _undoPrivacy.Click    += async (s, e) => await RunUndo("Privacy",        TweakEngine.UndoPrivacyTweaks,        _undoPrivacy);
            _undoResponsive.Click += async (s, e) => await RunUndo("Responsiveness", TweakEngine.UndoResponsivenessTweaks, _undoResponsive);
            _undoGaming.Click     += async (s, e) => await RunUndo("Gaming",         TweakEngine.UndoGamingTweaks,         _undoGaming);
            _undoNetwork.Click    += async (s, e) => await RunUndo("Network",        TweakEngine.UndoNetworkTweaks,        _undoNetwork);

            // Load any backups saved from a previous run
            TweakEngine.LoadBackups();
            RefreshUndoButtons();

            // Buttons row
            var btnPanel = new Panel { Dock = DockStyle.Fill, BackColor = BG, Padding = new Padding(0, 6, 0, 0) };
            leftCol.Controls.Add(btnPanel, 0, 1);

            btnRunSelected = new GlowButton("RUN SELECTED", ACCENT,
                new Rectangle(0, 0, 0, 42));   // width set by Anchor below
            btnRunAll = new GlowButton("RUN ALL", ACCENT2,
                new Rectangle(0, 0, 0, 42));

            btnRunSelected.Dock = DockStyle.Left;
            btnRunAll.Dock      = DockStyle.Left;
            btnRunSelected.Width = 160;
            btnRunAll.Width      = 130;
            btnRunSelected.Margin = new Padding(0, 0, 8, 0);

            btnRunSelected.Click += async (s, e) => await RunTweaks(selectedOnly: true);
            btnRunAll.Click      += async (s, e) => await RunTweaks(selectedOnly: false);
            btnPanel.Controls.Add(btnRunAll);
            btnPanel.Controls.Add(btnRunSelected);  // added second = drawn left-first

            // Progress + status row
            var progPanel = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                BackColor   = BG,
                ColumnCount = 1,
                RowCount    = 2,
                Padding     = new Padding(0, 6, 0, 0)
            };
            progPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));   // progress bar
            progPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // status label
            leftCol.Controls.Add(progPanel, 0, 2);

            var progBg = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = BORDER
            };
            progressBar = new Panel { Bounds = new Rectangle(0, 0, 0, 8), BackColor = ACCENT };
            progBg.Controls.Add(progressBar);
            progPanel.Controls.Add(progBg, 0, 0);

            lblStatus = new Label
            {
                Text      = "Ready.",
                Font      = FONT_BODY,
                ForeColor = TEXTDIM,
                AutoSize  = true,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = BG
            };
            progPanel.Controls.Add(lblStatus, 0, 1);

            // ── Right column ──────────────────────────────────────────────
            var rightCol = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 2,
                BackColor   = BG,
                Padding     = new Padding(4, 0, 0, 0)
            };
            rightCol.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            rightCol.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            body.Controls.Add(rightCol, 1, 0);

            // Notes card
            var infoCard = MakeCardDock("NOTES");
            rightCol.Controls.Add(infoCard, 0, 0);
            var infoInner = new Panel { Dock = DockStyle.Fill, BackColor = CARD, Padding = new Padding(10, 34, 10, 10) };
            infoCard.Controls.Add(infoInner);
            AddInfoLineDock(infoInner, ACCENT,  "• Run as Administrator for registry & service access.");
            AddInfoLineDock(infoInner, WARN,    "• A reboot is required for HAGS & timer tweaks.");
            AddInfoLineDock(infoInner, TEXTDIM, "• Bloatware removal also strips provisioned packages.");
            AddGithubLink(infoInner);

            // Summary card
            var sumCard = MakeCardDock("LAST RUN SUMMARY");
            rightCol.Controls.Add(sumCard, 0, 1);
            var sumInner = new Panel { Dock = DockStyle.Fill, BackColor = CARD, Padding = new Padding(10, 34, 10, 10) };
            sumCard.Controls.Add(sumInner);

            var sumBoxes = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                BackColor   = CARD,
                ColumnCount = 2,
                RowCount    = 1
            };
            sumBoxes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            sumBoxes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            sumBoxes.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            sumInner.Controls.Add(sumBoxes);
            _passLabel = AddSummaryBoxDock(sumBoxes, 0, ACCENT, "0", "Succeeded", "✔");
            _failLabel = AddSummaryBoxDock(sumBoxes, 1, DANGER, "0", "Failed",    "✘");

            // ── Log box ───────────────────────────────────────────────────
            var logCard = MakeCardDock("OUTPUT LOG");
            outer.Controls.Add(logCard, 0, 2);

            var logInner = new Panel { Dock = DockStyle.Fill, BackColor = BG, Padding = new Padding(8, 36, 8, 8) };
            logCard.Controls.Add(logInner);

            logBox = new DarkRichTextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = BG,
                ForeColor   = ACCENT,
                Font        = FONT_LOG,
                BorderStyle = BorderStyle.None,
                ReadOnly    = true,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                WordWrap    = false
            };
            logInner.Controls.Add(logBox);

            Log("Win11 Optimizer ready. Select tweaks and click Run.", TEXTDIM);
        }

        // ── Docking versions of helpers ───────────────────────────────────

        Panel MakeCardDock(string title)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = CARD, Margin = new Padding(4) };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(BORDER);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                using var br = new LinearGradientBrush(
                    new Rectangle(0, 0, card.Width, 2), ACCENT, ACCENT2, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(br, 0, 0, card.Width, 2);
            };
            var lbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = TEXTDIM,
                AutoSize  = true,
                Location  = new Point(12, 10),
                BackColor = CARD
            };
            card.Controls.Add(lbl);
            return card;
        }

        CheckBox MakeCheckRowDock(TableLayoutPanel parent, int row, string title, string subtitle,
                                  string category, out GlowButton undoBtn)
        {
            // Layout: checkbox+desc fill left, undo button docked right
            var cell = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                BackColor   = CARD,
                ColumnCount = 2,
                RowCount    = 1
            };
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));

            var left = new Panel { Dock = DockStyle.Fill, BackColor = CARD };
            var chk = new CheckBox
            {
                Text      = title,
                Font      = FONT_LABEL,
                ForeColor = TEXT,
                AutoSize  = true,
                Location  = new Point(6, 2),
                BackColor = CARD,
                FlatStyle = FlatStyle.Flat
            };
            chk.FlatAppearance.BorderColor        = BORDER;
            chk.FlatAppearance.CheckedBackColor   = ACCENT;
            chk.FlatAppearance.MouseOverBackColor = CARD;
            var desc = new Label
            {
                Text      = subtitle,
                Font      = FONT_BODY,
                ForeColor = TEXTDIM,
                AutoSize  = true,
                Location  = new Point(26, 22),
                BackColor = CARD
            };
            left.Controls.Add(chk);
            left.Controls.Add(desc);

            // Undo button — hidden until category has been applied
            undoBtn = new GlowButton("↩ UNDO", DANGER, new Rectangle(0, 0, 68, 32))
            {
                Dock    = DockStyle.None,
                Anchor  = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Visible = TweakEngine.HasBackup(category),
                Margin  = new Padding(2)
            };
            undoBtn.Dock = DockStyle.Fill;

            cell.Controls.Add(left,    0, 0);
            cell.Controls.Add(undoBtn, 1, 0);
            parent.Controls.Add(cell, 0, row);
            return chk;
        }

        void AddInfoLineDock(Panel parent, Color col, string text)
        {
            var flow = parent.Controls.Count == 0
                ? new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, BackColor = CARD, WrapContents = false }
                : (FlowLayoutPanel)parent.Controls[0];
            if (parent.Controls.Count == 0) parent.Controls.Add(flow);

            flow.Controls.Add(new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = col,
                AutoSize  = true,
                BackColor = CARD,
                Margin    = new Padding(0, 4, 0, 4)
            });
        }

        void AddGithubLink(Panel parent)
        {
            var flow = parent.Controls.Count == 0
                ? new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, BackColor = CARD, WrapContents = false }
                : (FlowLayoutPanel)parent.Controls[0];
            if (parent.Controls.Count == 0) parent.Controls.Add(flow);

            var link = new LinkLabel
            {
                Text      = "⭐  GitHub: github.com/ConnorCorn07/win11op",
                Font      = new Font("Segoe UI", 11f, FontStyle.Regular),
                AutoSize  = true,
                BackColor = CARD,
                Margin    = new Padding(0, 8, 0, 4)
            };
            link.LinkColor         = ACCENT2;
            link.ActiveLinkColor   = ACCENT;
            link.VisitedLinkColor  = ACCENT2;
            link.LinkBehavior      = LinkBehavior.HoverUnderline;
            link.Click += (s, e) =>
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = "https://github.com/ConnorCorn07/win11op",
                    UseShellExecute = true
                });
            flow.Controls.Add(link);
        }

        Label AddSummaryBoxDock(TableLayoutPanel parent, int col, Color accentCol, string count, string title, string icon)
        {
            var box = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(30, accentCol.R, accentCol.G, accentCol.B),
                Margin    = new Padding(4)
            };
            box.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var stripeBr = new SolidBrush(accentCol);
                g.FillRectangle(stripeBr, 0, 0, 5, box.Height);
                using var pen = new Pen(Color.FromArgb(80, accentCol.R, accentCol.G, accentCol.B), 1.5f);
                g.DrawRectangle(pen, 1, 1, box.Width - 3, box.Height - 3);
            };

            // Layout: title docked bottom, content fills rest
            var sub = new Label
            {
                Text      = title.ToUpper(),
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, accentCol.R, accentCol.G, accentCol.B),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock      = DockStyle.Bottom,
                Height    = 30,
                BackColor = Color.FromArgb(30, accentCol.R, accentCol.G, accentCol.B),
                Padding   = new Padding(14, 0, 0, 0)
            };

            // Middle area: number + icon side by side
            var middle = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = Color.FromArgb(30, accentCol.R, accentCol.G, accentCol.B)
            };
            middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60)); // number
            middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); // icon

            var num = new Label
            {
                Text      = count,
                Font      = new Font("Segoe UI", 42f, FontStyle.Bold),
                ForeColor = accentCol,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(30, accentCol.R, accentCol.G, accentCol.B),
                Padding   = new Padding(14, 0, 0, 0)
            };

            var lblIcon = new Label
            {
                Text      = icon,
                Font      = new Font("Segoe UI", 32f, FontStyle.Regular),
                ForeColor = Color.FromArgb(70, accentCol.R, accentCol.G, accentCol.B),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(30, accentCol.R, accentCol.G, accentCol.B)
            };

            middle.Controls.Add(num,     0, 0);
            middle.Controls.Add(lblIcon, 1, 0);
            box.Controls.Add(sub);
            box.Controls.Add(middle);
            parent.Controls.Add(box, col, 0);
            return num;
        }

        // ── Undo button visibility ────────────────────────────────────────
        void RefreshUndoButtons()
        {
            if (InvokeRequired) { Invoke(new Action(RefreshUndoButtons)); return; }
            _undoPerf.Visible       = TweakEngine.HasBackup("Performance");
            _undoPrivacy.Visible    = TweakEngine.HasBackup("Privacy");
            _undoResponsive.Visible = TweakEngine.HasBackup("Responsiveness");
            _undoGaming.Visible     = TweakEngine.HasBackup("Gaming");
            _undoNetwork.Visible    = TweakEngine.HasBackup("Network");
        }

        async Task RunUndo(string category, Func<List<TweakEngine.TweakResult>> undoAction,
                           GlowButton btn)
        {
            btn.Enabled = false;
            Log($"↩ Undoing {category} tweaks…", WARN);

            var results = await Task.Run(undoAction);

            int ok  = results.Count(r => r.Success);
            int bad = results.Count(r => !r.Success);
            Log($"┌─ UNDO {category.ToUpper()}  ({ok} restored, {bad} failed)", TEXTDIM);
            foreach (var r in results)
            {
                if (r.Success) Log($"│  ✔  {r.Name}", ACCENT);
                else           Log($"│  ✘  {r.Name}: {r.Error}", DANGER);
            }
            Log($"└─────────────────────────────────────", BORDER);
            Log($"↩ {category} undone. Reboot recommended.", ACCENT);
            SetStatus($"{category} tweaks undone. Reboot recommended.", ACCENT);

            RefreshUndoButtons();
            btn.Enabled = true;
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
            int w = total == 0 ? 0 : (int)((double)progressBar.Parent.Width * done / total);
            progressBar.Width = w;
            // Only update status text while actively running, not at completion
            if (total > 0 && done < total)
                lblStatus.Text = $"Running… {done}/{total}";
            else if (total == 0)
                lblStatus.Text = "Ready.";
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
            progressBar.Width     = 0;
            progressBar.BackColor = WARN;  // yellow while running
            _passLabel.Text       = "0";
            _failLabel.Text       = "0";

            bool doPerf    = !selectedOnly || chkPerf.Checked;
            bool doPrivacy = !selectedOnly || chkPrivacy.Checked;
            bool doRespond = !selectedOnly || chkResponsive.Checked;
            bool doGaming  = !selectedOnly || chkGaming.Checked;
            bool doNetwork = !selectedOnly || chkNetwork.Checked;
            bool doBloat   = !selectedOnly || chkBloat.Checked;

            _totalTweaks = (doPerf ? 8 : 0) + (doPrivacy ? 25 : 0) + (doRespond ? 8 : 0) +
                           (doGaming ? 10 : 0) + (doNetwork ? 8 : 0) + (doBloat ? 40 : 0);
            _doneTweaks = 0;
            SetProgress(0, _totalTweaks);

            // Track result count before each section so we can log per-tweak results
            int prevCount = 0;

            void LogSectionResults(string sectionName)
            {
                var all = TweakEngine.GetResults();
                var section = all.Skip(prevCount).ToList();
                prevCount = all.Count;

                int ok  = section.Count(r => r.Success);
                int bad = section.Count(r => !r.Success);

                Log($"┌─ {sectionName}  ({ok} ok, {bad} failed)", TEXTDIM);
                foreach (var r in section)
                {
                    if (r.Success)
                        Log($"│  ✔  {r.Name}", ACCENT);
                    else
                        Log($"│  ✘  {r.Name}: {r.Error}", DANGER);
                }
                Log($"└─────────────────────────────────────", BORDER);
            }

            await Task.Run(() =>
            {
                void Tick(string msg) { _doneTweaks++; SetProgress(_doneTweaks, _totalTweaks); Log(msg, TEXTDIM); }

                if (doPerf)
                {
                    Tick("→ Applying Performance tweaks…");
                    TweakEngine.ApplyPerformanceTweaks();
                    Invoke(new Action(() => LogSectionResults("PERFORMANCE")));
                }
                if (doPrivacy)
                {
                    Tick("→ Applying Privacy & Telemetry tweaks…");
                    TweakEngine.ApplyPrivacyTweaks();
                    Invoke(new Action(() => LogSectionResults("PRIVACY & TELEMETRY")));
                }
                if (doRespond)
                {
                    Tick("→ Applying Responsiveness tweaks…");
                    TweakEngine.ApplySystemResponsiveness();
                    Invoke(new Action(() => LogSectionResults("RESPONSIVENESS")));
                }
                if (doGaming)
                {
                    Tick("→ Applying Gaming tweaks…");
                    TweakEngine.ApplyGamingTweaks();
                    Invoke(new Action(() => LogSectionResults("GAMING")));
                }
                if (doNetwork)
                {
                    Tick("→ Applying Network tweaks…");
                    TweakEngine.ApplyNetworkTweaks();
                    Invoke(new Action(() => LogSectionResults("NETWORK")));
                }
                if (doBloat)
                {
                    TweakEngine.RemoveBloatware(msg => { Tick(msg); });
                    Invoke(new Action(() => LogSectionResults("BLOATWARE REMOVAL")));
                }
            });

            // ── Final summary ─────────────────────────────────────────────
            var results = TweakEngine.GetResults();
            int pass = results.Count(r => r.Success);
            int fail = results.Count(r => !r.Success);

            _passLabel.Text = pass.ToString();
            _failLabel.Text = fail.ToString();

            Log($"══ COMPLETE: {pass} succeeded, {fail} failed. Reboot recommended. ══", ACCENT);
            SetStatus($"Complete — {pass} succeeded, {fail} failed. Reboot recommended.", ACCENT);
            SetProgress(_totalTweaks, _totalTweaks);
            progressBar.BackColor = ACCENT;

            btnRunSelected.Enabled = btnRunAll.Enabled = true;
            RefreshUndoButtons();
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
            BackColor   = Color.FromArgb(10, 10, 14);
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

    // ── Dark scrollbar RichTextBox ────────────────────────────────────────
    public class DarkRichTextBox : RichTextBox
    {
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Force Explorer dark theme on the scrollbar
            SetWindowTheme(Handle, "DarkMode_Explorer", null);
        }
    }
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