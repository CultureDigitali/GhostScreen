# Changelog

## 1.1.0 (2026-08-15)

**4 new languages (now 11 total)**
- português, русский, 한국어, nederlands added to the engine and lang.txt
- Cyrillic/CJK fonts (Tahoma for ru, Malgun Gothic for ko, YaHei/Yu Gothic for zh/ja)

**Custom resolution**
- "Personalizzata (WxH)" radio + W / H / Hz numeric inputs (640–7680 × 480–4320, 25–240 Hz)
- Saved to registry, used by /apply

**Music v2**
- Real MIDI mode: GhostScreen generates a .mid (SMF) at runtime and plays it via MCI (repeat)
- Volume menu 25/50/75/100% (amplitude-scaled chiptune; setaudio for MIDI)
- Modes: Chiptune / MIDI / Off, persisted in registry

**New features**
- System tray ghost icon: show / apply / restart / exit + double-click to restore
- Minimize hides the window to the tray (it's a ghost now)
- Auto-start at logon (scheduled task `GhostScreen AutoApply` running `/apply`) — File ▾ menu, self-heals on launch
- Uninstaller (File ▾ + `/uninstall`): removes device, DriverStore package, UMDF files, task, registry
- Diagnostic report: ZIP on Desktop with log + WMI system info + driver state + settings
- Update check (File ▾): GitHub releases API, no telemetry, offline by default
- Themes: Teal (classic) / Plum / Eggplant / Dark Pro, persisted in registry
- HiDPI: SetProcessDPIAware for crisp rendering

**CLI additions**
- `/apply` (silent apply, exits), `/uninstall`, `/music:chip|midi|off`, `/volume:xx`, `/theme:teal|plum|eggplant|dark`

**Bug fixes**
- MIDI generator: out-of-range index `bass[8]` when emitting the final bass note-off (fixed)

**Tested**
- 11-language matrix with screenshots, silent /apply, MIDI play/stop, uninstall + reinstall round-trip,
  registry persistence (7 settings), update-check API — see docs/TESTED-ON.md

## 1.0.1 (2026-08-15)

**Multilingual (7 languages)**
- Full i18n: italiano, español, français, deutsch, english, 中文, 日本語
- Auto-detection from OS locale + Lingua menu + registry persistence (HKCU\Software\GhostScreen\Lang)
- CJK fonts (Microsoft YaHei / Yu Gothic UI) when needed
- Translations embedded as a single editable `lang.txt` resource

**Game Boy chiptune**
- Synthesized at runtime (2 square waves + noise, GB APU style), looped via PlaySound
- Toggle in File ▾ menu, stops on exit

**CLI**
- `/lang:xx` override, `/nosound`, `/quiet` (no message boxes, for automation)

**Bug fixes**
- Crash (ObjectDisposedException) when closing the app while the worker thread was logging — `Log()` now guarded and split into file/UI paths
- Thread-safety: resolution radios read on the UI thread before starting workers
- Guards on status/button updates after dispose

**Site**
- Landing page in 7 languages (docs/*.html) with hreflang, canonical, JSON-LD SoftwareApplication, Open Graph/Twitter cards
- sitemap.xml + robots.txt

## 1.0.0 (2026-08-15)

First public release.

**Features**
- Single-file EXE with all components embedded (driver, catalog, settings, devcon, helper script)
- Faithful Windows 95 GUI: gradient title bar, chiseled 3D buttons, MS Sans Serif, custom message boxes
- Resolution presets: 2560x1440 (default), 1920x1080, 1366x768, 1280x720 @ 60 Hz
- Full installation pipeline: DriverStore install, SYSTEM-elevated settings copy, device creation (devcon), display restart, resolution apply
- "Solo risoluzione" mode for machines where the driver is already installed
- "Riavvia display" mode to wake/refresh the virtual display
- Self-healing: detects missing driver, missing device, empty display enumeration, applies retries with automatic config reset
- Progress log to `%WINDIR%\Temp\GhostScreen.log` + on-screen log panel
- Custom-generated logo, icon (multi-size ICO) and banner

**Technical**
- .NET Framework 4.x, zero external runtime dependencies
- requireAdministrator manifest
- Idempotent: safe to re-run on fresh installations