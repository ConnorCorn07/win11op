using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public static class Theme
    {
        public static readonly Color BG          = Color.FromArgb(  8,   8,  18);  // --bg:  #080812
        public static readonly Color SURFACE     = Color.FromArgb( 19,  18,  42);  // --surface: #13122a
        public static readonly Color SURFACE2    = Color.FromArgb( 26,  24,  53);  // --surface2: #1a1835
        public static readonly Color CARD        = Color.FromArgb( 16,  15,  34);  // --card: #100f22

        public static readonly Color ACCENT      = Color.FromArgb(245, 200,  66);  // --accent: #f5c842
        public static readonly Color ACCENT_HOV  = Color.FromArgb(255, 215,  90);  // lighter gold on hover
        public static readonly Color ACCENT_GLOW = Color.FromArgb( 40,  30,   8);  // subtle warm tint when selected

        public static readonly Color METEOR      = Color.FromArgb(244,  81,  30);  // --meteor: #f4511e

        public static readonly Color SKY_DEEP    = Color.FromArgb( 43,  45, 184);  // --sky-deep: #2b2db8
        public static readonly Color SKY_PURPLE  = Color.FromArgb(124,  58, 237);  // --sky-purple: #7c3aed

        public static readonly Color SUCCESS     = Color.FromArgb( 34, 197,  94);  // green
        public static readonly Color WARNING     = Color.FromArgb(251, 191,  36);  // amber
        public static readonly Color DANGER      = Color.FromArgb(239,  68,  68);  // red

        public static readonly Color TEXT_PRI    = Color.FromArgb(240, 238, 252);  // --text: #f0eefc
        public static readonly Color TEXT_SEC    = Color.FromArgb(106, 103, 155);  // between --text-dim and --muted
        public static readonly Color TEXT_DIM    = Color.FromArgb(160, 157, 192);  // --text-dim: #a09dc0

        public static readonly Color BORDER      = Color.FromArgb( 42,  40,  80);  // --border: #2a2850
        public static readonly Color BORDER2     = Color.FromArgb( 61,  58, 112);  // --border2: #3d3a70

        public static readonly Color ACCENT_TEXT = Color.FromArgb(  8,   8,  18);  // dark text on gold buttons
    }

    public class TweakEntry
    {
        public string Name        { get; set; }
        public string Description { get; set; }
        public string Category    { get; set; }
        public string Icon        { get; set; }
        public bool   IsAdvanced  { get; set; }
        public bool   DefaultOn   { get; set; } = true;
        public string AdvancedKey { get; set; }
        public string TweakKey    { get; set; }
        public string WhatItChanges { get; set; }         // What this tweak actually changes — shown in hover tooltip
    }

    public static class TweakCatalog
    {
        // Factory shorthand — keeps the list readable
        private static TweakEntry E(string cat, string icon, bool on, string key,
                                    string name, string desc, string what)
            => new TweakEntry { Category=cat, Icon=icon, DefaultOn=on,
                                TweakKey=key, Name=name, Description=desc, WhatItChanges=what };
        private static TweakEntry Adv(string icon, string advKey, string tweakKey,
                                      string name, string desc, string what)
            => new TweakEntry { Category="Advanced", Icon=icon, DefaultOn=false,
                                IsAdvanced=true, AdvancedKey=advKey, TweakKey=tweakKey,
                                Name=name, Description=desc, WhatItChanges=what };

        public static readonly List<TweakEntry> All = new List<TweakEntry>
        {
            E("Performance","⚡",true,"Perf_PowerPlan",
                "High Performance Power Plan",
                "Switches power plan to maximum performance mode",
                "Runs: powercfg -setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"),
            E("Performance","🔋",true,"Perf_PowerThrottle",
                "Disable Power Throttling",
                "Prevents Windows throttling background CPU usage",
                "Sets HKLM\\...\\PowerThrottling → PowerThrottlingOff = 1"),
            E("Performance","🗂",true,"Perf_SysMain",
                "Disable SysMain (Superfetch)",
                "Stops preloading rarely-used apps into RAM",
                "sc config SysMain start=disabled + net stop SysMain\nSysMain preloads apps it predicts you'll use, consuming RAM and disk I/O. On SSDs the benefit is negligible."),
            E("Performance","🔍",true,"Perf_WSearch",
                "Disable Windows Search Indexer",
                "Removes background disk I/O from search indexing",
                "sc config WSearch start=disabled + net stop WSearch\nKills the background indexing service. Start menu search still works, just slower on first query."),
            E("Performance","⏱",true,"Perf_StartupDelay",
                "Remove Startup Delay",
                "Eliminates the artificial Explorer startup pause",
                "Sets HKCU\\...\\Explorer\\Serialize → StartupDelayInMSec = 0"),
            E("Performance","🖼",true,"Perf_VisualFX",
                "Visual Effects: Best Performance",
                "Turns off animations, shadows and fancy rendering",
                "Sets HKCU\\...\\VisualEffects → VisualFXSetting = 2"),
            E("Performance","📁",true,"Perf_NtfsLastAccess",
                "Disable NTFS Last-Access Timestamps",
                "Reduces filesystem writes on every file read",
                "Runs: fsutil behavior set disablelastaccess 1"),
            E("Performance","📄",true,"Perf_8Dot3",
                "Disable 8.3 Filenames",
                "Removes legacy short filename generation on NTFS",
                "Runs: fsutil behavior set disable8dot3 1"),
            E("Performance","💤",true,"Perf_Hibernate",
                "Disable Hibernation",
                "Frees several GB of disk space, speeds shutdown",
                "Runs: powercfg -h off"),
            E("Performance","🧠",true,"Perf_MemCompression",
                "Disable Memory Compression",
                "Reduces CPU overhead when RAM is under pressure",
                "Runs: Disable-MMAgent -MemoryCompression (PowerShell)"),
            E("Performance","⏰",true,"Perf_TimerRes",
                "Set Timer Resolution to 0.5ms",
                "Calls timeBeginPeriod(1) + registry key for sub-ms scheduler ticks",
                "Calls timeBeginPeriod(1) via P/Invoke + sets GlobalTimerResolutionRequests = 1"),

            E("Privacy","📡",true,"Priv_Telemetry",
                "Disable Telemetry",
                "Blocks Microsoft data collection at the registry level",
                "Sets AllowTelemetry = 0 in both machine and policy DataCollection keys"),
            E("Privacy","🛑",true,"Priv_DiagTrack",
                "Disable DiagTrack Service",
                "Stops the Connected User Experiences telemetry service",
                "Disables services: DiagTrack, dmwappushservice, RetailDemo, WerSvc"),
            E("Privacy","📢",true,"Priv_AdvertisingId",
                "Disable Advertising ID",
                "Prevents apps from accessing your ad tracking ID",
                "Sets AdvertisingInfo → Enabled = 0 (user) + DisabledByGroupPolicy = 1 (machine)"),
            E("Privacy","🔎",true,"Priv_BingStart",
                "Disable Bing in Start Menu",
                "Removes web search results from the Start search bar",
                "Sets DisableSearchBoxSuggestions = 1 + BingSearchEnabled = 0"),
            E("Privacy","🎙",true,"Priv_Cortana",
                "Disable Cortana Consent",
                "Turns off Cortana data collection consent flag",
                "Sets CortanaConsent = 0 in HKCU\\...\\Search"),
            E("Privacy","📊",true,"Priv_ActivityFeed",
                "Disable Activity Feed",
                "Stops Windows logging app and file activity history",
                "Sets EnableActivityFeed = 0, PublishUserActivities = 0, UploadUserActivities = 0"),
            E("Privacy","📍",true,"Priv_Location",
                "Disable Location Tracking",
                "Blocks apps from accessing your physical location",
                "Sets DisableLocation = 1 in LocationAndSensors policy key"),
            E("Privacy","📷",true,"Priv_Camera",
                "Block App Camera Access",
                "Prevents UWP apps from using the webcam by default",
                "Sets LetAppsAccessCamera = 2 (Force Deny) in AppPrivacy policy"),
            E("Privacy","⚠",true,"Priv_WER",
                "Disable Windows Error Reporting",
                "Stops crash dumps and reports being sent to Microsoft",
                "Sets WER\\Disabled = 1 in Windows Error Reporting policy key"),
            E("Privacy","🛡",true,"Priv_SmartScreen",
                "Disable SmartScreen (Explorer)",
                "Removes SmartScreen cloud checks in File Explorer",
                "Sets EnableSmartScreen = 0 in Windows System policy"),
            E("Privacy","🗓",true,"Priv_TelemetryTasks",
                "Disable Scheduled Telemetry Tasks",
                "Kills CEIP, AppraiserV2, Proxy and DiskDiag data tasks",
                "Disables 6 scheduled tasks under Microsoft\\Windows\\Application Experience, Autochk, CEIP, DiskDiagnostic"),
            E("Privacy","👁",true,"Priv_AppTracking",
                "Disable App Launch Tracking",
                "Stops Windows logging which apps you open and when",
                "Sets Start_TrackProgs = 0 in Explorer Advanced"),
            E("Privacy","💬",true,"Priv_Feedback",
                "Disable Feedback Requests",
                "Prevents Windows asking you to rate/review features",
                "Sets NumberOfSIUFInPeriod = 0 in HKCU\\...\\Siuf\\Rules"),
            E("Privacy","💬",true,"Priv_ChatIcon",
                "Disable Chat / Teams Taskbar Icon",
                "Removes the Teams/Chat pinned icon from the taskbar",
                "Sets TaskbarMn = 0 in Explorer Advanced"),
            E("Privacy","🤖",true,"Priv_Recall",
                "Disable Windows Recall",
                "Kills AI screenshot feature on Copilot+ PCs (no-op otherwise)",
                "Sets DisableAIDataAnalysis = 1 in both machine and user WindowsAI policy keys"),
            E("Privacy","🚫",true,"Priv_HostsBlock",
                "Block Telemetry Hosts",
                "Adds 35 Microsoft telemetry domains to the hosts file (0.0.0.0)",
                "Appends 35 entries to C:\\Windows\\System32\\drivers\\etc\\hosts"),

            E("Responsiveness","🖱",true,"Resp_MenuDelay",
                "Instant Menu Show",
                "Sets menu open delay to 0 ms for snappier menus",
                "Sets MenuShowDelay = 0 in HKCU\\Control Panel\\Desktop"),
            E("Responsiveness","⚡",true,"Resp_AppKill",
                "Fast App Kill Timeout",
                "Reduces wait time before force-killing frozen apps",
                "Sets WaitToKillAppTimeout = 2000ms, HungAppTimeout = 1000ms"),
            E("Responsiveness","⏹",true,"Resp_ServiceKill",
                "Fast Service Kill Timeout",
                "Cuts shutdown wait for slow-stopping services",
                "Sets WaitToKillServiceTimeout = 2000ms (machine-wide)"),
            E("Responsiveness","🔚",true,"Resp_AutoEndTasks",
                "Auto End Tasks on Shutdown",
                "Automatically kills hung apps instead of prompting",
                "Sets AutoEndTasks = 1 in HKCU\\Control Panel\\Desktop"),
            E("Responsiveness","⏱",true,"Resp_PlatformTick",
                "Platform Tick (High-Res Timer)",
                "Forces constant-rate high-resolution system timer",
                "Runs: bcdedit /set useplatformtick yes"),
            E("Responsiveness","💡",true,"Resp_WinTips",
                "Disable Windows Tips",
                "Stops the 'Did you know...' popups and suggestions",
                "Sets SoftLandingEnabled = 0 in ContentDeliveryManager"),
            E("Responsiveness","📰",true,"Resp_SuggestedContent",
                "Disable Suggested Content",
                "Removes app install suggestions from the Start menu",
                "Sets SubscribedContent-338389Enabled = 0 in ContentDeliveryManager"),

            E("Gaming","🖥",false,"Game_HAGS",
                "Enable HAGS",
                "Hardware-Accelerated GPU Scheduling — reduces GPU latency",
                "Sets HwSchMode = 2 in HKLM\\...\\GraphicsDrivers"),
            E("Gaming","🎮",false,"Game_GameMode",
                "Enable Game Mode",
                "Tells Windows to prioritize foreground game processes",
                "Sets AllowAutoGameMode = 1, AutoGameModeEnabled = 1 in GameBar registry"),
            E("Gaming","🖱",false,"Game_MouseAccel",
                "Disable Mouse Acceleration",
                "Removes pointer precision for 1:1 raw mouse input",
                "Sets MouseSpeed = 0, MouseThreshold1 = 0, MouseThreshold2 = 0"),
            E("Gaming","⚡",false,"Game_CPUPriority",
                "CPU Foreground Priority Boost",
                "Increases CPU time slice for the active window/game",
                "Sets Win32PrioritySeparation = 38 in PriorityControl"),
            E("Gaming","📹",false,"Game_DVR",
                "Disable Game DVR / Capture",
                "Turns off Xbox Game Bar background recording",
                "Sets AppCaptureEnabled = 0 (user) + AllowGameDVR = 0 (policy)"),
            E("Gaming","🪟",false,"Game_FSO",
                "Disable Fullscreen Optimisations",
                "Forces exclusive fullscreen for lower input latency",
                "Sets GameDVR_FSEBehaviorMode = 2, GameDVR_HonorUserFSEBehaviorMode = 1"),
            E("Gaming","🎯",false,"Game_GPUPower",
                "GPU Power: Prefer Maximum Performance",
                "Sets D3D power policy to never downclock the GPU",
                "Sets PerfLevelSrc = 0x3322 in the D3D GPU class driver key"),
            E("Gaming","🟢",false,"Game_NvidiaTelemetry",
                "Disable NVIDIA Telemetry Services",
                "Stops NvTelemetryContainer & NvDisplayContainerLS phoning home",
                "Disables services NvTelemetryContainer, NvDisplayContainerLS + 3 NVIDIA scheduled tasks"),

            E("Network","📶",false,"Net_Nagle",
                "Disable Nagle's Algorithm",
                "Reduces TCP packet buffering — lowers game ping",
                "Sets TcpAckFrequency = 1, TCPNoDelay = 1 on all network adapter subkeys"),
            E("Network","🔁",false,"Net_RSS",
                "Enable Receive-Side Scaling",
                "Spreads network processing across CPU cores",
                "Runs: netsh int tcp set global rss=enabled"),
            E("Network","🎛",false,"Net_TCPAutoTune",
                "TCP Auto-Tuning: Normal",
                "Enables adaptive TCP receive buffer scaling",
                "Runs: netsh int tcp set global autotuninglevel=normal"),
            E("Network","🚦",false,"Net_Throttle",
                "Disable Network Throttling Index",
                "Removes multimedia network rate caps",
                "Sets NetworkThrottlingIndex = 0xFFFFFFFF in Multimedia\\SystemProfile"),
            E("Network","🏎",false,"Net_MMResponsive",
                "Max Multimedia Responsiveness",
                "Sets SystemResponsiveness to 0 for games/audio",
                "Sets SystemResponsiveness = 0 in Multimedia\\SystemProfile"),
            E("Network","🔐",false,"Net_DoH",
                "DNS over HTTPS (Cloudflare 1.1.1.1)",
                "Enables DoH via Windows DNS Client, routes queries encrypted",
                "Sets EnableAutoDoh = 2, registers 1.1.1.1 and 1.0.0.1 with Cloudflare DoH template"),

            E("Bloatware","📰",false,"Bloat_Bing",
                "Remove Bing News & Weather",
                "Uninstalls BingNews and BingWeather UWP packages",
                "Runs Remove-AppxPackage for *BingNews*, *BingWeather*, *BingSearch* (user + provisioned)\n⚠ Cannot be undone — reinstall from Microsoft Store if needed."),
            E("Bloatware","🎵",false,"Bloat_Zune",
                "Remove Zune Music / Video",
                "Removes the legacy Groove Music and Movies & TV apps",
                "Runs Remove-AppxPackage for *ZuneVideo*, *ZuneMusic* (user + provisioned)\n⚠ Cannot be undone — reinstall from Microsoft Store if needed."),
            E("Bloatware","♟",false,"Bloat_Solitaire",
                "Remove Solitaire Collection",
                "Uninstalls the ad-supported Solitaire game suite",
                "Runs Remove-AppxPackage for *SolitaireCollection* (user + provisioned)\n⚠ Cannot be undone — reinstall from Microsoft Store if needed."),
            E("Bloatware","🗺",false,"Bloat_Maps",
                "Remove Windows Maps",
                "Strips the built-in Maps UWP application",
                "Runs Remove-AppxPackage for *WindowsMaps* (user + provisioned)\n⚠ Cannot be undone — reinstall from Microsoft Store if needed."),
            E("Bloatware","📱",false,"Bloat_PhoneLink",
                "Remove Phone Link / Your Phone",
                "Removes the Android phone companion app",
                "Runs Remove-AppxPackage for *YourPhone*, *PhoneLink* (user + provisioned)\n⚠ Cannot be undone — reinstall from Microsoft Store if needed."),
            E("Bloatware","🎬",false,"Bloat_Clipchamp",
                "Remove Clipchamp",
                "Removes the bundled Microsoft video editor",
                "Runs Remove-AppxPackage for *Clipchamp* (user + provisioned)\n⚠ Cannot be undone — reinstall from Microsoft Store if needed."),
            E("Bloatware","🎮",false,"Bloat_Xbox",
                "Remove Xbox Apps & Overlays",
                "Strips Xbox TCUI, App, GameOverlay, GamingOverlay",
                "Runs Remove-AppxPackage for *Xbox.TCUI*, *XboxApp*, *XboxGameOverlay*, *XboxGamingOverlay* (user + provisioned)\n⚠ Cannot be undone — reinstall from Microsoft Store if needed."),
            E("Bloatware","🛒",false,"Bloat_AdTiles",
                "Remove Third-Party Ad Tiles",
                "Removes LinkedIn, Disney, Spotify, TikTok, Instagram tiles",
                "Runs Remove-AppxPackage for *LinkedIn*, *Disney*, *Spotify*, *TikTok*, *Instagram*, *Facebook* (user + provisioned)\n⚠ Cannot be undone — reinstall from respective app stores if needed."),
            E("Bloatware","📦",false,"Bloat_Office",
                "Remove Office Hub & OneNote",
                "Uninstalls the bundled Office Hub and OneNote UWP apps",
                "Runs Remove-AppxPackage for *OfficeHub*, *OneNote* (user + provisioned)\n⚠ Cannot be undone — reinstall from Microsoft Store if needed. Does NOT affect desktop Office installs."),
            E("Bloatware","🗃",false,"Bloat_3D",
                "Remove 3D Viewer & Print 3D",
                "Removes legacy 3D apps nobody asked for",
                "Runs Remove-AppxPackage for *3DViewer*, *Print3D* (user + provisioned)\n⚠ Cannot be undone — reinstall from Microsoft Store if needed."),

            E("Security","🔇",false,"Sec_AutoRun",
                "Disable AutoRun / AutoPlay",
                "Blocks autorun.inf and AutoPlay on all drive types",
                "Sets NoDriveTypeAutoRun = 0xFF at user + machine scope, blocks autorun.inf via IniFileMapping"),
            E("Security","🖥",false,"Sec_RDP",
                "Disable Remote Desktop (RDP)",
                "Refuses all inbound RDP connections, closes firewall rule",
                "Sets fDenyTSConnections = 1 + disables 'Remote Desktop' firewall rule group"),
            E("Security","🪟",false,"Sec_SMBv1",
                "Disable SMBv1",
                "Removes the WannaCry/EternalBlue-vulnerable SMBv1 protocol",
                "Runs Set-SmbServerConfiguration -EnableSMB1Protocol $false + Disable-WindowsOptionalFeature SMB1Protocol\nSMBv1 is the protocol exploited by WannaCry ransomware. Modern Windows uses SMBv2/3. Safe to disable on any modern system."),
            E("Security","📡",false,"Sec_NetBIOS",
                "Disable NetBIOS over TCP/IP",
                "Stops NetBIOS on all adapters — prevents NBNS poisoning",
                "Runs SetTcpipNetbios(2) on all WMI network adapter configs\nNetBIOS broadcasts your machine name on the network and is a vector for NBNS/LLMNR poisoning attacks. No practical use on modern networks."),
            E("Security","🛡",false,"Sec_Defender",
                "Ensure Defender Real-Time Protection",
                "Forces Defender real-time monitoring ON via policy + cmdlet",
                "Sets DisableAntiSpyware = 0, DisableRealtimeMonitoring = 0 + runs Set-MpPreference -DisableRealtimeMonitoring $false"),

            Adv("⚙","ProcessorScheduling","Adv_ProcessorScheduling",
                "Processor Scheduling: Programs",
                "Win32PrioritySeparation=38, max foreground CPU boost",
                "Sets Win32PrioritySeparation = 38 in PriorityControl"),
            Adv("⏱","DisableDynamicTick","Adv_DynamicTick",
                "Disable Dynamic Tick",
                "Forces constant high-res IRQ8 timer, reduces micro-stutter",
                "Runs: bcdedit /set disabledynamictick yes"),
            Adv("🔥","DisableCpuThrottling","Adv_CPUThrottle",
                "Disable CPU Throttling",
                "Prevents Windows pulling background process CPU clocks",
                "Sets ValueMax = 0 in THROTTLE_POLICY key + powercfg PERFAUTONOMOUS = 0"),
            Adv("💾","EnableTrim","Adv_TRIM",
                "Ensure SSD TRIM Enabled",
                "Sets disabledeletenotify=0, keeps SSD write speeds consistent",
                "Runs: fsutil behavior set disabledeletenotify 0"),
            Adv("🎨","AggressiveAnimations","Adv_Animations",
                "Aggressive Animation Disabling",
                "Kills UserPreferencesMask, TaskbarAnim, MinAnimate bits",
                "Sets UserPreferencesMask binary value + TaskbarAnimations = 0, MinAnimate = 0, ListviewShadow = 0"),
        };

        public static IEnumerable<TweakEntry> ForCategory(string cat) =>
            cat == "All" ? All : All.Where(t => t.Category == cat);
    }

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
                const string cv = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";
                string raw = Microsoft.Win32.Registry.GetValue(cv, "CurrentBuildNumber", "0")?.ToString() ?? "0";
                Build = int.TryParse(raw, out int b) ? b : 0;
                string dv      = Microsoft.Win32.Registry.GetValue(cv, "DisplayVersion", "")?.ToString() ?? "";
                string winName = Build >= 22000 ? "Windows 11" : "Windows 10";
                DisplayName    = string.IsNullOrWhiteSpace(dv)
                    ? $"{winName} (Build {Build})"
                    : $"{winName} {dv} (Build {Build})";
            }
            catch { Build = 0; DisplayName = "Unknown Windows"; }
        }
    }

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

