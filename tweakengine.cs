using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;

namespace Win11Optimizer{
    public static class TweakEngine
    {
        //--- RESULT TRACKING ---

        public class TweakResult //Yay or nay
        {
            public string Name    { get; set; }
            public bool   Success { get; set; }
            public string Error   { get; set; }
        }

        private static readonly List<TweakResult> _results = new();

        public static IReadOnlyList<TweakResult> GetResults() => _results.AsReadOnly();
        public static void ClearResults() => _results.Clear();

        // ---HELPERS ---

        private static void SetRegistry(string keyPath, string valueName, object value, RegistryValueKind kind, string friendlyName = null)
        {
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
                    CreateNoWindow        = true,
                    UseShellExecute       = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };
                using var p = Process.Start(psi);
                p.WaitForExit();
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
                    FileName               = "powershell.exe",
                    Arguments              = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };
                using var p = Process.Start(psi);
                p.WaitForExit();
                _results.Add(new TweakResult { Name = name, Success = p.ExitCode == 0 });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PS FAIL] {name}: {ex.Message}");
                _results.Add(new TweakResult { Name = name, Success = false, Error = ex.Message });
            }
        }

        private static void DisableService(string serviceName)
            => RunCommand($"sc config {serviceName} start=disabled & net stop {serviceName} 2>nul",
                          $"Disable service: {serviceName}");

        private static void DeleteScheduledTask(string taskPath)
            => RunCommand($"schtasks /Change /TN \"{taskPath}\" /Disable 2>nul",
                          $"Disable task: {taskPath}");

        // --- PERFORMANCE ---

        public static void ApplyPerformanceTweaks()
        {
            // High-Performance power plan
            RunCommand("powercfg -setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                       "High Performance power plan");

            // Disable power throttling (keeps CPU clocks high)
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling",
                        "PowerThrottlingOff", 1, RegistryValueKind.DWord, "Disable Power Throttling");

            // Disable SysMain (Superfetch) and Windows Search
            DisableService("SysMain");
            DisableService("WSearch");

            // Reduce startup delay
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                        "StartupDelayInMSec", 0, RegistryValueKind.DWord, "Remove startup delay");

            // Visual performance — best performance preset
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                        "VisualFXSetting", 2, RegistryValueKind.DWord, "Visual effects: best performance");

            // NTFS: disable last-access timestamp updates & 8.3 filenames
            RunCommand("fsutil behavior set disablelastaccess 1",  "Disable NTFS last-access updates");
            RunCommand("fsutil behavior set disable8dot3 1",       "Disable 8.3 filenames");

            // Hibernation off (frees hiberfil.sys disk space; remove if you use Sleep on laptop)
            RunCommand("powercfg -h off", "Disable hibernation");

            // Disable memory compression
            RunPowerShell("Disable-MMAgent -MemoryCompression", "Disable memory compression");
        }

        // --- PRIVACY & TELEMETRY ---

        public static void ApplyPrivacyTweaks()
        {
            // ── Telemetry level → 0 (Security/Off) ──
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                        "AllowTelemetry", 0, RegistryValueKind.DWord, "Disable telemetry");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection",
                        "AllowTelemetry", 0, RegistryValueKind.DWord, "Disable telemetry (legacy key)");

            // ── Disable telemetry & diagnostic services ──
            DisableService("DiagTrack");               // Connected User Experiences & Telemetry
            DisableService("dmwappushservice");        // WAP Push Message Routing
            DisableService("RetailDemo");              // Retail Demo service
            DisableService("WerSvc");                  // Windows Error Reporting

            // ── Disable diagnostic data collection scheduled tasks ──
            DeleteScheduledTask(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser");
            DeleteScheduledTask(@"\Microsoft\Windows\Application Experience\ProgramDataUpdater");
            DeleteScheduledTask(@"\Microsoft\Windows\Autochk\Proxy");
            DeleteScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator");
            DeleteScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip");
            DeleteScheduledTask(@"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector");

            // ── Advertising & tracking ──
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                        "Enabled", 0, RegistryValueKind.DWord, "Disable Advertising ID");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo",
                        "DisabledByGroupPolicy", 1, RegistryValueKind.DWord, "Disable Advertising ID (policy)");

            // ── Start Menu / Search telemetry ──
            SetRegistry(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer",
                        "DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord, "Disable Bing in Start");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                        "Start_TrackProgs", 0, RegistryValueKind.DWord, "Disable app launch tracking");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search",
                        "BingSearchEnabled", 0, RegistryValueKind.DWord, "Disable Bing Search integration");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search",
                        "CortanaConsent", 0, RegistryValueKind.DWord, "Disable Cortana consent");

            // ── Activity / Timeline history ──
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",
                        "EnableActivityFeed", 0, RegistryValueKind.DWord, "Disable Activity Feed");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",
                        "PublishUserActivities", 0, RegistryValueKind.DWord, "Disable publishing user activities");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",
                        "UploadUserActivities", 0, RegistryValueKind.DWord, "Disable uploading user activities");

            // ── Feedback frequency → Never ──
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Siuf\Rules",
                        "NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord, "Disable feedback requests");

            // ── Location tracking ──
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",
                        "DisableLocation", 1, RegistryValueKind.DWord, "Disable location tracking");

            // ── Camera / microphone app access ──
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                        "LetAppsAccessCamera", 2, RegistryValueKind.DWord, "Block app camera access");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                        "LetAppsAccessMicrophone", 2, RegistryValueKind.DWord, "Block app microphone access");

            // ── Disable Windows Error Reporting ──
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting",
                        "Disabled", 1, RegistryValueKind.DWord, "Disable Windows Error Reporting");

            // ── Disable SmartScreen for Explorer (still enforced by Defender) ──
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System",
                        "EnableSmartScreen", 0, RegistryValueKind.DWord, "Disable SmartScreen (Explorer)");
        }

        // --- SYSTEM RESPONSIVENESS ---

        public static void ApplySystemResponsiveness()
        {
            // Instant menus
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop",
                        "MenuShowDelay", "0", RegistryValueKind.String, "Instant menu show");

            // Faster app/service shutdown
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop",
                        "WaitToKillAppTimeout", "2000", RegistryValueKind.String, "Fast app kill timeout");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop",
                        "HungAppTimeout", "1000", RegistryValueKind.String, "Fast hung app timeout");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control",
                        "WaitToKillServiceTimeout", "2000", RegistryValueKind.String, "Fast service kill timeout");

            // Auto end tasks on shutdown
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop",
                        "AutoEndTasks", "1", RegistryValueKind.String, "Auto end tasks on shutdown");

            // High-resolution system timer (1 ms) via bcdedit
            RunCommand("bcdedit /set useplatformtick yes",  "Platform tick (high-res timer)");
            RunCommand("bcdedit /deletevalue useplatformclock 2>nul", "Remove platform clock override");

            // Disable Windows Tips & suggestions
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                        "SoftLandingEnabled", 0, RegistryValueKind.DWord, "Disable Windows Tips");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                        "SubscribedContent-338389Enabled", 0, RegistryValueKind.DWord, "Disable suggested content");
        }

        // --- GAMING ---

        public static void ApplyGamingTweaks()
        {
            // Hardware-Accelerated GPU Scheduling (requires reboot + compatible GPU driver)
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                        "HwSchMode", 2, RegistryValueKind.DWord, "Enable HAGS");

            // Game Mode
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar",
                        "AllowAutoGameMode", 1, RegistryValueKind.DWord, "Enable Game Mode");
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar",
                        "AutoGameModeEnabled", 1, RegistryValueKind.DWord, "Enable Auto Game Mode");

            // Disable mouse acceleration
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse",
                        "MouseSpeed", "0", RegistryValueKind.String, "Disable mouse acceleration");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse",
                        "MouseThreshold1", "0", RegistryValueKind.String, "Mouse threshold 1");
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse",
                        "MouseThreshold2", "0", RegistryValueKind.String, "Mouse threshold 2");

            // Foreground process priority boost
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl",
                        "Win32PrioritySeparation", 38, RegistryValueKind.DWord, "CPU foreground priority boost");

            // Disable Xbox Game Bar overlay (reduces background overhead)
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR",
                        "AppCaptureEnabled", 0, RegistryValueKind.DWord, "Disable Game DVR capture");
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\GameDVR",
                        "AllowGameDVR", 0, RegistryValueKind.DWord, "Disable Game DVR (policy)");

            // Disable fullscreen optimizations globally
            SetRegistry(@"HKEY_CURRENT_USER\System\GameConfigStore",
                        "GameDVR_FSEBehaviorMode", 2, RegistryValueKind.DWord, "Disable FSO globally");
            SetRegistry(@"HKEY_CURRENT_USER\System\GameConfigStore",
                        "GameDVR_HonorUserFSEBehaviorMode", 1, RegistryValueKind.DWord, "Honor FSO setting");
        }


        //--- NETWORK OPTIMIZATIONS ---

        public static void ApplyNetworkTweaks()
        {
            // Disable Nagle's Algorithm on all adapters
            DisableNaglesAlgorithm();

            // Larger TCP receive window auto-tuning
            RunCommand("netsh int tcp set global autotuninglevel=normal", "TCP auto-tuning: normal");

            // Enable Receive Side Scaling
            RunCommand("netsh int tcp set global rss=enabled", "Enable RSS");

            // Enable ECN (Explicit Congestion Notification)
            RunCommand("netsh int tcp set global ecncapability=enabled", "Enable ECN");

            // Disable TCP/IP task offload (can help on some NICs with latency)
            // may need to comment this out
            RunCommand("netsh int tcp set global chimney=disabled", "Disable TCP chimney offload");

            // Set DNS over HTTPS resolver (Cloudflare) — 
            // RunCommand("netsh dns add encryption server=1.1.1.1 dohtemplate=https://cloudflare-dns.com/dns-query");

            // Reduce network throttling index (0xffffffff = disable throttling for games/media)
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                        "NetworkThrottlingIndex", unchecked((int)0xffffffff), RegistryValueKind.DWord,
                        "Disable network throttling");

            // System responsiveness for games (0 = max responsiveness for multimedia tasks)
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                        "SystemResponsiveness", 0, RegistryValueKind.DWord, "Max multimedia responsiveness");
        }

        // --- BLOATWARE REMOVAL ---

        // Apps that must never be removed (removing these can break Windows)
        private static readonly HashSet<string> _safeList = new(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.WindowsStore",
            "Microsoft.Windows.Photos",
            "Microsoft.WindowsCalculator",
            "Microsoft.WindowsNotepad",
            "Microsoft.Paint",
            "Microsoft.ScreenSketch",
            "Microsoft.WindowsTerminal"
        };

        public static void RemoveBloatware(Action<string> logCallback)
        {
            string[] bloatwarePatterns =
            {
                "*BingNews*",          "*BingWeather*",      "*BingSearch*",
                "*ZuneVideo*",         "*ZuneMusic*",
                "*SkypeApp*",          "*SolitaireCollection*",
                "*GetStarted*",        "*FeedbackHub*",
                "*WindowsMaps*",       "*YourPhone*",        "*PhoneLink*",
                "*Clipchamp*",          "*MixedReality*",
                "*PowerAutomateDesktop*",
                "*LinkedIn*",          "*Disney*",
                "*Spotify*",           "*TikTok*",
                "*Instagram*",         "*Facebook*",
                "*OfficeHub*",         "*OneNote*",
                "*People*",            "*ToDos*",
                "*Todos*",             "*Widgets*",
                "*Xbox.TCUI*",         "*XboxApp*",
                "*XboxGameOverlay*",   "*XboxGamingOverlay*",
                "*XboxSpeechToTextOverlay*",
                "*3DViewer*",          "*Print3D*",
                "*Wallet*",            "*Advertising*"
            };

            foreach (string pattern in bloatwarePatterns)
            {
                // Skip if the package name contains a safelisted app name
                bool isSafe = false;
                foreach (string safe in _safeList)
                    if (pattern.Contains(safe, StringComparison.OrdinalIgnoreCase)) { isSafe = true; break; }
                if (isSafe) continue;

                string friendlyName = pattern.Replace("*", "").Trim();
                logCallback?.Invoke($"Removing {friendlyName}...");

                string removeUser = $"Get-AppxPackage {pattern} | Remove-AppxPackage -ErrorAction SilentlyContinue";
                string removeProv = $"Get-AppxProvisionedPackage -Online | " +
                                    $"Where-Object {{ $_.PackageName -like '{pattern}' }} | " +
                                    $"Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue";

                RunPowerShell(removeUser, $"Remove (user) {friendlyName}");
                RunPowerShell(removeProv, $"Remove (provisioned) {friendlyName}");
            }

            logCallback?.Invoke("Bloatware removal complete.");
        }



        // NAGLE'S ALGORITHM  (called from ApplyNetworkTweaks)

        public static void DisableNaglesAlgorithm()
        {
            const string interfacesPath =
                @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";

            try
            {
                using RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(interfacesPath, writable: true);
                if (baseKey == null)
                {
                    _results.Add(new TweakResult
                        { Name = "Disable Nagle's Algorithm", Success = false, Error = "Base key not found" });
                    return;
                }

                foreach (string subKeyName in baseKey.GetSubKeyNames())
                {
                    using RegistryKey subKey = baseKey.OpenSubKey(subKeyName, writable: true);
                    subKey?.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                    subKey?.SetValue("TCPNoDelay",      1, RegistryValueKind.DWord);
                }

                _results.Add(new TweakResult { Name = "Disable Nagle's Algorithm", Success = true });
            }
            catch (Exception ex)
            {
                _results.Add(new TweakResult
                    { Name = "Disable Nagle's Algorithm", Success = false, Error = ex.Message });
            }
        }
    }
}