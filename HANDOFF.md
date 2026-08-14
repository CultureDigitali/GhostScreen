# GhostScreen 95 — Handoff & Developer Guide

This document is the complete brief for anyone (human developer **or AI agent**) taking
over this project. Read it top to bottom before touching anything.

---

## 1. What this project is

**GhostScreen 95** is a single-file Windows utility that installs a *virtual display*
on a headless Windows PC (no physical monitor), unlocking real resolutions up to 4K.
The whole product — driver, catalog, settings, tools, GUI, translations, music — is
embedded in **one EXE** with zero runtime dependencies beyond the built-in
.NET Framework 4.x (preinstalled on Windows 10/11).

Product identity (non-negotiable, decided by the owner):

- Name: **GhostScreen 95**. Tagline: *"The display that doesn't exist."*
- Style: **faithful Windows 95 GUI** (gradient title bar, chiseled buttons, MS Sans Serif).
  No modern widgets, no rounded corners, no dark-mode-by-default.
- Author credit everywhere: **Luigi Strazzullo per Culture Digitali Srl**.
- One single-file EXE. No installers, no DLLs, no internet needed (except the optional
  update check). Idempotent and re-runnable.

## 2. Repository & live endpoints

- Repo (public): https://github.com/CultureDigitali/GhostScreen
  - `gh` CLI authenticated as **CultureDigitali**
  - git identity: `CultureDigitali` / `culturedigitali@users.noreply.github.com`
- Releases: tag `v1.1.0` → `GhostScreen-1.1.0.exe` (609,792 bytes)
  - Download URL pattern: `https://github.com/CultureDigitali/GhostScreen/releases/download/v1.1.0/GhostScreen-1.1.0.exe`
- GitHub Pages: https://culturedigitali.github.io/GhostScreen/ (7 language pages, SEO-optimized)
- Topics on the repo: virtual-display, headless-pc, windows-95, remote-desktop, display-driver, resolution, csharp, dotnet, retro-ui, ghostscreen

## 3. Layout

```
GhostScreen/
├── src/
│   ├── GhostScreen.cs      # ENTIRE application (single file, C# 5, ~1800 lines)
│   ├── lang.txt            # ALL translations: 81 keys × 11 languages, TAB-separated
│   ├── app.manifest        # requireAdministrator, Win10 compatibility
│   └── build.ps1           # build script (csc + embedded resources)
├── drivers/                # 6 embedded files (mttvdd.inf, MttVDD.cat, MttVDD.dll,
│                           # vdd_settings.xml, devcon.exe, copy_settings.cmd)
├── assets/                 # logo.png, banner.png (1280x640), icon.ico
├── releases/               # built EXEs (NOT in git — .gitignore)
├── docs/                   # site pages (index.html = EN, it/es/fr/de/zh/ja.html),
│                           # PRODUCT.md, CASE-STUDIES.md, TESTED-ON.md, sitemap.xml, robots.txt
├── README.md               # marketing README (keep it!)
├── LICENSE                 # MIT, credits to Luigi Strazzullo / Culture Digitali Srl
├── CHANGELOG.md
└── HANDOFF.md              # this file
```

## 4. Build (critical: only these steps)