public class MainForm : Form
    {
        private Panel _topBar;
        private Panel _sidebar;
        private Panel _mainArea;
        private Panel _bottomBar;
        private Panel _logPanel;

        private Label _winVerBadge;
        private Label _adminBadge;

        private Panel       _searchBar;
        private TextBox     _searchBox;
        private FlatButton  _clearSearchBtn;
        private string      _searchQuery = "";

        private static readonly string[] SidebarCategories =
        {
            "All", "Performance", "Privacy", "Responsiveness",
            "Gaming", "Network", "Bloatware", "Security", "Advanced", "Startup", "History"
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
            ["Startup"]        = "🚀",
            ["History"]        = "📋",
        };

        private string _activeCategory = "All";

        private FlowLayoutPanel          _tileGrid;
        private readonly List<TweakTile> _tiles = new();
        private Label      _statusLabel;
        private Label      _selCountLabel;
        private Panel      _progOuter;
        private Panel      _progInner;
        private FlatButton _runBtn;
        private FlatButton _undoBtn;
        private FlatButton _clearBtn;
        private CheckBox   _restoreChk;

        private Panel _histPanel;

        private StartupTab _startupTab;

        private RichTextBox _logBox;

        private Panel       _tooltip;
        private Label       _ttTitle;
        private Label       _ttWhat;
        private System.Windows.Forms.Timer _ttHideTimer;

        private bool _isRunning = false;
        private int  _totalTweaks, _doneTweaks;

        public MainForm()
        {
            InitUI();
            PopulateGrid("All");
            UpdateSelCount();
            if (AdminWarning.Show) _adminBadge.Visible = true;
        }

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

            _topBar.Dock    = DockStyle.Top;
            _bottomBar.Dock = DockStyle.Bottom;
            _sidebar.Dock   = DockStyle.Left;
            _mainArea.Dock  = DockStyle.Fill;
            _logPanel.Dock  = DockStyle.None;

            Controls.Add(_mainArea);
            Controls.Add(_sidebar);
            Controls.Add(_bottomBar);
            Controls.Add(_topBar);
            Controls.Add(_logPanel);
            BuildTooltip();
        }

        private void LayoutAll()
        {
            if (_logPanel.Visible)
            {
                int logH  = _logPanel.Height;
                int sideW = _sidebar.Width;
                int botY  = ClientSize.Height - _bottomBar.Height - logH;
                int logW  = ClientSize.Width - sideW;
                _logPanel.SetBounds(sideW, botY, logW, logH);
                _logPanel.BringToFront();
                _mainArea.Padding = new Padding(0, 0, 0, logH);
            }
            else
            {
                _mainArea.Padding = new Padding(0);
            }
        }

