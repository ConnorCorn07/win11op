using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
#nullable disable warnings

namespace Win11Optimizer
{
    public static class TweakEngine
    {
        // ── RESULT TRACKING ───────────────────────────────────────────────

        public class TweakResult
        {
            public string  Name    { get; set; } = string.Empty;
            public bool    Success { get; set; }
            public string  Error   { get; set; }
        }

        private static readonly List<TweakResult> _results = new();
        public static IReadOnlyList<TweakResult> GetResults() => _results.AsReadOnly();
        public static void ClearResults() => _results.Clear();

        // ── BACKUP / RESTORE ──────────────────────────────────────────────

        public class BackupEntry
        {
            public string Category  { get; set; }
            public string KeyPath   { get; set; }
            public string ValueName { get; set; }
            public string ValueData { get; set; }
            public string ValueKind { get; set; }
            public bool   Existed   { get; set; }
        }

        private static readonly List<BackupEntry> _backups = new();
        private static readonly string BackupFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tweaks_backup.json");

        private static readonly HashSet<string> _appliedCategories =
            new(StringComparer.OrdinalIgnoreCase);
        public static IReadOnlyCollection<string> AppliedCategories => _appliedCategories;
        public static bool HasBackup(string category) => _appliedCategories.Contains(category);

        private static void BackupRegistry(string category, string keyPath, string valueName)
        {
            try
            {
                object current = Registry.GetValue(keyPath, valueName, null);
                RegistryValueKind kind = RegistryValueKind.Unknown;
                string[] parts = keyPath.Split('\\', 2);
                RegistryKey root = parts[0] switch
                {
                    "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
                    "HKEY_CURRENT_USER"  => Registry.CurrentUser,
                    "HKEY_CLASSES_ROOT"  => Registry.ClassesRoot,
                    "HKEY_USERS"         => Registry.Users,
                    _                    => null
                };
                if (root != null) { using var key = root.OpenSubKey(parts[1]); if (key != null) kind = key.GetValueKind(valueName); }
                _backups.Add(new BackupEntry { Category = category, KeyPath = keyPath, ValueName = valueName, ValueData = current?.ToString() ?? "", ValueKind = kind.ToString(), Existed = current != null });
            }
            catch
            {
                _backups.Add(new BackupEntry { Category = category, KeyPath = keyPath, ValueName = valueName, ValueData = "", ValueKind = RegistryValueKind.Unknown.ToString(), Existed = false });
            }
        }

        public static void SaveBackups()
        {
            try { File.WriteAllText(BackupFile, JsonSerializer.Serialize(_backups, new JsonSerializerOptions { WriteIndented = true })); }
            catch (Exception ex) { Debug.WriteLine($"[BACKUP SAVE FAIL] {ex.Message}"); }
        }

        public static void LoadBackups()
        {
            try
            {
                if (!File.Exists(BackupFile)) return;
                var loaded = JsonSerializer.Deserialize<List<BackupEntry>>(File.ReadAllText(BackupFile));
                if (loaded == null) return;
                _backups.Clear(); _backups.AddRange(loaded);
                foreach (var b in _backups) _appliedCategories.Add(b.Category);
            }
            catch (Exception ex) { Debug.WriteLine($"[BACKUP LOAD FAIL] {ex.Message}"); }
        }

        public static List<TweakResult> RestoreCategory(string category)
        {
            var res      = new List<TweakResult>();
            var toRemove = new List<BackupEntry>();
            foreach (var entry in _backups)
            {
                if (!entry.Category.Equals(category, StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    string[] parts = entry.KeyPath.Split('\\', 2);
                    RegistryKey root = parts[0] switch
                    {
                        "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
                        "HKEY_CURRENT_USER"  => Registry.CurrentUser,
                        "HKEY_CLASSES_ROOT"  => Registry.ClassesRoot,
                        "HKEY_USERS"         => Registry.Users,
                        _                    => null
                    };
                    if (!entry.Existed)
                    {
                        root?.OpenSubKey(parts[1], writable: true)?.DeleteValue(entry.ValueName, false);
                        res.Add(new TweakResult { Name = $"Removed {entry.ValueName}", Success = true });
                    }
                    else
                    {
                        var kind = Enum.Parse<RegistryValueKind>(entry.ValueKind);
                        object val = kind switch
                        {
                            RegistryValueKind.DWord => int.Parse(entry.ValueData),
                            RegistryValueKind.QWord => long.Parse(entry.ValueData),
                            _                       => entry.ValueData
                        };
                        Registry.SetValue(entry.KeyPath, entry.ValueName, val, kind);
                        res.Add(new TweakResult { Name = $"Restored {entry.ValueName}", Success = true });
                    }
                    toRemove.Add(entry);
                }
                catch (Exception ex) { res.Add(new TweakResult { Name = $"Restore {entry.ValueName}", Success = false, Error = ex.Message }); }
            }
            foreach (var e in toRemove) _backups.Remove(e);
            if (!_backups.Exists(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase)))
                _appliedCategories.Remove(category);
            SaveBackups();
            return res;
        }

