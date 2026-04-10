using System;
using Microsoft.Win32;
using System.Diagnostics;

namespace Win11Optimizer{
    public class TweakEngine{
        private static void SetRegistry(string keyPath, string valueName, object value, RegistryValueKind kind){
            try{
                Registry.SetValue(keyPath, valueName, value, kind);
            }
            catch (Exception ex){
                // In a real app, log this error to your UI
                Debug.WriteLine($"Failed to set {valueName}: {ex.Message}");
            }
        }

        // method runs command lines
        private static void RunCommand(string command){
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "/c " + command){
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using (Process process = Process.Start(processInfo)){
                process.WaitForExit();
            }
        }

        // --- PERFORMANCE TWEAKS ---

        public static void ApplyPerformanceTweaks(){
            // Enable High Performance Power Plan
            RunCommand("powercfg -setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

            // Disable Superfetch (SysMain) and Windows Search Indexing
            RunCommand("sc config sysmain start=disabled & net stop sysmain");
            RunCommand("sc config WSearch start=disabled & net stop WSearch");

            // Disable Startup Delay (Sets delay to 0 milliseconds)
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "StartupDelayInMSec", 0, RegistryValueKind.DWord);
        }

        // --- PRIVACY TWEAKS ---

        public static void ApplyPrivacyTweaks(){
            // Disable Telemetry
            SetRegistry(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord);
            RunCommand("sc config DiagTrack start=disabled & net stop DiagTrack");

            // Disable Advertising ID
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0, RegistryValueKind.DWord);

            // Disable Bing Web Search in Start Menu
            SetRegistry(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord);

            // Disable App Launch Tracking
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackProgs", 0, RegistryValueKind.DWord);
        }

        // --- System Responsiveness ---

        public static void ApplySystemResponsiveness(){
        // 0 Menu Show Delay (Default is 400ms)
        SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop", "MenuShowDelay", "0", RegistryValueKind.String);

        // End Tasks Faster (WaitToKillAppTimeout & LowLevelHooksTimeout) - close apps quickly on shutdown/crash
        SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop", "WaitToKillAppTimeout", "2000", RegistryValueKind.String);
        SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Desktop", "HungAppTimeout", "1000", RegistryValueKind.String);
        SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control", "WaitToKillServiceTimeout", "2000", RegistryValueKind.String);

        // Disable Memory Compression - Powershell
        RunCommand("powershell -Command \"Disable-MMAgent -MemoryCompression\"");
        }

        public static void RemoveBloatware(Action<string> logCallback){
            // A list of app patterns to target for removal
            string[] bloatwareList = {
            "*bingnews*", "*bingweather*", "*zunevideo*", "*zunemusic*", 
            "*skypeapp*", "*solitairecollection*", "*getstarted*", 
            "*feedbackhub*", "*windowsmaps*", "*yourphone*", 
            "*clipchamp*", "*mixedreality*", "*actipro*", "*powerautomatedesktop*",
            "*linkedin*", "*disney*", "*spotify*", "*tiktok*", "*instagram*",
            "*officehub*", "*onenote*", "*people*", "*todos*"
            };

            foreach (string app in bloatwareList){
                logCallback?.Invoke($"Removing {app.Replace("*", "")}...");

                // 1. Remove from current user
                string removeUserApp = $"Get-AppxPackage {app} | Remove-AppxPackage";
        
                // 2. Remove from System Provisioning (prevents re-install)
                string removeProvApp = $"Get-AppxProvisionedPackage -Online | Where-Object {{ $_.PackageName -like '{app}' }} | Remove-AppxProvisionedPackage -Online";

                RunPowerShell(removeUserApp);
                RunPowerShell(removeProvApp);
            }
    
            logCallback?.Invoke("Bloatware removal complete.");
        }

        // Specialized PowerShell runner for cleaner execution
        private static void RunPowerShell(string script){
            ProcessStartInfo psi = new ProcessStartInfo{
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi)?.WaitForExit();
        }

        // --- GAMING TWEAKS ---

        public static void ApplyGamingTweaks(){
            // Enable Hardware-Accelerated GPU Scheduling (Requires Reboot)
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord);

            // Enable Game Mode
            SetRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "AllowAutoGameMode", 1, RegistryValueKind.DWord);

            // Disable Mouse Acceleration (Enhance Pointer Precision)
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseSpeed", "0", RegistryValueKind.String);
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold1", "0", RegistryValueKind.String);
            SetRegistry(@"HKEY_CURRENT_USER\Control Panel\Mouse", "MouseThreshold2", "0", RegistryValueKind.String);

            // CPU Scheduler Priority (Prioritizes foreground applications/games)
            SetRegistry(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38, RegistryValueKind.DWord);
        }

        // --- ADVANCED TWEAK: NAGLE'S ALGORITHM ---
        
        public static void DisableNaglesAlgorithm(){
            // Nagle's algorithm requires iterating through active network adapters
            string interfacesPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
            using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(interfacesPath, true)){
                if (baseKey != null){
                    foreach (string subKeyName in baseKey.GetSubKeyNames()){
                        using (RegistryKey subKey = baseKey.OpenSubKey(subKeyName, true)){
                            // TcpAckFrequency = 1 and TCPNoDelay = 1 lowers ping
                            subKey.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                            subKey.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                        }
                    }
                }
            }
        }
    }
}