private void BuildTopBar()
        {
            _topBar = new Panel { BackColor = Theme.SURFACE, Height = 60 };
            _topBar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                // Bottom border line
                using var p = new Pen(Theme.BORDER);
                g.DrawLine(p, 0, _topBar.Height - 1, _topBar.Width, _topBar.Height - 1);
                // Subtle gold top accent bar (like website nav)
                using var bar = new SolidBrush(Color.FromArgb(30, Theme.ACCENT.R, Theme.ACCENT.G, Theme.ACCENT.B));
                g.FillRectangle(bar, 0, 0, _topBar.Width, 2);
            };

            // "CORN_STUDIOS" style logo label — matches website nav-logo font
            var cornLbl = new Label
            {
                Text      = "CORN_STUDIOS",
                Font      = new Font("Courier New", 7f, FontStyle.Bold),
                ForeColor = Theme.ACCENT,
                AutoSize  = true,
                Location  = new Point(18, 10),
                BackColor = Color.Transparent
            };

            var titleLbl = new Label
            {
                Text      = "WIN11 OPTIMIZER  ⚡",
                Font      = new Font("Courier New", 9f, FontStyle.Bold),
                ForeColor = Theme.TEXT_PRI,
                AutoSize  = true,
                Location  = new Point(18, 30)
            };

            _winVerBadge = new Label { Visible = false };

            _adminBadge = new Label
            {
                Text      = "⚠  Not running as Administrator — some tweaks may fail",
                AutoSize  = true,
                Font      = new Font("Courier New", 7.5f),
                ForeColor = Theme.WARNING,
                Location  = new Point(18, 44),
                Visible   = false
            };

            var ghLink = new FlatButton("⭐  GITHUB ↗", Theme.ACCENT)
            {
                AutoSize  = false,
                Size      = new Size(115, 30),
                Font      = new Font("Courier New", 7.5f, FontStyle.Bold),
                ForeColor = Theme.ACCENT_TEXT,
                Cursor    = Cursors.Hand
            };
            ghLink.FlatAppearance.BorderSize         = 0;
            ghLink.FlatAppearance.MouseOverBackColor = Theme.ACCENT_HOV;
            ghLink.Click += (s, e) => Process.Start(new ProcessStartInfo
                { FileName = "https://github.com/Corn-Studios/win11op", UseShellExecute = true });
            _topBar.SizeChanged += (s, e) =>
                ghLink.Location = new Point(_topBar.Width - ghLink.Width - 16, (_topBar.Height - ghLink.Height) / 2);

            _topBar.Controls.AddRange(new Control[]
                { cornLbl, titleLbl, _winVerBadge, _adminBadge, ghLink });
        }

        //  SIDEBAR --

        private void BuildSidebar()
        {
            _sidebar = new Panel { BackColor = Theme.SURFACE, Width = 210 };
            _sidebar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var p = new Pen(Theme.BORDER);
                g.DrawLine(p, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
                // Subtle purple gradient at top — matches website section hero
                using var grad = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, _sidebar.Width, 80),
                    Color.FromArgb(18, Theme.SKY_PURPLE.R, Theme.SKY_PURPLE.G, Theme.SKY_PURPLE.B),
                    Color.Transparent,
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical);
                g.FillRectangle(grad, 0, 0, _sidebar.Width, 80);
            };

            // "// CATEGORIES" — website section-label mono style with gold accent
            var hdr = new Label
            {
                Text      = "// CATEGORIES",
                Font      = new Font("Courier New", 7f, FontStyle.Bold),
                ForeColor = Theme.ACCENT,
                AutoSize  = true,
                Location  = new Point(14, 14)
            };
            _sidebar.Controls.Add(hdr);

            int y = 38;
            foreach (var cat in SidebarCategories)
            {
                if (cat == "Startup" || cat == "History")
                {
                    var div = new Panel
                    {
                        BackColor = Theme.BORDER,
                        Bounds    = new Rectangle(8, y + 2, 194, 1)
                    };
                    _sidebar.Controls.Add(div);
                    y += 10;
                }

                var btn = MakeSidebarBtn(cat);
                btn.SetBounds(8, y, 194, 36);
                _sidebar.Controls.Add(btn);
                y += 38;
            }

            y += 8;
            var selAll = new FlatButton("✔ Select All", Theme.ACCENT);
            selAll.SetBounds(8, y, 92, 28);
            selAll.Click += (s, e) => SetAllInView(true);

            var selNone = new FlatButton("✘ None", Theme.SURFACE2);
            selNone.SetBounds(108, y, 94, 28);
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
                ForeColor = active ? Theme.ACCENT_TEXT : Theme.TEXT_DIM,
                Font      = new Font("Courier New", 8f, active ? FontStyle.Bold : FontStyle.Regular),
                Cursor    = Cursors.Hand,
                Tag       = cat
            };
            btn.FlatAppearance.BorderSize           = 0;
            btn.FlatAppearance.MouseOverBackColor   = active
                ? Theme.ACCENT_HOV
                : Color.FromArgb(26, 24, 53);  // website --surface2

            // Badge paint — draws a small pill with selection count on the right
            btn.Paint += (s, e) =>
            {
                int selCount = cat == "All"
                    ? _tiles.Count(t => t.IsChecked)
                    : _tiles.Count(t => t.IsChecked && t.Entry.Category == cat);
                if (selCount == 0) return;

                string badge = selCount.ToString();
                bool   isActive = cat == _activeCategory;
                Color  pillBg   = isActive
                    ? Theme.ACCENT
                    : Color.FromArgb(50, Theme.ACCENT.R, Theme.ACCENT.G, Theme.ACCENT.B);
                Color  pillFg   = isActive
                    ? Theme.ACCENT_TEXT
                    : Theme.ACCENT;

                using var badgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                SizeF  sz      = g.MeasureString(badge, badgeFont);
                int    pw      = (int)sz.Width + 10;
                int    ph      = 16;
                int    px      = btn.Width - pw - 8;
                int    py      = (btn.Height - ph) / 2;

                using var pillBr = new SolidBrush(pillBg);
                g.FillRoundedRect(pillBr, px, py, pw, ph, 8);
                using var textBr = new SolidBrush(pillFg);
                g.DrawString(badge, badgeFont, textBr,
                    px + (pw - sz.Width) / 2f,
                    py + (ph - sz.Height) / 2f);
            };

            btn.Click += (s, e) =>
            {
                _activeCategory = cat;
                ClearSearch();
                RefreshSidebar();
                if      (cat == "History") ShowHistory();
                else if (cat == "Startup") ShowStartup();
                else                       PopulateGrid(cat);
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
                    btn.ForeColor = active ? Theme.ACCENT_TEXT : Theme.TEXT_DIM;
                    btn.Font      = new Font("Courier New", 8f, active ? FontStyle.Bold : FontStyle.Regular);
                    btn.FlatAppearance.MouseOverBackColor = active
                        ? Theme.ACCENT_HOV
                        : Color.FromArgb(26, 24, 53);
                }
            }
        }

