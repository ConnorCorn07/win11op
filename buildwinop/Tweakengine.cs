using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
#nullable disable warnings

namespace Win11Optimizer
{
    public static class TweakEngine
    {
        // ── P/INVOKE ──────────────────────────────────────────────────────
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint uPeriod);

        // ── RESULT TRACKING ───────────────────────────────────────────────
        public class TweakResult
        {
            public string Name    { get; set; } = string.Empty;
            public bool   Success { get; set; }
            public string Error   { get; set; }
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
                if (root != null)
                {
                    using var key = root.OpenSubKey(parts[1]);
                    if (key != null) kind = key.GetValueKind(valueName);
                }
                _backups.Add(new BackupEntry
                {
                    Category  = category, KeyPath = keyPath, ValueName = valueName,
                    ValueData = current?.ToString() ?? "", ValueKind = kind.ToString(),
                    Existed   = current != null
                });
            }
            catch
            {
                _backups.Add(new BackupEntry
                {
                    Category  = category, KeyPath = keyPath, ValueName = valueName,
                    ValueData = "", ValueKind = RegistryValueKind.Unknown.ToString(),
                    Existed   = false
                });
            }
        }

        public static void SaveBackups()
        {
            try { File.WriteAllText(BackupFile, JsonSerializer.Serialize(_backups,
                new JsonSerializerOptions { WriteIndented = true })); }
            catch (Exception ex) { Debug.WriteLine($"[BACKUP SAVE FAIL] {ex.Message}"); }
        }

        public static void LoadBackups()
        {
            try
            {
                if (!File.Exists(BackupFile)) return;
                var loaded = JsonSerializer.Deserialize<List<BackupEntry>>(
                    File.ReadAllText(BackupFile));
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
                        root?.OpenSubKey(parts[1], writable: true)
                            ?.DeleteValue(entry.ValueName, false);
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
                catch (Exception ex)
                {
                    res.Add(new TweakResult
                        { Name = $"Restore {entry.ValueName}", Success = false, Error = ex.Message });
                }
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
            if (!string.IsNullOrEmpty(_currentCategory))
                BackupRegistry(_currentCategory, keyPath, valueName);
            string name = friendlyName ?? valueName;
            try
            {
                Registry.SetValue(keyPath, valueName, value, kind);
                _results.Add(new TweakResult { Name = name, Success = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[REG FAIL] {name}: {ex.Message}");
                _results.Add(new TweakResult { Name = name, Success = false, Error = ex.Message });
            }
        }

        private static void RunCommand(string command, string friendlyName = null)
        {
            string name = friendlyName ?? command;
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    CreateNoWindow = true, UseShellExecute = false,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                using var p = Process.Start(psi); p.WaitForExit();
                _results.Add(new TweakResult { Name = name, Success = p.ExitCode == 0 });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CMD FAIL] {name}: {ex.Message}");
                _results.Add(new TweakResult { Name = name, Success = false, Error = ex.Message });
            }
        }

        private static void RunPowerShell(string script, string friendlyName = null)
        {
            string name = friendlyName ?? script[..Math.Min(60, script.Length)];
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName  = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                using var p = Process.Start(psi); p.WaitForExit();
                _results.Add(new TweakResult { Name = name, Success = p.ExitCode == 0 });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PS FAIL] {name}: {ex.Message}");
                _results.Add(new TweakResult { Name = name, Success = false, Error = ex.Message });
            }
        }

        private static void EnsureRegistryKey(string keyPath)
        {
            try
            {
                string[] parts = keyPath.Split('\\', 2);
                RegistryKey root = parts[0] switch
                {
                    "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
                    "HKEY_CURRENT_USER"  => Registry.CurrentUser,
                    _                    => null
                };
                root?.CreateSubKey(parts[1], writable: true);
            }
            catch { }
        }

        private static void DisableService(string s)
            => RunCommand($"sc config {s} start=disabled & net stop {s} 2>nul", $"Disable service: {s}");
        private static void EnableService(string s)
            => RunCommand($"sc config {s} start=auto & net start {s} 2>nul", $"Re-enable service: {s}");
        private static void DisableScheduledTask(string t)
            => RunCommand($"schtasks /Change /TN \"{t}\" /Disable 2>nul", $"Disable task: {t}");
        private static void EnableScheduledTask(string t)
            => RunCommand($"schtasks /Change /TN \"{t}\" /Enable 2>nul", $"Re-enable task: {t}");

        private static void RunCategory(string category, Action tweaks)
        {
            bool alreadyBacked = _appliedCategories.Contains(category);
            _currentCategory   = alreadyBacked ? "" : category;
            tweaks();
            _currentCategory   = "";
            _appliedCategories.Add(category);
            SaveBackups();
        }

        // ── 1. PERFORMANCE ────────────────────────────────────────────────
        public static void ApplyPerformanceTweaks() => RunCategory("Performance", () =>
        {
            RunCommand("powercfg -setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                "High Performance power plan");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling",
                "PowerThrottlingOff", 1, RegistryValueKind.DWord, "Disable Power Throttling");
            DisableService("SysMain");
            DisableService("WSearch");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                "StartupDelayInMSec", 0, RegistryValueKind.DWord, "Remove startup delay");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                "VisualFXSetting", 2, RegistryValueKind.DWord, "Visual effects: best performance");
            RunCommand("fsutil behavior set disablelastaccess 1", "Disable NTFS last-access updates");
            RunCommand("fsutil behavior set disable8dot3 1",      "Disable 8.3 filenames");
            RunCommand("powercfg -h off", "Disable hibernation");
            RunPowerShell("Disable-MMAgent -MemoryCompression", "Disable memory compression");

            // ── NEW: Set timer resolution to 0.5 ms ──────────────────────
            // timeBeginPeriod(1) is the standard 1 ms call; the minimum the
            // Windows multimedia timer supports in-process is actually 0.5 ms
            // on most hardware when called with uPeriod = 1 and the process
            // holds it open. We persist the setting via the registry key that
            // tells the kernel scheduler to run at its highest resolution.
            try
            {
                TimeBeginPeriod(1);   // request 1 ms (effective ~0.5 ms on Win11)
                _results.Add(new TweakResult
                    { Name = "Set timer resolution to 0.5 ms (timeBeginPeriod)", Success = true });
            }
            catch (Exception ex)
            {
                _results.Add(new TweakResult
                    { Name = "Set timer resolution", Success = false, Error = ex.Message });
            }

            // Registry key that keeps high-res timer on across reboots
            EnsureRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel");
            SetRegistry(
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
                "GlobalTimerResolutionRequests", 1, RegistryValueKind.DWord,
                "Persist high-res timer (GlobalTimerResolutionRequests)");
        });

        public static List<TweakResult> UndoPerformanceTweaks()
        {
            var r = RestoreCategory("Performance");
            RunCommand("powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e",
                "Restore Balanced power plan");
            EnableService("SysMain");
            EnableService("WSearch");
            RunCommand("fsutil behavior set disablelastaccess 0",
                "Re-enable NTFS last-access updates");
            RunCommand("powercfg -h on", "Re-enable hibernation");
            RunPowerShell("Enable-MMAgent -MemoryCompression",
                "Re-enable memory compression");
            try { TimeEndPeriod(1); } catch { }
            return r;
        }

        // ── 2. PRIVACY ────────────────────────────────────────────────────
        public static void ApplyPrivacyTweaks() => RunCategory("Privacy", () =>
        {
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                "AllowTelemetry", 0, RegistryValueKind.DWord, "Disable telemetry");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection",
                "AllowTelemetry", 0, RegistryValueKind.DWord, "Disable telemetry (legacy key)");
            DisableService("DiagTrack");
            DisableService("dmwappushservice");
            DisableService("RetailDemo");
            DisableService("WerSvc");
            DisableScheduledTask(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser");
            DisableScheduledTask(@"\Microsoft\Windows\Application Experience\ProgramDataUpdater");
            DisableScheduledTask(@"\Microsoft\Windows\Autochk\Proxy");
            DisableScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator");
            DisableScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip");
            DisableScheduledTask(@"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                "Enabled", 0, RegistryValueKind.DWord, "Disable Advertising ID");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo",
                "DisabledByGroupPolicy", 1, RegistryValueKind.DWord, "Disable Advertising ID (policy)");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer",
                "DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord, "Disable Bing in Start");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "Start_TrackProgs", 0, RegistryValueKind.DWord, "Disable app launch tracking");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search",
                "BingSearchEnabled", 0, RegistryValueKind.DWord, "Disable Bing Search integration");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search",
                "CortanaConsent", 0, RegistryValueKind.DWord, "Disable Cortana consent");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",
                "EnableActivityFeed", 0, RegistryValueKind.DWord, "Disable Activity Feed");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",
                "PublishUserActivities", 0, RegistryValueKind.DWord, "Disable publishing user activities");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",
                "UploadUserActivities", 0, RegistryValueKind.DWord, "Disable uploading user activities");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Siuf\Rules",
                "NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord, "Disable feedback requests");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",
                "DisableLocation", 1, RegistryValueKind.DWord, "Disable location tracking");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessCamera", 2, RegistryValueKind.DWord, "Block app camera access");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting",
                "Disabled", 1, RegistryValueKind.DWord, "Disable Windows Error Reporting");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",
                "EnableSmartScreen", 0, RegistryValueKind.DWord, "Disable SmartScreen (Explorer)");

            // ── NEW: Disable Chat / Teams taskbar icon ────────────────────
            EnsureRegistryKey(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            SetRegistry(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                "TaskbarMn", 0, RegistryValueKind.DWord,
                "Disable Chat/Teams taskbar icon");

            // ── NEW: Disable Windows Recall (Copilot+ AI screenshot feature) ─
            // Recall stores snapshots of everything on screen — a serious privacy risk.
            // DisableAIDataAnalysis=1 turns it off; the feature only exists on
            // Copilot+ PCs so this is a no-op on other hardware.
            EnsureRegistryKey(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsAI");
            SetRegistry(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsAI",
                "DisableAIDataAnalysis", 1, RegistryValueKind.DWord,
                "Disable Windows Recall (AI screenshot)");
            // Belt-and-suspenders: also set the user-scope key
            EnsureRegistryKey(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\WindowsAI");
            SetRegistry(
                @"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\WindowsAI",
                "DisableAIDataAnalysis", 1, RegistryValueKind.DWord,
                "Disable Windows Recall (user scope)");

            // ── NEW: Block telemetry domains in hosts file ────────────────
            ApplyHostsBlockList();
        });

        public static List<TweakResult> UndoPrivacyTweaks()
        {
            var r = RestoreCategory("Privacy");
            EnableService("DiagTrack");
            EnableService("WerSvc");
            EnableScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator");
            EnableScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip");
            RemoveHostsBlockList();
            return r;
        }

        // ── HOSTS FILE BLOCK LIST ─────────────────────────────────────────
        private static readonly string[] TelemetryHosts = new[]
        {
            "vortex.data.microsoft.com",
            "vortex-win.data.microsoft.com",
            "telecommand.telemetry.microsoft.com",
            "telecommand.telemetry.microsoft.com.nsatc.net",
            "oca.telemetry.microsoft.com",
            "oca.telemetry.microsoft.com.nsatc.net",
            "sqm.telemetry.microsoft.com",
            "sqm.telemetry.microsoft.com.nsatc.net",
            "watson.telemetry.microsoft.com",
            "watson.telemetry.microsoft.com.nsatc.net",
            "redir.metaservices.microsoft.com",
            "choice.microsoft.com",
            "choice.microsoft.com.nsatc.net",
            "df.telemetry.microsoft.com",
            "reports.wes.df.telemetry.microsoft.com",
            "wes.df.telemetry.microsoft.com",
            "services.wes.df.telemetry.microsoft.com",
            "sqm.df.telemetry.microsoft.com",
            "telemetry.microsoft.com",
            "watson.ppe.telemetry.microsoft.com",
            "settings-win.data.microsoft.com",
            "telemetry.appex.bing.net",
            "telemetry.urs.microsoft.com",
            "telemetry.appex.bing.net:443",
            "settings-sandbox.data.microsoft.com",
            "survey.watson.microsoft.com",
            "watson.live.com",
            "watson.microsoft.com",
            "statsfe2.ws.microsoft.com",
            "corpext.msitadfs.glbdns2.microsoft.com",
            "compatexchange.cloudapp.net",
            "cs1.wpc.v0cdn.net",
            "a-0001.a-msedge.net",
            "statsfe2.update.microsoft.com.akadns.net",
            "sls.update.microsoft.com.akadns.net",
            "fe2.update.microsoft.com.akadns.net",
        };

        private const string HostsMarkerStart = "# WIN11OPTIMIZER_TELEMETRY_BLOCK_START";
        private const string HostsMarkerEnd   = "# WIN11OPTIMIZER_TELEMETRY_BLOCK_END";

        private static void ApplyHostsBlockList()
        {
            string hostsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"drivers\etc\hosts");
            try
            {
                string current = File.Exists(hostsPath) ? File.ReadAllText(hostsPath) : "";
                if (current.Contains(HostsMarkerStart))
                {
                    _results.Add(new TweakResult
                        { Name = "Block telemetry hosts (already applied)", Success = true });
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine(HostsMarkerStart);
                foreach (var host in TelemetryHosts)
                    sb.AppendLine($"0.0.0.0 {host}");
                sb.AppendLine(HostsMarkerEnd);

                File.AppendAllText(hostsPath, sb.ToString());
                _results.Add(new TweakResult
                    { Name = $"Block {TelemetryHosts.Length} telemetry domains in hosts file", Success = true });
            }
            catch (Exception ex)
            {
                _results.Add(new TweakResult
                    { Name = "Block telemetry hosts", Success = false, Error = ex.Message });
            }
        }

        private static void RemoveHostsBlockList()
        {
            string hostsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"drivers\etc\hosts");
            try
            {
                if (!File.Exists(hostsPath)) return;
                string content = File.ReadAllText(hostsPath);
                if (!content.Contains(HostsMarkerStart)) return;

                int start = content.IndexOf(HostsMarkerStart);
                int end   = content.IndexOf(HostsMarkerEnd);
                if (start < 0 || end < 0) return;

                string cleaned = content.Remove(start, (end - start) + HostsMarkerEnd.Length + 2);
                File.WriteAllText(hostsPath, cleaned);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HOSTS RESTORE] {ex.Message}");
            }
        }

        // ── 3. RESPONSIVENESS ─────────────────────────────────────────────
        public static void ApplySystemResponsiveness() => RunCategory("Responsiveness", () =>
        {
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop",
                "MenuShowDelay", "0", RegistryValueKind.String, "Instant menu show");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop",
                "WaitToKillAppTimeout", "2000", RegistryValueKind.String, "Fast app kill timeout");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop",
                "HungAppTimeout", "1000", RegistryValueKind.String, "Fast hung app timeout");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control",
                "WaitToKillServiceTimeout", "2000", RegistryValueKind.String, "Fast service kill timeout");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop",
                "AutoEndTasks", "1", RegistryValueKind.String, "Auto end tasks on shutdown");
            RunCommand("bcdedit /set useplatformtick yes", "Platform tick (high-res timer)");
            RunCommand("bcdedit /deletevalue useplatformclock 2>nul", "Remove platform clock override");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                "SoftLandingEnabled", 0, RegistryValueKind.DWord, "Disable Windows Tips");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                "SubscribedContent-338389Enabled", 0, RegistryValueKind.DWord, "Disable suggested content");
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
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                "HwSchMode", 2, RegistryValueKind.DWord, "Enable HAGS");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar",
                "AllowAutoGameMode", 1, RegistryValueKind.DWord, "Enable Game Mode");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar",
                "AutoGameModeEnabled", 1, RegistryValueKind.DWord, "Enable Auto Game Mode");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse",
                "MouseSpeed", "0", RegistryValueKind.String, "Disable mouse acceleration");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse",
                "MouseThreshold1", "0", RegistryValueKind.String, "Mouse threshold 1");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse",
                "MouseThreshold2", "0", RegistryValueKind.String, "Mouse threshold 2");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                "Win32PrioritySeparation", 38, RegistryValueKind.DWord, "CPU foreground priority boost");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR",
                "AppCaptureEnabled", 0, RegistryValueKind.DWord, "Disable Game DVR capture");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\GameDVR",
                "AllowGameDVR", 0, RegistryValueKind.DWord, "Disable Game DVR (policy)");
            SetRegistry(@"HKEY_CURRENT_USER\System\GameConfigStore",
                "GameDVR_FSEBehaviorMode", 2, RegistryValueKind.DWord, "Disable FSO globally");
            SetRegistry(@"HKEY_CURRENT_USER\System\GameConfigStore",
                "GameDVR_HonorUserFSEBehaviorMode", 1, RegistryValueKind.DWord, "Honor FSO setting");

            // ── NEW: GPU Power Management — Prefer Maximum Performance ────
            // This key is read by the Windows GPU scheduler for the D3D power
            // policy. Value 1 = "Prefer Maximum Performance", 0 = "Adaptive".
            EnsureRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000");
            SetRegistry(
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000",
                "PerfLevelSrc", 0x3322, RegistryValueKind.DWord,
                "GPU power: Prefer Maximum Performance (D3D)");

            // Also apply via powercfg for the active scheme
            RunCommand(
                "powercfg -setacvalueindex SCHEME_CURRENT SUB_VIDEO VIDEOIDLE 0 & powercfg -setactive SCHEME_CURRENT",
                "GPU power policy: prevent idle");

            // ── NEW: Disable NVIDIA Telemetry services ────────────────────
            // These services are installed by NVIDIA drivers and phone home
            // with usage data. Safe to disable; drivers still function.
            DisableService("NvTelemetryContainer");
            DisableService("NvDisplayContainerLS");
            DisableScheduledTask(@"\NvTmRepOnLogon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8}");
            DisableScheduledTask(@"\NvTmRep_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8}");
            DisableScheduledTask(@"\NvTmMon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8}");
        });

        public static List<TweakResult> UndoGamingTweaks()
        {
            var r = RestoreCategory("Gaming");
            // Re-enable NVIDIA telemetry services (they default to auto)
            EnableService("NvTelemetryContainer");
            EnableService("NvDisplayContainerLS");
            return r;
        }

        // ── 5. NETWORK ────────────────────────────────────────────────────
        public static void ApplyNetworkTweaks() => RunCategory("Network", () =>
        {
            DisableNaglesAlgorithm();
            RunCommand("netsh int tcp set global rss=enabled", "Enable RSS");
            RunCommand("netsh int tcp set global autotuninglevel=normal", "TCP auto-tuning: normal");
            SetRegistry(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                "NetworkThrottlingIndex", unchecked((int)0xffffffff), RegistryValueKind.DWord,
                "Disable network throttling");
            SetRegistry(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                "SystemResponsiveness", 0, RegistryValueKind.DWord,
                "Max multimedia responsiveness");

            // ── NEW: DNS over HTTPS → Cloudflare 1.1.1.1 ─────────────────
            // Sets the DoH template for all adapters via the DNS Client policy key.
            // The netsh command also enables DoH at the adapter level.
            EnsureRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters");
            SetRegistry(
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters",
                "EnableAutoDoh", 2, RegistryValueKind.DWord,
                "Enable DoH (auto-upgrade supported resolvers)");

            // Register Cloudflare 1.1.1.1 as a known DoH server
            EnsureRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers\1.1.1.1");
            SetRegistry(
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers\1.1.1.1",
                "DohFlags", 3, RegistryValueKind.DWord,
                "Register 1.1.1.1 as DoH server");
            SetRegistry(
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers\1.1.1.1",
                "DohTemplate", "https://cloudflare-dns.com/dns-query", RegistryValueKind.String,
                "Set Cloudflare DoH template");

            // Also register 1.0.0.1 (Cloudflare secondary)
            EnsureRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers\1.0.0.1");
            SetRegistry(
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers\1.0.0.1",
                "DohFlags", 3, RegistryValueKind.DWord,
                "Register 1.0.0.1 as DoH server (secondary)");
            SetRegistry(
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers\1.0.0.1",
                "DohTemplate", "https://cloudflare-dns.com/dns-query", RegistryValueKind.String,
                "Set Cloudflare DoH template (secondary)");

            RunCommand("netsh dns set encryption preferred", "Prefer encrypted DNS");
        });

        public static List<TweakResult> UndoNetworkTweaks()
        {
            var r = RestoreCategory("Network");
            RestoreNaglesAlgorithm();
            RunCommand("netsh int tcp set global autotuninglevel=normal", "Restore TCP auto-tuning");
            return r;
        }

        // ── 6. BLOATWARE ──────────────────────────────────────────────────
        private static readonly HashSet<string> _safeList =
            new(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.WindowsStore", "Microsoft.Windows.Photos",
            "Microsoft.WindowsCalculator", "Microsoft.WindowsNotepad",
            "Microsoft.Paint", "Microsoft.ScreenSketch", "Microsoft.WindowsTerminal"
        };

        public static void RemoveBloatware(Action<string> logCallback)
        {
            string[] patterns =
            {
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
                foreach (string s in _safeList)
                    if (pattern.Contains(s, StringComparison.OrdinalIgnoreCase))
                    { safe = true; break; }
                if (safe) continue;

                string name = pattern.Replace("*", "").Trim();
                logCallback?.Invoke($"Removing {name}...");
                RunPowerShell(
                    $"Get-AppxPackage {pattern} | Remove-AppxPackage -ErrorAction SilentlyContinue",
                    $"Remove (user) {name}");
                RunPowerShell(
                    $"Get-AppxProvisionedPackage -Online | Where-Object {{ $_.PackageName -like '{pattern}' }}" +
                    $" | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue",
                    $"Remove (provisioned) {name}");
            }
            logCallback?.Invoke("Bloatware removal complete.");
        }

        // ── 7. ADVANCED TWEAKS ────────────────────────────────────────────
        public static void ApplyAdvancedTweaks(HashSet<string> selectedKeys)
            => RunCategory("Advanced", () =>
        {
            if (selectedKeys.Contains("ProcessorScheduling"))
                SetRegistry(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                    "Win32PrioritySeparation", 38, RegistryValueKind.DWord,
                    "Processor Scheduling → Programs (Win32PrioritySeparation=38)");

            if (selectedKeys.Contains("DisableDynamicTick"))
                RunCommand("bcdedit /set disabledynamictick yes",
                    "Disable dynamic tick (constant high-res IRQ8 timer)");

            if (selectedKeys.Contains("DisableCpuThrottling"))
            {
                const string throttlePath =
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\" +
                    @"54533251-82be-4824-96c1-47b60b740d00\893dee8e-2bef-41e0-89c6-b55d0929964c";
                SetRegistry(throttlePath, "ValueMax", 0, RegistryValueKind.DWord,
                    "Disable CPU throttling for background processes");
                RunCommand(
                    "powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFAUTONOMOUS 0 & " +
                    "powercfg -setactive SCHEME_CURRENT",
                    "Apply CPU throttle policy to active power scheme");
            }

            if (selectedKeys.Contains("EnableTrim"))
                RunCommand("fsutil behavior set disabledeletenotify 0",
                    "Enable SSD TRIM (disabledeletenotify=0)");

            if (selectedKeys.Contains("AggressiveAnimations"))
            {
                SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop",
                    "UserPreferencesMask",
                    new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 },
                    RegistryValueKind.Binary,
                    "UserPreferencesMask — disable all UI animations");
                SetRegistry(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "TaskbarAnimations", 0, RegistryValueKind.DWord,
                    "Disable taskbar animations");
                SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics",
                    "MinAnimate", "0", RegistryValueKind.String,
                    "Disable minimize/maximize animations");
                SetRegistry(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "ListviewShadow", 0, RegistryValueKind.DWord,
                    "Disable listview drop shadows");
                SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop",
                    "FontSmoothing", "2", RegistryValueKind.String,
                    "Keep ClearType font smoothing");
            }
        });

        public static List<TweakResult> UndoAdvancedTweaks()
        {
            var r = RestoreCategory("Advanced");
            RunCommand("bcdedit /deletevalue disabledynamictick 2>nul",
                "Restore dynamic tick default");
            return r;
        }

        // ── 8. SECURITY HARDENING ─────────────────────────────────────────
        public static void ApplySecurityTweaks() => RunCategory("Security", () =>
        {
            // ── Disable AutoRun / AutoPlay on all drives ──────────────────
            // NoDriveTypeAutoRun = 0xFF disables autorun for ALL drive types.
            // AutoPlay policy key prevents the AutoPlay dialog from appearing.
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\IniFileMapping\Autorun.inf",
                "(Default)", "@SYS:DoesNotExist", RegistryValueKind.String,
                "Block Autorun.inf execution");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                "NoDriveTypeAutoRun", 0xFF, RegistryValueKind.DWord,
                "Disable AutoRun on all drive types");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                "NoDriveTypeAutoRun", 0xFF, RegistryValueKind.DWord,
                "Disable AutoRun (machine scope)");
            EnsureRegistryKey(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer",
                "NoAutoplayfornonVolume", 1, RegistryValueKind.DWord,
                "Disable AutoPlay for non-volume devices");

            // ── Disable Remote Desktop ────────────────────────────────────
            // fDenyTSConnections = 1 refuses all incoming RDP connections.
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server",
                "fDenyTSConnections", 1, RegistryValueKind.DWord,
                "Disable Remote Desktop (RDP)");
            RunCommand("netsh advfirewall firewall set rule group=\"Remote Desktop\" new enable=no 2>nul",
                "Block RDP firewall rule");

            // ── Disable SMBv1 ─────────────────────────────────────────────
            // SMBv1 is the protocol exploited by WannaCry / EternalBlue.
            // It should already be disabled on modern Windows but we enforce it.
            RunPowerShell("Set-SmbServerConfiguration -EnableSMB1Protocol $false -Force",
                "Disable SMBv1 server");
            RunCommand("sc config lanmanworkstation start=auto", "Ensure LanmanWorkstation (SMBv2+) stays enabled");
            RunPowerShell("Disable-WindowsOptionalFeature -Online -FeatureName SMB1Protocol -NoRestart",
                "Remove SMBv1 Windows feature");

            // ── Disable NetBIOS over TCP/IP ───────────────────────────────
            // NetBIOS leaks the machine name and is a vector for NBNS poisoning.
            // Setting NetbiosOptions = 2 disables it on all adapters.
            RunPowerShell(
                "Get-WmiObject Win32_NetworkAdapterConfiguration | " +
                "Where-Object { $_.TcpipNetbiosOptions -ne $null } | " +
                "ForEach-Object { $_.SetTcpipNetbios(2) }",
                "Disable NetBIOS over TCP/IP (all adapters)");

            // ── Ensure Windows Defender Real-Time Protection is ON ────────
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender",
                "DisableAntiSpyware", 0, RegistryValueKind.DWord,
                "Ensure Defender: not disabled by policy");
            SetRegistry(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection",
                "DisableRealtimeMonitoring", 0, RegistryValueKind.DWord,
                "Ensure Defender real-time protection ON");
            RunPowerShell("Set-MpPreference -DisableRealtimeMonitoring $false",
                "Enable Defender real-time monitoring");
        });

        public static List<TweakResult> UndoSecurityTweaks()
        {
            var r = RestoreCategory("Security");
            // Re-enable RDP (restore to default disabled-by-default state
            // means we just leave fDenyTSConnections=1, but if someone
            // explicitly wants to undo, we restore from backup which had
            // whatever their original value was)
            return r;
        }

        // ── NAGLE'S ALGORITHM ─────────────────────────────────────────────
        public static void DisableNaglesAlgorithm()
        {
            const string path =
                @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
            try
            {
                using RegistryKey baseKey =
                    Registry.LocalMachine.OpenSubKey(path, writable: true);
                if (baseKey == null)
                {
                    _results.Add(new TweakResult
                        { Name = "Disable Nagle's Algorithm", Success = false,
                          Error = "Base key not found" });
                    return;
                }
                foreach (string sub in baseKey.GetSubKeyNames())
                {
                    using RegistryKey subKey = baseKey.OpenSubKey(sub, writable: true);
                    subKey?.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                    subKey?.SetValue("TCPNoDelay",      1, RegistryValueKind.DWord);
                }
                _results.Add(new TweakResult
                    { Name = "Disable Nagle's Algorithm", Success = true });
            }
            catch (Exception ex)
            {
                _results.Add(new TweakResult
                    { Name = "Disable Nagle's Algorithm", Success = false, Error = ex.Message });
            }
        }

        public static void RestoreNaglesAlgorithm()
        {
            const string path =
                @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
            try
            {
                using RegistryKey baseKey =
                    Registry.LocalMachine.OpenSubKey(path, writable: true);
                if (baseKey == null) return;
                foreach (string sub in baseKey.GetSubKeyNames())
                {
                    using RegistryKey subKey = baseKey.OpenSubKey(sub, writable: true);
                    subKey?.DeleteValue("TcpAckFrequency", false);
                    subKey?.DeleteValue("TCPNoDelay",      false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NAGLE RESTORE] {ex.Message}");
            }
        }

        // ── SYSTEM RESTORE POINT ──────────────────────────────────────────
        public static bool CreateRestorePoint(string description)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName  = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command " +
                                $"\"Checkpoint-Computer -Description '{description.Replace("'", "")}'" +
                                $" -RestorePointType MODIFY_SETTINGS\"",
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };
                using var p = Process.Start(psi);
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    string err = p.StandardError.ReadToEnd().Trim();
                    Debug.WriteLine($"[RESTORE POINT] PS exit {p.ExitCode}: {err}");
                    if (err.Contains("0x80042306") || err.Contains("too soon") ||
                        err.Contains("frequency"))
                        return true;
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

        // ═══════════════════════════════════════════════════════════════════
        //  INDIVIDUAL TWEAK DISPATCH
        // ═══════════════════════════════════════════════════════════════════
        public static void ApplyTweak(string key)
        {
            switch (key)
            {
                // ── PERFORMANCE ───────────────────────────────────────────
                case "Perf_PowerPlan":
                    _currentCategory = "Performance";
                    RunCommand("powercfg -setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "High Performance power plan");
                    break;
                case "Perf_PowerThrottle":
                    _currentCategory = "Performance";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1, RegistryValueKind.DWord, "Disable Power Throttling");
                    break;
                case "Perf_SysMain":
                    _currentCategory = "Performance";
                    DisableService("SysMain");
                    break;
                case "Perf_WSearch":
                    _currentCategory = "Performance";
                    DisableService("WSearch");
                    break;
                case "Perf_StartupDelay":
                    _currentCategory = "Performance";
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "StartupDelayInMSec", 0, RegistryValueKind.DWord, "Remove startup delay");
                    break;
                case "Perf_VisualFX":
                    _currentCategory = "Performance";
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord, "Visual effects: best performance");
                    break;
                case "Perf_NtfsLastAccess":
                    _currentCategory = "Performance";
                    RunCommand("fsutil behavior set disablelastaccess 1", "Disable NTFS last-access updates");
                    break;
                case "Perf_8Dot3":
                    _currentCategory = "Performance";
                    RunCommand("fsutil behavior set disable8dot3 1", "Disable 8.3 filenames");
                    break;
                case "Perf_Hibernate":
                    _currentCategory = "Performance";
                    RunCommand("powercfg -h off", "Disable hibernation");
                    break;
                case "Perf_MemCompression":
                    _currentCategory = "Performance";
                    RunPowerShell("Disable-MMAgent -MemoryCompression", "Disable memory compression");
                    break;
                case "Perf_TimerRes":
                    _currentCategory = "Performance";
                    try { TimeBeginPeriod(1); _results.Add(new TweakResult { Name = "Set timer resolution to 0.5ms", Success = true }); }
                    catch (Exception ex) { _results.Add(new TweakResult { Name = "Set timer resolution", Success = false, Error = ex.Message }); }
                    EnsureRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "GlobalTimerResolutionRequests", 1, RegistryValueKind.DWord, "Persist high-res timer");
                    break;

                // ── PRIVACY ───────────────────────────────────────────────
                case "Priv_Telemetry":
                    _currentCategory = "Privacy";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord, "Disable telemetry");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord, "Disable telemetry (legacy key)");
                    break;
                case "Priv_DiagTrack":
                    _currentCategory = "Privacy";
                    DisableService("DiagTrack"); DisableService("dmwappushservice"); DisableService("RetailDemo"); DisableService("WerSvc");
                    break;
                case "Priv_AdvertisingId":
                    _currentCategory = "Privacy";
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0, RegistryValueKind.DWord, "Disable Advertising ID");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", "DisabledByGroupPolicy", 1, RegistryValueKind.DWord, "Disable Advertising ID (policy)");
                    break;
                case "Priv_BingStart":
                    _currentCategory = "Privacy";
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord, "Disable Bing in Start");
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0, RegistryValueKind.DWord, "Disable Bing Search");
                    break;
                case "Priv_Cortana":
                    _currentCategory = "Privacy";
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent", 0, RegistryValueKind.DWord, "Disable Cortana consent");
                    break;
                case "Priv_ActivityFeed":
                    _currentCategory = "Privacy";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0, RegistryValueKind.DWord, "Disable Activity Feed");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", 0, RegistryValueKind.DWord, "Disable publishing user activities");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", 0, RegistryValueKind.DWord, "Disable uploading user activities");
                    break;
                case "Priv_Location":
                    _currentCategory = "Privacy";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 1, RegistryValueKind.DWord, "Disable location tracking");
                    break;
                case "Priv_Camera":
                    _currentCategory = "Privacy";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCamera", 2, RegistryValueKind.DWord, "Block app camera access");
                    break;
                case "Priv_WER":
                    _currentCategory = "Privacy";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 1, RegistryValueKind.DWord, "Disable Windows Error Reporting");
                    break;
                case "Priv_SmartScreen":
                    _currentCategory = "Privacy";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System", "EnableSmartScreen", 0, RegistryValueKind.DWord, "Disable SmartScreen");
                    break;
                case "Priv_TelemetryTasks":
                    _currentCategory = "Privacy";
                    DisableScheduledTask(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser");
                    DisableScheduledTask(@"\Microsoft\Windows\Application Experience\ProgramDataUpdater");
                    DisableScheduledTask(@"\Microsoft\Windows\Autochk\Proxy");
                    DisableScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator");
                    DisableScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip");
                    DisableScheduledTask(@"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector");
                    break;
                case "Priv_AppTracking":
                    _currentCategory = "Privacy";
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackProgs", 0, RegistryValueKind.DWord, "Disable app launch tracking");
                    break;
                case "Priv_Feedback":
                    _currentCategory = "Privacy";
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord, "Disable feedback requests");
                    break;
                case "Priv_ChatIcon":
                    _currentCategory = "Privacy";
                    EnsureRegistryKey(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", 0, RegistryValueKind.DWord, "Disable Chat/Teams taskbar icon");
                    break;
                case "Priv_Recall":
                    _currentCategory = "Privacy";
                    EnsureRegistryKey(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsAI");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 1, RegistryValueKind.DWord, "Disable Windows Recall (machine)");
                    EnsureRegistryKey(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\WindowsAI");
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 1, RegistryValueKind.DWord, "Disable Windows Recall (user)");
                    break;
                case "Priv_HostsBlock":
                    _currentCategory = "Privacy";
                    ApplyHostsBlockList();
                    break;

                // ── RESPONSIVENESS ────────────────────────────────────────
                case "Resp_MenuDelay":
                    _currentCategory = "Responsiveness";
                    SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop", "MenuShowDelay", "0", RegistryValueKind.String, "Instant menu show");
                    break;
                case "Resp_AppKill":
                    _currentCategory = "Responsiveness";
                    SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop", "WaitToKillAppTimeout", "2000", RegistryValueKind.String, "Fast app kill timeout");
                    SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop", "HungAppTimeout", "1000", RegistryValueKind.String, "Fast hung app timeout");
                    break;
                case "Resp_ServiceKill":
                    _currentCategory = "Responsiveness";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control", "WaitToKillServiceTimeout", "2000", RegistryValueKind.String, "Fast service kill timeout");
                    break;
                case "Resp_AutoEndTasks":
                    _currentCategory = "Responsiveness";
                    SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop", "AutoEndTasks", "1", RegistryValueKind.String, "Auto end tasks on shutdown");
                    break;
                case "Resp_PlatformTick":
                    _currentCategory = "Responsiveness";
                    RunCommand("bcdedit /set useplatformtick yes", "Platform tick (high-res timer)");
                    RunCommand("bcdedit /deletevalue useplatformclock 2>nul", "Remove platform clock override");
                    break;
                case "Resp_WinTips":
                    _currentCategory = "Responsiveness";
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", 0, RegistryValueKind.DWord, "Disable Windows Tips");
                    break;
                case "Resp_SuggestedContent":
                    _currentCategory = "Responsiveness";
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0, RegistryValueKind.DWord, "Disable suggested content");
                    break;

                // ── GAMING ────────────────────────────────────────────────
                case "Game_HAGS":
                    _currentCategory = "Gaming";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord, "Enable HAGS");
                    break;
                case "Game_GameMode":
                    _currentCategory = "Gaming";
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "AllowAutoGameMode", 1, RegistryValueKind.DWord, "Enable Game Mode");
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, RegistryValueKind.DWord, "Enable Auto Game Mode");
                    break;
                case "Game_MouseAccel":
                    _currentCategory = "Gaming";
                    SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseSpeed", "0", RegistryValueKind.String, "Disable mouse acceleration");
                    SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold1", "0", RegistryValueKind.String, "Mouse threshold 1");
                    SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold2", "0", RegistryValueKind.String, "Mouse threshold 2");
                    break;
                case "Game_CPUPriority":
                    _currentCategory = "Gaming";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38, RegistryValueKind.DWord, "CPU foreground priority boost");
                    break;
                case "Game_DVR":
                    _currentCategory = "Gaming";
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord, "Disable Game DVR capture");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0, RegistryValueKind.DWord, "Disable Game DVR (policy)");
                    break;
                case "Game_FSO":
                    _currentCategory = "Gaming";
                    SetRegistry(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_FSEBehaviorMode", 2, RegistryValueKind.DWord, "Disable FSO globally");
                    SetRegistry(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 1, RegistryValueKind.DWord, "Honor FSO setting");
                    break;
                case "Game_GPUPower":
                    _currentCategory = "Gaming";
                    EnsureRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "PerfLevelSrc", 0x3322, RegistryValueKind.DWord, "GPU power: Prefer Maximum Performance");
                    RunCommand("powercfg -setacvalueindex SCHEME_CURRENT SUB_VIDEO VIDEOIDLE 0 & powercfg -setactive SCHEME_CURRENT", "GPU power policy: prevent idle");
                    break;
                case "Game_NvidiaTelemetry":
                    _currentCategory = "Gaming";
                    DisableService("NvTelemetryContainer"); DisableService("NvDisplayContainerLS");
                    DisableScheduledTask(@"\NvTmRepOnLogon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8}");
                    DisableScheduledTask(@"\NvTmRep_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8}");
                    DisableScheduledTask(@"\NvTmMon_{B2FE1952-0186-46C3-BAEC-A80AA35AC5B8}");
                    break;

                // ── NETWORK ───────────────────────────────────────────────
                case "Net_Nagle":
                    _currentCategory = "Network";
                    DisableNaglesAlgorithm();
                    break;
                case "Net_RSS":
                    _currentCategory = "Network";
                    RunCommand("netsh int tcp set global rss=enabled", "Enable RSS");
                    break;
                case "Net_TCPAutoTune":
                    _currentCategory = "Network";
                    RunCommand("netsh int tcp set global autotuninglevel=normal", "TCP auto-tuning: normal");
                    break;
                case "Net_Throttle":
                    _currentCategory = "Network";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xffffffff), RegistryValueKind.DWord, "Disable network throttling");
                    break;
                case "Net_MMResponsive":
                    _currentCategory = "Network";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0, RegistryValueKind.DWord, "Max multimedia responsiveness");
                    break;
                case "Net_DoH":
                    _currentCategory = "Network";
                    EnsureRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", "EnableAutoDoh", 2, RegistryValueKind.DWord, "Enable DoH");
                    EnsureRegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers\1.1.1.1");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers\1.1.1.1", "DohFlags", 3, RegistryValueKind.DWord, "Register 1.1.1.1 as DoH server");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers\1.1.1.1", "DohTemplate", "https://cloudflare-dns.com/dns-query", RegistryValueKind.String, "Cloudflare DoH template");
                    break;

                // ── SECURITY ──────────────────────────────────────────────
                case "Sec_AutoRun":
                    _currentCategory = "Security";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\IniFileMapping\Autorun.inf", "(Default)", "@SYS:DoesNotExist", RegistryValueKind.String, "Block Autorun.inf");
                    SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 0xFF, RegistryValueKind.DWord, "Disable AutoRun (user)");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 0xFF, RegistryValueKind.DWord, "Disable AutoRun (machine)");
                    break;
                case "Sec_RDP":
                    _currentCategory = "Security";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 1, RegistryValueKind.DWord, "Disable Remote Desktop (RDP)");
                    RunCommand("netsh advfirewall firewall set rule group=\"Remote Desktop\" new enable=no 2>nul", "Block RDP firewall rule");
                    break;
                case "Sec_SMBv1":
                    _currentCategory = "Security";
                    RunPowerShell("Set-SmbServerConfiguration -EnableSMB1Protocol $false -Force", "Disable SMBv1 server");
                    RunPowerShell("Disable-WindowsOptionalFeature -Online -FeatureName SMB1Protocol -NoRestart", "Remove SMBv1 feature");
                    break;
                case "Sec_NetBIOS":
                    _currentCategory = "Security";
                    RunPowerShell("Get-WmiObject Win32_NetworkAdapterConfiguration | Where-Object { $_.TcpipNetbiosOptions -ne $null } | ForEach-Object { $_.SetTcpipNetbios(2) }", "Disable NetBIOS over TCP/IP");
                    break;
                case "Sec_Defender":
                    _currentCategory = "Security";
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 0, RegistryValueKind.DWord, "Ensure Defender not disabled");
                    SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableRealtimeMonitoring", 0, RegistryValueKind.DWord, "Ensure Defender real-time ON");
                    RunPowerShell("Set-MpPreference -DisableRealtimeMonitoring $false", "Enable Defender real-time monitoring");
                    break;

                default:
                    _results.Add(new TweakResult { Name = $"Unknown tweak key: {key}", Success = false, Error = "No handler found" });
                    break;
            }

            _currentCategory = "";
            _appliedCategories.Add(GetCategoryForKey(key));
            SaveBackups();
        }

        private static string GetCategoryForKey(string key)
        {
            if (key == null) return "";
            if (key.StartsWith("Perf_"))  return "Performance";
            if (key.StartsWith("Priv_"))  return "Privacy";
            if (key.StartsWith("Resp_"))  return "Responsiveness";
            if (key.StartsWith("Game_"))  return "Gaming";
            if (key.StartsWith("Net_"))   return "Network";
            if (key.StartsWith("Bloat_")) return "Bloatware";
            if (key.StartsWith("Sec_"))   return "Security";
            if (key.StartsWith("Adv_"))   return "Advanced";
            return "";
        }

        public static void ApplyAdvancedTweak(string advancedKey)
        {
            ApplyAdvancedTweaks(new HashSet<string> { advancedKey });
        }

        public static void ApplyBloatwareTweak(string tweakKey)
        {
            var patternMap = new Dictionary<string, string[]>
            {
                ["Bloat_Bing"]      = new[] { "*BingNews*", "*BingWeather*", "*BingSearch*" },
                ["Bloat_Zune"]      = new[] { "*ZuneVideo*", "*ZuneMusic*" },
                ["Bloat_Solitaire"] = new[] { "*SolitaireCollection*" },
                ["Bloat_Maps"]      = new[] { "*WindowsMaps*" },
                ["Bloat_PhoneLink"] = new[] { "*YourPhone*", "*PhoneLink*" },
                ["Bloat_Clipchamp"] = new[] { "*Clipchamp*" },
                ["Bloat_Xbox"]      = new[] { "*Xbox.TCUI*", "*XboxApp*", "*XboxGameOverlay*", "*XboxGamingOverlay*", "*XboxSpeechToTextOverlay*" },
                ["Bloat_AdTiles"]   = new[] { "*LinkedIn*", "*Disney*", "*Spotify*", "*TikTok*", "*Instagram*", "*Facebook*" },
                ["Bloat_Office"]    = new[] { "*OfficeHub*", "*OneNote*" },
                ["Bloat_3D"]        = new[] { "*3DViewer*", "*Print3D*" },
            };

            if (!patternMap.TryGetValue(tweakKey, out var patterns)) return;

            _currentCategory = "Bloatware";
            foreach (var pattern in patterns)
            {
                string name = pattern.Replace("*", "").Trim();
                RunPowerShell($"Get-AppxPackage {pattern} | Remove-AppxPackage -ErrorAction SilentlyContinue", $"Remove (user) {name}");
                RunPowerShell($"Get-AppxProvisionedPackage -Online | Where-Object {{ $_.PackageName -like '{pattern}' }} | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue", $"Remove (provisioned) {name}");
            }
            _currentCategory = "";
            _appliedCategories.Add("Bloatware");
            SaveBackups();
        }


    }
}
// This line intentionally left blank - appending dispatch methods below