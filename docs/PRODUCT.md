# GhostScreen 95 — Product Vision

## The one-liner

> GhostScreen 95 is a phantom monitor for headless Windows machines — a single EXE
> that installs a virtual display and unlocks real resolutions up to 4K, wrapped in a
> loving, faithful Windows 95 interface.

## Why it exists

Thousands of machines run with no display attached: servers, kiosks, laptops with
broken screens, HTPCs, VMs, GPU farms. Windows responds by degrading the desktop to
800x600 — or worse — and remote sessions inherit the mess. The hardware fix (a HDMI
dummy plug) is a $10 dongle you have to buy and keep plugged in forever. The software
fix was scattered across driver files, devcon invocations and arcane commands.

GhostScreen packages the whole fix into **one double-clickable EXE** that works on a
fresh install with zero internet access.

## Why Windows 95

Because it's the last UI that was *honest*:

- gradient title bars instead of flattened glass,
- buttons you can *see* being pressed,
- a status bar that tells you what's happening,
- no telemetry, no dark mode debates.

The '95 aesthetic isn't nostalgia for nostalgia's sake — it's a statement of intent:
*this tool does one job, visibly, and gets out of the way.*

## Naming

**GhostScreen** — a screen that is there and isn't. **95** — the era of the UI, and the
year when "2560x1440" sounded like science fiction. Together: a phantom monitor
beamed in from 1995, with 2026 resolutions.

## Product principles

1. **One file.** Everything — driver, catalog, settings, tools — embedded. Distribution
   is copying a single EXE.
2. **Idempotent.** Run it 1 or 100 times; the end state is the same.
3. **Self-healing.** Missing driver? Installs it. Stale? Replaces it. Display absent?
   Recreates it. Resolution wrong? Retries, resets, reports.
4. **Offline.** No downloads, no telemetry, no cloud. It works in an air-gapped rack.
5. **Visible.** A progress log you can actually read, and a status bar that tells you
   what's happening.

## Architecture

```
GhostScreen.exe (single file, .NET Framework 4.x, no external deps)
├── GUI: Windows 95-style shell (custom-painted title bar, buttons, message boxes)
├── Embedded resources:
│   ├── mttvdd.inf / MttVDD.cat / MttVDD.dll   (Virtual-Display-Driver, MIT)
│   ├── vdd_settings.xml                        (target resolution table)
│   ├── devcon.exe                              (WDK, device creation)
│   └── copy_settings.cmd                       (SYSTEM elevation helper)
└── Engine:
    ├── WMI detection (ROOT\DISPLAY\*)          → is the driver there?
    ├── pnputil /add-driver /install            → DriverStore installation
    ├── scheduled-task SYSTEM copy              → settings into DriverStore
    ├── devcon install ROOT\MttVDD              → create the device
    ├── pnputil /restart-device                 → wake the display up
    └── ChangeDisplaySettings (P/Invoke)        → apply resolution, verify, retry
```

## Roadmap

- **v1.1** — custom resolution input, multi-monitor virtual displays, refresh-rate picker
- **v1.2** — silent command-line mode (`GhostScreen.exe /silent 2560x1440`) for scripts
- **v1.3** — localization, dark gray "Pro" skin (still '95-styled, obviously)

## FAQ

**Is this a hack?**
It's the standard mechanism: a signed virtual display driver + standard Windows APIs.
Nothing is patched, hooked or cracked.

**Why does it need admin?**
Drivers install into the DriverStore and devices get created — that requires elevation
by design. The EXE asks once.

**Does it survive a reboot?**
The driver and settings persist. The resolution is re-applied on demand; a desktop
session (RDP) remembers it.

**Will it break my real monitor?**
It adds a *second* display. Your real one stays untouched — and once the virtual
monitor exists, your real one can even be turned off entirely.

## Credits

GhostScreen 95 is **designed and developed by Luigi Strazzullo for Culture Digitali Srl**.
The virtual display driver is the open-source Virtual-Display-Driver by MikeTheTech (MIT);
`devcon.exe` comes from the Microsoft Windows Driver Kit.

## Disclaimer

GhostScreen is an independent project. Not affiliated with Microsoft; Windows 95 is a
trademark of Microsoft Corporation. The virtual display driver is the open-source
Virtual-Display-Driver by MikeTheTech (MIT).