private void BuildMainArea()
        {
            _mainArea = new Panel { BackColor = Theme.BG };

            BuildSearchBar();

            _tileGrid = new FlowLayoutPanel
            {
                AutoScroll    = true,
                WrapContents  = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor     = Theme.BG,
                Padding       = new Padding(12),
                Dock          = DockStyle.Fill,
            };
            _tileGrid.HorizontalScroll.Enabled = false;
            _tileGrid.HorizontalScroll.Visible = false;

            _histPanel = new Panel
            {
                AutoScroll = true,
                BackColor  = Theme.BG,
                Padding    = new Padding(20),
                Dock       = DockStyle.Fill,
                Visible    = false
            };

            _startupTab = new StartupTab();

            _mainArea.Controls.Add(_histPanel);
            _mainArea.Controls.Add(_startupTab);
            _mainArea.Controls.Add(_tileGrid);
            _mainArea.Controls.Add(_searchBar); // add last so it docks on top
        }

private void BuildSearchBar()
        {
            _searchBar = new Panel
            {
                BackColor = Theme.SURFACE,
                Height    = 56,
                Dock      = DockStyle.Top,
                Padding   = new Padding(10, 8, 10, 8)
            };
            _searchBar.Paint += (s, e) =>
            {
                using var p = new Pen(Theme.BORDER);
                e.Graphics.DrawLine(p, 0, _searchBar.Height - 1,
                    _searchBar.Width, _searchBar.Height - 1);
            };

            var searchIcon = new Label
            {
                Text      = "🔍",
                Font      = new Font("Segoe UI Emoji", 11f),
                AutoSize  = false,
                Size      = new Size(28, 32),
                Location  = new Point(10, 10),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _searchBox = new TextBox
            {
                Font        = new Font("Segoe UI", 10f),
                ForeColor   = Theme.TEXT_PRI,
                BackColor   = Theme.CARD,
                BorderStyle = BorderStyle.None,
                Location    = new Point(42, 16),
                Height      = 26,
                Width       = 260,
                Text        = "Search tweaks..."
            };

            // Placeholder behaviour
            bool hasPlaceholder = true;
            _searchBox.ForeColor = Theme.TEXT_SEC;

            _searchBox.Enter += (s, e) =>
            {
                if (hasPlaceholder)
                {
                    _searchBox.Text      = "";
                    _searchBox.ForeColor = Theme.TEXT_PRI;
                    hasPlaceholder       = false;
                }
            };
            _searchBox.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_searchBox.Text))
                {
                    _searchBox.Text      = "Search tweaks...";
                    _searchBox.ForeColor = Theme.TEXT_SEC;
                    hasPlaceholder       = true;
                }
            };
            _searchBox.TextChanged += (s, e) =>
            {
                if (hasPlaceholder) return;
                _searchQuery = _searchBox.Text.Trim().ToLower();
                _clearSearchBtn.Visible = !string.IsNullOrEmpty(_searchQuery);
                ApplySearchFilter();
            };

            _clearSearchBtn = new FlatButton("✕", Theme.SURFACE2)
            {
                Size     = new Size(22, 22),
                Location = new Point(306, 15),
                Font     = new Font("Segoe UI", 8f),
                ForeColor = Theme.TEXT_SEC,
                Visible   = false
            };
            _clearSearchBtn.Click += (s, e) => ClearSearch();

            var divider = new Panel
            {
                BackColor = Theme.BORDER,
                Size      = new Size(1, 32),
                Location  = new Point(338, 10)
            };

            var presetLbl = new Label
            {
                Text      = "PRESETS",
                Font      = new Font("Courier New", 6.5f, FontStyle.Bold),
                ForeColor = Theme.ACCENT,
                AutoSize  = true,
                Location  = new Point(352, 5),
                BackColor = Color.Transparent
            };

            // Shared style helper
            static void StylePreset(FlatButton b) {
                b.FlatAppearance.BorderSize  = 1;
                b.FlatAppearance.BorderColor = Color.FromArgb(204, 255, 0);   // lime border
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 204, 255, 0); // faint lime glow
            }

            var presetRecommended = new FlatButton("⭐ Recommended", Theme.SURFACE2)
            {
                Size      = new Size(138, 28),
                Location  = new Point(352, 20),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.ACCENT
            };
            StylePreset(presetRecommended);
            presetRecommended.Click += (s, e) => ApplyPreset("Recommended");

            var presetGaming = new FlatButton("🎮 Gaming PC", Theme.SURFACE2)
            {
                Size      = new Size(110, 28),
                Location  = new Point(498, 20),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.ACCENT
            };
            StylePreset(presetGaming);
            presetGaming.Click += (s, e) => ApplyPreset("Gaming");

            var presetPrivacy = new FlatButton("🔒 Privacy", Theme.SURFACE2)
            {
                Size      = new Size(90, 28),
                Location  = new Point(616, 20),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.ACCENT
            };
            StylePreset(presetPrivacy);
            presetPrivacy.Click += (s, e) => ApplyPreset("Privacy");

            var presetSecurity = new FlatButton("🛡 Security", Theme.SURFACE2)
            {
                Size      = new Size(92, 28),
                Location  = new Point(714, 20),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.ACCENT
            };
            StylePreset(presetSecurity);
            presetSecurity.Click += (s, e) => ApplyPreset("Security");

            var presetMinimal = new FlatButton("🪶 Minimal", Theme.SURFACE2)
            {
                Size      = new Size(84, 28),
                Location  = new Point(814, 20),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.ACCENT
            };
            StylePreset(presetMinimal);
            presetMinimal.Click += (s, e) => ApplyPreset("Minimal");

            // Nuclear stays red — intentionally different to signal danger
            var presetNuclear = new FlatButton("☢ Nuclear", Color.FromArgb(45, 20, 20))
            {
                Size      = new Size(88, 28),
                Location  = new Point(906, 20),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.DANGER
            };
            presetNuclear.FlatAppearance.BorderSize  = 1;
            presetNuclear.FlatAppearance.BorderColor = Color.FromArgb(100, 40, 40);
            presetNuclear.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 30, 30);
            presetNuclear.Click += (s, e) => ApplyPreset("Nuclear");

            _searchBar.Controls.AddRange(new Control[]
            {
                searchIcon, _searchBox, _clearSearchBtn, divider,
                presetLbl, presetRecommended, presetGaming, presetPrivacy, presetSecurity,
                presetMinimal, presetNuclear
            });
        }

