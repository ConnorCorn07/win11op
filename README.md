# Win11 Optimizer

A clean, open-source Windows 11 optimizer built in C# / WinForms. Designed to be dropped onto a fresh Windows install and run once to apply performance, privacy, gaming, and network tweaks — with full per-category undo support.

THIS WAS VIBE CODED USING GENERATIVE AI - (Claude, ofc)

---

## Features

| Category | What it does |
|---|---|
| ⚡ Performance | High performance power plan, disables SysMain & Windows Search, NTFS optimizations, removes startup delay, best performance visual mode |
| 🔒 Privacy & Telemetry | Disables all telemetry services and scheduled tasks, removes advertising ID, disables Bing/Cortana in Start, blocks activity feed and location tracking |
| 🖥 Responsiveness | Instant menus, faster shutdown timers, high-resolution system clock, disables Windows Tips |
| 🎮 Gaming | Enables HAGS & Game Mode, disables mouse acceleration, boosts foreground CPU priority, disables Game DVR and fullscreen optimizations |
| 🌐 Network | Disables Nagle's Algorithm, enables RSS, TCP auto-tuning, removes network throttling index |
| 🗑 Bloatware Removal | Removes pre-installed Microsoft and third-party bloat (Bing apps, Xbox overlays, TikTok, LinkedIn, etc.) from both user and provisioned packages |

### Per-Category Undo
Every registry change is backed up before being applied. After running a category, an **↩ UNDO** button appears next to it — click it to fully restore that category to its pre-tweak state. Backups persist across app restarts via `tweaks_backup.json`.

---

## Requirements

- Windows 10 or 11 (64-bit)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Must be run as **Administrator**

---

## Build from Source

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Clone the repo:
   ```
   git clone https://github.com/ConnorCorn07/win11op.git
   ```
3. Navigate to the build folder and compile:
   ```
   cd win11op/buildwinop
   dotnet build -c Release
   ```
4. Run the output exe as Administrator:
   ```
   bin\Release\net8.0-windows\Win11Optimizer.exe
   ```

---

## Notes

- A **reboot is required** after applying tweaks for HAGS and timer changes to take full effect
- Bloatware removal **cannot be undone** — removed apps must be reinstalled manually from the Microsoft Store if needed
- The optimizer backs up all registry changes to `tweaks_backup.json` next to the exe

---

## License

MIT — see [LICENSE](LICENSE)