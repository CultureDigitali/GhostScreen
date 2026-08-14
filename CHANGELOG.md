# Changelog

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