private void ApplySearchFilter()
        {
            if (string.IsNullOrEmpty(_searchQuery))
            {
                // No filter — show everything normally
                foreach (Control c in _tileGrid.Controls)
                    c.Visible = true;
                UpdateSelCount();
                return;
            }

            // Track which section headers have at least one visible tile
            SectionHeader currentHeader = null;
            bool          headerHasHit  = false;

            foreach (Control c in _tileGrid.Controls)
            {
                if (c is SectionHeader hdr)
                {
                    // Finalize previous header visibility
                    if (currentHeader != null)
                        currentHeader.Visible = headerHasHit;

                    currentHeader = hdr;
                    headerHasHit  = false;
                }
                else if (c is TweakTile tile)
                {
                    bool match = tile.Entry.Name.ToLower().Contains(_searchQuery)
                              || tile.Entry.Description.ToLower().Contains(_searchQuery)
                              || tile.Entry.Category.ToLower().Contains(_searchQuery)
                              || (tile.Entry.WhatItChanges?.ToLower().Contains(_searchQuery) ?? false);

                    tile.Visible = match;
                    if (match) headerHasHit = true;
                }
            }

            // Finalize last header
            if (currentHeader != null)
                currentHeader.Visible = headerHasHit;

            UpdateSelCount();
        }

        private void ClearSearch()
        {
            _searchQuery            = "";
            _searchBox.Text         = "Search tweaks...";
            _searchBox.ForeColor    = Theme.TEXT_SEC;
            _clearSearchBtn.Visible = false;
            // Re-show everything
            foreach (Control c in _tileGrid.Controls)
                c.Visible = true;
            UpdateSelCount();
        }

