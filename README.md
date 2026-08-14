<div align="center">

<img src="assets/banner.png" alt="GhostScreen 95" width="640"/>

# GhostScreen 95

### The display that doesn't exist.

**Your headless PC just got a monitor. A phantom one. In 4K.**

<br/>

**Single-file EXE · No installers · No internet needed · Windows 10/11 x64**

</div>

---

## The problem

Your machine is a **headless box**: a server in a rack, a laptop with a broken screen, a
workstation reached over RDP or Chrome Remote Desktop. There is no physical monitor —
so Windows gives you **no monitor at all**.

And then everything breaks:

- Remote sessions cap at a blurry **800x600** (or 640x480)
- Apps you *know* are fullscreen show a tiny centered window
- Games, design tools and media players refuse to render properly
- UI scaling makes everything look like a spreadsheet from 1993

You can't buy a monitor for a machine you'll never look at. So what do you do?

## The fix

**GhostScreen 95** installs a *virtual display* — a monitor that exists only in software —
and dials in the resolution **you** choose: **2560x1440**, **1920x1080**, **1366x768**,
**1280x720**, right out of the box at 60 Hz.

The OS thinks a beautiful monitor is plugged in. Remote clients happily render
widescreen, full-HD sessions. Your headless rig behaves like a normal desktop —
because, as far as Windows is concerned, it *is* one.

> A ghost screen. It's there. It isn't. Nobody knows. Windows loves it.

## What you get

| | |
|---|---|
| **One EXE, everything inside** | Driver, catalog, settings, tools — all embedded. No installer, no leftovers. |
| **Windows 95 soul** | A faithful '95-era UI. Gradient title bar, chiseled buttons, MS Sans Serif. Because why not. |
| **Choose your resolution** | 2560x1440, 1920x1080, 1366x768, 1280x720 — up to 4K supported by the driver. |
| **Self-healing** | Detects a missing or stale driver, reinstalls, restarts the display, retries. Works on a fresh format. |
| **Zero internet** | Everything runs offline, from the EXE. |
| **Re-runnable** | You formatted? Run it again. It's idempotent. |

## Quick start

1. Download `GhostScreen-1.0.0.exe` from [releases](releases/).
2. Right-click → **Run as administrator**.
3. Pick your resolution. Hit **Installa e Applica**.
4. Done. Your remote session is now 2560x1440. 🎩

<sup>Fine print: requires Windows 10/11 x64 and the built-in .NET Framework 4.x (preinstalled).
The 60-second run applies the resolution; a reboot persists it.</sup>

## Use cases

- **Headless servers** managed over RDP — real resolution, real productivity
- **Laptops with broken screens** — HDMI out is dead, but the machine isn't
- **HTPCs** attached to a TV that's often off — keep the desktop sane
- **CI/CD and test labs** — stable virtual displays for UI tests
- **Remote work** via Chrome Remote Desktop / AnyDesk / TeamViewer — crisp widescreen
- **GPU farms and mining rigs** — dummy-plug alternative without the dongle

See [docs/CASE-STUDIES.md](docs/CASE-STUDIES.md) for the full stories.

## Documentation

- [Product vision](docs/PRODUCT.md)
- [Case studies](docs/CASE-STUDIES.md)
- [Build from source](src/build.ps1) — needs only the .NET Framework csc.exe

## Credits

**GhostScreen 95 is crafted by [Luigi Strazzullo](https://github.com/CultureDigitali)
for Culture Digitali Srl** — with the '95-era honesty we all deserve.

- Virtual display driver: [MikeTheTech / Virtual-Display-Driver](https://github.com/itsmikethetech/Virtual-Display-Driver) (MIT)
- `devcon.exe`: Windows Driver Kit (Microsoft)
- Everything else: original work by Luigi Strazzullo / Culture Digitali Srl

**Live project site:** [https://CultureDigitali.github.io/GhostScreen](https://CultureDigitali.github.io/GhostScreen)

## License

[MIT](LICENSE). GhostScreen is not affiliated with Microsoft. Windows 95 is a trademark
of Microsoft Corporation — we just miss it.

<div align="center">

<img src="assets/logo.png" alt="GhostScreen logo" width="96"/>

*"Il monitor non c'è, ma la risoluzione sì."*

</div>