        // ── HELPERS ───────────────────────────────────────────────────────

        private static string _currentCategory = "";

        private static void SetRegistry(string keyPath, string valueName, object value,
                                        RegistryValueKind kind, string friendlyName = null)
        {
            if (!string.IsNullOrEmpty(_currentCategory)) BackupRegistry(_currentCategory, keyPath, valueName);
            string name = friendlyName ?? valueName;
            try { Registry.SetValue(keyPath, valueName, value, kind); _results.Add(new TweakResult { Name = name, Success = true }); }
            catch (Exception ex) { Debug.WriteLine($"[REG FAIL] {name}: {ex.Message}"); _results.Add(new TweakResult { Name = name, Success = false, Error = ex.Message }); }
        }

        private static void RunCommand(string command, string friendlyName = null)
        {
            string name = friendlyName ?? command;
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c " + command) { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
                using var p = Process.Start(psi); p.WaitForExit();
                _results.Add(new TweakResult { Name = name, Success = p.ExitCode == 0 });
            }
            catch (Exception ex) { Debug.WriteLine($"[CMD FAIL] {name}: {ex.Message}"); _results.Add(new TweakResult { Name = name, Success = false, Error = ex.Message }); }
        }

        private static void RunPowerShell(string script, string friendlyName = null)
        {
            string name = friendlyName ?? script[..Math.Min(60, script.Length)];
            try
            {
                var psi = new ProcessStartInfo { FileName = "powershell.exe", Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"", UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                using var p = Process.Start(psi); p.WaitForExit();
                _results.Add(new TweakResult { Name = name, Success = p.ExitCode == 0 });
            }
            catch (Exception ex) { Debug.WriteLine($"[PS FAIL] {name}: {ex.Message}"); _results.Add(new TweakResult { Name = name, Success = false, Error = ex.Message }); }
        }

        private static void DisableService(string s)  => RunCommand($"sc config {s} start=disabled & net stop {s} 2>nul",  $"Disable service: {s}");
        private static void EnableService(string s)   => RunCommand($"sc config {s} start=auto & net start {s} 2>nul",     $"Re-enable service: {s}");
        private static void DisableScheduledTask(string t) => RunCommand($"schtasks /Change /TN \"{t}\" /Disable 2>nul", $"Disable task: {t}");
        private static void EnableScheduledTask(string t)  => RunCommand($"schtasks /Change /TN \"{t}\" /Enable 2>nul",  $"Re-enable task: {t}");

        private static void RunCategory(string category, Action tweaks)
        {
            bool alreadyBacked = _appliedCategories.Contains(category);
            _currentCategory = alreadyBacked ? "" : category;
            tweaks();
            _currentCategory = "";
            _appliedCategories.Add(category);
            SaveBackups();
        }

        // ── 1. PERFORMANCE ────────────────────────────────────────────────

        public static void ApplyPerformanceTweaks() => RunCategory("Performance", () =>
        {
            RunCommand("powercfg -setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "High Performance power plan");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1, RegistryValueKind.DWord, "Disable Power Throttling");
            DisableService("SysMain");
            DisableService("WSearch");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "StartupDelayInMSec", 0, RegistryValueKind.DWord, "Remove startup delay");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord, "Visual effects: best performance");
            RunCommand("fsutil behavior set disablelastaccess 1", "Disable NTFS last-access updates");
            RunCommand("fsutil behavior set disable8dot3 1",      "Disable 8.3 filenames");
            RunCommand("powercfg -h off", "Disable hibernation");
            RunPowerShell("Disable-MMAgent -MemoryCompression", "Disable memory compression");
        });