private void ApplyPreset(string preset)
        {
            // If we're on History, switch to All first
            if (_activeCategory == "History" || _activeCategory == "Startup")
            {
                _activeCategory = "All";
                RefreshSidebar();
                PopulateGrid("All");
            }

            // Clear search so all tiles are visible
            ClearSearch();

            switch (preset)
            {
                case "Recommended":
                    // Select only DefaultOn tiles
                    foreach (var t in _tiles)
                        t.IsChecked = t.Entry.DefaultOn;
                    SetStatus("Preset applied: Recommended (safe defaults)", Theme.ACCENT);
                    break;

                case "Gaming":
                    // Recommended defaults + all Gaming and Network tweaks
                    foreach (var t in _tiles)
                        t.IsChecked = t.Entry.DefaultOn
                                   || t.Entry.Category == "Gaming"
                                   || t.Entry.Category == "Network";
                    SetStatus("Preset applied: Gaming PC (recommended + gaming + network)", Theme.SUCCESS);
                    break;

                case "Privacy":
                    // All Privacy tiles + recommended defaults
                    foreach (var t in _tiles)
                        t.IsChecked = t.Entry.DefaultOn
                                   || t.Entry.Category == "Privacy";
                    SetStatus("Preset applied: Privacy (recommended + all privacy tweaks)", Color.FromArgb(168, 85, 247));
                    break;

                case "Security":
                    // All Security tiles + recommended defaults
                    foreach (var t in _tiles)
                        t.IsChecked = t.Entry.DefaultOn
                                   || t.Entry.Category == "Security";
                    SetStatus("Preset applied: Security (recommended + all security tweaks)", Color.FromArgb(16, 185, 129));
                    break;

                case "Nuclear":
                    // Everything except Bloatware (irreversible) and Advanced (risky)
                    foreach (var t in _tiles)
                        t.IsChecked = t.Entry.Category != "Bloatware"
                                   && t.Entry.Category != "Advanced";
                    SetStatus("Preset applied: Nuclear — all tweaks except Bloatware & Advanced", Theme.DANGER);
                    break;

                case "Minimal":
                    // Only the lightest, safest tweaks — responsiveness + privacy basics
                    foreach (var t in _tiles)
                        t.IsChecked = t.Entry.TweakKey is
                            "Resp_MenuDelay" or "Resp_AppKill" or "Resp_ServiceKill" or
                            "Resp_AutoEndTasks" or "Resp_WinTips" or "Resp_SuggestedContent" or
                            "Priv_AdvertisingId" or "Priv_BingStart" or "Priv_ChatIcon" or
                            "Priv_Feedback" or "Priv_AppTracking" or "Priv_Recall" or
                            "Perf_StartupDelay" or "Perf_VisualFX";
                    SetStatus("Preset applied: Minimal — safe UI & privacy tweaks only", Theme.TEXT_SEC);
                    break;
            }

            UpdateSelCount();
        }

private static readonly string[] CategoryOrder =
        {
            "Performance", "Privacy", "Responsiveness",
            "Gaming", "Network", "Bloatware", "Security", "Advanced"
        };

        private void PopulateGrid(string filter)
        {
            _histPanel.Visible  = false;
            _startupTab.Visible = false;
            _tileGrid.Visible   = true;
            _searchBar.Visible  = true;

            _tileGrid.SuspendLayout();
            _tileGrid.Controls.Clear();
            _tiles.Clear();

            IEnumerable<TweakEntry> source = filter == "All"
                ? TweakCatalog.All
                : TweakCatalog.All.Where(t => t.Category == filter);

            var groups = source
                .GroupBy(t => t.Category)
                .OrderBy(g => Array.IndexOf(CategoryOrder, g.Key));

            SectionHeader.ResetIndex();
            foreach (var group in groups)
            {
                string emoji = CatEmoji.TryGetValue(group.Key, out var em) ? em : "📦";

                var hdr = new SectionHeader(group.Key, emoji);
                _tileGrid.Controls.Add(hdr);

                foreach (var entry in group)
                {
                    var tile = new TweakTile(entry);
                    tile.IsChecked = entry.DefaultOn;
                    // Mark already-applied tiles
                    if (TweakEngine.HasBackup(entry.Category))
                        tile.SetApplied(true);
                    tile.CheckedChanged += (s, e_) => { UpdateSelCount(); SetStatus("Ready", Theme.TEXT_SEC); };
                    // Tooltip wiring
                    tile.MouseEnter += (s, e_) => ShowTooltip((TweakTile)s);
                    tile.MouseLeave += (s, e_) => HideTooltip();
                    foreach (Control child in tile.Controls)
                    {
                        child.MouseEnter += (s, e_) => ShowTooltip(tile);
                        child.MouseLeave += (s, e_) => HideTooltip();
                    }
                    _tiles.Add(tile);
                    _tileGrid.Controls.Add(tile);
                }
            }

            _tileGrid.ResumeLayout(true);
            UpdateSelCount();
        }

        private void SetAllInView(bool check)
        {
            foreach (var t in _tiles.Where(t => t.Visible))
                t.IsChecked = check;
            UpdateSelCount();
        }

        private void UpdateSelCount()
        {
            // Count only visible checked tiles so search doesn't confuse the counter
            int count = _tiles.Count(t => t.IsChecked && t.Visible);
            if (_selCountLabel == null) return;
            // Repaint sidebar buttons so badges refresh
            foreach (Control c in _sidebar.Controls)
                if (c is Button) c.Invalidate();

            _selCountLabel.Text = count == 0
                ? "No tweaks selected"
                : $"{count} tweak{(count == 1 ? "" : "s")} selected";

            if (_undoBtn != null)
            {
                var cats = _tiles.Where(t => t.IsChecked).Select(t => t.Entry.Category).Distinct();
                _undoBtn.Enabled = cats.Any(c => TweakEngine.HasBackup(c));
            }
        }

