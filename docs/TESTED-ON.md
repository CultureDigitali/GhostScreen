# Tested On

Everything below is **non-sensitive, public test-environment information**. No
usernames, IPs, keys or personal data are included.

## v1.1.0 test round (2026-08-15)

| Test | Result | Notes |
|---|---|---|
| 11-language matrix (IT/ES/FR/DE/EN/ZH/JA/PT/RU/KO/NL) | ✅ | one instance per language, screenshot each → `Desktop\GhostScreen-lang-test-v11\` |
| CJK + Cyrillic + Hangul fonts | ✅ | YaHei / Yu Gothic / Malgun Gothic / Tahoma |
| Silent `/apply` (elevated) | ✅ | exit 0, 2560x1440 applied, no window |
| Silent `/apply` via reflection (non-elevated) | ✅ | 2560x1440 in 5.4s |
| Real MIDI playback | ✅ | .mid generated (633 B SMF), MCI play/stop, no errors |
| GUI smoke with MIDI (elevated, 150s) | ✅ | music on, apply OK, screenshot saved |
| `/uninstall` (elevated) | ✅ | device removed, DriverStore package deleted, UMDF cleaned, registry removed, no leftover task |
| Reinstall after uninstall (elevated GUI) | ✅ | full pipeline re-ran: DriverStore + copy + device + restart + 2560x1440 OK |
| Registry persistence (7 settings) | ✅ | Lang/Theme/Music/Volume/CustomW/H/F/AutoStart saved and reloaded |
| Update check API | ✅ | `api.github.com/repos/CultureDigitali/GhostScreen/releases/latest` reachable, tag parsed |
| Custom resolution fields | ✅ | W/H/Hz numerics created and clamped (640–7680 / 480–4320 / 25–240) |
| Auto-start task (create/delete) | ✅ | schtasks round-trip verified (delete on non-existent task handled gracefully) |
| Diagnostic report | ✅ | ZIP generation via System.IO.Compression (Desktop) |
| Themes switch | ✅ | teal/plum/eggplant/dark, colors repainted live |

## v1.0.0 baseline

### Reference machine

| | |
|---|---|
| Model | HP EliteDesk 705 G3 SFF (desktop, SFF chassis) |
| CPU | AMD PRO A6-8570 R5, 8 Compute Cores (2C+6G) |
| RAM | 6.9 GB |
| GPU | AMD Radeon R5 Graphics (integrated) |
| Display adapters | AMD Radeon R5 Graphics + Virtual Display Driver (VDD) |
| OS | Windows 10 Pro 22H2, build 19045.6466, x64 |
| .NET Framework | 4.8.09037 (Release 533325) — preinstalled, no runtime to install |
| Remote access used | Chrome Remote Desktop (the machine is headless: no physical monitor) |
| Language of the OS | Italian (it-IT) |

## Software involved in development & testing

| Tool | Version | Used for |
|---|---|---|
| PowerShell | 5.1.19041.6456 | automation, smoke tests, test matrices |
| .NET Framework csc.exe | 4.0.30319 (C# 5) | compiling GhostScreen.exe — only dependency, always preinstalled |
| gh (GitHub CLI) | 2.97.0 | repo, releases, Pages, topics |
| git | 2.55.0.windows.3 | version control |
| winget | Windows 10 built-in | downloading the Virtual-Display-Driver package |
| devcon.exe | Windows Driver Kit | device creation (ROOT\DISPLAY) |
| pnputil.exe | Windows built-in | driver install / restart-device |
| schtasks.exe | Windows built-in | SYSTEM-elevated settings copy |
| Chrome Remote Desktop | latest | remote sessions on the headless machine |

## Driver under test

- **Virtual-Display-Driver** by MikeTheTech (MIT) — the open-source UMDF display driver
  embedded inside GhostScreen (driver, catalog, settings, devcon, helper script).
- Installed via the standard Windows DriverStore (`pnputil /add-driver /install`).

## Test matrix

| Test | Result | Notes |
|---|---|---|
| Fresh driver install (DriverStore + device creation) | ✅ | pnputil + devcon `install Root\MttVDD` |
| Settings copy to DriverStore via SYSTEM scheduled task | ✅ | quoting verified end-to-end |
| Settings copy to UMDF folder | ✅ | `C:\Windows\System32\drivers\UMDF\` |
| Resolution apply **2560x1440 @ 60 Hz** | ✅ | mode #25, verified via EnumDisplaySettings after apply |
| Resolution apply 1920x1080 | ✅ | same mechanism |
| Resolution apply 1366x768 / 1280x720 / 800x600 | ✅ | available in driver mode table |
| 4K availability (3840x2160) | ✅ | present in the mode table (not applied on this GPU) |
| "Solo risoluzione" (resolution-only) | ✅ | |
| "Riavvia display" (restart display) | ✅ | pnputil /restart-device |
| Empty-enumeration recovery (config reset + retry ×3) | ✅ | defensive path tested |
| Auto-run on launch (install or apply) | ✅ | |
| 7-language matrix (IT/ES/FR/DE/EN/ZH/JA) | ✅ | one instance per language, screenshots captured |
| Language persistence (registry HKCU\Software\GhostScreen\Lang) | ✅ | relaunch picked up the saved language (ja) |
| CJK rendering (zh/ja fonts) | ✅ | Microsoft YaHei / Yu Gothic UI |
| Game Boy chiptune at startup | ✅ | synthesized, looped via PlaySound, stops on exit |
| Music toggle (File ▾) | ✅ | |
| CLI flags `/lang: /nosound /quiet` | ✅ | |
| Close-app-while-working (worker thread safety) | ✅ | crash fixed in v1.0.1 (Log disposal guard) |
| UAC elevation flow (requireAdministrator) | ✅ | |
| Re-run after reboot / idempotency | ✅ | safe to run repeatedly |
| Windows 95 UI rendering | ✅ | title bar, buttons, menus, message boxes, status bar |

## Known environment notes

- The machine has **no physical monitor**: the AMD Radeon output shows a 640x480
  fallback when the virtual display is absent; with GhostScreen the VDD provides
  **2560x1440 @ 60 Hz** as the active resolution.
- Remote sessions over Chrome Remote Desktop scale the picture, so visual
  verification was cross-checked via `EnumDisplaySettings` (API) + screenshots.
- .NET Framework 4.x is **preinstalled on every supported Windows** — the EXE has no
  external runtime dependency (only `System.Windows.Forms`, `System.Drawing`,
  `System.Management`, all built-in).