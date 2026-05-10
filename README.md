# ⚡ Win11 Optimizer

> A clean, open-source Windows 10/11 optimizer built in C# / WinForms.  
> Designed to be dropped onto a fresh Windows install and run once to apply performance, privacy, gaming, network, and security tweaks — with full per-category undo support.

**Version:** `1.1.0`  
**Platform:** Windows 10 / 11 (64-bit)  
**Runtime:** .NET 8 Desktop Runtime  
**License:** MIT

---

## What's New in v1.0.0

### 🚀 Startup Manager
A full startup program manager integrated directly into Win11 Optimizer as a dedicated sidebar tab.

- Reads startup entries from all three standard Windows locations:
  - `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (current user registry)
  - `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` (system-wide registry)
  - User and common startup folders
- Enable or disable individual startup entries using the same technique as Windows Task Manager (`StartupApproved\Run` binary key)
- One-click **Enable All** / **Disable All** controls
- Per-entry **impact indicator** (High / Medium / Low) based on known heavy hitters like OneDrive, Discord, Steam, Zoom, and Teams
- Color-coded **source badges** — Registry (User), Registry (System), Startup Folder
- **Delete** entries permanently with confirmation prompt
- Startup folder items handled gracefully — disable is not supported by Windows for folder shortcuts, so the UI explains this and directs to Delete instead
- **Refresh** button to reload the list at any time

### 🎨 Full UI Rework — Corn Studios Design Language
The entire application visual style has been reworked to match the [Corn Studios website](https://corn-studios.github.io) aesthetic.

## Features

| Category | What it does |
|---|---|
| ⚡ Performance | High performance power plan, disables SysMain & Windows Search, NTFS optimizations, removes startup delay, best performance visual mode, sets timer resolution to 0.5ms via `timeBeginPeriod` |
| 🔒 Privacy & Telemetry | Disables all telemetry services and scheduled tasks, removes advertising ID, disables Bing/Cortana in Start, blocks activity feed and location tracking, disables Windows Recall (Copilot+ AI screenshot), removes Chat/Teams taskbar icon, blocks 35 Microsoft telemetry domains in the hosts file |
| 🖥 Responsiveness | Instant menus, faster shutdown timers, high-resolution system clock, disables Windows Tips and suggested content |
| 🎮 Gaming | Enables HAGS & Game Mode, disables mouse acceleration, boosts foreground CPU priority, disables Game DVR, disables fullscreen optimizations, sets GPU power policy to Prefer Maximum Performance, disables NVIDIA telemetry services |
| 🌐 Network | Disables Nagle's Algorithm, enables RSS, TCP auto-tuning, removes network throttling index, enables DNS over HTTPS via Cloudflare 1.1.1.1 |
| 🗑 Bloatware Removal | Removes pre-installed Microsoft and third-party bloat (Bing apps, Xbox overlays, TikTok, LinkedIn, etc.) from both user and provisioned packages |
| 🔐 Security Hardening | Disables AutoRun/AutoPlay on all drive types, disables Remote Desktop (RDP), disables SMBv1, disables NetBIOS over TCP/IP, enforces Windows Defender real-time protection |
| ⚠ Advanced Tweaks | CPU scheduler tuning (Win32PrioritySeparation), disable dynamic tick, disable CPU throttling for background processes, ensure SSD TRIM is enabled, aggressive animation disabling |
| 🚀 Startup Manager | View, enable, disable, and delete startup entries from registry and startup folders |

### Per-Category Undo
Every registry change is backed up before being applied. After running a category, an **↩ Undo Selected** button appears in the bottom bar — click it to fully restore that category to its pre-tweak state. Backups persist across app restarts via `tweaks_backup.json`.

---

## Requirements

- Windows 10 or 11 (64-bit)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- Administrator privileges (required for registry, service, and hosts file changes)

---

## Releases

1. Go to [Releases](https://github.com/Corn-Studios/win11op/releases) and download the latest `.exe`
2. Right-click → **Run as Administrator**

## Build from Source

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Clone the repo:
   ```
   git clone https://github.com/Corn-Studios/win11op.git
   ```
3. Build:
   ```
   cd win11op/buildwinop
   dotnet build -c Release
   ```
4. Run as Administrator:
   ```
   bin\Release\net10.0-windows\Win11Optimizer.exe
   ```

---

## Notes

- A **reboot is required** after applying tweaks for HAGS, timer resolution, and SMBv1 changes to take full effect
- Bloatware removal cannot be undone — removed apps must be reinstalled from the Microsoft Store
- All registry changes are backed up to `tweaks_backup.json` next to the exe before being applied
- Windows Recall tweaks are a no-op on non-Copilot+ PCs — safe to apply on any hardware
- NVIDIA telemetry tweaks are a no-op if NVIDIA drivers are not installed
- The hosts file block list is cleanly removed by the Privacy undo function
- Startup folder shortcuts cannot be disabled (Windows limitation) — only deleted

---

## License

MIT — see [LICENSE](LICENSE)

---

## AI Disclosure

> ⚠ This project contains code written with the assistance of **Claude by Anthropic** (claude.ai).  
> Specifically, the **Startup Manager** feature (`StartupManager.cs`, `StartupTab.cs`) and the **v1.0.0 UI rework** were developed with Claude Sonnet. All code has been reviewed and tested by the project maintainer.