private void ShowHistory()
        {
            _tileGrid.Visible   = false;
            _searchBar.Visible  = false;
            _startupTab.Visible = false;
            _histPanel.Visible  = true;
            BuildHistoryContent();
        }

        private void ShowStartup()
        {
            _tileGrid.Visible   = false;
            _searchBar.Visible  = false;
            _histPanel.Visible  = false;
            _startupTab.Visible = true;
            _startupTab.Activate();
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
                Text      = "// RUN HISTORY",
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

                Label HL(string text, float size, FontStyle fs, Color fg, int y)
                    => new Label { Text=text, Font=new Font("Segoe UI",size,fs), ForeColor=fg,
                                   AutoSize=true, Location=new Point(14,y), BackColor=Color.Transparent };
                Color sc = entry.Failed == 0 ? Theme.SUCCESS : Theme.WARNING;
                string stText = $"✔ {entry.Passed} succeeded   ✘ {entry.Failed} failed"
                              + (entry.RestorePoint ? "   🛡 Restore Point" : "");
                card.Controls.Add(HL(entry.Timestamp,                9f,  FontStyle.Bold,    Theme.ACCENT,   10));
                card.Controls.Add(HL(entry.WindowsVer,               8.5f,FontStyle.Regular, Theme.TEXT_SEC, 28));
                card.Controls.Add(HL($"Categories: {entry.Categories}",9f,FontStyle.Regular, Theme.TEXT_PRI, 46));
                card.Controls.Add(HL(stText,                         9f,  FontStyle.Bold,    sc,             64));

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

private void BuildBottomBar()
        {
            _bottomBar = new Panel { BackColor = Theme.SURFACE, Height = 136 };
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
                ForeColor = Theme.TEXT_DIM,
                AutoSize  = true,
                Location  = new Point(16, 24)
            };

            _selCountLabel = new Label
            {
                Text      = "No tweaks selected",
                ForeColor = Theme.TEXT_DIM,
                AutoSize  = true,
                Location  = new Point(16, 46)
            };

            _restoreChk = new CheckBox
            {
                Text      = "🛡 Create Restore Point before running",
                ForeColor = Theme.TEXT_DIM,
                BackColor = Color.Transparent,
                Checked   = true,
                AutoSize  = true,
                Location  = new Point(16, 92),
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

            _runBtn = new FlatButton("⚡  RUN SELECTED", Theme.ACCENT)
            {
                Size      = new Size(165, 36),
                Font      = new Font("Courier New", 8.5f, FontStyle.Bold),
                ForeColor = Theme.ACCENT_TEXT
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
                // Log button pinned top-right, progress bar stops before it
                logToggle.Location = new Point(r - 76, 12);
                _progOuter.Width   = Math.Max(200, r - 96);
            };

            _bottomBar.Controls.AddRange(new Control[]
            {
                _progOuter, _statusLabel, _selCountLabel,
                _restoreChk, _undoBtn, _clearBtn, _runBtn, logToggle
            });
        }

private void BuildLogPanel()
        {
            _logBox = new RichTextBox
            {
                BackColor   = Color.FromArgb(8, 8, 20),
                ForeColor   = Color.FromArgb(180, 200, 255),
                BorderStyle = BorderStyle.None,
                ReadOnly    = true,
                Font        = new Font("Consolas", 8.5f),
                Dock        = DockStyle.Fill,
                ScrollBars  = RichTextBoxScrollBars.Vertical
            };

            _logPanel = new Panel
            {
                BackColor = Color.FromArgb(8, 8, 20),
                Visible   = false,
                Height    = 180
            };

            var hdr = new Panel { Dock = DockStyle.Top, Height = 26, BackColor = Theme.SURFACE };
            var hdrLbl = new Label
            {
                Text      = "// OUTPUT LOG",
                Font      = new Font("Courier New", 7.5f, FontStyle.Bold),
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

private void BuildTooltip()
        {
            _ttTitle = new Label
            {
                Font      = new Font("Segoe UI Semibold", 9f),
                ForeColor = Theme.TEXT_PRI,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Location  = new Point(20, 10),
                Size      = new Size(268, 18)
            };

            _ttWhat = new Label
            {
                Font      = new Font("Consolas", 7.5f),
                ForeColor = Theme.TEXT_SEC,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Location  = new Point(20, 32),
                Size      = new Size(268, 120)
            };

            _tooltip = new Panel
            {
                BackColor = Color.FromArgb(13, 12, 28),
                Size      = new Size(300, 0),   // height set dynamically
                Visible   = false,
                Padding   = new Padding(12)
            };
            _tooltip.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(80, Theme.ACCENT.R, Theme.ACCENT.G, Theme.ACCENT.B), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, _tooltip.Width - 1, _tooltip.Height - 1);
                using var bar = new SolidBrush(Theme.ACCENT);
                e.Graphics.FillRectangle(bar, 0, 0, 3, _tooltip.Height);
            };
            _tooltip.Controls.Add(_ttTitle);
            _tooltip.Controls.Add(_ttWhat);

            // Hide timer — small delay so moving between tiles doesn't flicker
            _ttHideTimer = new System.Windows.Forms.Timer { Interval = 120 };
            _ttHideTimer.Tick += (s, e) => { _ttHideTimer.Stop(); _tooltip.Visible = false; };

            Controls.Add(_tooltip);
            _tooltip.BringToFront();
        }

        private void ShowTooltip(TweakTile tile)
        {
            _ttHideTimer.Stop();
            if (string.IsNullOrEmpty(tile.Entry.WhatItChanges)) return;

            _ttTitle.Text = tile.Entry.Name;

            // WhatItChanges uses literal \n — split into cmd line + explanation
            string raw   = tile.Entry.WhatItChanges;
            int    split = raw.IndexOf("\n");
            if (split >= 0)
            {
                _ttWhat.Font      = new Font("Consolas", 7.5f);
                string cmdLine    = raw.Substring(0, split).Trim();
                string explain    = raw.Substring(split + 1).Trim();
                _ttWhat.Text      = cmdLine + "\n\n" + explain;
                _ttWhat.ForeColor = Theme.TEXT_SEC;
            }
            else
            {
                _ttWhat.Text      = raw;
                _ttWhat.ForeColor = Theme.TEXT_SEC;
            }

            // Measure required height dynamically
            using (var g = _ttWhat.CreateGraphics())
            {
                var szF          = g.MeasureString(_ttWhat.Text, _ttWhat.Font, _ttWhat.Width);
                int txtH         = (int)szF.Height + 8;
                _ttWhat.Height   = Math.Max(txtH, 30);
                _tooltip.Height  = _ttWhat.Top + _ttWhat.Height + 16;
            }

            // Position to the right of tile, flip left if needed, clamp vertically
            Point screenPt   = tile.PointToScreen(Point.Empty);
            Point clientPt   = PointToClient(screenPt);
            int   tx         = clientPt.X + tile.Width + 6;
            int   ty         = clientPt.Y;
            if (tx + _tooltip.Width > ClientSize.Width - 10)
                tx = clientPt.X - _tooltip.Width - 6;
            if (ty + _tooltip.Height > ClientSize.Height - _bottomBar.Height - 10)
                ty = ClientSize.Height - _bottomBar.Height - _tooltip.Height - 10;

            _tooltip.Location = new Point(tx, ty);
            _tooltip.Visible  = true;
            _tooltip.BringToFront();
        }

        private void HideTooltip() => _ttHideTimer.Start();

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

            await Task.Run(() =>
            {
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

                    // Mark tile as running right before it executes (not all at once up front)
                    Invoke(new Action(() =>
                    {
                        tile.SetStatus(TileStatus.Running);
                        _doneTweaks++;
                        SetProgress(_doneTweaks, _totalTweaks);
                        SetStatus($"[{_doneTweaks}/{_totalTweaks}]  {entry.Category} → {entry.Name}", Theme.WARNING);
                        AppendLog($"  → {entry.Name}…");
                        // Scroll the tile into view
                        _tileGrid.ScrollControlIntoView(tile);
                    }));

                    if (entry.Category == "Bloatware")
                        TweakEngine.ApplyBloatwareTweak(entry.TweakKey);
                    else if (entry.IsAdvanced && entry.AdvancedKey != null)
                        TweakEngine.ApplyAdvancedTweak(entry.AdvancedKey);
                    else
                        TweakEngine.ApplyTweak(entry.TweakKey);

                    Invoke(new Action(() => LogTweak(entry.Name)));
                }
                if (lastCat != null)
                    Invoke(new Action(() => AppendLog("└─────────────────────────────────────")));
            });

            var results = TweakEngine.GetResults();
            int pass = results.Count(r => r.Success);
            int fail = results.Count(r => !r.Success);

            foreach (var t in selected)
            {
                t.SetStatus(TileStatus.Done);
                t.SetApplied(true);
            }

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
            if (MessageBox.Show("Some tweaks require a reboot to take full effect.\n\nWould you like to reboot now?",
                "Reboot Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                Process.Start(new ProcessStartInfo("shutdown.exe",
                    "/r /t 10 /c \"Win11 Optimizer: Rebooting to apply tweaks.\"")
                    { UseShellExecute = false, CreateNoWindow = true });
        }
    }

    //  TILE STATUS ENUM

    public enum TileStatus { None, Running, Done }

    public class TweakTile : Panel
    {
        private bool       _checked;
        private bool       _applied;           // was previously run
        private TileStatus _status      = TileStatus.None;
        private string     _statusText  = "";
        private Color      _statusColor = Theme.TEXT_SEC;

        public TweakEntry Entry { get; }
        public event EventHandler CheckedChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsChecked
        {
            get => _checked;
            set
            {
                _checked  = value;
                // Subtle gold-tinted warm surface when selected — matches website featured card
                BackColor = value ? Color.FromArgb(22, 19, 38) : Theme.CARD;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private static readonly Dictionary<string, Color> CatAccent = new()
        {
            // Each category gets a distinct color — website accent is lime,
            // but tiles need contrast so we use a reduced palette.
            ["Performance"]    = Color.FromArgb(204, 255,   0),  // lime (primary accent)
            ["Privacy"]        = Color.FromArgb(139, 92,  246),  // violet
            ["Responsiveness"] = Color.FromArgb(  6, 182, 212),  // cyan
            ["Gaming"]         = Color.FromArgb( 34, 197,  94),  // green
            ["Network"]        = Color.FromArgb(251, 191,  36),  // amber
            ["Bloatware"]      = Color.FromArgb(239,  68,  68),  // red
            ["Advanced"]       = Color.FromArgb(249, 115,  22),  // orange
            ["Security"]       = Color.FromArgb( 20, 184, 166),  // teal
        };

        public TweakTile(TweakEntry entry)
        {
            Entry     = entry;
            Size      = new Size(230, 130);
            BackColor = Theme.CARD;
            Margin    = new Padding(5);
            Cursor    = Cursors.Hand;

            Color accent = CatAccent.TryGetValue(entry.Category, out var ac) ? ac : Theme.ACCENT;

            Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using var borderPen = new Pen(_checked ? Color.FromArgb(120, accent.R, accent.G, accent.B) : Theme.BORDER, 1f);
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

                using var accentBr = new SolidBrush(_checked ? accent : Color.FromArgb(60, accent.R, accent.G, accent.B));
                g.FillRectangle(accentBr, 0, 0, 2, Height);

                if (_checked)
                {
                    // Checkmark indicator — gold filled circle with dark check
                    g.FillEllipse(accentBr, Width - 20, 8, 12, 12);
                    using var wp = new Pen(Theme.ACCENT_TEXT, 1.5f);
                    g.DrawLines(wp, new[]
                    {
                        new Point(Width - 18, 14),
                        new Point(Width - 15, 17),
                        new Point(Width - 11, 11)
                    });
                }

                // "Already applied" indicator — subtle green dot + text bottom-left
                if (_applied && _status == TileStatus.None)
                {
                    using var dotBr  = new SolidBrush(Theme.SUCCESS);
                    using var txtFnt = new Font("Courier New", 6.5f);
                    using var txtBr  = new SolidBrush(Color.FromArgb(80, Theme.SUCCESS.R, Theme.SUCCESS.G, Theme.SUCCESS.B));
                    g.FillEllipse(dotBr, 10, Height - 16, 6, 6);
                    g.DrawString("applied", txtFnt, txtBr, new PointF(20, Height - 18));
                }

                if (!string.IsNullOrEmpty(_statusText))
                {
                    using var sf = new Font("Courier New", 7f);
                    using var sb = new SolidBrush(_statusColor);
                    g.DrawString(_statusText, sf, sb, new PointF(12, Height - 18));
                }
            };

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

            var nameLbl = new Label
            {
                Text         = entry.Name,
                Font         = new Font("Courier New", 7.5f, FontStyle.Bold),
                ForeColor    = Theme.TEXT_PRI,
                AutoSize     = false,
                Size         = new Size(170, 40),
                Location     = new Point(54, 8),
                BackColor    = Color.Transparent,
                AutoEllipsis = true,
                UseMnemonic  = false
            };

            var descLbl = new Label
            {
                Text      = entry.Description,
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Theme.TEXT_DIM,
                AutoSize  = false,
                Size      = new Size(218, 38),
                Location  = new Point(10, 74),
                BackColor = Color.Transparent
            };

            Color badgeBg = Color.FromArgb(30, accent.R, accent.G, accent.B);
            Color badgeFg = Color.FromArgb(180, accent.R, accent.G, accent.B);
            var catBadge = new Label
            {
                Text      = entry.IsAdvanced ? "⚠ ADV" : entry.Category.ToUpper(),
                Font      = new Font("Courier New", 6.5f, FontStyle.Bold),
                ForeColor = badgeFg,
                BackColor = badgeBg,
                AutoSize  = true,
                Location  = new Point(10, 52),
                Padding   = new Padding(3, 1, 3, 1)
            };

            Controls.AddRange(new Control[] { iconLbl, nameLbl, descLbl, catBadge });

            void Toggle(object s, EventArgs ev) => IsChecked = !_checked;
            base.Click    += Toggle;
            iconLbl.Click  += Toggle;
            nameLbl.Click  += Toggle;
            descLbl.Click  += Toggle;
            catBadge.Click += Toggle;

            MouseEnter += (s, ev) => { if (!_checked) BackColor = Theme.SURFACE; };
            MouseLeave += (s, ev) => { if (!_checked) BackColor = Theme.CARD; };
        }

        public void SetApplied(bool applied)
        {
            _applied = applied;
            Invalidate();
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

public class SectionHeader : Panel
    {
        private static int _sectionIndex = 0;
        public static void ResetIndex() => _sectionIndex = 0;

        public SectionHeader(string title, string emoji)
        {
            _sectionIndex++;
            string index = _sectionIndex.ToString("D2");

            Height    = 52;
            Margin    = new Padding(5, 20, 5, 4);
            BackColor = Color.Transparent;

            // "01 — CATEGORY" monospace label, website style
            var indexLbl = new Label
            {
                Text      = $"{index} — {title.ToUpper()}",
                Font      = new Font("Courier New", 7f, FontStyle.Bold),
                ForeColor = Theme.ACCENT,
                AutoSize  = true,
                Location  = new Point(6, 8),
                BackColor = Color.Transparent
            };

            var nameLbl = new Label
            {
                Text      = $"{emoji}  {title}",
                Font      = new Font("Courier New", 9f, FontStyle.Bold),
                ForeColor = Theme.TEXT_PRI,
                AutoSize  = true,
                Location  = new Point(4, 24),
                BackColor = Color.Transparent
            };

            Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.BORDER, 1);
                e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            };

            Controls.AddRange(new Control[] { indexLbl, nameLbl });

            ParentChanged += (s, e) => FitToParent();
            ParentChanged += (s, e) =>
            {
                if (Parent != null)
                    Parent.SizeChanged += (ps, pe) => FitToParent();
            };
        }

        private void FitToParent()
        {
            if (Parent == null) return;
            int w = Parent.ClientSize.Width
                  - Parent.Padding.Horizontal
                  - Margin.Horizontal
                  - SystemInformation.VerticalScrollBarWidth
                  - 2;
            if (w > 0) Width = w;
            Invalidate();
        }
    }

    //  FLAT BUTTON

    public class FlatButton : Button
    {
        public FlatButton(string text, Color bg)
        {
            Text      = text;
            BackColor = bg;
            // Black text on lime, white text on dark surfaces
            bool isAccent = bg.R > 150 && bg.G > 200 && bg.B < 50;
            ForeColor = isAccent ? Theme.ACCENT_TEXT : Theme.TEXT_PRI;
            FlatStyle = FlatStyle.Flat;
            Cursor    = Cursors.Hand;
            Font      = new Font("Segoe UI", 8.5f);
            FlatAppearance.BorderSize  = isAccent ? 0 : 1;
            FlatAppearance.BorderColor = Theme.BORDER;
            FlatAppearance.MouseOverBackColor = isAccent
                ? Theme.ACCENT_HOV
                : Color.FromArgb(Math.Min(bg.R + 15, 255),
                                 Math.Min(bg.G + 15, 255),
                                 Math.Min(bg.B + 15, 255));
            FlatAppearance.MouseDownBackColor = isAccent
                ? Color.FromArgb(180, 230, 0)
                : bg;
        }
    }

    //  DARK RICH TEXT BOX

    public class DarkRichTextBox : RichTextBox
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string app, string id);
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SetWindowTheme(Handle, "DarkMode_Explorer", null);
        }
    }

internal static class GraphicsEx
    {
        public static void FillRoundedRect(this System.Drawing.Graphics g,
            System.Drawing.Brush brush, int x, int y, int w, int h, int r)
        {
            int d = r * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(x,         y,         d, d, 180, 90);
            path.AddArc(x + w - d, y,         d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d,   0, 90);
            path.AddArc(x,         y + h - d, d, d,  90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }

    //  ENTRY POINT

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
            try { using var id = WindowsIdentity.GetCurrent();
                  return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator); }
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