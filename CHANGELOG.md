# Changelog

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