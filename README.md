# ⚡ Win11 Optimizer

> A clean, open-source Windows 10/11 optimizer built in C# / WinForms.  
> Designed to be dropped onto a fresh Windows install and run once to apply performance, privacy, gaming, network, and security tweaks — with full per-tweak undo support.

**Version:** `1.1.0`  
**Platform:** Windows 10 / 11 (64-bit)  
**Runtime:** .NET 10 Desktop Runtime  
**License:** MIT

---

## What's New in v1.1.0

### 🔍 Live System State Detection
Win11 Optimizer now scans your system on startup and automatically detects tweaks that are already applied — even if you set them up manually before ever running the app. Detected tweaks show a purple **applied** indicator so you know exactly what's already done before you run anything.

### 💾 Per-Tweak Applied State Persistence
Applied state is now tracked at the individual tweak level, not just per category. Every tweak you apply is remembered across app restarts via `applied_tweaks.json`. Returning to the app after a reboot shows exactly which tweaks were already run.

### 📦 Export & Import Tweak Profiles
Save your current tweak selection to a `.w11profile` file and load it back on any machine. Profiles include a name, creation timestamp, and version — and the importer reports how many tweaks matched your current version when loading an older profile.

### 🆕 What's New Dialog
Win11 Optimizer now detects version changes on launch and offers to open the release notes on GitHub — so you always know what changed without having to go looking.

### 🔧 Bug Fixes
- Fixed GitHub button linking to the old personal repo instead of the Corn Studios org
- Fixed `StartupApproved\Run32` registry key used for system startup entries — was silently failing to disable 64-bit HKLM startup entries; now correctly targets `StartupApproved\Run`
- Fixed `ImplicitUsings` mismatch in the project file

### ⚙ .NET 10 Migration
Upgraded from .NET 8 to .NET 10.

---

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

### Per-Tweak Undo
Every registry change is backed up before being applied. After running tweaks, the **↩ Undo Selected** button in the bottom bar lets you fully restore any category to its pre-tweak state. Backups persist across app restarts via `tweaks_backup.json`.

### Tweak Profiles
Save and load your tweak selection as a `.w11profile` file using the **↑ Export Profile** and **↓ Import Profile** buttons in the bottom bar. Useful for setting up multiple machines or sharing a config.

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
- Bloatware removal cannot be undone: removed apps must be reinstalled from the Microsoft Store
- All registry changes are backed up to `tweaks_backup.json` next to the exe before being applied
- Applied tweak state is tracked per-tweak in `applied_tweaks.json` next to the exe
- Windows Recall tweaks are a no-op on non-Copilot+ PCs: safe to apply on any hardware
- NVIDIA telemetry tweaks are a no-op if NVIDIA drivers are not installed
- The hosts file block list is cleanly removed by the Privacy undo function
- Startup folder shortcuts cannot be disabled (Windows limitation), only deleted

---

## License

MIT — see [LICENSE](LICENSE)

---

## AI Disclosure

> ⚠ This project contains code written with the assistance of **Claude by Anthropic** (claude.ai).  
> The **Startup Manager** feature and the **v1.0.0 UI rework** were developed with Claude Sonnet. All code has been reviewed and tested by the project maintainer.