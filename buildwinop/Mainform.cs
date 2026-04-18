using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Win11Optimizer
{
    // ═══════════════════════════════════════════════════════════════════════
    //  THEME
    // ═══════════════════════════════════════════════════════════════════════
    public static class Theme
    {
        public static readonly Color BG         = Color.FromArgb(13, 13, 18);
        public static readonly Color SURFACE    = Color.FromArgb(22, 22, 30);
        public static readonly Color SURFACE2   = Color.FromArgb(30, 30, 40);
        public static readonly Color ACCENT     = Color.FromArgb(99, 102, 241);
        public static readonly Color ACCENT_HOV = Color.FromArgb(129, 132, 255);
        public static readonly Color SUCCESS    = Color.FromArgb(34, 197, 94);
        public static readonly Color WARNING    = Color.FromArgb(251, 191, 36);
        public static readonly Color DANGER     = Color.FromArgb(239, 68, 68);
        public static readonly Color TEXT_PRI   = Color.FromArgb(240, 240, 255);
        public static readonly Color TEXT_SEC   = Color.FromArgb(140, 140, 170);
        public static readonly Color BORDER     = Color.FromArgb(40, 40, 58);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TWEAK CATALOG  (mirrors AppCatalog / AppEntry from App Downloader)
    // ═══════════════════════════════════════════════════════════════════════
    public class TweakEntry
    {
        public string Name        { get; set; }
        public string Description { get; set; }
        public string Category    { get; set; }
        public string Icon        { get; set; }
        public bool   IsAdvanced  { get; set; }
        public bool   DefaultOn   { get; set; } = true;
        public string AdvancedKey { get; set; }
        public string TweakKey    { get; set; }  // unique key for individual dispatch
    }

    public static class TweakCatalog
    {
        public static readonly List<TweakEntry> All = new List<TweakEntry>
        {
            // ── PERFORMANCE ───────────────────────────────────────────────
            new TweakEntry { Category = "Performance", Icon = "⚡", DefaultOn = true,
                TweakKey    = "Perf_PowerPlan",
                Name = "High Performance Power Plan",
                Description = "Switches power plan to maximum performance mode" },
            new TweakEntry { Category = "Performance", Icon = "🔋", DefaultOn = true,
                TweakKey    = "Perf_PowerThrottle",
                Name = "Disable Power Throttling",
                Description = "Prevents Windows throttling background CPU usage" },
            new TweakEntry { Category = "Performance", Icon = "🗂", DefaultOn = true,
                TweakKey    = "Perf_SysMain",
                Name = "Disable SysMain (Superfetch)",
                Description = "Stops preloading rarely-used apps into RAM" },
            new TweakEntry { Category = "Performance", Icon = "🔍", DefaultOn = true,
                TweakKey    = "Perf_WSearch",
                Name = "Disable Windows Search Indexer",
                Description = "Removes background disk I/O from search indexing" },
            new TweakEntry { Category = "Performance", Icon = "⏱", DefaultOn = true,
                TweakKey    = "Perf_StartupDelay",
                Name = "Remove Startup Delay",
                Description = "Eliminates the artificial Explorer startup pause" },
            new TweakEntry { Category = "Performance", Icon = "🖼", DefaultOn = true,
                TweakKey    = "Perf_VisualFX",
                Name = "Visual Effects: Best Performance",
                Description = "Turns off animations, shadows and fancy rendering" },
            new TweakEntry { Category = "Performance", Icon = "📁", DefaultOn = true,
                TweakKey    = "Perf_NtfsLastAccess",
                Name = "Disable NTFS Last-Access Timestamps",
                Description = "Reduces filesystem writes on every file read" },
            new TweakEntry { Category = "Performance", Icon = "📄", DefaultOn = true,
                TweakKey    = "Perf_8Dot3",
                Name = "Disable 8.3 Filenames",
                Description = "Removes legacy short filename generation on NTFS" },
            new TweakEntry { Category = "Performance", Icon = "💤", DefaultOn = true,
                TweakKey    = "Perf_Hibernate",
                Name = "Disable Hibernation",
                Description = "Frees several GB of disk space, speeds shutdown" },
            new TweakEntry { Category = "Performance", Icon = "🧠", DefaultOn = true,
                TweakKey    = "Perf_MemCompression",
                Name = "Disable Memory Compression",
                Description = "Reduces CPU overhead when RAM is under pressure" },

            new TweakEntry { Category = "Performance", Icon = "⏰", DefaultOn = true,
                TweakKey    = "Perf_TimerRes",
                Name = "Set Timer Resolution to 0.5ms",
                Description = "Calls timeBeginPeriod(1) + registry key for sub-ms scheduler ticks" },
            // ── PRIVACY ───────────────────────────────────────────────────
            new TweakEntry { Category = "Privacy", Icon = "📡", DefaultOn = true,
                TweakKey    = "Priv_Telemetry",
                Name = "Disable Telemetry",
                Description = "Blocks Microsoft data collection at the registry level" },
            new TweakEntry { Category = "Privacy", Icon = "🛑", DefaultOn = true,
                TweakKey    = "Priv_DiagTrack",
                Name = "Disable DiagTrack Service",
                Description = "Stops the Connected User Experiences telemetry service" },
            new TweakEntry { Category = "Privacy", Icon = "📢", DefaultOn = true,
                TweakKey    = "Priv_AdvertisingId",
                Name = "Disable Advertising ID",
                Description = "Prevents apps from accessing your ad tracking ID" },
            new TweakEntry { Category = "Privacy", Icon = "🔎", DefaultOn = true,
                TweakKey    = "Priv_BingStart",
                Name = "Disable Bing in Start Menu",
                Description = "Removes web search results from the Start search bar" },
            new TweakEntry { Category = "Privacy", Icon = "🎙", DefaultOn = true,
                TweakKey    = "Priv_Cortana",
                Name = "Disable Cortana Consent",
                Description = "Turns off Cortana data collection consent flag" },
            new TweakEntry { Category = "Privacy", Icon = "📊", DefaultOn = true,
                TweakKey    = "Priv_ActivityFeed",
                Name = "Disable Activity Feed",
                Description = "Stops Windows logging app and file activity history" },
            new TweakEntry { Category = "Privacy", Icon = "📍", DefaultOn = true,
                TweakKey    = "Priv_Location",
                Name = "Disable Location Tracking",
                Description = "Blocks apps from accessing your physical location" },
            new TweakEntry { Category = "Privacy", Icon = "📷", DefaultOn = true,
                TweakKey    = "Priv_Camera",
                Name = "Block App Camera Access",
                Description = "Prevents UWP apps from using the webcam by default" },
            new TweakEntry { Category = "Privacy", Icon = "⚠", DefaultOn = true,
                TweakKey    = "Priv_WER",
                Name = "Disable Windows Error Reporting",
                Description = "Stops crash dumps and reports being sent to Microsoft" },
            new TweakEntry { Category = "Privacy", Icon = "🛡", DefaultOn = true,
                TweakKey    = "Priv_SmartScreen",
                Name = "Disable SmartScreen (Explorer)",
                Description = "Removes SmartScreen cloud checks in File Explorer" },
            new TweakEntry { Category = "Privacy", Icon = "🗓", DefaultOn = true,
                TweakKey    = "Priv_TelemetryTasks",
                Name = "Disable Scheduled Telemetry Tasks",
                Description = "Kills CEIP, AppraiserV2, Proxy and DiskDiag data tasks" },
            new TweakEntry { Category = "Privacy", Icon = "👁", DefaultOn = true,
                TweakKey    = "Priv_AppTracking",
                Name = "Disable App Launch Tracking",
                Description = "Stops Windows logging which apps you open and when" },
            new TweakEntry { Category = "Privacy", Icon = "💬", DefaultOn = true,
                TweakKey    = "Priv_Feedback",
                Name = "Disable Feedback Requests",
                Description = "Prevents Windows asking you to rate/review features" },

            new TweakEntry { Category = "Privacy", Icon = "💬", DefaultOn = true,
                TweakKey    = "Priv_ChatIcon",
                Name = "Disable Chat / Teams Taskbar Icon",
                Description = "Removes the Teams/Chat pinned icon from the taskbar" },
            new TweakEntry { Category = "Privacy", Icon = "🤖", DefaultOn = true,
                TweakKey    = "Priv_Recall",
                Name = "Disable Windows Recall",
                Description = "Kills AI screenshot feature on Copilot+ PCs (no-op otherwise)" },
            new TweakEntry { Category = "Privacy", Icon = "🚫", DefaultOn = true,
                TweakKey    = "Priv_HostsBlock",
                Name = "Block Telemetry Hosts",
                Description = "Adds 35 Microsoft telemetry domains to the hosts file (0.0.0.0)" },
            // ── RESPONSIVENESS ────────────────────────────────────────────
            new TweakEntry { Category = "Responsiveness", Icon = "🖱", DefaultOn = true,
                TweakKey    = "Resp_MenuDelay",
                Name = "Instant Menu Show",
                Description = "Sets menu open delay to 0 ms for snappier menus" },
            new TweakEntry { Category = "Responsiveness", Icon = "⚡", DefaultOn = true,
                TweakKey    = "Resp_AppKill",
                Name = "Fast App Kill Timeout",
                Description = "Reduces wait time before force-killing frozen apps" },
            new TweakEntry { Category = "Responsiveness", Icon = "⏹", DefaultOn = true,
                TweakKey    = "Resp_ServiceKill",
                Name = "Fast Service Kill Timeout",
                Description = "Cuts shutdown wait for slow-stopping services" },
            new TweakEntry { Category = "Responsiveness", Icon = "🔚", DefaultOn = true,
                TweakKey    = "Resp_AutoEndTasks",
                Name = "Auto End Tasks on Shutdown",
                Description = "Automatically kills hung apps instead of prompting" },
            new TweakEntry { Category = "Responsiveness", Icon = "⏱", DefaultOn = true,
                TweakKey    = "Resp_PlatformTick",
                Name = "Platform Tick (High-Res Timer)",
                Description = "Forces constant-rate high-resolution system timer" },
            new TweakEntry { Category = "Responsiveness", Icon = "💡", DefaultOn = true,
                TweakKey    = "Resp_WinTips",
                Name = "Disable Windows Tips",
                Description = "Stops the 'Did you know...' popups and suggestions" },
            new TweakEntry { Category = "Responsiveness", Icon = "📰", DefaultOn = true,
                TweakKey    = "Resp_SuggestedContent",
                Name = "Disable Suggested Content",
                Description = "Removes app install suggestions from the Start menu" },

            // ── GAMING ────────────────────────────────────────────────────
            new TweakEntry { Category = "Gaming", Icon = "🖥", DefaultOn = false,
                TweakKey    = "Game_HAGS",
                Name = "Enable HAGS",
                Description = "Hardware-Accelerated GPU Scheduling — reduces GPU latency" },
            new TweakEntry { Category = "Gaming", Icon = "🎮", DefaultOn = false,
                TweakKey    = "Game_GameMode",
                Name = "Enable Game Mode",
                Description = "Tells Windows to prioritize foreground game processes" },
            new TweakEntry { Category = "Gaming", Icon = "🖱", DefaultOn = false,
                TweakKey    = "Game_MouseAccel",
                Name = "Disable Mouse Acceleration",
                Description = "Removes pointer precision for 1:1 raw mouse input" },
            new TweakEntry { Category = "Gaming", Icon = "⚡", DefaultOn = false,
                TweakKey    = "Game_CPUPriority",
                Name = "CPU Foreground Priority Boost",
                Description = "Increases CPU time slice for the active window/game" },
            new TweakEntry { Category = "Gaming", Icon = "📹", DefaultOn = false,
                TweakKey    = "Game_DVR",
                Name = "Disable Game DVR / Capture",
                Description = "Turns off Xbox Game Bar background recording" },
            new TweakEntry { Category = "Gaming", Icon = "🪟", DefaultOn = false,
                TweakKey    = "Game_FSO",
                Name = "Disable Fullscreen Optimisations",
                Description = "Forces exclusive fullscreen for lower input latency" },

            new TweakEntry { Category = "Gaming", Icon = "🎯", DefaultOn = false,
                TweakKey    = "Game_GPUPower",
                Name = "GPU Power: Prefer Maximum Performance",
                Description = "Sets D3D power policy to never downclock the GPU" },
            new TweakEntry { Category = "Gaming", Icon = "🟢", DefaultOn = false,
                TweakKey    = "Game_NvidiaTelemetry",
                Name = "Disable NVIDIA Telemetry Services",
                Description = "Stops NvTelemetryContainer & NvDisplayContainerLS phoning home" },
            // ── NETWORK ───────────────────────────────────────────────────
            new TweakEntry { Category = "Network", Icon = "📶", DefaultOn = false,
                TweakKey    = "Net_Nagle",
                Name = "Disable Nagle's Algorithm",
                Description = "Reduces TCP packet buffering — lowers game ping" },
            new TweakEntry { Category = "Network", Icon = "🔁", DefaultOn = false,
                TweakKey    = "Net_RSS",
                Name = "Enable Receive-Side Scaling",
                Description = "Spreads network processing across CPU cores" },
            new TweakEntry { Category = "Network", Icon = "🎛", DefaultOn = false,
                TweakKey    = "Net_TCPAutoTune",
                Name = "TCP Auto-Tuning: Normal",
                Description = "Enables adaptive TCP receive buffer scaling" },
            new TweakEntry { Category = "Network", Icon = "🚦", DefaultOn = false,
                TweakKey    = "Net_Throttle",
                Name = "Disable Network Throttling Index",
                Description = "Removes multimedia network rate caps" },
            new TweakEntry { Category = "Network", Icon = "🏎", DefaultOn = false,
                TweakKey    = "Net_MMResponsive",
                Name = "Max Multimedia Responsiveness",
                Description = "Sets SystemResponsiveness to 0 for games/audio" },

            new TweakEntry { Category = "Network", Icon = "🔐", DefaultOn = false,
                TweakKey    = "Net_DoH",
                Name = "DNS over HTTPS (Cloudflare 1.1.1.1)",
                Description = "Enables DoH via Windows DNS Client, routes queries encrypted" },
            // ── BLOATWARE ─────────────────────────────────────────────────
            new TweakEntry { Category = "Bloatware", Icon = "📰", DefaultOn = false,
                TweakKey    = "Bloat_Bing",
                Name = "Remove Bing News & Weather",
                Description = "Uninstalls BingNews and BingWeather UWP packages" },
            new TweakEntry { Category = "Bloatware", Icon = "🎵", DefaultOn = false,
                TweakKey    = "Bloat_Zune",
                Name = "Remove Zune Music / Video",
                Description = "Removes the legacy Groove Music and Movies & TV apps" },
            new TweakEntry { Category = "Bloatware", Icon = "♟", DefaultOn = false,
                TweakKey    = "Bloat_Solitaire",
                Name = "Remove Solitaire Collection",
                Description = "Uninstalls the ad-supported Solitaire game suite" },
            new TweakEntry { Category = "Bloatware", Icon = "🗺", DefaultOn = false,
                TweakKey    = "Bloat_Maps",
                Name = "Remove Windows Maps",
                Description = "Strips the built-in Maps UWP application" },
            new TweakEntry { Category = "Bloatware", Icon = "📱", DefaultOn = false,
                TweakKey    = "Bloat_PhoneLink",
                Name = "Remove Phone Link / Your Phone",
                Description = "Removes the Android phone companion app" },
            new TweakEntry { Category = "Bloatware", Icon = "🎬", DefaultOn = false,
                TweakKey    = "Bloat_Clipchamp",
                Name = "Remove Clipchamp",
                Description = "Removes the bundled Microsoft video editor" },
            new TweakEntry { Category = "Bloatware", Icon = "🎮", DefaultOn = false,
                TweakKey    = "Bloat_Xbox",
                Name = "Remove Xbox Apps & Overlays",
                Description = "Strips Xbox TCUI, App, GameOverlay, GamingOverlay" },
            new TweakEntry { Category = "Bloatware", Icon = "🛒", DefaultOn = false,
                TweakKey    = "Bloat_AdTiles",
                Name = "Remove Third-Party Ad Tiles",
                Description = "Removes LinkedIn, Disney, Spotify, TikTok, Instagram tiles" },
            new TweakEntry { Category = "Bloatware", Icon = "📦", DefaultOn = false,
                TweakKey    = "Bloat_Office",
                Name = "Remove Office Hub & OneNote",
                Description = "Uninstalls the bundled Office Hub and OneNote UWP apps" },
            new TweakEntry { Category = "Bloatware", Icon = "🗃", DefaultOn = false,
                TweakKey    = "Bloat_3D",
                Name = "Remove 3D Viewer & Print 3D",
                Description = "Removes legacy 3D apps nobody asked for" },

            // ── SECURITY HARDENING ──────────────────────────────────────────────
            new TweakEntry { Category = "Security", Icon = "🔇", DefaultOn = false,
                TweakKey    = "Sec_AutoRun",
                Name = "Disable AutoRun / AutoPlay",
                Description = "Blocks autorun.inf and AutoPlay on all drive types" },
            new TweakEntry { Category = "Security", Icon = "🖥", DefaultOn = false,
                TweakKey    = "Sec_RDP",
                Name = "Disable Remote Desktop (RDP)",
                Description = "Refuses all inbound RDP connections, closes firewall rule" },
            new TweakEntry { Category = "Security", Icon = "🪟", DefaultOn = false,
                TweakKey    = "Sec_SMBv1",
                Name = "Disable SMBv1",
                Description = "Removes the WannaCry/EternalBlue-vulnerable SMBv1 protocol" },
            new TweakEntry { Category = "Security", Icon = "📡", DefaultOn = false,
                TweakKey    = "Sec_NetBIOS",
                Name = "Disable NetBIOS over TCP/IP",
                Description = "Stops NetBIOS on all adapters — prevents NBNS poisoning" },
            new TweakEntry { Category = "Security", Icon = "🛡", DefaultOn = false,
                TweakKey    = "Sec_Defender",
                Name = "Ensure Defender Real-Time Protection",
                Description = "Forces Defender real-time monitoring ON via policy + cmdlet" },

            // ── ADVANCED ──────────────────────────────────────────────────
            new TweakEntry { Category = "Advanced", Icon = "⚙", DefaultOn = false, IsAdvanced = true, AdvancedKey = "ProcessorScheduling",
                TweakKey    = "Adv_ProcessorScheduling",
                Name = "Processor Scheduling: Programs",
                Description = "Win32PrioritySeparation=38, max foreground CPU boost" },
            new TweakEntry { Category = "Advanced", Icon = "⏱", DefaultOn = false, IsAdvanced = true, AdvancedKey = "DisableDynamicTick",
                TweakKey    = "Adv_DynamicTick",
                Name = "Disable Dynamic Tick",
                Description = "Forces constant high-res IRQ8 timer, reduces micro-stutter" },
            new TweakEntry { Category = "Advanced", Icon = "🔥", DefaultOn = false, IsAdvanced = true, AdvancedKey = "DisableCpuThrottling",
                TweakKey    = "Adv_CPUThrottle",
                Name = "Disable CPU Throttling",
                Description = "Prevents Windows pulling background process CPU clocks" },
            new TweakEntry { Category = "Advanced", Icon = "💾", DefaultOn = false, IsAdvanced = true, AdvancedKey = "EnableTrim",
                TweakKey    = "Adv_TRIM",
                Name = "Ensure SSD TRIM Enabled",
                Description = "Sets disabledeletenotify=0, keeps SSD write speeds consistent" },
            new TweakEntry { Category = "Advanced", Icon = "🎨", DefaultOn = false, IsAdvanced = true, AdvancedKey = "AggressiveAnimations",
                TweakKey    = "Adv_Animations",
                Name = "Aggressive Animation Disabling",
                Description = "Kills UserPreferencesMask, TaskbarAnim, MinAnimate bits" },
        };

        public static IEnumerable<TweakEntry> ForCategory(string cat) =>
            cat == "All" ? All : All.Where(t => t.Category == cat);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  WINDOWS VERSION
    // ═══════════════════════════════════════════════════════════════════════
    public static class WinVersion
    {
        public static int    Build       { get; private set; }
        public static string DisplayName { get; private set; } = "Unknown";
        public static bool   IsWin11     => Build >= 22000;
        public static bool   IsWin10     => Build >= 10240 && Build < 22000;

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
                string winName = Build >= 22000 ? "Windows 11" : "Windows 10";
                DisplayName = string.IsNullOrWhiteSpace(dv)
                    ? $"{winName} (Build {Build})"
                    : $"{winName} {dv} (Build {Build})";
            }
            catch { Build = 0; DisplayName = "Unknown Windows"; }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CHANGE LOG
    // ═══════════════════════════════════════════════════════════════════════
    public static class ChangeLog
    {
        static readonly string LogFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "changelog.json");

        public class RunEntry
        {
            public string       Timestamp    { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            public string       WindowsVer   { get; set; } = WinVersion.DisplayName;
            public string       Categories   { get; set; } = "";
            public int          Passed       { get; set; }
            public int          Failed       { get; set; }
            public bool         RestorePoint { get; set; }
            public List<string> Details      { get; set; } = new();
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
            _entries.Insert(0, entry);
            Save();
        }

        static void Save()
        {
            try
            {
                File.WriteAllText(LogFile,
                    System.Text.Json.JsonSerializer.Serialize(_entries,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static void Clear()
        {
            _entries.Clear();
            try { if (File.Exists(LogFile)) File.Delete(LogFile); } catch { }
        }
    }

    public static class AdminWarning { public static bool Show { get; set; } = false; }

    // ═══════════════════════════════════════════════════════════════════════
    //  MAIN FORM
    // ═══════════════════════════════════════════════════════════════════════
    public class MainForm : Form
    {
        // ── Layout ─────────────────────────────────────────────────────────
        private Panel _topBar;
        private Panel _sidebar;
        private Panel _mainArea;
        private Panel _bottomBar;
        private Panel _logPanel;

        // ── Top bar ────────────────────────────────────────────────────────
        private Label _winVerBadge;
        private Label _adminBadge;

        // ── Sidebar ────────────────────────────────────────────────────────
        private static readonly string[] SidebarCategories =
        {
            "All", "Performance", "Privacy", "Responsiveness",
            "Gaming", "Network", "Bloatware", "Security", "Advanced", "History"
        };

        private static readonly Dictionary<string, string> CatEmoji = new()
        {
            ["All"]            = "🏠",
            ["Performance"]    = "⚡",
            ["Privacy"]        = "🔒",
            ["Responsiveness"] = "🖥",
            ["Gaming"]         = "🎮",
            ["Network"]        = "🌐",
            ["Bloatware"]      = "🗑",
            ["Advanced"]       = "⚠",
            ["Security"]       = "🔒",
            ["History"]        = "📋",
        };

        private string _activeCategory = "All";

        // ── Grid ───────────────────────────────────────────────────────────
        private FlowLayoutPanel          _tileGrid;
        private readonly List<TweakTile> _tiles = new();
        private HashSet<string>          _advancedKeys = new();

        // ── Bottom bar ─────────────────────────────────────────────────────
        private Label      _statusLabel;
        private Label      _selCountLabel;
        private Panel      _progOuter;
        private Panel      _progInner;
        private FlatButton _runBtn;
        private FlatButton _undoBtn;
        private FlatButton _clearBtn;
        private CheckBox   _restoreChk;

        // ── History ────────────────────────────────────────────────────────
        private Panel _histPanel;

        // ── Log ────────────────────────────────────────────────────────────
        private RichTextBox _logBox;

        // ── State ──────────────────────────────────────────────────────────
        private bool _isRunning = false;
        private int  _totalTweaks, _doneTweaks;

        public MainForm()
        {
            InitUI();
            PopulateGrid("All");
            UpdateSelCount();
            if (AdminWarning.Show) _adminBadge.Visible = true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  INIT
        // ─────────────────────────────────────────────────────────────────
        private void InitUI()
        {
            Text            = "Win11 Optimizer";
            Size            = new Size(1200, 760);
            MinimumSize     = new Size(960, 620);
            BackColor       = Theme.BG;
            ForeColor       = Theme.TEXT_PRI;
            Font            = new Font("Segoe UI", 9f);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;

            BuildTopBar();
            BuildSidebar();
            BuildMainArea();
            BuildBottomBar();
            BuildLogPanel();

            // Dock-based layout: Top -> Bottom -> Left -> Fill
            // This is the only reliable way to get FlowLayoutPanel wrapping right.
            _topBar.Dock     = DockStyle.Top;
            _bottomBar.Dock  = DockStyle.Bottom;
            _sidebar.Dock    = DockStyle.Left;
            _mainArea.Dock   = DockStyle.Fill;
            _logPanel.Dock   = DockStyle.None; // manually positioned inside LayoutAll

            // Add in correct dock order (Bottom before Fill, Top last wins)
            Controls.Add(_mainArea);
            Controls.Add(_sidebar);
            Controls.Add(_bottomBar);
            Controls.Add(_topBar);
            Controls.Add(_logPanel);
        }

        private void LayoutAll()
        {
            // Dock handles all the main panels. We only need to manually
            // position the log overlay panel at the bottom of the main area.
            if (_logPanel.Visible)
            {
                int logH  = _logPanel.Height;
                int sideW = _sidebar.Width;
                int top   = _topBar.Height;
                int botY  = ClientSize.Height - _bottomBar.Height - logH;
                int logW  = ClientSize.Width - sideW;
                _logPanel.SetBounds(sideW, botY, logW, logH);
                _logPanel.BringToFront();
                // Shrink main area to make room for log
                _mainArea.Padding = new Padding(0, 0, 0, logH);
            }
            else
            {
                _mainArea.Padding = new Padding(0);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  TOP BAR
        // ─────────────────────────────────────────────────────────────────
        private void BuildTopBar()
        {
            _topBar = new Panel { BackColor = Theme.SURFACE, Height = 60 };
            _topBar.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.BORDER);
                e.Graphics.DrawLine(p, 0, _topBar.Height - 1, _topBar.Width, _topBar.Height - 1);
            };

            var titleLbl = new Label
            {
                Text      = "⚡  Win11 Optimizer",
                Font      = new Font("Segoe UI Semibold", 13f),
                ForeColor = Theme.TEXT_PRI,
                AutoSize  = true,
                Location  = new Point(18, 17)
            };

            _winVerBadge = new Label
            {
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5f),
                Location  = new Point(270, 21),
                ForeColor = WinVersion.IsWin11 ? Theme.SUCCESS : Theme.WARNING,
                Text      = (WinVersion.IsWin11 ? "✔" : "⚠") + $"  {WinVersion.DisplayName}"
            };

            _adminBadge = new Label
            {
                Text      = "⚠  Not running as Administrator — some tweaks may fail",
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.WARNING,
                Location  = new Point(18, 40),
                Visible   = false
            };

            var ghLink = new LinkLabel
            {
                Text             = "⭐  github.com/ConnorCorn07/win11op",
                Font             = new Font("Segoe UI", 8.5f),
                AutoSize         = true,
                BackColor        = Theme.SURFACE,
                LinkColor        = Theme.TEXT_SEC,
                ActiveLinkColor  = Theme.ACCENT,
                VisitedLinkColor = Theme.TEXT_SEC,
                LinkBehavior     = LinkBehavior.HoverUnderline
            };
            ghLink.Click += (s, e) => Process.Start(new ProcessStartInfo
                { FileName = "https://github.com/ConnorCorn07/win11op", UseShellExecute = true });
            _topBar.SizeChanged += (s, e) =>
                ghLink.Location = new Point(_topBar.Width - ghLink.PreferredWidth - 18, 20);

            _topBar.Controls.AddRange(new Control[]
                { titleLbl, _winVerBadge, _adminBadge, ghLink });
        }

        // ─────────────────────────────────────────────────────────────────
        //  SIDEBAR
        // ─────────────────────────────────────────────────────────────────
        private void BuildSidebar()
        {
            _sidebar = new Panel { BackColor = Theme.SURFACE, Width = 200 };
            _sidebar.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.BORDER);
                e.Graphics.DrawLine(p, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
            };

            var hdr = new Label
            {
                Text      = "CATEGORIES",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.TEXT_SEC,
                AutoSize  = true,
                Location  = new Point(14, 14)
            };
            _sidebar.Controls.Add(hdr);

            int y = 38;
            foreach (var cat in SidebarCategories)
            {
                // Divider before History
                if (cat == "History")
                {
                    var div = new Panel
                    {
                        BackColor = Theme.BORDER,
                        Bounds    = new Rectangle(8, y + 2, 184, 1)
                    };
                    _sidebar.Controls.Add(div);
                    y += 10;
                }

                var btn = MakeSidebarBtn(cat);
                btn.SetBounds(8, y, 184, 36);
                _sidebar.Controls.Add(btn);
                y += 38;
            }

            y += 8;
            var selAll = new FlatButton("✔ Select All", Theme.ACCENT);
            selAll.SetBounds(8, y, 88, 28);
            selAll.Click += (s, e) => SetAllInView(true);

            var selNone = new FlatButton("✘ None", Theme.SURFACE2);
            selNone.SetBounds(104, y, 88, 28);
            selNone.Click += (s, e) => SetAllInView(false);

            _sidebar.Controls.AddRange(new Control[] { selAll, selNone });
        }

        private Button MakeSidebarBtn(string cat)
        {
            bool   active = _activeCategory == cat;
            string emoji  = CatEmoji.TryGetValue(cat, out var em) ? em : "📦";

            var btn = new Button
            {
                Text      = $"  {emoji}  {cat}",
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                BackColor = active ? Theme.ACCENT : Color.Transparent,
                ForeColor = active ? Color.White : Theme.TEXT_SEC,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand,
                Tag       = cat
            };
            btn.FlatAppearance.BorderSize           = 0;
            btn.FlatAppearance.MouseOverBackColor   = active
                ? Theme.ACCENT_HOV
                : Color.FromArgb(35, 35, 50);

            btn.Click += (s, e) =>
            {
                _activeCategory = cat;
                RefreshSidebar();
                if (cat == "History") ShowHistory();
                else                  PopulateGrid(cat);
            };
            return btn;
        }

        private void RefreshSidebar()
        {
            foreach (Control c in _sidebar.Controls)
            {
                if (c is Button btn && btn.Tag is string cat && CatEmoji.ContainsKey(cat))
                {
                    bool active = cat == _activeCategory;
                    btn.BackColor = active ? Theme.ACCENT : Color.Transparent;
                    btn.ForeColor = active ? Color.White : Theme.TEXT_SEC;
                    btn.FlatAppearance.MouseOverBackColor = active
                        ? Theme.ACCENT_HOV
                        : Color.FromArgb(35, 35, 50);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  MAIN AREA
        // ─────────────────────────────────────────────────────────────────
        private void BuildMainArea()
        {
            _mainArea = new Panel { BackColor = Theme.BG };

            // Dock = Fill gives FlowLayoutPanel a definite width from its
            // parent, which is what WrapContents needs to break rows correctly.
            _tileGrid = new FlowLayoutPanel
            {
                AutoScroll    = true,
                WrapContents  = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor     = Theme.BG,
                Padding       = new Padding(10),
                Dock          = DockStyle.Fill,
            };
            _tileGrid.HorizontalScroll.Enabled = false;
            _tileGrid.HorizontalScroll.Visible = false;

            _histPanel = new Panel
            {
                AutoScroll = true,
                BackColor  = Theme.BG,
                Padding    = new Padding(16),
                Dock       = DockStyle.Fill,
                Visible    = false
            };

            _mainArea.Controls.Add(_histPanel);
            _mainArea.Controls.Add(_tileGrid);
        }

        // ─────────────────────────────────────────────────────────────────
        //  GRID POPULATION  (mirrors PopulateApps from App Downloader)
        // ─────────────────────────────────────────────────────────────────
        private static readonly string[] CategoryOrder =
        {
            "Performance", "Privacy", "Responsiveness",
            "Gaming", "Network", "Bloatware", "Security", "Advanced"
        };

        private void PopulateGrid(string filter)
        {
            _histPanel.Visible = false;
            _tileGrid.Visible  = true;

            _tileGrid.SuspendLayout();
            _tileGrid.Controls.Clear();
            _tiles.Clear();

            IEnumerable<TweakEntry> source = filter == "All"
                ? TweakCatalog.All
                : TweakCatalog.All.Where(t => t.Category == filter);

            // Group by category, preserve defined order
            var groups = source
                .GroupBy(t => t.Category)
                .OrderBy(g => Array.IndexOf(CategoryOrder, g.Key));

            foreach (var group in groups)
            {
                string emoji = CatEmoji.TryGetValue(group.Key, out var em) ? em : "📦";

                // Section header — must be very wide so FlowLayoutPanel breaks before it
                var hdr = new SectionHeader(group.Key, emoji);
                _tileGrid.Controls.Add(hdr);

                foreach (var entry in group)
                {
                    var tile = new TweakTile(entry);
                    tile.IsChecked = entry.DefaultOn;

                    tile.CheckedChanged += (s, e_) => UpdateSelCount();

                    _tiles.Add(tile);
                    _tileGrid.Controls.Add(tile);
                }
            }

            _tileGrid.ResumeLayout(true);
            UpdateSelCount();
        }

        private void SetAllInView(bool check)
        {
            foreach (var t in _tiles) t.IsChecked = check;
            if (!check) _advancedKeys.Clear();
            UpdateSelCount();
        }

        private void UpdateSelCount()
        {
            int count = _tiles.Count(t => t.IsChecked);
            if (_selCountLabel == null) return;

            _selCountLabel.Text = count == 0
                ? "No tweaks selected"
                : $"{count} tweak{(count == 1 ? "" : "s")} selected";

            if (_undoBtn != null)
            {
                var cats = _tiles.Where(t => t.IsChecked).Select(t => t.Entry.Category).Distinct();
                _undoBtn.Enabled = cats.Any(c => TweakEngine.HasBackup(c));
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  HISTORY
        // ─────────────────────────────────────────────────────────────────
        private void ShowHistory()
        {
            _tileGrid.Visible  = false;
            _histPanel.Visible = true;
            BuildHistoryContent();
        }

        private void BuildHistoryContent()
        {
            _histPanel.Controls.Clear();
            int y = 0;

            var topRow = new Panel
            {
                Left      = 0, Top = y, Height = 44,
                Width     = _histPanel.ClientSize.Width,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            var titleLbl = new Label
            {
                Text      = "RUN HISTORY",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TEXT_SEC,
                AutoSize  = true,
                Location  = new Point(0, 13)
            };
            var clearBtn = new FlatButton("Clear History", Theme.DANGER)
            {
                Size      = new Size(120, 28),
                Anchor    = AnchorStyles.Right | AnchorStyles.Top,
                ForeColor = Color.White
            };
            topRow.SizeChanged += (s, e) => clearBtn.Location = new Point(topRow.Width - 124, 8);
            clearBtn.Location   = new Point(topRow.Width - 124, 8);
            clearBtn.Click += (s, e) =>
            {
                if (MessageBox.Show("Clear all run history?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                { ChangeLog.Clear(); BuildHistoryContent(); }
            };
            topRow.Controls.Add(titleLbl);
            topRow.Controls.Add(clearBtn);
            _histPanel.Controls.Add(topRow);
            y += 52;

            if (ChangeLog.Entries.Count == 0)
            {
                _histPanel.Controls.Add(new Label
                {
                    Text      = "No runs recorded yet. Apply some tweaks to start tracking history.",
                    Font      = new Font("Segoe UI", 10f),
                    ForeColor = Theme.TEXT_SEC,
                    AutoSize  = true,
                    Location  = new Point(0, y + 10)
                });
                return;
            }

            foreach (var entry in ChangeLog.Entries)
            {
                var card = new Panel
                {
                    Left      = 0, Top = y,
                    Width     = _histPanel.ClientSize.Width - 4,
                    BackColor = Theme.SURFACE,
                    Padding   = new Padding(14, 10, 14, 10),
                    Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                card.Paint += (s, e) =>
                {
                    using var pen    = new Pen(Theme.BORDER);
                    using var stripe = new SolidBrush(Theme.ACCENT);
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                    e.Graphics.FillRectangle(stripe, 0, 0, 3, card.Height);
                };

                var tsLbl  = new Label { Text = entry.Timestamp,                              Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.ACCENT,   AutoSize = true, Location = new Point(14, 10), BackColor = Color.Transparent };
                var osLbl  = new Label { Text = entry.WindowsVer,                             Font = new Font("Segoe UI", 8.5f),               ForeColor = Theme.TEXT_SEC, AutoSize = true, Location = new Point(14, 28), BackColor = Color.Transparent };
                var catLbl = new Label { Text = $"Categories: {entry.Categories}",            Font = new Font("Segoe UI", 9f),                 ForeColor = Theme.TEXT_PRI, AutoSize = true, Location = new Point(14, 46), BackColor = Color.Transparent };
                Color sc   = entry.Failed == 0 ? Theme.SUCCESS : Theme.WARNING;
                var stLbl  = new Label { Text = $"✔ {entry.Passed} succeeded   ✘ {entry.Failed} failed" + (entry.RestorePoint ? "   🛡 Restore Point" : ""),
                                         Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = sc, AutoSize = true, Location = new Point(14, 64), BackColor = Color.Transparent };

                card.Controls.Add(tsLbl); card.Controls.Add(osLbl);
                card.Controls.Add(catLbl); card.Controls.Add(stLbl);

                int cardH = 86;
                if (entry.Details.Count > 0)
                {
                    string dt = string.Join("  ·  ", entry.Details.Take(8))
                        + (entry.Details.Count > 8 ? $"  … +{entry.Details.Count - 8} more" : "");
                    var dL = new Label
                    {
                        Text      = dt,
                        Font      = new Font("Consolas", 7.5f),
                        ForeColor = Theme.TEXT_SEC,
                        AutoSize  = false,
                        Width     = card.Width - 30,
                        Height    = 30,
                        Location  = new Point(14, 82),
                        BackColor = Color.Transparent
                    };
                    card.Controls.Add(dL);
                    cardH = 118;
                }

                card.Height = cardH;
                _histPanel.Controls.Add(card);
                y += cardH + 8;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  BOTTOM BAR
        // ─────────────────────────────────────────────────────────────────
        private void BuildBottomBar()
        {
            _bottomBar = new Panel { BackColor = Theme.SURFACE, Height = 130 };
            _bottomBar.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.BORDER);
                e.Graphics.DrawLine(p, 0, 0, _bottomBar.Width, 0);
            };

            _progOuter = new Panel
            {
                BackColor = Theme.BORDER,
                Location  = new Point(16, 12),
                Size      = new Size(500, 6)
            };
            _progInner = new Panel
            {
                BackColor = Theme.ACCENT,
                Location  = Point.Empty,
                Size      = new Size(0, 6)
            };
            _progOuter.Controls.Add(_progInner);

            _statusLabel = new Label
            {
                Text      = "Ready",
                ForeColor = Theme.TEXT_SEC,
                AutoSize  = true,
                Location  = new Point(16, 24)
            };

            _selCountLabel = new Label
            {
                Text      = "No tweaks selected",
                ForeColor = Theme.TEXT_SEC,
                AutoSize  = true,
                Location  = new Point(16, 46)
            };

            _restoreChk = new CheckBox
            {
                Text      = "🛡 Create Restore Point before running",
                ForeColor = Theme.TEXT_SEC,
                BackColor = Color.Transparent,
                Checked   = true,
                AutoSize  = true,
                Location  = new Point(16, 86),
                FlatStyle = FlatStyle.Flat
            };
            _restoreChk.FlatAppearance.BorderColor        = Theme.BORDER;
            _restoreChk.FlatAppearance.CheckedBackColor   = Theme.ACCENT;
            _restoreChk.FlatAppearance.MouseOverBackColor = Theme.SURFACE;

            _undoBtn = new FlatButton("↩ Undo Selected", Theme.SURFACE2)
            {
                Size    = new Size(150, 36),
                Enabled = false
            };
            _undoBtn.Click += OnUndoClicked;

            _clearBtn = new FlatButton("Clear Selection", Theme.SURFACE2)
                { Size = new Size(130, 36) };
            _clearBtn.Click += (s, e) => SetAllInView(false);

            _runBtn = new FlatButton("⚡  Run Selected", Theme.ACCENT)
            {
                Size      = new Size(160, 36),
                Font      = new Font("Segoe UI Semibold", 10f),
                ForeColor = Color.White
            };
            _runBtn.Click += OnRunClicked;

            var logToggle = new FlatButton("📋 Log", Theme.SURFACE2)
                { Size = new Size(70, 26) };
            logToggle.Click += (s, e) => ToggleLog();

            _bottomBar.SizeChanged += (s, e) =>
            {
                int r = _bottomBar.Width - 16;
                _runBtn.Location   = new Point(r - 160, 82);
                _clearBtn.Location = new Point(r - 300, 82);
                _undoBtn.Location  = new Point(r - 460, 82);
                logToggle.Location = new Point(r - 82,  12);
                _progOuter.Width   = Math.Max(200, r - 16);
            };

            _bottomBar.Controls.AddRange(new Control[]
            {
                _progOuter, _statusLabel, _selCountLabel,
                _restoreChk, _undoBtn, _clearBtn, _runBtn, logToggle
            });
        }

        // ─────────────────────────────────────────────────────────────────
        //  LOG PANEL
        // ─────────────────────────────────────────────────────────────────
        private void BuildLogPanel()
        {
            _logBox = new RichTextBox
            {
                BackColor   = Color.FromArgb(10, 10, 14),
                ForeColor   = Color.FromArgb(100, 220, 100),
                BorderStyle = BorderStyle.None,
                ReadOnly    = true,
                Font        = new Font("Consolas", 8.5f),
                Dock        = DockStyle.Fill,
                ScrollBars  = RichTextBoxScrollBars.Vertical
            };

            _logPanel = new Panel
            {
                BackColor = Color.FromArgb(10, 10, 14),
                Visible   = false,
                Height    = 180
            };

            var hdr = new Panel { Dock = DockStyle.Top, Height = 26, BackColor = Theme.SURFACE };
            var hdrLbl = new Label
            {
                Text      = "OUTPUT LOG",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.TEXT_SEC,
                AutoSize  = true,
                Location  = new Point(10, 6),
                BackColor = Color.Transparent
            };
            var closeBtn = new FlatButton("✕", Theme.SURFACE)
            {
                Size      = new Size(24, 24),
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Theme.TEXT_SEC
            };
            closeBtn.Click += (s, e) => { _logPanel.Visible = false; LayoutAll(); };
            hdr.SizeChanged += (s, e) => closeBtn.Location = new Point(hdr.Width - 26, 1);
            hdr.Controls.AddRange(new Control[] { hdrLbl, closeBtn });

            _logPanel.Controls.Add(_logBox);
            _logPanel.Controls.Add(hdr);
        }

        private void ToggleLog()
        {
            _logPanel.Visible = !_logPanel.Visible;
            LayoutAll();
        }

        // ─────────────────────────────────────────────────────────────────
        //  RUN
        // ─────────────────────────────────────────────────────────────────
        private async void OnRunClicked(object sender, EventArgs e)
        {
            if (_isRunning) return;

            var selected = _tiles.Where(t => t.IsChecked).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select at least one tweak to run.",
                    "Nothing selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _isRunning      = true;
            _runBtn.Enabled = false;
            _runBtn.Text    = "⏳ Running...";
            TweakEngine.ClearResults();

            // Restore point
            bool rpCreated = false;
            if (_restoreChk.Checked)
            {
                SetStatus("Creating System Restore Point…", Theme.WARNING);
                AppendLog("🛡 Creating System Restore Point…");
                bool ok = await Task.Run(() =>
                    TweakEngine.CreateRestorePoint("Win11Optimizer — before tweaks"));
                AppendLog(ok ? "🛡 Restore Point created." : "⚠ Restore Point failed or skipped.");
                rpCreated = ok;
            }

            _totalTweaks = selected.Count;
            _doneTweaks  = 0;
            SetProgress(0, _totalTweaks);

            var catNames   = new List<string>();
            var logDetails = new List<string>();
            int prevCount  = 0;

            void LogTweak(string tileName)
            {
                var all = TweakEngine.GetResults();
                var sec = all.Skip(prevCount).ToList();
                prevCount = all.Count;
                foreach (var r in sec)
                {
                    AppendLog(r.Success ? $"  ✔  {r.Name}" : $"  ✘  {r.Name}: {r.Error}");
                    logDetails.Add((r.Success ? "✔ " : "✘ ") + r.Name);
                }
            }

            foreach (var t in selected) t.SetStatus(TileStatus.Running);

            await Task.Run(() =>
            {
                // Group selected tiles by category order for logical sequencing,
                // but only apply the individual tweaks that were actually checked.
                var ordered = selected
                    .OrderBy(t => Array.IndexOf(new[] {
                        "Performance","Privacy","Responsiveness",
                        "Gaming","Network","Bloatware","Security","Advanced"
                    }, t.Entry.Category))
                    .ToList();

                string lastCat = null;
                foreach (var tile in ordered)
                {
                    var entry = tile.Entry;
                    if (entry.Category != lastCat)
                    {
                        if (lastCat != null)
                            Invoke(new Action(() => AppendLog("└─────────────────────────────────────")));
                        Invoke(new Action(() => AppendLog($"┌─ {entry.Category.ToUpper()}")));
                        if (!catNames.Contains(entry.Category))
                            catNames.Add(entry.Category);
                        lastCat = entry.Category;
                    }

                    Invoke(new Action(() =>
                    {
                        _doneTweaks++;
                        SetProgress(_doneTweaks, _totalTweaks);
                        AppendLog($"  → {entry.Name}…");
                    }));

                    // Bloatware tiles share one pattern-based engine call
                    if (entry.Category == "Bloatware")
                    {
                        TweakEngine.ApplyBloatwareTweak(entry.TweakKey);
                    }
                    else if (entry.IsAdvanced && entry.AdvancedKey != null)
                    {
                        TweakEngine.ApplyAdvancedTweak(entry.AdvancedKey);
                    }
                    else
                    {
                        TweakEngine.ApplyTweak(entry.TweakKey);
                    }

                    Invoke(new Action(() => LogTweak(entry.Name)));
                }
                if (lastCat != null)
                    Invoke(new Action(() => AppendLog("└─────────────────────────────────────")));
            });

            var results = TweakEngine.GetResults();
            int pass = results.Count(r => r.Success);
            int fail = results.Count(r => !r.Success);

            foreach (var t in selected) t.SetStatus(TileStatus.Done);

            SetProgress(_totalTweaks, _totalTweaks);
            SetStatus($"Complete — {pass} succeeded, {fail} failed.",
                fail == 0 ? Theme.SUCCESS : Theme.WARNING);
            AppendLog($"══ COMPLETE: {pass} succeeded, {fail} failed. Reboot recommended. ══");

            ChangeLog.AddEntry(new ChangeLog.RunEntry
            {
                Categories   = string.Join(", ", catNames),
                Passed       = pass,
                Failed       = fail,
                RestorePoint = rpCreated,
                Details      = logDetails
            });

            _runBtn.Text    = "⚡  Run Selected";
            _runBtn.Enabled = true;
            _isRunning      = false;
            UpdateSelCount();
            PromptReboot();
        }

        // ─────────────────────────────────────────────────────────────────
        //  UNDO
        // ─────────────────────────────────────────────────────────────────
        private async void OnUndoClicked(object sender, EventArgs e)
        {
            var undoCats = _tiles
                .Where(t => t.IsChecked && TweakEngine.HasBackup(t.Entry.Category))
                .Select(t => t.Entry.Category)
                .Distinct()
                .ToList();

            if (undoCats.Count == 0) return;
            _undoBtn.Enabled = false;

            foreach (var cat in undoCats)
            {
                AppendLog($"↩ Undoing {cat}…");
                List<TweakEngine.TweakResult> res = null;

                await Task.Run(() =>
                {
                    res = cat switch
                    {
                        "Performance"    => TweakEngine.UndoPerformanceTweaks(),
                        "Privacy"        => TweakEngine.UndoPrivacyTweaks(),
                        "Responsiveness" => TweakEngine.UndoResponsivenessTweaks(),
                        "Gaming"         => TweakEngine.UndoGamingTweaks(),
                        "Network"        => TweakEngine.UndoNetworkTweaks(),
                        "Advanced"       => TweakEngine.UndoAdvancedTweaks(),
                        "Security"       => TweakEngine.UndoSecurityTweaks(),
                        _                => new List<TweakEngine.TweakResult>()
                    };
                });

                AppendLog($"  ↩ {cat} done — {res.Count(r => r.Success)} restored.");
            }

            SetStatus("Undo complete. Reboot recommended.", Theme.SUCCESS);
            UpdateSelCount();
        }

        // ─────────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────────
        private void SetStatus(string msg, Color col = default)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetStatus(msg, col))); return; }
            _statusLabel.Text      = msg;
            _statusLabel.ForeColor = col == default ? Theme.TEXT_SEC : col;
        }

        private void SetProgress(int done, int total)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetProgress(done, total))); return; }
            int w = total == 0 ? 0 : (int)((double)_progOuter.Width * done / total);
            _progInner.Width = w;
        }

        private void AppendLog(string msg)
        {
            if (_logBox.InvokeRequired)
            { _logBox.Invoke(new Action(() => AppendLog(msg))); return; }
            _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            _logBox.ScrollToCaret();
        }

        private void PromptReboot()
        {
            var r = MessageBox.Show(
                "Some tweaks require a reboot to take full effect.\n\nWould you like to reboot now?",
                "Reboot Required", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (r == DialogResult.Yes)
                Process.Start(new ProcessStartInfo
                {
                    FileName        = "shutdown.exe",
                    Arguments       = "/r /t 10 /c \"Win11 Optimizer: Rebooting to apply tweaks.\"",
                    UseShellExecute = false,
                    CreateNoWindow  = true
                });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TILE STATUS ENUM
    // ═══════════════════════════════════════════════════════════════════════
    public enum TileStatus { None, Running, Done }

    // ═══════════════════════════════════════════════════════════════════════
    //  TWEAK TILE  (exact mirror of AppTile from App Downloader)
    // ═══════════════════════════════════════════════════════════════════════
    public class TweakTile : Panel
    {
        private bool       _checked;
        private TileStatus _status      = TileStatus.None;
        private string     _statusText  = "";
        private Color      _statusColor = Theme.TEXT_SEC;

        public TweakEntry Entry { get; }

        public event EventHandler CheckedChanged;

        public bool IsChecked
        {
            get => _checked;
            set
            {
                _checked  = value;
                BackColor = value ? Theme.SURFACE2 : Theme.SURFACE;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // Per-category accent colour
        private static readonly Dictionary<string, Color> CatAccent = new()
        {
            ["Performance"]    = Color.FromArgb( 99, 102, 241),
            ["Privacy"]        = Color.FromArgb(168,  85, 247),
            ["Responsiveness"] = Color.FromArgb(  6, 182, 212),
            ["Gaming"]         = Color.FromArgb( 34, 197,  94),
            ["Network"]        = Color.FromArgb(251, 191,  36),
            ["Bloatware"]      = Color.FromArgb(239,  68,  68),
            ["Advanced"]       = Color.FromArgb(249, 115,  22),
            ["Security"]       = Color.FromArgb( 16, 185, 129),   // emerald green
        };

        public TweakTile(TweakEntry entry)
        {
            Entry     = entry;
            Size      = new Size(230, 130);
            BackColor = Theme.SURFACE;
            Margin    = new Padding(5);
            Cursor    = Cursors.Hand;

            Color accent = CatAccent.TryGetValue(entry.Category, out var ac) ? ac : Theme.ACCENT;

            // ── Paint ──────────────────────────────────────────────────────
            Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Border
                using var borderPen = new Pen(_checked ? accent : Theme.BORDER, 1.5f);
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

                // Left accent bar
                using var accentBr = new SolidBrush(accent);
                g.FillRectangle(accentBr, 0, 0, 3, Height);

                // Checkbox tick
                if (_checked)
                {
                    g.FillRectangle(accentBr, Width - 22, 6, 16, 16);
                    using var wp = new Pen(Color.White, 2f);
                    g.DrawLines(wp, new[]
                    {
                        new Point(Width - 19, 14),
                        new Point(Width - 15, 18),
                        new Point(Width - 9,   9)
                    });
                }

                // Status text
                if (!string.IsNullOrEmpty(_statusText))
                {
                    using var sf = new Font("Segoe UI", 7.5f);
                    using var sb = new SolidBrush(_statusColor);
                    g.DrawString(_statusText, sf, sb, new PointF(12, Height - 18));
                }
            };

            // ── Icon ───────────────────────────────────────────────────────
            var iconLbl = new Label
            {
                Text      = entry.Icon,
                Font      = new Font("Segoe UI Emoji", 18f),
                AutoSize  = false,
                Size      = new Size(40, 40),
                Location  = new Point(8, 8),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Name ───────────────────────────────────────────────────────
            var nameLbl = new Label
            {
                Text         = entry.Name,
                Font         = new Font("Segoe UI Semibold", 9f),
                ForeColor    = Theme.TEXT_PRI,
                AutoSize     = false,
                Size         = new Size(170, 40),
                Location     = new Point(54, 8),
                BackColor    = Color.Transparent,
                AutoEllipsis = true,
                UseMnemonic  = false
            };

            // ── Description ────────────────────────────────────────────────
            var descLbl = new Label
            {
                Text      = entry.Description,
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Theme.TEXT_SEC,
                AutoSize  = false,
                Size      = new Size(218, 38),
                Location  = new Point(10, 74),
                BackColor = Color.Transparent
            };

            // ── Category badge  (mirrors "winget" / "direct" badge in AppTile) ─
            Color badgeBg = Color.FromArgb(30,  accent.R, accent.G, accent.B);
            Color badgeFg = Color.FromArgb(180, accent.R, accent.G, accent.B);
            var catBadge = new Label
            {
                Text      = entry.IsAdvanced ? "⚠ advanced" : entry.Category.ToLower(),
                Font      = new Font("Segoe UI", 7f),
                ForeColor = badgeFg,
                BackColor = badgeBg,
                AutoSize  = true,
                Location  = new Point(10, 52),
                Padding   = new Padding(3, 1, 3, 1)
            };

            Controls.AddRange(new Control[] { iconLbl, nameLbl, descLbl, catBadge });

            // ── Toggle on click (all children bubble up) ───────────────────
            void Toggle(object s, EventArgs ev) => IsChecked = !_checked;
            base.Click    += Toggle;
            iconLbl.Click += Toggle;
            nameLbl.Click += Toggle;
            descLbl.Click += Toggle;
            catBadge.Click += Toggle;

            MouseEnter += (s, ev) => { if (!_checked) BackColor = Color.FromArgb(26, 26, 38); };
            MouseLeave += (s, ev) => { if (!_checked) BackColor = Theme.SURFACE; };
        }

        public void SetStatus(TileStatus status)
        {
            _status = status;
            switch (status)
            {
                case TileStatus.Running:
                    _statusText  = "⏳ Running...";
                    _statusColor = Theme.WARNING;
                    break;
                case TileStatus.Done:
                    _statusText  = "✔ Done";
                    _statusColor = Theme.SUCCESS;
                    break;
                default:
                    _statusText = "";
                    break;
            }
            Invalidate();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SECTION HEADER  (exact mirror of App Downloader SectionHeader)
    // ═══════════════════════════════════════════════════════════════════════
    public class SectionHeader : Panel
    {
        public SectionHeader(string title, string emoji)
        {
            // Height fixed; width is set dynamically when added to the
            // FlowLayoutPanel via ParentChanged so it never exceeds the
            // panel's client width (which would create a horizontal scrollbar).
            Height    = 44;
            Margin    = new Padding(5, 16, 5, 4);
            BackColor = Color.Transparent;

            var bar = new Panel
            {
                BackColor = Theme.ACCENT,
                Size      = new Size(3, 26),
                Location  = new Point(4, 9)
            };

            var lbl = new Label
            {
                Text      = $"{emoji}  {title}",
                Font      = new Font("Segoe UI Semibold", 11f),
                ForeColor = Theme.TEXT_PRI,
                AutoSize  = true,
                Location  = new Point(14, 10),
                BackColor = Color.Transparent
            };

            Paint += (s, e) =>
            {
                int lineY = Height - 6;
                using var pen = new Pen(Color.FromArgb(45, 45, 65), 1);
                e.Graphics.DrawLine(pen, lbl.Right + 10, lineY, Width - 20, lineY);
            };

            Controls.AddRange(new Control[] { bar, lbl });

            // Fit width to parent whenever we are added to the FlowLayoutPanel
            ParentChanged += (s, e) => FitToParent();
            // Also refit if the parent resizes (e.g. window resize)
            ParentChanged += (s, e) =>
            {
                if (Parent != null)
                    Parent.SizeChanged += (ps, pe) => FitToParent();
            };
        }

        private void FitToParent()
        {
            if (Parent == null) return;
            // Account for the FlowLayoutPanel padding and our own margin
            int w = Parent.ClientSize.Width
                  - Parent.Padding.Horizontal
                  - Margin.Horizontal
                  - SystemInformation.VerticalScrollBarWidth
                  - 2;
            if (w > 0) Width = w;
            Invalidate();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FLAT BUTTON
    // ═══════════════════════════════════════════════════════════════════════
    public class FlatButton : Button
    {
        public FlatButton(string text, Color bg)
        {
            Text      = text;
            BackColor = bg;
            ForeColor = Theme.TEXT_PRI;
            FlatStyle = FlatStyle.Flat;
            Cursor    = Cursors.Hand;
            FlatAppearance.BorderSize           = 0;
            FlatAppearance.MouseOverBackColor   = ControlPaint.Light(bg, 0.08f);
            FlatAppearance.MouseDownBackColor   = ControlPaint.Dark(bg, 0.08f);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ENTRY POINT
    // ═══════════════════════════════════════════════════════════════════════
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException +=
                    (s, e) => LogCrash(e.Exception);
                AppDomain.CurrentDomain.UnhandledException +=
                    (s, e) => LogCrash(e.ExceptionObject as Exception);

                WinVersion.Detect();
                ChangeLog.Load();

                if (!IsAdmin())
                {
                    var choice = MessageBox.Show(
                        "Win11 Optimizer needs Administrator privileges to apply " +
                        "registry and service tweaks.\n\n" +
                        "Would you like to relaunch as Administrator now?",
                        "Administrator Required",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button1);

                    if (choice == DialogResult.Yes)
                    {
                        try { Process.Start(new ProcessStartInfo
                            { FileName = Application.ExecutablePath,
                              UseShellExecute = true, Verb = "runas" }); }
                        catch { }
                        return;
                    }
                    AdminWarning.Show = true;
                }

                Application.Run(new MainForm());
            }
            catch (Exception ex) { LogCrash(ex); }
        }

        static bool IsAdmin()
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
                MessageBox.Show($"Crash logged to:\n{lp}\n\n{ex?.Message}",
                    "Win11Optimizer — Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}