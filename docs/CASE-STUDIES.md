# Case Studies

Five real-world situations where a phantom monitor saves the day.

---

## 1. The Rack Server Nobody Ever Sees

**Setup.** A Windows Server in a colo rack, managed exclusively over RDP. The GPU is
a cheap GT 710 — present so the machine can render, never attached to anything.

**Before.** Every RDP session dropped to 800x600. Deployment scripts, browser-based
dashboards and monitoring UIs rendered at phone-dictionary scale. Screenshots taken
for compliance looked like a 1993 spreadsheet. Users complained. Compliance
complained. Everyone complained.

**After GhostScreen.** One elevated run. Session resolution: **1920x1080**. Dashboards
fit, screenshots are legible, nobody complains. The server still has no monitor.

> *Result: 10 minutes of setup, permanent full-HD remote sessions.*

---

## 2. The Laptop With a Broken Screen

**Setup.** A field laptop whose display died after a drop — LCD cracked, HDMI out
still functional, machine otherwise perfect. Used for data collection, driven through
Chrome Remote Desktop from a phone.

**Before.** The GPU refused to output anything with no panel attached. Remote sessions
were 640x480 with the desktop crammed into a corner. The data-collection app — which
required a minimum window size — **refused to start**.

**After GhostScreen.** The virtual display provides a stable 1366x768 desktop. The app
launches, the remote view is readable, the field job continues with a laptop that
would otherwise be e-waste.

> *Result: a $0 fix salvages a machine that "needed" a $400 repair.*

---

## 3. The HTPC Attached to a TV That's Off

**Setup.** A living-room HTPC wired to a TV that's switched off 90% of the time.
Automated tasks: downloading, transcoding, home-assistant dashboards.

**Before.** Windows detects the TV going to standby and drops the desktop to a
degraded mode. When the TV comes back, the UI is wrong, the taskbar is oversized and
the media player misbehaves until a manual resolution dance.

**After GhostScreen.** The virtual display anchors the desktop at 1080p regardless of
TV state. The TV is just a window into a stable desktop — turn it on, and everything
is exactly where it was.

> *Result: no more resolution roulette every time the TV wakes up.*

---

## 4. The CI/CD UI Test Lab

**Setup.** A fleet of test agents running UI automation (Selenium, WinAppDriver, and
friends). No displays, ever. Headless execution in the pipeline.

**Before.** Flaky tests. UI frameworks need a real desktop at a known resolution to
position elements reliably; the flaky 800x600 fallback produced sporadic failures
that nobody could reproduce locally.

**After GhostScreen.** Every agent gets a fixed 2560x1440 virtual desktop. Test
reproducibility jumps; the "works on my machine" plague retreats to other departments.

> *Result: fewer flakes, faster pipelines, calmer engineers.*

---

## 5. The GPU Farm

**Setup.** A small cluster of GPU cards (render nodes, hash boards, ML inference)
with no display outputs connected — the PCIe slots are all compute.

**Before.** Some toolkits insist on an attached display (or at least a display
context) to initialize. The workaround was HDMI dummy plugs: $10 each, always
shipping, occasionally failing, perpetually in the way.

**After GhostScreen.** A software dummy plug. Free, instant, re-runnable after any
reinstall — including after a full format of the node.

> *Result: the dongle drawer gets one shelf emptier.*

---

## 6. The Remote-Work Colleague

**Setup.** A workstation at the office, reached from home via Chrome Remote Desktop /
AnyDesk / TeamViewer. No monitor in the office — the desk is shared.

**Before.** Widescreen monitors are the office norm; remote sessions returned
800x600 or an arbitrary fraction of the workstation's *last known* resolution.
Spreadsheets scrolled sideways, presentations looked off, text was huge.

**After GhostScreen.** The virtual display pins the office machine at 2560x1440 —
widescreen, readable, exactly like sitting at the desk. The colleague's remote
experience finally matches the hardware they'd be using.

> *Result: remote work that doesn't feel like remote work.*

---

*Every story is the same story: Windows needs a monitor to be sane, and
GhostScreen 95 provides one that doesn't exist.*