        public static List<TweakResult> UndoPerformanceTweaks()
        {
            var r = RestoreCategory("Performance");
            RunCommand("powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e", "Restore Balanced power plan");
            EnableService("SysMain"); EnableService("WSearch");
            RunCommand("fsutil behavior set disablelastaccess 0", "Re-enable NTFS last-access updates");
            RunCommand("powercfg -h on", "Re-enable hibernation");
            RunPowerShell("Enable-MMAgent -MemoryCompression", "Re-enable memory compression");
            return r;
        }

        // ── 2. PRIVACY ────────────────────────────────────────────────────

        public static void ApplyPrivacyTweaks() => RunCategory("Privacy", () =>
        {
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",         "AllowTelemetry",          0, RegistryValueKind.DWord, "Disable telemetry");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry",   0, RegistryValueKind.DWord, "Disable telemetry (legacy key)");
            DisableService("DiagTrack"); DisableService("dmwappushservice"); DisableService("RetailDemo"); DisableService("WerSvc");
            DisableScheduledTask(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser");
            DisableScheduledTask(@"\Microsoft\Windows\Application Experience\ProgramDataUpdater");
            DisableScheduledTask(@"\Microsoft\Windows\Autochk\Proxy");
            DisableScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator");
            DisableScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip");
            DisableScheduledTask(@"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",   "Enabled",                 0, RegistryValueKind.DWord, "Disable Advertising ID");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo",        "DisabledByGroupPolicy",   1, RegistryValueKind.DWord, "Disable Advertising ID (policy)");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer",                "DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord, "Disable Bing in Start");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackProgs",        0, RegistryValueKind.DWord, "Disable app launch tracking");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search",            "BingSearchEnabled",       0, RegistryValueKind.DWord, "Disable Bing Search integration");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search",            "CortanaConsent",          0, RegistryValueKind.DWord, "Disable Cortana consent");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",                 "EnableActivityFeed",      0, RegistryValueKind.DWord, "Disable Activity Feed");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",                 "PublishUserActivities",   0, RegistryValueKind.DWord, "Disable publishing user activities");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",                 "UploadUserActivities",    0, RegistryValueKind.DWord, "Disable uploading user activities");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Siuf\Rules",                               "NumberOfSIUFInPeriod",    0, RegistryValueKind.DWord, "Disable feedback requests");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",     "DisableLocation",         1, RegistryValueKind.DWord, "Disable location tracking");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",             "LetAppsAccessCamera",     2, RegistryValueKind.DWord, "Block app camera access");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting","Disabled",                1, RegistryValueKind.DWord, "Disable Windows Error Reporting");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",                 "EnableSmartScreen",       0, RegistryValueKind.DWord, "Disable SmartScreen (Explorer)");
        });

        public static List<TweakResult> UndoPrivacyTweaks()
        {
            var r = RestoreCategory("Privacy");
            EnableService("DiagTrack"); EnableService("WerSvc");
            EnableScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator");
            EnableScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip");
            return r;
        }

        // ── 3. RESPONSIVENESS ─────────────────────────────────────────────

        public static void ApplySystemResponsiveness() => RunCategory("Responsiveness", () =>
        {
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop", "MenuShowDelay",        "0",    RegistryValueKind.String, "Instant menu show");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop", "WaitToKillAppTimeout", "2000", RegistryValueKind.String, "Fast app kill timeout");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop", "HungAppTimeout",       "1000", RegistryValueKind.String, "Fast hung app timeout");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control", "WaitToKillServiceTimeout", "2000", RegistryValueKind.String, "Fast service kill timeout");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop", "AutoEndTasks", "1", RegistryValueKind.String, "Auto end tasks on shutdown");
            RunCommand("bcdedit /set useplatformtick yes",            "Platform tick (high-res timer)");
            RunCommand("bcdedit /deletevalue useplatformclock 2>nul", "Remove platform clock override");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", 0, RegistryValueKind.DWord, "Disable Windows Tips");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0, RegistryValueKind.DWord, "Disable suggested content");
        });

        public static List<TweakResult> UndoResponsivenessTweaks()
        {
            var r = RestoreCategory("Responsiveness");
            RunCommand("bcdedit /deletevalue useplatformtick 2>nul", "Restore platform tick default");
            return r;
        }

        // ── 4. GAMING ─────────────────────────────────────────────────────

        public static void ApplyGamingTweaks() => RunCategory("Gaming", () =>
        {
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",     "HwSchMode",                2,   RegistryValueKind.DWord,  "Enable HAGS");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar",                            "AllowAutoGameMode",        1,   RegistryValueKind.DWord,  "Enable Game Mode");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar",                            "AutoGameModeEnabled",      1,   RegistryValueKind.DWord,  "Enable Auto Game Mode");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse",                                   "MouseSpeed",               "0", RegistryValueKind.String, "Disable mouse acceleration");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse",                                   "MouseThreshold1",          "0", RegistryValueKind.String, "Mouse threshold 1");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse",                                   "MouseThreshold2",          "0", RegistryValueKind.String, "Mouse threshold 2");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",     "Win32PrioritySeparation",  38,  RegistryValueKind.DWord,  "CPU foreground priority boost");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR",     "AppCaptureEnabled",        0,   RegistryValueKind.DWord,  "Disable Game DVR capture");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\GameDVR",          "AllowGameDVR",             0,   RegistryValueKind.DWord,  "Disable Game DVR (policy)");
            SetRegistry(@"HKEY_CURRENT_USER\System\GameConfigStore",                                "GameDVR_FSEBehaviorMode",  2,   RegistryValueKind.DWord,  "Disable FSO globally");
            SetRegistry(@"HKEY_CURRENT_USER\System\GameConfigStore",                                "GameDVR_HonorUserFSEBehaviorMode", 1, RegistryValueKind.DWord, "Honor FSO setting");
        });

        public static List<TweakResult> UndoGamingTweaks() => RestoreCategory("Gaming");

        // ── 5. NETWORK ────────────────────────────────────────────────────

        public static void ApplyNetworkTweaks() => RunCategory("Network", () =>
        {
            DisableNaglesAlgorithm();
            RunCommand("netsh int tcp set global rss=enabled",            "Enable RSS");
            RunCommand("netsh int tcp set global autotuninglevel=normal", "TCP auto-tuning: normal");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xffffffff), RegistryValueKind.DWord, "Disable network throttling");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness",   0, RegistryValueKind.DWord, "Max multimedia responsiveness");
        });

        public static List<TweakResult> UndoNetworkTweaks()
        {
            var r = RestoreCategory("Network");
            RestoreNaglesAlgorithm();
            RunCommand("netsh int tcp set global autotuninglevel=normal", "Restore TCP auto-tuning");
            return r;
        }

        // ── 6. BLOATWARE ──────────────────────────────────────────────────

        private static readonly HashSet<string> _safeList = new(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.WindowsStore", "Microsoft.Windows.Photos",
            "Microsoft.WindowsCalculator", "Microsoft.WindowsNotepad",
            "Microsoft.Paint", "Microsoft.ScreenSketch", "Microsoft.WindowsTerminal"
        };

        public static void RemoveBloatware(Action<string> logCallback)
        {
            string[] patterns = {
                "*BingNews*","*BingWeather*","*BingSearch*","*ZuneVideo*","*ZuneMusic*",
                "*SkypeApp*","*SolitaireCollection*","*GetStarted*","*FeedbackHub*",
                "*WindowsMaps*","*YourPhone*","*PhoneLink*","*Clipchamp*","*MixedReality*",
                "*PowerAutomateDesktop*","*LinkedIn*","*Disney*","*Spotify*","*TikTok*",
                "*Instagram*","*Facebook*","*OfficeHub*","*OneNote*","*People*",
                "*ToDos*","*Todos*","*Widgets*","*Xbox.TCUI*","*XboxApp*",
                "*XboxGameOverlay*","*XboxGamingOverlay*","*XboxSpeechToTextOverlay*",
                "*3DViewer*","*Print3D*","*Wallet*","*Advertising*"
            };
            foreach (string pattern in patterns)
            {
                bool safe = false;
                foreach (string s in _safeList) if (pattern.Contains(s, StringComparison.OrdinalIgnoreCase)) { safe = true; break; }
                if (safe) continue;
                string name = pattern.Replace("*", "").Trim();
                logCallback?.Invoke($"Removing {name}...");
                RunPowerShell($"Get-AppxPackage {pattern} | Remove-AppxPackage -ErrorAction SilentlyContinue", $"Remove (user) {name}");
                RunPowerShell($"Get-AppxProvisionedPackage -Online | Where-Object {{ $_.PackageName -like '{pattern}' }} | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue", $"Remove (provisioned) {name}");
            }
            logCallback?.Invoke("Bloatware removal complete.");
        }

        // ── 7. ADVANCED TWEAKS ────────────────────────────────────────────
        // Each tweak is gated by the key string from AdvancedTweakDialog.
        // Only tweaks whose keys appear in selectedKeys are applied.

        public static void ApplyAdvancedTweaks(HashSet<string> selectedKeys) => RunCategory("Advanced", () =>
        {
            // ── Processor Scheduling → Programs ───────────────────────────
            // Win32PrioritySeparation = 0x26 (38):
            //   Bits 0-1 = 10 (variable interval)   Bits 2-3 = 10 (long intervals)
            //   Bits 4-5 = 10 (foreground boost = 3x) → programs get maximum CPU time
            if (selectedKeys.Contains("ProcessorScheduling"))
                SetRegistry(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                    "Win32PrioritySeparation", 38, RegistryValueKind.DWord,
                    "Processor Scheduling → Programs (Win32PrioritySeparation=38)");

            // ── Disable Dynamic Tick ──────────────────────────────────────
            // Forces a constant high-resolution timer interrupt (IRQ8).
            // Prevents Windows coarsening the clock when the CPU is idle,
            // which eliminates timer-related micro-stutter in games.
            if (selectedKeys.Contains("DisableDynamicTick"))
                RunCommand("bcdedit /set disabledynamictick yes",
                    "Disable dynamic tick (constant high-res IRQ8 timer)");

            // ── Disable CPU Throttling for Background Processes ───────────
            // The THROTTLE_POLICY PowerSetting controls whether Windows
            // is allowed to throttle background process CPU frequency.
            // Setting ValueMax = 0 tells the power policy "no throttling allowed."
            if (selectedKeys.Contains("DisableCpuThrottling"))
            {
                const string throttlePath =
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\" +
                    @"54533251-82be-4824-96c1-47b60b740d00\893dee8e-2bef-41e0-89c6-b55d0929964c";
                SetRegistry(throttlePath, "ValueMax", 0, RegistryValueKind.DWord,
                    "Disable CPU throttling for background processes");

                // Also push it into the active scheme via powercfg for immediate effect
                RunCommand(
                    "powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFAUTONOMOUS 0 & " +
                    "powercfg -setactive SCHEME_CURRENT",
                    "Apply CPU throttle policy to active power scheme");
            }

            // ── Enable SSD TRIM ───────────────────────────────────────────
            // disabledeletenotify = 0 means TRIM notifications ARE sent (enabled).
            // This is the Windows default, but it can get reset by OEM tools.
            if (selectedKeys.Contains("EnableTrim"))
                RunCommand("fsutil behavior set disabledeletenotify 0",
                    "Enable SSD TRIM (disabledeletenotify=0)");

            // ── Aggressive Animation Disabling ────────────────────────────
            // UserPreferencesMask: byte-level bitmask controlling many UI animations.
            // The value below (hex 90,12,03,80,10,00,00,00) disables:
            //   • Animate windows when minimizing/maximizing   (bit 0)
            //   • Animate controls and elements inside windows (bit 3)
            //   • Fade or slide menus into view                (bit 1)
            //   • Fade or slide tooltips into view             (bit 4)
            //   • Show shadows under mouse pointer             (bit 2)
            //   • Smooth-scroll list boxes                     (bit 5)
            // This goes beyond VisualFXSetting=2 which only sets the SystemParametersInfo
            // SPI_SETVISUALEFFECTS flag without touching the individual bits.
            if (selectedKeys.Contains("AggressiveAnimations"))
            {
                SetRegistry(
                    @"HKEY_CURRENT_USER\Control Panel\Desktop",
                    "UserPreferencesMask",
                    new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 },
                    RegistryValueKind.Binary,
                    "UserPreferencesMask — disable all UI animations");

                SetRegistry(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "TaskbarAnimations", 0, RegistryValueKind.DWord,
                    "Disable taskbar animations");

                SetRegistry(
                    @"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics",
                    "MinAnimate", "0", RegistryValueKind.String,
                    "Disable minimize/maximize animations");

                SetRegistry(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "ListviewShadow", 0, RegistryValueKind.DWord,
                    "Disable listview drop shadows");

                SetRegistry(
                    @"HKEY_CURRENT_USER\Control Panel\Desktop",
                    "FontSmoothing", "2", RegistryValueKind.String,
                    "Keep ClearType font smoothing (retain readability)");
            }
        });

        public static List<TweakResult> UndoAdvancedTweaks()
        {
            var r = RestoreCategory("Advanced");
            // Undo bcdedit changes that aren't registry-backed
            RunCommand("bcdedit /deletevalue disabledynamictick 2>nul", "Restore dynamic tick default");
            return r;
        }

        // ── NAGLE'S ALGORITHM ─────────────────────────────────────────────

        public static void DisableNaglesAlgorithm()
        {
            const string path = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
            try
            {
                using RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(path, writable: true);
                if (baseKey == null) { _results.Add(new TweakResult { Name = "Disable Nagle's Algorithm", Success = false, Error = "Base key not found" }); return; }
                foreach (string sub in baseKey.GetSubKeyNames())
                {
                    using RegistryKey subKey = baseKey.OpenSubKey(sub, writable: true);
                    subKey?.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                    subKey?.SetValue("TCPNoDelay",      1, RegistryValueKind.DWord);
                }
                _results.Add(new TweakResult { Name = "Disable Nagle's Algorithm", Success = true });
            }
            catch (Exception ex) { _results.Add(new TweakResult { Name = "Disable Nagle's Algorithm", Success = false, Error = ex.Message }); }
        }

        public static void RestoreNaglesAlgorithm()
        {
            const string path = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
            try
            {
                using RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(path, writable: true);
                if (baseKey == null) return;
                foreach (string sub in baseKey.GetSubKeyNames())
                {
                    using RegistryKey subKey = baseKey.OpenSubKey(sub, writable: true);
                    subKey?.DeleteValue("TcpAckFrequency", false);
                    subKey?.DeleteValue("TCPNoDelay",      false);
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[NAGLE RESTORE] {ex.Message}"); }
        }

        // ── SYSTEM RESTORE POINT ──────────────────────────────────────────
        // Uses Checkpoint-Computer (PowerShell / WMI) which requires:
        //   • Admin privileges
        //   • System Protection enabled on the C: drive
        //   • The VSS service to be running
        // Returns true if the restore point was created successfully.
        public static bool CreateRestorePoint(string description)
        {
            try
            {
                // First ensure System Restore is enabled on the system drive.
                // vssadmin list shadowstorage would be more accurate but
                // Checkpoint-Computer gives us the clearest success/failure signal.
                var psi = new ProcessStartInfo
                {
                    FileName  = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command " +
                                $"\"Checkpoint-Computer -Description '{description.Replace("'", "")}' -RestorePointType MODIFY_SETTINGS\"",
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };
                using var p = Process.Start(psi);
                p.WaitForExit();

                // Exit code 0 = success. Non-zero can mean "too soon since last RP"
                // (Windows throttles restore points to one per 24h by default).
                // We treat both as a soft-success — the previous RP is still valid.
                if (p.ExitCode != 0)
                {
                    string err = p.StandardError.ReadToEnd().Trim();
                    Debug.WriteLine($"[RESTORE POINT] PS exit {p.ExitCode}: {err}");
                    // Check if it failed due to the 24h frequency limit — not a real failure
                    if (err.Contains("0x80042306") || err.Contains("too soon") || err.Contains("frequency"))
                        return true;   // previous RP within 24h is fine
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RESTORE POINT FAIL] {ex.Message}");
                return false;
            }
        }
    }
}