The app is compiled with the **.NET Framework 4.0.30319 csc.exe** (C# 5) — no NuGet,
no MSBuild, no dependencies. Newer csc versions would break the code (language features,
nullable, etc.). Command:

```powershell
powershell -ExecutionPolicy Bypass -File src\build.ps1
```

`build.ps1` embeds the 6 driver files + banner.png + logo.png + lang.txt as resources
(`Res.*` names) and outputs `releases\GhostScreen-1.1.0.exe`. **If you bump the version,
update: build.ps1 output name, `VERSION` const in GhostScreen.cs, lang.txt `about_text`,
the release asset, and the site pages (7 HTML files: download links + JSON-LD version/fileSize).**

Compiler path (fallback in build.ps1): `%windir%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.

## 5. How the app works

- `Program.Main` parses CLI flags (`/lang: /nosound /quiet /apply /uninstall /music: /volume: /theme:`),
  calls `SetProcessDPIAware()`, then builds `MainForm`.
- Silent modes (`/apply`, `/uninstall`) skip all UI: the ctor reads the registry and runs
  the engine synchronously, then the process exits. `/apply` is used by the auto-start task.
- Install pipeline (worker thread):
  1. extract embedded driver files to `%TEMP%\GhostScreen-run`
  2. `pnputil /add-driver mttvdd.inf /install` → DriverStore
  3. copy `vdd_settings.xml` into the DriverStore folder via a SYSTEM scheduled task
     (the *only* way to write there as a standard process) + UMDF folder
  4. `devcon install ... Root\MttVDD` (device creation)
  5. `pnputil /restart-device` on `ROOT\MttVDD` and `ROOT\DISPLAY\0000`
  6. `EnumDisplaySettings` scan → find matching mode (default 2560x1440@60) →
     `ChangeDisplaySettings` with retries + empty-enumeration recovery
- Uninstall: `devcon remove Root\MttVDD` → `pnputil /delete-driver <store inf> /force` →
  delete UMDF settings → delete auto-start task → delete `HKCU\Software\GhostScreen`.
- Music: `Chiptune` synthesizes a Game Boy-style loop (2 square waves + noise, 140 BPM)
  as a WAV in memory (PlaySound SND_MEMORY|SND_LOOP); `Midi` generates an SMF .mid at
  runtime and plays it via `mciSendString` (MCI, repeat). Volume 25–100%.
- i18n: `L` class parses `lang.txt` (key<TAB>11 translations) at startup.
  Fonts per language: zh→Microsoft YaHei, ja→Yu Gothic UI, ko→Malgun Gothic, ru→Tahoma.
- Settings in `HKCU\Software\GhostScreen`: Lang, Theme, Music (0/1/2), Volume, CustomW/H/F, AutoStart.
- Update check: `WebClient` GET to the GitHub releases API (only when the user clicks
  the menu item — no telemetry).

## 6. Testing (what to run after any change)

Non-elevated (no UAC):
- **11-language matrix**: `C:\Users\PCF\AppData\Local\Temp\opencode\lang_one.ps1 -lg <code>`
  (isolated process per language, saves a screenshot to Desktop) — run for all 11 codes.
- Silent `/apply` via reflection (see `silent_test.ps1` in the same temp folder).
- MIDI build/play/stop via reflection.

Elevated (user must click UAC — the machine is headless):
- GUI smoke with `/lang:en /music:midi` (150s, then check `C:\Windows\Temp\GhostScreen.log`).
- `/apply` (should exit 0 and apply).
- `/uninstall` then GUI relaunch (auto-reinstall) — verify device back + 2560x1440.

Log file: `C:\Windows\Temp\GhostScreen.log` (UTF-8; PowerShell console shows mojibake —
read it with `Get-Content -Encoding UTF8` or in an editor).

## 7. Known quirks & rules of the road

- **C# 5 only.** No string interpolation, no `nameof`, no out-var, no expression-bodied
  members. Closures over loop variables are fine (each `it` is declared inside the loop).
- **Do NOT change the embedded driver files** unless the upstream
  (MikeTheTech/Virtual-Display-Driver) is updated — the install pipeline is tuned to them.
- `Application.Run` + `Application.Exit` in *one process* poisons later message loops
  (WM_QUIT) — never chain two GUI runs in the same test process; use separate processes.
- The GUI cannot be killed from a non-elevated shell (Stop-Process → Access denied):
  ask the user to close the window.
- `WebClient` has **no Timeout property** in .NET 4.x — don't add it.
- Console log lines look garbled for CJK — the file itself is correct UTF-8.
- Keep the marketing tone of README/site; the owner loves the '95 voice.

## 8. Release checklist (when you publish a new version)

1. Bump version in: `build.ps1` (out name), `VERSION` const, `about_text` in lang.txt.
2. Rebuild, smoke test (matrix + /apply + uninstall round-trip).
3. Update the 7 site pages (download links, JSON-LD version/fileSize, hreflang stays).
4. Update README (table), CHANGELOG.md, docs/TESTED-ON.md.
5. `gh release create vX.Y.Z releases\GhostScreen-X.Y.Z.exe --notes "..."`.
6. Commit + push (site auto-deploys via Pages).
7. Copy the EXE to `C:\Users\PCF\Desktop\GhostScreen.exe`.
8. Update the package ZIP (see §9) and the copy on the pen drive.

## 9. Distribution

- Official download: GitHub release asset.
- Local package for offline handover: `releases\GhostScreen-1.1.0-package.zip`
  (EXE + sources + drivers + assets + docs + README/LICENSE/CHANGELOG + this HANDOFF).
- Pen drive copy: `D:\FILE UTILI\VIBE CODE PROJECT\GhostScreen\`.

## 10. Contacts & ownership

- Product owner: **Luigi Strazzullo** — **Culture Digitali Srl**
- Repo admin: CultureDigitali (GitHub org/account)
- License: MIT (see LICENSE)