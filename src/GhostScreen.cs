using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Net;
using System.Security.Principal;
using Microsoft.Win32;
using System.IO.Compression;

namespace GhostScreen {
    static class Program {
        public static bool Quiet, ApplyMode, UninstallMode;
        public static int MusicMode = -1;   // -1 unset, 0 chip, 1 midi, 2 off
        public static int Volume = -1;      // -1 unset
        public static string ThemeCode = null;

        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args) {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            foreach (string a0 in args) {
                string a = a0.ToLowerInvariant();
                if (a.StartsWith("/lang:")) {
                    string c = a.Substring(6);
                    if (c == "it" || c == "es" || c == "fr" || c == "de" || c == "en" || c == "zh" || c == "ja" || c == "pt" || c == "ru" || c == "ko" || c == "nl")
                        L.OverrideLang = c;
                } else if (a == "/nosound") {
                    Chiptune.Muted = true;
                    Midi.Muted = true;
                } else if (a == "/quiet") {
                    Quiet = true;
                } else if (a == "/apply") {
                    ApplyMode = true; Quiet = true;
                    Chiptune.Muted = true; Midi.Muted = true;
                } else if (a == "/uninstall") {
                    UninstallMode = true; Quiet = true;
                    Chiptune.Muted = true; Midi.Muted = true;
                } else if (a.StartsWith("/music:")) {
                    string m = a.Substring(7);
                    if (m == "chip") MusicMode = 0;
                    else if (m == "midi") MusicMode = 1;
                    else if (m == "off") MusicMode = 2;
                } else if (a.StartsWith("/volume:")) {
                    int v;
                    if (int.TryParse(a.Substring(8), out v)) Volume = Math.Max(0, Math.Min(100, v));
                } else if (a.StartsWith("/theme:")) {
                    string t = a.Substring(7);
                    if (t == "teal" || t == "plum" || t == "eggplant" || t == "dark") ThemeCode = t;
                }
            }
            MainForm f = new MainForm();
            if (!ApplyMode && !UninstallMode) Application.Run(f);
        }
    }

    // ============================================================
    // L: translation engine (11 languages)
    // ============================================================
    static class L {
        public static string OverrideLang = null;
        static Dictionary<string, Dictionary<string, string>> T = new Dictionary<string, Dictionary<string, string>>();
        static string[] langs = { "it", "es", "fr", "de", "en", "zh", "ja", "pt", "ru", "ko", "nl" };
        static string[] nativeNames = { "Italiano", "Español", "Français", "Deutsch", "English", "中文", "日本語", "Português", "Русский", "한국어", "Nederlands" };
        static bool loaded;
        public static string Code = "en";
        public static Font UIFont;

        public static void EnsureLoaded() {
            if (loaded) return;
            loaded = true;
            UIFont = new Font("MS Sans Serif", 8F);
            try {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Res.lang.txt")) {
                    if (s != null) {
                        using (StreamReader r = new StreamReader(s, Encoding.UTF8)) {
                            string line;
                            while ((line = r.ReadLine()) != null) {
                                if (line.Length == 0) continue;
                                string[] p = line.Split('\t');
                                if (p.Length < 12) continue;
                                Dictionary<string, string> d = new Dictionary<string, string>();
                                for (int i = 0; i < 11; i++) d[langs[i]] = p[i + 1].Replace("\\n", "\n");
                                T[p[0]] = d;
                            }
                        }
                    }
                }
            } catch { }
        }

        public static string Detect() {
            try {
                string c = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
                if (c == "it" || c == "es" || c == "fr" || c == "de" || c == "zh" || c == "ja" || c == "pt" || c == "ru" || c == "ko" || c == "nl") return c;
            } catch { }
            return "en";
        }

        public static void Set(string code) {
            EnsureLoaded();
            Code = code;
            string fam = "MS Sans Serif";
            if (code == "zh") fam = "Microsoft YaHei";
            else if (code == "ja") fam = "Yu Gothic UI";
            else if (code == "ko") fam = "Malgun Gothic";
            else if (code == "ru") fam = "Tahoma";
            try {
                using (Font f = new Font(fam, 8F)) UIFont = new Font(fam, 8F);
            } catch {
                UIFont = new Font("MS Sans Serif", 8F);
            }
        }

        public static string Get(string key, params object[] fmt) {
            EnsureLoaded();
            string v = null;
            Dictionary<string, string> d;
            if (T.TryGetValue(key, out d)) {
                if (!d.TryGetValue(Code, out v)) d.TryGetValue("en", out v);
            }
            if (v == null) v = key;
            if (fmt != null && fmt.Length > 0) {
                try { v = string.Format(v, fmt); } catch { }
            }
            return v;
        }

        public static string[] NativeNames { get { return nativeNames; } }
        public static string[] Codes { get { return langs; } }
    }

    // ============================================================
    // Chiptune: Game Boy style music, synthesized at runtime
    // (2 square waves + noise, like a real GB APU)
    // ============================================================
    static class Chiptune {
        public static bool Muted;
        static byte[] wav;
        static bool playing;
        static int curVol = -1;
        const int SR = 44100;

        [DllImport("winmm.dll")]
        static extern bool PlaySound(byte[] pszSound, IntPtr hmod, uint fdwSound);
        [DllImport("winmm.dll")]
        static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

        const uint SND_ASYNC = 0x0001;
        const uint SND_NODEFAULT = 0x0002;
        const uint SND_MEMORY = 0x0004;
        const uint SND_LOOP = 0x0008;

        public static void Start(int vol) {
            if (Muted || (playing && curVol == vol)) return;
            try {
                wav = Build(vol);
                PlaySound((string)null, IntPtr.Zero, 0);
                playing = PlaySound(wav, IntPtr.Zero, SND_ASYNC | SND_MEMORY | SND_LOOP | SND_NODEFAULT);
                curVol = vol;
            } catch { }
        }

        public static void Stop() {
            try { PlaySound((string)null, IntPtr.Zero, 0); } catch { }
            playing = false;
            curVol = -1;
        }

        static void AddSquare(double[] buf, int start, int len, double freq, double amp, double duty) {
            double phase = 0;
            for (int i = 0; i < len && start + i < buf.Length; i++) {
                double t = (double)i / len;
                double env = 1.0 - 0.2 * t;
                double v = (phase % 1.0 < duty) ? 1.0 : -1.0;
                buf[start + i] += v * amp * env;
                phase += freq / SR;
            }
        }

        static void AddNoise(double[] buf, int start, int len, double amp) {
            uint s = 0xACE1u;
            for (int i = 0; i < len && start + i < buf.Length; i++) {
                s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                double v = ((s & 1u) == 1u) ? 1.0 : -1.0;
                double t = (double)i / len;
                buf[start + i] += v * amp * (1.0 - t);
            }
        }

        static byte[] Build(int volPct) {
            double vol = Math.Max(0.0, Math.Min(1.0, volPct / 100.0));
            double eighth = 60.0 / 140.0 / 2.0;
            int eighthS = (int)(eighth * SR);
            int n = 32 * eighthS;
            double[] mix = new double[n];

            double[] lead = {
                523.25,659.26,783.99,1046.50, 493.88,587.33,783.99,987.77,
                523.25,659.26,783.99,1046.50, 440.00,523.25,659.26,880.00,
                392.00,493.88,587.33,783.99, 329.63,392.00,493.88,659.26,
                523.25,659.26,783.99,1046.50, 392.00,523.25,659.26,1046.50
            };
            double[] bass = { 130.81,196.00, 110.00,164.81, 98.00,146.83, 130.81,196.00 };

            for (int i = 0; i < 32; i++) {
                int t0 = i * eighthS;
                AddSquare(mix, t0, (int)(eighthS * 0.88), lead[i], 0.32 * vol, 0.5);
                if (i % 2 == 1) AddNoise(mix, t0, (int)(0.05 * SR), 0.09 * vol);
            }
            for (int i = 0; i < 8; i++) {
                int t0 = i * 4 * eighthS;
                AddSquare(mix, t0, 4 * eighthS - 2, bass[i], 0.42 * vol, 0.5);
            }

            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms, Encoding.ASCII);
            int dataLen = n * 2;
            w.Write(Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + dataLen);
            w.Write(Encoding.ASCII.GetBytes("WAVE"));
            w.Write(Encoding.ASCII.GetBytes("fmt "));
            w.Write(16);
            w.Write((short)1);       // PCM
            w.Write((short)1);       // mono
            w.Write(SR);
            w.Write(SR * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(Encoding.ASCII.GetBytes("data"));
            w.Write(dataLen);
            for (int i = 0; i < n; i++) {
                double v = mix[i];
                if (v > 0.98) v = 0.98;
                if (v < -0.98) v = -0.98;
                w.Write((short)(v * 32767));
            }
            w.Flush();
            return ms.ToArray();
        }
    }

    // ============================================================
    // Midi: real .mid playback via MCI (General MIDI square lead)
    // ============================================================
    static class Midi {
        public static bool Muted;
        static string path;
        static bool playing;

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        static extern int mciSendString(string command, StringBuilder ret, int retLen, IntPtr hwnd);

        public static void Play(int vol) {
            if (Muted || playing) return;
            try {
                if (path == null || !File.Exists(path)) path = Build();
                mciSendString("close ghostmidi", null, 0, IntPtr.Zero);
                string open = "open \"" + path + "\" type sequencer alias ghostmidi";
                if (mciSendString(open, null, 0, IntPtr.Zero) == 0) {
                    SetVolume(vol);
                    mciSendString("play ghostmidi repeat", null, 0, IntPtr.Zero);
                    playing = true;
                }
            } catch { }
        }

        public static void SetVolume(int vol) {
            try {
                mciSendString("setaudio ghostmidi volume to " + Math.Max(0, Math.Min(1000, vol * 10)), null, 0, IntPtr.Zero);
            } catch { }
        }

        public static void Stop() {
            try {
                if (playing) { mciSendString("close ghostmidi", null, 0, IntPtr.Zero); playing = false; }
            } catch { }
        }

        static int Freq2Note(double freq) {
            return (int)Math.Round(69 + 12 * Math.Log(freq / 440.0, 2));
        }

        static void WriteVar(BinaryWriter w, int v) {
            uint u = (uint)v;
            byte[] b = new byte[4];
            int n = 0;
            b[n++] = (byte)(u & 0x7F);
            u >>= 7;
            while (u > 0) {
                b[n++] = (byte)((u & 0x7F) | 0x80);
                u >>= 7;
            }
            for (int i = n - 1; i >= 0; i--) w.Write(b[i]);
        }

        static string Build() {
            string p = Path.Combine(Path.GetTempPath(), "GhostScreen95.mid");
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            w.Write(Encoding.ASCII.GetBytes("MThd"));
            w.Write(4);
            w.Write((short)0);   // format 0
            w.Write((short)1);   // one track
            w.Write((short)96);  // division

            MemoryStream tr = new MemoryStream();
            BinaryWriter t = new BinaryWriter(tr);
            WriteVar(t, 0); t.Write((byte)0xFF); t.Write((byte)0x51); t.Write((byte)3);
            t.Write((byte)0x06); t.Write((byte)0x8A); t.Write((byte)0x63); // tempo 140 BPM
            WriteVar(t, 0); t.Write((byte)0xC0); t.Write((byte)0); t.Write((byte)80); // ch0: square lead
            WriteVar(t, 0); t.Write((byte)0xC1); t.Write((byte)1); t.Write((byte)38); // ch1: synth bass

            double[] lead = {
                523.25,659.26,783.99,1046.50, 493.88,587.33,783.99,987.77,
                523.25,659.26,783.99,1046.50, 440.00,523.25,659.26,880.00,
                392.00,493.88,587.33,783.99, 329.63,392.00,493.88,659.26,
                523.25,659.26,783.99,1046.50, 392.00,523.25,659.26,1046.50
            };
            double[] bass = { 130.81,196.00, 110.00,164.81, 98.00,146.83, 130.81,196.00 };
            int eighth = 48; // 96 / 2

            for (int i = 0; i < 32; i++) {
                int note = Freq2Note(lead[i]);
                WriteVar(t, 0); t.Write((byte)0x90); t.Write((byte)0); t.Write((byte)note); t.Write((byte)95);
                if (i % 2 == 1) { WriteVar(t, 0); t.Write((byte)0x99); t.Write((byte)1); t.Write((byte)42); t.Write((byte)50); }
                if (i % 4 == 0) {
                    int bn = Freq2Note(bass[i / 4]);
                    WriteVar(t, 0); t.Write((byte)0x91); t.Write((byte)1); t.Write((byte)bn); t.Write((byte)100);
                }
                WriteVar(t, (int)(eighth * 0.88)); t.Write((byte)0x80); t.Write((byte)0); t.Write((byte)note); t.Write((byte)64);
                if (i % 2 == 1) { WriteVar(t, 0); t.Write((byte)0x89); t.Write((byte)1); t.Write((byte)42); t.Write((byte)50); }
                if (i % 4 == 3) {
                    int bn = Freq2Note(bass[(i - 3) / 4]);
                    WriteVar(t, 0); t.Write((byte)0x81); t.Write((byte)1); t.Write((byte)bn); t.Write((byte)64);
                }
                WriteVar(t, 96 - (int)(eighth * 0.88));
            }
            WriteVar(t, 0); t.Write((byte)0xFF); t.Write((byte)0x2F); t.Write((byte)0);

            w.Write(Encoding.ASCII.GetBytes("MTrk"));
            w.Write((int)tr.Length);
            tr.WriteTo(ms);
            File.WriteAllBytes(p, ms.ToArray());
            return p;
        }
    }

    // ============================================================
    // W95 styling + themes
    // ============================================================
    static class W95 {
        public static Color Face, Light, Shadow, Dark, Title1, Title2, Navy, Green, Teal;

        static W95() { SetTheme("teal"); }

        public static void SetTheme(string t) {
            Face = Color.FromArgb(192, 192, 192);
            Light = Color.White;
            Shadow = Color.FromArgb(128, 128, 128);
            Dark = Color.Black;
            Green = Color.FromArgb(0, 96, 0);
            switch (t) {
                case "plum":
                    Title1 = Color.FromArgb(88, 0, 88);
                    Title2 = Color.FromArgb(196, 120, 208);
                    Navy = Color.FromArgb(58, 0, 58);
                    Teal = Color.FromArgb(140, 70, 150);
                    break;
                case "eggplant":
                    Title1 = Color.FromArgb(47, 47, 95);
                    Title2 = Color.FromArgb(140, 150, 216);
                    Navy = Color.FromArgb(30, 30, 60);
                    Teal = Color.FromArgb(70, 70, 120);
                    break;
                case "dark":
                    Title1 = Color.FromArgb(32, 32, 32);
                    Title2 = Color.FromArgb(96, 96, 96);
                    Navy = Color.FromArgb(16, 16, 16);
                    Teal = Color.FromArgb(64, 64, 64);
                    Green = Color.FromArgb(0, 160, 0);
                    break;
                default:
                    Title1 = Color.FromArgb(0, 0, 128);
                    Title2 = Color.FromArgb(16, 132, 208);
                    Navy = Color.FromArgb(0, 0, 64);
                    Teal = Color.FromArgb(0, 128, 128);
                    break;
            }
        }
    }

    class W95Button : Button {
        public bool DefaultButton { get; set; }

        public W95Button() {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            BackColor = W95.Face;
            ForeColor = W95.Dark;
        }

        protected override void OnPaint(PaintEventArgs e) {
            ButtonState st = ButtonState.Normal;
            if (Capture) st |= ButtonState.Pushed;
            ControlPaint.DrawButton(e.Graphics, ClientRectangle, st);
            if (DefaultButton && Enabled) {
                using (Pen p = new Pen(W95.Dark))
                    e.Graphics.DrawRectangle(p, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            }
            if (Focused && Enabled)
                ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(5, 5, ClientSize.Width - 10, ClientSize.Height - 10));
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, W95.Dark,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
    }

    class W95MenuRenderer : ToolStripProfessionalRenderer {
        public W95MenuRenderer() : base(new ProfessionalColorTable()) { }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) {
            e.Graphics.Clear(W95.Face);
        }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) {
            Rectangle r = new Rectangle(Point.Empty, e.Item.Size);
            if (e.Item.Selected)
                e.Graphics.FillRectangle(new SolidBrush(W95.Title1), r);
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
            e.TextColor = e.Item.Selected ? Color.White : W95.Dark;
            base.OnRenderItemText(e);
        }
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) {
            int y = e.Item.Height / 2 - 1;
            using (Pen p = new Pen(W95.Shadow)) e.Graphics.DrawLine(p, 2, y, e.Item.Width - 3, y);
            using (Pen p = new Pen(W95.Light)) e.Graphics.DrawLine(p, 2, y + 1, e.Item.Width - 3, y + 1);
        }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) {
            using (Pen p = new Pen(W95.Dark))
                e.Graphics.DrawRectangle(p, 0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
        }
    }

    class W95MsgBox : Form {
        public static void Show(string text, string caption, Icon appIcon, bool error) {
            W95MsgBox box = new W95MsgBox(text, caption, appIcon, error, false);
            box.ShowDialog();
        }

        public static bool ShowYesNo(string text, string caption, Icon appIcon) {
            W95MsgBox box = new W95MsgBox(text, caption, appIcon, false, true);
            return box.ShowDialog() == DialogResult.Yes;
        }

        Rectangle closeRect;
        Font fnt;

        W95MsgBox(string text, string caption, Icon appIcon, bool error, bool yesNo) {
            fnt = L.UIFont;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = W95.Face;
            StartPosition = FormStartPosition.CenterParent;
            DoubleBuffered = true;
            Text = caption;

            int tw = TextRenderer.MeasureText(text, fnt).Width;
            int w = Math.Max(320, Math.Min(tw + 140, 620));
            int h = 160;
            ClientSize = new Size(w, h);

            PictureBox pic = new PictureBox();
            pic.Location = new Point(18, 44);
            pic.Size = new Size(32, 32);
            pic.BackColor = W95.Face;
            if (appIcon != null) pic.Image = new Icon(appIcon, 32, 32).ToBitmap();

            Label lb = new Label();
            lb.Text = text;
            lb.Font = fnt;
            lb.BackColor = W95.Face;
            lb.ForeColor = W95.Dark;
            lb.Location = new Point(62, 42);
            lb.AutoSize = false;
            lb.Size = new Size(w - 92, 78);
            lb.TextAlign = ContentAlignment.MiddleLeft;

            W95Button ok = new W95Button();
            ok.Text = L.Get("msg_ok");
            ok.Font = fnt;
            ok.Size = new Size(90, 26);
            ok.Location = new Point((w - 90) / 2, h - 42);
            ok.DialogResult = DialogResult.OK;

            W95Button yes = new W95Button();
            yes.Text = L.Get("msg_yes");
            yes.Font = fnt;
            yes.Size = new Size(90, 26);
            yes.Location = new Point((w - 190) / 2, h - 42);
            yes.DialogResult = DialogResult.Yes;

            W95Button no = new W95Button();
            no.Text = L.Get("msg_no");
            no.Font = fnt;
            no.Size = new Size(90, 26);
            no.Location = new Point((w - 190) / 2 + 100, h - 42);
            no.DialogResult = DialogResult.No;

            Controls.Add(pic);
            Controls.Add(lb);
            if (yesNo) { Controls.Add(yes); Controls.Add(no); AcceptButton = yes; }
            else { Controls.Add(ok); AcceptButton = ok; }
            closeRect = new Rectangle(w - 22, 3, 18, 15);
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            ControlPaint.DrawBorder3D(g, ClientRectangle, Border3DStyle.Raised, Border3DSide.All);
            Rectangle tr = new Rectangle(2, 2, ClientSize.Width - 4, 20);
            using (LinearGradientBrush b = new LinearGradientBrush(tr, W95.Title1, W95.Title2, 90F))
                g.FillRectangle(b, tr);
            g.DrawString(Text, new Font(fnt, FontStyle.Bold), Brushes.White, 8, 5);
            ControlPaint.DrawButton(g, closeRect, ButtonState.Normal);
            using (Pen p = new Pen(W95.Dark)) {
                int cx = closeRect.Left + 4, cy = closeRect.Top + 3;
                g.DrawLine(p, cx, cy, cx + 9, cy + 9);
                g.DrawLine(p, cx + 9, cy, cx, cy + 9);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (closeRect.Contains(e.Location)) Close();
        }
    }

    // ============================================================
    // DemoTour: first-run wizard with Win95 look
    // ============================================================
    class DemoTour : Form {
        static int step;
        static Label lblTitle, lblText, lblPage;
        static W95Button btnNext, btnBack, btnCancel;
        static PictureBox pic;
        static Icon appIcon;
        static Font tourFont = new Font("MS Sans Serif", 8F);

        static string[][] pages = new string[][] {
            new string[] {
                "Welcome to GhostScreen 95!",
                "This wizard will guide you through\n" +
                "the features of your new virtual\n" +
                "display software.\n\n" +
                "Click Next to continue."
            },
            new string[] {
                "What is GhostScreen?",
                "GhostScreen creates a virtual display\n" +
                "on headless PCs (no physical monitor\n" +
                "connected). Perfect for remote desktop,\n" +
                "media servers, and automation."
            },
            new string[] {
                "Select Resolution",
                "Choose your target resolution from\n" +
                "the main window. You can pick a\n" +
                "preset or enter custom dimensions.\n\n" +
                "Higher resolution = more workspace."
            },
            new string[] {
                "Install & Apply",
                "Click 'Install' to install the virtual\n" +
                "display driver, then 'Apply' to set\n" +
                "the resolution.\n\n" +
                "The driver runs silently in the background."
            },
            new string[] {
                "Music & Themes",
                "Enjoy chiptune or MIDI music while\n" +
                "you work! Switch themes from the\n" +
                "File menu: Teal, Plum, Eggplant, Dark.\n\n" +
                "You can also adjust the volume."
            },
            new string[] {
                "You're All Set!",
                "GhostScreen 95 is ready to use.\n\n" +
                "The virtual display will stay active\n" +
                "until you restart your PC.\n\n" +
                "Enjoy your invisible monitor!"
            }
        };

        public static void Run(Form parent, Icon icon) {
            appIcon = icon;
            step = 0;
            using (DemoTour tour = new DemoTour()) {
                tour.ShowDialog(parent);
            }
        }

        DemoTour() {
            FormBorderStyle = FormBorderStyle.None;
            BackColor = W95.Face;
            ClientSize = new Size(420, 260);
            StartPosition = FormStartPosition.CenterParent;
            DoubleBuffered = true;

            // Ghost monitor icon
            pic = new PictureBox();
            pic.Location = new Point(16, 44);
            pic.Size = new Size(48, 48);
            pic.BackColor = W95.Face;
            if (appIcon != null) pic.Image = new Icon(appIcon, 48, 48).ToBitmap();

            lblTitle = new Label();
            lblTitle.Font = new Font("MS Sans Serif", 9F, FontStyle.Bold);
            lblTitle.ForeColor = W95.Dark;
            lblTitle.BackColor = W95.Face;
            lblTitle.Location = new Point(72, 42);
            lblTitle.Size = new Size(330, 20);

            lblText = new Label();
            lblText.Font = tourFont;
            lblText.ForeColor = W95.Dark;
            lblText.BackColor = W95.Face;
            lblText.Location = new Point(72, 68);
            lblText.Size = new Size(330, 110);
            lblText.TextAlign = ContentAlignment.TopLeft;

            lblPage = new Label();
            lblPage.Font = tourFont;
            lblPage.ForeColor = Color.FromArgb(128, 128, 128);
            lblPage.BackColor = W95.Face;
            lblPage.TextAlign = ContentAlignment.MiddleCenter;
            lblPage.Size = new Size(400, 16);

            btnCancel = new W95Button();
            btnCancel.Text = "Cancel";
            btnCancel.Font = tourFont;
            btnCancel.Size = new Size(80, 26);
            btnCancel.Location = new Point(16, ClientSize.Height - 40);
            btnCancel.Click += delegate { Close(); };

            btnBack = new W95Button();
            btnBack.Text = "< Back";
            btnBack.Font = tourFont;
            btnBack.Size = new Size(80, 26);
            btnBack.Location = new Point(240, ClientSize.Height - 40);
            btnBack.Enabled = false;
            btnBack.Click += delegate { step--; UpdatePage(); };

            btnNext = new W95Button();
            btnNext.Text = "Next >";
            btnNext.Font = tourFont;
            btnNext.Size = new Size(80, 26);
            btnNext.Location = new Point(324, ClientSize.Height - 40);
            btnNext.Click += delegate {
                if (step < pages.Length - 1) { step++; UpdatePage(); }
                else Close();
            };

            Controls.Add(pic);
            Controls.Add(lblTitle);
            Controls.Add(lblText);
            Controls.Add(lblPage);
            Controls.Add(btnCancel);
            Controls.Add(btnBack);
            Controls.Add(btnNext);

            UpdatePage();
        }

        void UpdatePage() {
            lblTitle.Text = pages[step][0];
            lblText.Text = pages[step][1];
            lblPage.Text = "Step " + (step + 1) + " of " + pages.Length;
            btnBack.Enabled = step > 0;
            btnNext.Text = (step == pages.Length - 1) ? "Finish" : "Next >";
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            ControlPaint.DrawBorder3D(g, ClientRectangle, Border3DStyle.Raised, Border3DSide.All);
            // Title bar
            Rectangle tr = new Rectangle(3, 3, ClientSize.Width - 6, 20);
            using (SolidBrush tb = new SolidBrush(W95.Title1))
                g.FillRectangle(tb, tr);
            using (SolidBrush tg = new SolidBrush(W95.Title2))
                g.FillRectangle(tg, tr.X, tr.Y, tr.Width, 2);
            using (Font f = new Font("MS Sans Serif", 8F, FontStyle.Bold))
                g.DrawString("GhostScreen 95 - Tour", f, Brushes.White, 8, 5);
            // Progress dots
            int dotX = (ClientSize.Width - pages.Length * 14) / 2;
            for (int i = 0; i < pages.Length; i++) {
                Brush b = (i <= step) ? Brushes.White : new SolidBrush(W95.Shadow);
                g.FillEllipse(b, dotX + i * 14, ClientSize.Height - 58, 8, 8);
                if (i != step) b.Dispose();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Y <= 22) {
                Rectangle cr = new Rectangle(ClientSize.Width - 24, 4, 18, 16);
                if (cr.Contains(e.Location)) Close();
            }
        }
    }

    class MainForm : Form {
        const string VERSION = "1.1.0";

        // ---------- UI ----------
        RadioButton rbQ, rbF, rbH, rbV, rbC;
        NumericUpDown nudW, nudH, nudF;
        Label lblW, lblH, lblF;
        W95Button btnInstall, btnApply, btnRestart, btnAbout;
        TextBox txtLog;
        Label lblStatus, lblSeg;
        Button btnFile, btnLang, btnHelp;
        ContextMenuStrip cmFile, cmLang, cmHelp, cmTray;
        PictureBox pbHead;
        Rectangle minRect, closeRect;
        bool dragging;
        Point dragOff;
        Icon appIcon;
        Bitmap banner, logo;
        NotifyIcon ni;

        // ---------- state ----------
        Thread worker;
        volatile bool busy;
        string LogFile;
        readonly object logLock = new object();
        int selW, selH, selFreq;
        int musicMode = 0;      // 0 chip, 1 midi, 2 off
        int volume = 100;
        string themeCode = "teal";
        bool autoStart;
        int customW = 2560, customH = 1440, customF = 60;

        // ---------- P/Invoke ----------
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int ChangeDisplaySettings(ref DEVMODE dm, int flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int EnumDisplaySettings(string dev, int mode, ref DEVMODE dm);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DEVMODE {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
            public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
            public int dmFields;
            public int dmPositionX, dmPositionY;
            public int dmDisplayOrientation, dmDisplayFixedOutput;
            public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType;
            public int dmReserved1, dmReserved2;
            public int dmPanningWidth, dmPanningHeight;
        }

        const int DM_BITSPERPEL = 0x40000;
        const int DM_PELSWIDTH = 0x80000;
        const int DM_PELSHEIGHT = 0x100000;
        const int DM_DISPLAYFREQUENCY = 0x400000;
        const int DISP_CHANGE_SUCCESSFUL = 0;

        public MainForm() {
            try { appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            try {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Res.banner.png"))
                    banner = new Bitmap(s);
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Res.logo.png"))
                    logo = new Bitmap(s);
            } catch { }

            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            LogFile = Path.Combine(win, "Temp", "GhostScreen.log");

            // ---- settings from registry ----
            string code = L.Detect();
            bool isFirstRun = true;
            try {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\GhostScreen")) {
                    if (k != null) {
                        isFirstRun = false;
                        string v = k.GetValue("Lang") as string;
                        if (v != null && v.Length == 2) code = v;
                        string th = k.GetValue("Theme") as string;
                        if (th != null) themeCode = th;
                        int mv;
                        if (int.TryParse(k.GetValue("Music") as string, out mv)) musicMode = mv;
                        int vl;
                        if (int.TryParse(k.GetValue("Volume") as string, out vl)) volume = vl;
                        int cw;
                        if (int.TryParse(k.GetValue("CustomW") as string, out cw)) customW = cw;
                        int ch;
                        if (int.TryParse(k.GetValue("CustomH") as string, out ch)) customH = ch;
                        int cf;
                        if (int.TryParse(k.GetValue("CustomF") as string, out cf)) customF = cf;
                        string as_ = k.GetValue("AutoStart") as string;
                        autoStart = as_ == "1";
                    }
                }
            } catch { }
            if (L.OverrideLang != null) code = L.OverrideLang;
            if (Program.ThemeCode != null) themeCode = Program.ThemeCode;
            if (Program.MusicMode >= 0) musicMode = Program.MusicMode;
            if (Program.Volume >= 0) volume = Program.Volume;
            if (musicMode < 0 || musicMode > 2) musicMode = 0;
            if (volume < 0 || volume > 100) volume = 100;
            W95.SetTheme(themeCode);
            L.Set(code);
            Font = L.UIFont;

            // ---- silent modes ----
            if (Program.ApplyMode) { RunSilentApply(); return; }
            if (Program.UninstallMode) { RunSilentUninstall(); return; }

            Text = "GhostScreen 95";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = W95.Face;
            ClientSize = new Size(648, 584);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            Icon = appIcon;

            // ---- title bar height ----
            int TB = 24;

            // ---- header banner ----
            if (banner != null) {
                pbHead = new PictureBox();
                pbHead.Image = banner;
                pbHead.Location = new Point(4, TB + 4);
                pbHead.Size = new Size(ClientSize.Width - 8, 140);
                pbHead.BackColor = W95.Teal;
                pbHead.SizeMode = PictureBoxSizeMode.StretchImage;
            }

            // ---- menu bar ----
            Panel menuBar = new Panel();
            menuBar.Location = new Point(4, TB + 148);
            menuBar.Size = new Size(ClientSize.Width - 8, 22);
            menuBar.BackColor = W95.Face;

            btnFile = NewMenuBtn(4, 2, 40);
            btnFile.Click += delegate { cmFile.Show(menuBar, new Point(btnFile.Left, btnFile.Bottom + 2)); };
            btnLang = NewMenuBtn(46, 2, 56);
            btnLang.Click += delegate { cmLang.Show(menuBar, new Point(btnLang.Left, btnLang.Bottom + 2)); };
            btnHelp = NewMenuBtn(104, 2, 24);
            btnHelp.Click += delegate { cmHelp.Show(menuBar, new Point(btnHelp.Left, btnHelp.Bottom + 2)); };

            cmFile = NewMenu();
            cmLang = NewMenu();
            cmHelp = NewMenu();

            menuBar.Controls.Add(btnFile);
            menuBar.Controls.Add(btnLang);
            menuBar.Controls.Add(btnHelp);

            // ---- resolution group (left) ----
            GroupBox gRes = new GroupBox();
            gResBox = gRes;
            gRes.Text = L.Get("grp_res");
            gRes.Font = Font;
            gRes.ForeColor = W95.Dark;
            gRes.BackColor = W95.Face;
            gRes.Location = new Point(12, TB + 176);
            gRes.Size = new Size(356, 190);

            rbQ = NewRadio(16, 28); rbQ.Checked = true;
            rbF = NewRadio(16, 54);
            rbH = NewRadio(16, 80);
            rbV = NewRadio(16, 106);

            rbC = NewRadio(180, 28);
            rbC.CheckedChanged += delegate {
                bool on = rbC.Checked;
                nudW.Enabled = on; nudH.Enabled = on; nudF.Enabled = on;
            };
            lblW = NewSmallLabel(180, 54, 80);
            nudW = NewNud(264, 52, 80, 640, 7680);
            lblH = NewSmallLabel(180, 80, 80);
            nudH = NewNud(264, 78, 80, 480, 4320);
            lblF = NewSmallLabel(180, 106, 80);
            nudF = NewNud(264, 104, 80, 25, 240);
            nudW.Value = Math.Max(640, Math.Min(7680, customW));
            nudH.Value = Math.Max(480, Math.Min(4320, customH));
            nudF.Value = Math.Max(25, Math.Min(240, customF));
            nudW.Enabled = false; nudH.Enabled = false; nudF.Enabled = false;

            Label note = new Label();
            noteText = note;
            note.Font = Font;
            note.ForeColor = Color.FromArgb(80, 80, 80);
            note.BackColor = W95.Face;
            note.Location = new Point(16, 136);
            note.Size = new Size(320, 40);
            note.Text = L.Get("res_note");
            gRes.Controls.Add(rbQ); gRes.Controls.Add(rbF); gRes.Controls.Add(rbH); gRes.Controls.Add(rbV);
            gRes.Controls.Add(rbC);
            gRes.Controls.Add(lblW); gRes.Controls.Add(nudW);
            gRes.Controls.Add(lblH); gRes.Controls.Add(nudH);
            gRes.Controls.Add(lblF); gRes.Controls.Add(nudF);
            gRes.Controls.Add(note);

            // ---- actions (right side, stacked) ----
            int actX = 380, actW = 130, actH = 30, actGap = 36;
            btnInstall = NewButton(actX, TB + 176, actW, actH);
            btnInstall.DefaultButton = true;
            btnInstall.Click += delegate { StartInstall(); };

            btnApply = NewButton(actX, TB + 176 + actGap, actW, actH);
            btnApply.Click += delegate { StartApply(); };

            btnRestart = NewButton(actX, TB + 176 + actGap * 2, actW, actH);
            btnRestart.Click += delegate { StartRestart(); };

            btnAbout = NewButton(actX, TB + 176 + actGap * 3, actW, actH);
            btnAbout.Click += delegate { ShowAbout(); };

            // ---- log area ----
            GroupBox gLog = new GroupBox();
            gLogBox = gLog;
            gLog.Text = L.Get("grp_log");
            gLog.Font = Font;
            gLog.ForeColor = W95.Dark;
            gLog.BackColor = W95.Face;
            gLog.Location = new Point(12, TB + 374);
            gLog.Size = new Size(ClientSize.Width - 24, 158);

            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.BorderStyle = BorderStyle.Fixed3D;
            txtLog.BackColor = Color.White;
            txtLog.Font = new Font("Courier New", 8.5F);
            txtLog.Location = new Point(14, 28);
            txtLog.Size = new Size(ClientSize.Width - 52, 120);
            gLog.Controls.Add(txtLog);

            // ---- status bar ----
            Panel statusBar = new Panel();
            statusBar.Location = new Point(0, ClientSize.Height - 28);
            statusBar.Size = new Size(ClientSize.Width, 28);
            statusBar.BackColor = W95.Face;

            lblStatus = new Label();
            lblStatus.Font = Font;
            lblStatus.ForeColor = W95.Dark;
            lblStatus.BackColor = W95.Face;
            lblStatus.Location = new Point(14, 7);
            lblStatus.AutoSize = true;

            lblSeg = new Label();
            lblSeg.Text = "GhostScreen 95";
            lblSeg.Font = Font;
            lblSeg.ForeColor = W95.Dark;
            lblSeg.BackColor = W95.Face;
            lblSeg.Location = new Point(ClientSize.Width - 110, 7);
            lblSeg.AutoSize = true;

            statusBar.Controls.Add(lblStatus);
            statusBar.Controls.Add(lblSeg);

            // ---- add controls in order ----
            if (pbHead != null) Controls.Add(pbHead);
            Controls.Add(menuBar);
            Controls.Add(gRes);
            Controls.Add(gLog);
            Controls.Add(btnInstall); Controls.Add(btnApply);
            Controls.Add(btnRestart); Controls.Add(btnAbout);
            Controls.Add(statusBar);

            minRect = new Rectangle(ClientSize.Width - 46, 4, 18, 16);
            closeRect = new Rectangle(ClientSize.Width - 24, 4, 18, 16);
            AcceptButton = btnInstall;

            // ---- tray ----
            ni = new NotifyIcon();
            ni.Icon = appIcon;
            ni.Text = "GhostScreen 95";
            ni.Visible = true;
            ni.DoubleClick += delegate { ShowForm(); };
            cmTray = NewMenu();
            ni.ContextMenuStrip = cmTray;

            ApplyLanguage();
            if (musicMode == 0) Chiptune.Start(volume);
            else if (musicMode == 1) Midi.Play(volume);
            Log(L.Get("log_music", MusicName()));
            Log(L.Get("log_lang", L.Code));
            Log(L.Get("log_admin", IsAdmin()));
            if (!IsAdmin()) Log(L.Get("log_not_admin"));
            Log(L.Get("log_cur_res", CurrentResolution()));
            if (autoStart && IsAdmin()) {
                string cmd = "\"" + Application.ExecutablePath + "\" /apply";
                Run("schtasks.exe", "/create /tn \"GhostScreen AutoApply\" /tr \"" + cmd + "\" /sc onlogon /rl highest /f");
            }

            // ---- demo tour on first launch ----
            if (isFirstRun && !Program.Quiet) {
                DemoTour.Run(this, appIcon);
            }

            if (IsVddInstalled()) {
                Log(L.Get("log_drv_inst"));
                StartApply();
            } else {
                Log(L.Get("log_drv_miss"));
                StartInstall();
            }
        }

        // ---------- silent modes ----------
        void RunSilentApply() {
            Log("Silent /apply: " + L.Get("log_custom", customW, customH, customF));
            selW = customW; selH = customH; selFreq = customF;
            if (IsVddInstalled()) DoApply(); else DoInstall();
        }

        void RunSilentUninstall() {
            Log("Silent /uninstall");
            DoUninstall();
        }

        // ---------- close ----------
        protected override void OnFormClosed(FormClosedEventArgs e) {
            base.OnFormClosed(e);
            Chiptune.Stop();
            Midi.Stop();
            try { if (ni != null) { ni.Visible = false; ni.Dispose(); ni = null; } } catch { }
            try {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\GhostScreen")) {
                    k.SetValue("Lang", L.Code);
                    k.SetValue("Theme", themeCode);
                    k.SetValue("Music", musicMode);
                    k.SetValue("Volume", volume);
                    k.SetValue("CustomW", customW);
                    k.SetValue("CustomH", customH);
                    k.SetValue("CustomF", customF);
                    k.SetValue("AutoStart", autoStart ? "1" : "0");
                }
            } catch { }
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized) {
                Hide();
                WindowState = FormWindowState.Normal;
            }
        }

        void ShowForm() {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        Button NewMenuBtn(int x, int y, int w) {
            Button b = new Button();
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = W95.Face;
            b.ForeColor = W95.Dark;
            b.Font = Font;
            b.Location = new Point(x, y);
            b.Size = new Size(w, 18);
            b.TextAlign = ContentAlignment.MiddleLeft;
            return b;
        }

        ContextMenuStrip NewMenu() {
            ContextMenuStrip m = new ContextMenuStrip();
            m.RenderMode = ToolStripRenderMode.Professional;
            m.Renderer = new W95MenuRenderer();
            m.Font = L.UIFont;
            return m;
        }

        Label NewSmallLabel(int x, int y, int w) {
            Label l = new Label();
            l.Location = new Point(x, y);
            l.Size = new Size(w, 18);
            l.Font = L.UIFont;
            l.ForeColor = W95.Dark;
            l.BackColor = W95.Face;
            l.TextAlign = ContentAlignment.MiddleLeft;
            return l;
        }

        NumericUpDown NewNud(int x, int y, int w, int min, int max) {
            NumericUpDown n = new NumericUpDown();
            n.Location = new Point(x, y);
            n.Size = new Size(w, 20);
            n.Font = L.UIFont;
            n.Minimum = min;
            n.Maximum = max;
            n.BorderStyle = BorderStyle.Fixed3D;
            return n;
        }

        void ApplyLanguage() {
            Font = L.UIFont;
            SetFontRecursive(this, L.UIFont);

            btnFile.Text = L.Get("file");
            btnLang.Text = L.Get("language");
            btnHelp.Text = L.Get("help");

            rbQ.Text = L.Get("res2560");
            rbF.Text = "1920x1080";
            rbH.Text = "1366x768";
            rbV.Text = "1280x720";
            rbC.Text = L.Get("custom_label");
            lblW.Text = L.Get("cust_w");
            lblH.Text = L.Get("cust_h");
            lblF.Text = L.Get("cust_f");
            noteText.Text = L.Get("res_note");
            ((GroupBox)gResBox).Text = L.Get("grp_res");
            ((GroupBox)gLogBox).Text = L.Get("grp_log");
            btnInstall.Text = L.Get("btn_install");
            btnApply.Text = L.Get("btn_apply");
            btnRestart.Text = L.Get("btn_restart");
            btnAbout.Text = L.Get("btn_about");
            lblSeg.Text = "GhostScreen 95";

            RebuildMenus();
            Invalidate();
        }

        void RebuildMenus() {
            // ---- File ----
            cmFile.Items.Clear();

            ToolStripMenuItem miMusic = new ToolStripMenuItem(L.Get("music"));
            ToolStripMenuItem miChip = new ToolStripMenuItem(L.Get("music_chip"));
            miChip.Tag = 0; miChip.Checked = musicMode == 0;
            miChip.Click += delegate { SetMusicMode(0); };
            ToolStripMenuItem miMidi = new ToolStripMenuItem(L.Get("music_midi"));
            miMidi.Tag = 1; miMidi.Checked = musicMode == 1;
            miMidi.Click += delegate { SetMusicMode(1); };
            ToolStripMenuItem miOff = new ToolStripMenuItem(L.Get("music_off"));
            miOff.Tag = 2; miOff.Checked = musicMode == 2;
            miOff.Click += delegate { SetMusicMode(2); };
            miMusic.DropDownItems.Add(miChip);
            miMusic.DropDownItems.Add(miMidi);
            miMusic.DropDownItems.Add(miOff);
            cmFile.Items.Add(miMusic);

            ToolStripMenuItem miVol = new ToolStripMenuItem(L.Get("volume"));
            int[] vols = { 25, 50, 75, 100 };
            foreach (int v in vols) {
                ToolStripMenuItem it = new ToolStripMenuItem(v + "%");
                it.Tag = v;
                it.Checked = volume == v;
                it.Click += delegate { SetVolume((int)((ToolStripMenuItem)it).Tag); };
                miVol.DropDownItems.Add(it);
            }
            cmFile.Items.Add(miVol);

            ToolStripMenuItem miTheme = new ToolStripMenuItem(L.Get("theme"));
            string[] themes = { "teal", "plum", "eggplant", "dark" };
            string[] themeKeys = { "theme_teal", "theme_plum", "theme_eggplant", "theme_dark" };
            for (int i = 0; i < themes.Length; i++) {
                string tc = themes[i];
                ToolStripMenuItem it = new ToolStripMenuItem(L.Get(themeKeys[i]));
                it.Tag = tc;
                it.Checked = themeCode == tc;
                it.Click += delegate { SetTheme((string)((ToolStripMenuItem)it).Tag); };
                miTheme.DropDownItems.Add(it);
            }
            cmFile.Items.Add(miTheme);

            cmFile.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem miAuto = new ToolStripMenuItem(L.Get("autostart"));
            miAuto.CheckOnClick = true;
            miAuto.Checked = autoStart;
            miAuto.Click += delegate { ToggleAutoStart(); };
            cmFile.Items.Add(miAuto);

            cmFile.Items.Add(new ToolStripSeparator());

            cmFile.Items.Add(L.Get("report"), null, delegate { StartReport(); });
            cmFile.Items.Add(L.Get("checkupdate"), null, delegate { StartCheckUpdate(); });

            cmFile.Items.Add(new ToolStripSeparator());

            cmFile.Items.Add(L.Get("uninstall"), null, delegate { StartUninstall(); });

            cmFile.Items.Add(new ToolStripSeparator());

            cmFile.Items.Add(L.Get("exit"), null, delegate { Close(); });

            // ---- Language ----
            cmLang.Items.Clear();
            for (int i = 0; i < L.Codes.Length; i++) {
                string ccode = L.Codes[i];
                ToolStripMenuItem it = new ToolStripMenuItem(L.NativeNames[i]);
                it.Checked = (L.Code == ccode);
                it.Click += delegate { SwitchLang(ccode); };
                cmLang.Items.Add(it);
            }

            // ---- Help ----
            cmHelp.Items.Clear();
            cmHelp.Items.Add(L.Get("about"), null, delegate { ShowAbout(); });

            // ---- Tray ----
            cmTray.Items.Clear();
            cmTray.Items.Add(L.Get("tray_show"), null, delegate { ShowForm(); });
            cmTray.Items.Add(L.Get("tray_apply"), null, delegate { StartApply(); });
            cmTray.Items.Add(L.Get("tray_restart"), null, delegate { StartRestart(); });
            cmTray.Items.Add(new ToolStripSeparator());
            cmTray.Items.Add(L.Get("tray_quit"), null, delegate { Close(); });
        }

        void SetFontRecursive(Control parent, Font f) {
            foreach (Control c in parent.Controls) {
                try { c.Font = f; } catch { }
                SetFontRecursive(c, f);
            }
        }

        void Recolor() {
            RecolorRecursive(this);
            Invalidate();
        }

        void RecolorRecursive(Control parent) {
            foreach (Control c in parent.Controls) {
                if (!(c is TextBox) && !(c is PictureBox) && !(c is NumericUpDown)) {
                    try { c.BackColor = W95.Face; } catch { }
                    try { c.ForeColor = W95.Dark; } catch { }
                }
                RecolorRecursive(c);
            }
        }

        void SwitchLang(string code) {
            if (L.Code == code) return;
            L.Set(code);
            ApplyLanguage();
            lblStatus.Text = L.Get("st_ready");
            lblStatus.ForeColor = W95.Navy;
            Log(L.Get("log_lang", code));
        }

        void SetMusicMode(int mode) {
            if (musicMode == mode) return;
            musicMode = mode;
            Chiptune.Stop();
            Midi.Stop();
            if (musicMode == 0) Chiptune.Start(volume);
            else if (musicMode == 1) Midi.Play(volume);
            Log(L.Get("log_music", MusicName()));
            RebuildMenus();
        }

        void SetVolume(int v) {
            if (volume == v) return;
            volume = v;
            if (musicMode == 0) Chiptune.Start(volume);
            else if (musicMode == 1) Midi.SetVolume(volume);
            RebuildMenus();
        }

        void SetTheme(string t) {
            if (themeCode == t) return;
            themeCode = t;
            W95.SetTheme(t);
            Recolor();
            RebuildMenus();
        }

        void ToggleAutoStart() {
            autoStart = !autoStart;
            string task = "\"GhostScreen AutoApply\"";
            if (autoStart) {
                string cmd = "\"" + Application.ExecutablePath + "\" /apply";
                Run("schtasks.exe", "/create /tn " + task + " /tr \"" + cmd + "\" /sc onlogon /rl highest /f");
            } else {
                Run("schtasks.exe", "/delete /tn " + task + " /f");
            }
            string m = L.Get(autoStart ? "st_autostart_on" : "st_autostart_off");
            Log(m);
            SetStatus(m, W95.Navy);
            RebuildMenus();
        }

        string MusicName() {
            if (musicMode == 0) return L.Get("music_chip");
            if (musicMode == 1) return L.Get("music_midi");
            return L.Get("music_off");
        }

        void ShowAbout() {
            W95MsgBox.Show(L.Get("about_text"), L.Get("about_title"), appIcon, false);
        }

        RadioButton NewRadio(int x, int y) {
            RadioButton r = new RadioButton();
            r.Font = Font;
            r.ForeColor = W95.Dark;
            r.BackColor = W95.Face;
            r.Location = new Point(x, y);
            r.AutoSize = true;
            return r;
        }

        W95Button NewButton(int x, int y, int w, int h) {
            W95Button b = new W95Button();
            b.Font = Font;
            b.Size = new Size(w, h);
            b.Location = new Point(x, y);
            return b;
        }

        Label noteText;
        Control gResBox, gLogBox;

        // ---------- title bar ----------
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            // Window frame
            ControlPaint.DrawBorder3D(g, ClientRectangle, Border3DStyle.Raised, Border3DSide.All);

            // Title bar
            Rectangle tr = new Rectangle(3, 3, ClientSize.Width - 6, 20);
            using (SolidBrush tb = new SolidBrush(W95.Title1))
                g.FillRectangle(tb, tr);
            // Gradient accent (top 2px lighter)
            using (SolidBrush tg = new SolidBrush(W95.Title2))
                g.FillRectangle(tg, tr.X, tr.Y, tr.Width, 2);

            // Icon
            if (appIcon != null) g.DrawIcon(appIcon, new Rectangle(6, 4, 16, 16));

            // Title text (white, bold, shadow)
            Font fTitle = new Font("MS Sans Serif", 8F, FontStyle.Bold);
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                g.DrawString("GhostScreen 95", fTitle, sb, 27, 6);
            g.DrawString("GhostScreen 95", fTitle, Brushes.White, 26, 5);

            // Close button (3D raised)
            ControlPaint.DrawButton(g, closeRect, ButtonState.Normal);
            using (Pen p = new Pen(W95.Dark)) {
                int cx = closeRect.Left + 4, cy = closeRect.Top + 3;
                g.DrawLine(p, cx, cy, cx + 9, cy + 9);
                g.DrawLine(p, cx + 9, cy, cx, cy + 9);
            }
            // Minimize button
            ControlPaint.DrawButton(g, minRect, ButtonState.Normal);
            using (Pen p = new Pen(W95.Dark)) {
                g.DrawLine(p, minRect.Left + 3, minRect.Top + 11, minRect.Left + 13, minRect.Top + 11);
            }
            fTitle.Dispose();
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Y <= 20) {
                if (closeRect.Contains(e.Location)) { Close(); return; }
                if (minRect.Contains(e.Location)) { WindowState = FormWindowState.Minimized; return; }
                dragging = true;
                dragOff = new Point(e.X, e.Y);
            }
        }
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (dragging) {
                Point p = Cursor.Position;
                Location = new Point(p.X - dragOff.X, p.Y - dragOff.Y);
            }
        }
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            dragging = false;
        }

        // ---------- actions ----------
        void ReadSelection() {
            if (rbC != null && rbC.Checked) {
                selW = Math.Max(640, Math.Min(7680, (int)nudW.Value));
                selH = Math.Max(480, Math.Min(4320, (int)nudH.Value));
                selFreq = Math.Max(25, Math.Min(240, (int)nudF.Value));
                customW = selW; customH = selH; customF = selFreq;
                Log(L.Get("log_custom", selW, selH, selFreq));
                return;
            }
            selFreq = 60;
            selW = 2560; selH = 1440;
            if (rbF.Checked) { selW = 1920; selH = 1080; }
            else if (rbH.Checked) { selW = 1366; selH = 768; }
            else if (rbV.Checked) { selW = 1280; selH = 720; }
        }

        void StartInstall() {
            if (busy) return;
            busy = true;
            SetBusyUI(true);
            ReadSelection();
            Step(L.Get("st_install"));
            worker = new Thread(DoInstall);
            worker.IsBackground = true;
            worker.Start();
        }

        void StartApply() {
            if (busy) return;
            busy = true;
            SetBusyUI(true);
            ReadSelection();
            Step(L.Get("st_apply"));
            worker = new Thread(DoApply);
            worker.IsBackground = true;
            worker.Start();
        }

        void StartRestart() {
            if (busy) return;
            busy = true;
            SetBusyUI(true);
            ReadSelection();
            Step(L.Get("st_restart"));
            worker = new Thread(DoRestart);
            worker.IsBackground = true;
            worker.Start();
        }

        void StartUninstall() {
            if (busy) return;
            if (!W95MsgBox.ShowYesNo(L.Get("uninstall_confirm"), "GhostScreen 95", appIcon)) return;
            busy = true;
            SetBusyUI(true);
            Step(L.Get("st_uninstall"));
            worker = new Thread(DoUninstall);
            worker.IsBackground = true;
            worker.Start();
        }

        void StartReport() {
            if (busy) return;
            busy = true;
            SetBusyUI(true);
            Step(L.Get("st_report"));
            worker = new Thread(DoReport);
            worker.IsBackground = true;
            worker.Start();
        }

        void StartCheckUpdate() {
            if (busy) return;
            busy = true;
            SetBusyUI(true);
            Step(L.Get("checkupdate"));
            worker = new Thread(DoCheckUpdate);
            worker.IsBackground = true;
            worker.Start();
        }

        void Step(string m) {
            Log(m);
            SetStatus(m, W95.Navy);
        }

        void DoInstall() {
            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string pnputil = Path.Combine(win, "System32", "pnputil.exe");
            string work = Path.Combine(Path.GetTempPath(), "GhostScreen-run");
            try {
                Log("Extract driver files to " + work);
                Directory.CreateDirectory(work);
                Extract("Res.mttvdd.inf", work, "mttvdd.inf");
                Extract("Res.MttVDD.cat", work, "MttVDD.cat");
                Extract("Res.MttVDD.dll", work, "MttVDD.dll");
                Extract("Res.vdd_settings.xml", work, "vdd_settings.xml");
                Extract("Res.devcon.exe", work, "devcon.exe");
                Extract("Res.copy_settings.cmd", work, "copy_settings.cmd");
                string inf = Path.Combine(work, "mttvdd.inf");

                Step(L.Get("st_driverstore"));
                Run(pnputil, "/add-driver \"" + inf + "\" /install");

                string storeDir = FindDriverStoreDir(win);
                if (storeDir != null) {
                    Log("DriverStore: " + storeDir);
                    string task = "GhostCopy_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    string copyCmd = Path.Combine(work, "copy_settings.cmd");
                    Run("schtasks.exe", "/create /tn " + task + " /tr \"\"" + copyCmd + "\" \"" + storeDir + "\"\" /sc once /st 00:00 /ru SYSTEM /f");
                    Run("schtasks.exe", "/run /tn " + task);
                    Thread.Sleep(6000);
                    Run("schtasks.exe", "/delete /tn " + task + " /f");
                    if (File.Exists(Path.Combine(storeDir, "vdd_settings.xml"))) Log("vdd_settings.xml -> DriverStore: OK");
                    else Log("!! vdd_settings.xml -> DriverStore: NOT present");
                } else {
                    Log("!! DriverStore folder not found");
                }

                string umdf = Path.Combine(win, "System32", "drivers", "UMDF");
                try {
                    File.Copy(Path.Combine(work, "vdd_settings.xml"), Path.Combine(umdf, "vdd_settings.xml"), true);
                    Log("vdd_settings.xml -> UMDF: OK");
                } catch (Exception ex) {
                    Log("!! vdd_settings.xml -> UMDF: " + ex.Message);
                }

                Step(L.Get("st_create_dev"));
                if (DeviceMissing()) {
                    Run(Path.Combine(work, "devcon.exe"), "install \"" + inf + "\" \"Root\\MttVDD\"");
                } else {
                    Log("Virtual Display device already present");
                }

                Thread.Sleep(4000);
                Step(L.Get("st_restart_dev"));
                Run(pnputil, "/restart-device \"ROOT\\MttVDD\"");
                Run(pnputil, "/restart-device \"ROOT\\DISPLAY\\0000\"");
                Thread.Sleep(5000);

                Step(L.Get("st_wait_display"));
                if (!WaitForDisplayReady(25)) Log("!! display not ready after 25s, continuing anyway");
                ApplySelectedResolution();
                Finish(L.Get("fin_install"));
            } catch (Exception ex) {
                Log("FATAL: " + ex);
                Finish(L.Get("fin_error") + ex.Message);
            }
        }

        void DoApply() {
            try {
                Step(L.Get("st_wait_display"));
                if (!WaitForDisplayReady(25)) Log("!! display not ready after 25s, continuing anyway");
                ApplySelectedResolution();
                Finish(L.Get("fin_apply"));
            } catch (Exception ex) {
                Log("FATAL: " + ex);
                Finish(L.Get("fin_error") + ex.Message);
            }
        }

        void DoRestart() {
            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string pnputil = Path.Combine(win, "System32", "pnputil.exe");
            try {
                Step(L.Get("st_restart_dev"));
                Run(pnputil, "/restart-device \"ROOT\\MttVDD\"");
                Run(pnputil, "/restart-device \"ROOT\\DISPLAY\\0000\"");
                Thread.Sleep(6000);
                Step(L.Get("st_wait_display"));
                if (!WaitForDisplayReady(20)) Log("!! display not ready after 20s");
                ApplySelectedResolution();
                Finish(L.Get("fin_restart"));
            } catch (Exception ex) {
                Log("FATAL: " + ex);
                Finish(L.Get("fin_error") + ex.Message);
            }
        }

        void DoUninstall() {
            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string pnputil = Path.Combine(win, "System32", "pnputil.exe");
            try {
                string work = Path.Combine(Path.GetTempPath(), "GhostScreen-run");
                Directory.CreateDirectory(work);
                Extract("Res.devcon.exe", work, "devcon.exe");
                string devcon = Path.Combine(work, "devcon.exe");
                Log("Remove virtual display device");
                Run(devcon, "remove \"Root\\MttVDD\"");

                string root = Path.Combine(win, "System32", "DriverStore", "FileRepository");
                if (Directory.Exists(root)) {
                    foreach (string dir in Directory.GetDirectories(root, "mttvdd.inf_*")) {
                        string inf = Path.Combine(dir, "mttvdd.inf");
                        if (File.Exists(inf)) Run(pnputil, "/delete-driver \"" + inf + "\" /force");
                        else Log("!! no inf in " + dir);
                    }
                }

                string umdf = Path.Combine(win, "System32", "drivers", "UMDF", "vdd_settings.xml");
                try { if (File.Exists(umdf)) { File.Delete(umdf); Log("Deleted " + umdf); } } catch (Exception ex) { Log("!! " + ex.Message); }

                Run("schtasks.exe", "/delete /tn \"GhostScreen AutoApply\" /f");

                try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\GhostScreen", false); Log("Registry settings removed"); } catch { }

                Thread.Sleep(3000);
                Finish(L.Get("fin_uninstall"));
            } catch (Exception ex) {
                Log("FATAL: " + ex);
                Finish(L.Get("fin_error") + ex.Message);
            }
        }

        void DoReport() {
            try {
                string dir = Path.Combine(Path.GetTempPath(), "GhostScreen-report");
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);

                if (File.Exists(LogFile)) File.Copy(LogFile, Path.Combine(dir, "GhostScreen.log"), true);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("GhostScreen 95 - diagnostic report");
                sb.AppendLine("Version: " + VERSION);
                sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine();
                try {
                    using (System.Management.ManagementObjectSearcher s = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem")) {
                        foreach (System.Management.ManagementObject o in s.Get()) {
                            sb.AppendLine("OS: " + o["Caption"] + " " + o["Version"] + " (build " + o["BuildNumber"] + ")");
                            o.Dispose();
                        }
                    }
                } catch (Exception ex) { sb.AppendLine("OS: n/a (" + ex.Message + ")"); }
                try {
                    using (System.Management.ManagementObjectSearcher s = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Processor")) {
                        foreach (System.Management.ManagementObject o in s.Get()) {
                            sb.AppendLine("CPU: " + o["Name"]);
                            o.Dispose();
                        }
                    }
                } catch { }
                try {
                    using (System.Management.ManagementObjectSearcher s = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem")) {
                        foreach (System.Management.ManagementObject o in s.Get()) {
                            sb.AppendLine("RAM: " + Math.Round(Convert.ToDouble(o["TotalPhysicalMemory"]) / 1048576.0 / 1024.0, 1) + " GB");
                            sb.AppendLine("Model: " + o["Manufacturer"] + " " + o["Model"]);
                            o.Dispose();
                        }
                    }
                } catch { }
                try {
                    using (System.Management.ManagementObjectSearcher s = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_VideoController")) {
                        foreach (System.Management.ManagementObject o in s.Get()) {
                            sb.AppendLine("Display: " + o["Name"] + " [" + o["VideoModeDescription"] + "]");
                            o.Dispose();
                        }
                    }
                } catch { }
                try {
                    using (System.Management.ManagementObjectSearcher s = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3")) {
                        foreach (System.Management.ManagementObject o in s.Get()) {
                            sb.AppendLine("Disk " + o["DeviceID"] + ": " + Math.Round(Convert.ToDouble(o["FreeSpace"]) / 1048576.0 / 1024.0, 1) + " GB free / " + Math.Round(Convert.ToDouble(o["Size"]) / 1048576.0 / 1024.0, 1) + " GB");
                            o.Dispose();
                        }
                    }
                } catch { }
                sb.AppendLine();
                sb.AppendLine("Runtime: .NET " + Environment.Version + ", " + (Environment.Is64BitProcess ? "x64" : "x86") + ", Admin: " + IsAdmin());
                sb.AppendLine("Virtual display installed: " + IsVddInstalled());
                sb.AppendLine("Current resolution: " + CurrentResolution());
                string root = Path.Combine(win(), "System32", "DriverStore", "FileRepository");
                try {
                    if (Directory.Exists(root)) {
                        foreach (string dir2 in Directory.GetDirectories(root, "mttvdd.inf_*")) sb.AppendLine("DriverStore: " + dir2);
                    }
                } catch { }
                try {
                    using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\GhostScreen")) {
                        if (k != null) {
                            sb.AppendLine();
                            sb.AppendLine("Settings:");
                            foreach (string name in k.GetValueNames()) sb.AppendLine("  " + name + " = " + k.GetValue(name));
                        }
                    }
                } catch { }
                File.WriteAllText(Path.Combine(dir, "system.txt"), sb.ToString(), Encoding.UTF8);

                string zip = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "GhostScreen-Report-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip");
                ZipFile.CreateFromDirectory(dir, zip);
                string m = L.Get("report_done", zip);
                Log(m);
                SetStatus(m, W95.Green);
                if (!Program.Quiet) {
                    try { Invoke(new Action(delegate { W95MsgBox.Show(m, L.Get("report_title"), appIcon, false); })); } catch { }
                }
            } catch (Exception ex) {
                string m = L.Get("report_fail", ex.Message);
                Log(m);
                SetStatus(m, Color.Firebrick);
                if (!Program.Quiet) {
                    try { Invoke(new Action(delegate { W95MsgBox.Show(m, L.Get("report_title"), appIcon, true); })); } catch { }
                }
            }
            busy = false;
            SetBusyUI(false);
        }

        static string win() {
            return Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        }

        void DoCheckUpdate() {
            try {
                string json = null;
                using (WebClient wc = new WebClient()) {
                    wc.Headers["User-Agent"] = "GhostScreen/" + VERSION;
                    json = wc.DownloadString("https://api.github.com/repos/CultureDigitali/GhostScreen/releases/latest");
                }
                string tag = null;
                int i = json.IndexOf("\"tag_name\":\"");
                if (i >= 0) {
                    i += 12;
                    int j = json.IndexOf('"', i);
                    if (j > i) tag = json.Substring(i, j - i);
                }
                string remote = tag == null ? null : tag.TrimStart('v');
                if (remote == null || !IsNewer(remote, VERSION)) {
                    string m = L.Get("update_none", VERSION);
                    Log(m);
                    SetStatus(m, W95.Navy);
                    if (!Program.Quiet) {
                        try { Invoke(new Action(delegate { W95MsgBox.Show(m, L.Get("update_title"), appIcon, false); })); } catch { }
                    }
                } else {
                    string m = L.Get("update_new", tag);
                    Log(m);
                    bool go = false;
                    if (!Program.Quiet) {
                        try { Invoke(new Action(delegate { go = W95MsgBox.ShowYesNo(m, L.Get("update_title"), appIcon); })); } catch { }
                    }
                    if (go) {
                        try { Process.Start("https://github.com/CultureDigitali/GhostScreen/releases"); } catch { }
                    }
                }
            } catch (Exception ex) {
                string m = L.Get("update_fail", ex.Message);
                Log(m);
                SetStatus(m, Color.Firebrick);
                if (!Program.Quiet) {
                    try { Invoke(new Action(delegate { W95MsgBox.Show(m, L.Get("update_title"), appIcon, true); })); } catch { }
                }
            }
            busy = false;
            SetBusyUI(false);
        }

        static bool IsNewer(string a, string b) {
            string[] pa = a.Split('.');
            string[] pb = b.Split('.');
            for (int i = 0; i < 3; i++) {
                int x = i < pa.Length ? ParseNum(pa[i]) : 0;
                int y = i < pb.Length ? ParseNum(pb[i]) : 0;
                if (x != y) return x > y;
            }
            return false;
        }

        static int ParseNum(string s) {
            int v;
            return int.TryParse(s, out v) ? v : 0;
        }

        void ApplySelectedResolution() {
            Step(L.Get("st_applying", selW, selH));
            string res = ApplyResolution(selW, selH, selFreq);
            Log("Result: " + res);
            Thread.Sleep(3000);
            Log(L.Get("log_cur_res", CurrentResolution()));
        }

        void Finish(string msg) {
            busy = false;
            bool ok = !msg.StartsWith("ERRORE") && !msg.StartsWith("ERROR") && !msg.StartsWith("ERREUR") && !msg.StartsWith("FEHLER") && !msg.StartsWith("错误") && !msg.StartsWith("エラー") && !msg.StartsWith("ERRO") && !msg.StartsWith("ОШИБКА") && !msg.StartsWith("오류") && !msg.StartsWith("FOUT");
            if (ok) {
                string cur = CurrentResolution();
                Log(msg + " " + L.Get("res_now", cur));
                SetStatus(msg + "  " + L.Get("res_now", cur), W95.Green);
                if (!Program.Quiet) {
                    string fmsg = msg + "\r\n\r\n" + L.Get("res_now", cur) + "\r\n\r\n" + L.Get("log_path", LogFile);
                    try {
                        Invoke(new Action(delegate { W95MsgBox.Show(fmsg, "GhostScreen 95", appIcon, false); }));
                    } catch { }
                }
            } else {
                SetStatus(msg, Color.Firebrick);
                if (!Program.Quiet) {
                    string fmsg = msg + "\r\n\r\n" + L.Get("log_path", LogFile);
                    try {
                        Invoke(new Action(delegate { W95MsgBox.Show(fmsg, "GhostScreen 95", appIcon, true); }));
                    } catch { }
                }
            }
            SetBusyUI(false);
        }

        void SetBusyUI(bool b) {
            try {
                if (IsDisposed || btnInstall == null || btnInstall.IsDisposed) return;
                if (InvokeRequired) { Invoke(new Action<bool>(SetBusyUI), b); return; }
                btnInstall.Enabled = !b;
                btnApply.Enabled = !b;
                btnRestart.Enabled = !b;
                btnFile.Enabled = !b;
                btnLang.Enabled = !b;
                btnHelp.Enabled = !b;
            } catch { }
        }

        void SetStatus(string text, Color c) {
            try {
                if (IsDisposed || lblStatus == null || lblStatus.IsDisposed) return;
                if (InvokeRequired) { Invoke(new Action<string, Color>(SetStatus), text, c); return; }
                lblStatus.Text = text;
                lblStatus.ForeColor = c;
            } catch { }
        }

        void Log(string m) {
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + m;
            lock (logLock) {
                try { File.AppendAllText(LogFile, line + "\r\n"); } catch { }
            }
            AppendUi(line);
        }

        void AppendUi(string line) {
            try {
                if (IsDisposed || txtLog == null || txtLog.IsDisposed) return;
                if (InvokeRequired) { Invoke(new Action<string>(AppendUi), line); return; }
                txtLog.AppendText(line + "\r\n");
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
            } catch { }
        }

        void Extract(string resName, string dir, string fileName) {
            string dest = Path.Combine(dir, fileName);
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName)) {
                if (s == null) throw new Exception("Missing embedded resource: " + resName);
                using (FileStream fs = new FileStream(dest, FileMode.Create, FileAccess.Write)) s.CopyTo(fs);
            }
        }

        void Run(string exe, string args) {
            try {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process p = Process.Start(psi)) {
                    string o = p.StandardOutput.ReadToEnd();
                    string e = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    Log("  [" + Path.GetFileName(exe) + " " + args + "]");
                    if (!string.IsNullOrEmpty(o.Trim())) Log("    out: " + o.Trim());
                    if (!string.IsNullOrEmpty(e.Trim())) Log("    err: " + e.Trim());
                    if (p.ExitCode != 0) Log("    exit=" + p.ExitCode);
                }
            } catch (Exception ex) {
                Log("!! command failed (" + exe + " " + args + "): " + ex.Message);
            }
        }

        static string FindDriverStoreDir(string win) {
            string root = Path.Combine(win, "System32", "DriverStore", "FileRepository");
            if (!Directory.Exists(root)) return null;
            DirectoryInfo best = null;
            try {
                foreach (DirectoryInfo d in new DirectoryInfo(root).GetDirectories("mttvdd.inf_*")) {
                    if (best == null || d.LastWriteTime > best.LastWriteTime) best = d;
                }
            } catch { }
            return best == null ? null : best.FullName;
        }

        static bool IsVddInstalled() {
            try {
                using (System.Management.ManagementObjectSearcher s = new System.Management.ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'ROOT\\\\DISPLAY\\\\%' AND Name LIKE '%Virtual Display%'")) {
                    foreach (System.Management.ManagementObject o in s.Get()) { o.Dispose(); return true; }
                }
            } catch { }
            return false;
        }

        static bool DeviceMissing() { return !IsVddInstalled(); }

        static bool IsAdmin() {
            try {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal p = new WindowsPrincipal(id);
                return p.IsInRole(WindowsBuiltInRole.Administrator);
            } catch { return false; }
        }

        static bool WaitForDisplayReady(int seconds) {
            for (int i = 0; i < seconds * 2; i++) {
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                if (EnumDisplaySettings(null, -1, ref dm) != 0 && dm.dmPelsWidth > 0) return true;
                Thread.Sleep(500);
            }
            return false;
        }

        string ApplyResolution(int w, int h, int freq) {
            for (int attempt = 1; attempt <= 3; attempt++) {
                int i = 0;
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                bool any = false;
                while (EnumDisplaySettings(null, i, ref dm) != 0) {
                    any = true;
                    if (dm.dmPelsWidth == w && dm.dmPelsHeight == h && (freq == 0 || dm.dmDisplayFrequency == freq)) {
                        int r = ChangeDisplaySettings(ref dm, 0);
                        if (r == DISP_CHANGE_SUCCESSFUL) return "OK (mode #" + i + ", " + w + "x" + h + ")";
                        return "mode found but rejected (code " + r + ")";
                    }
                    i++;
                }
                if (!any && attempt == 1) {
                    Log("  empty enumeration: resetting display config...");
                    DEVMODE nul = new DEVMODE();
                    nul.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                    ChangeDisplaySettings(ref nul, 0);
                    Thread.Sleep(2500);
                }
                if (attempt < 3) { Log("  retry " + attempt + "/3..."); Thread.Sleep(3000); }
            }
            DEVMODE d = new DEVMODE();
            d.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            EnumDisplaySettings(null, -1, ref d);
            d.dmBitsPerPel = 32;
            d.dmPelsWidth = w;
            d.dmPelsHeight = h;
            d.dmDisplayFrequency = freq;
            d.dmFields = DM_BITSPERPEL | DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY;
            int r2 = ChangeDisplaySettings(ref d, 0);
            if (r2 == DISP_CHANGE_SUCCESSFUL) return "OK (manual, " + w + "x" + h + ")";
            return "FAIL: code " + r2 + " (0=ok, -2=not supported, -5=not updated). Reboot and retry.";
        }

        string CurrentResolution() {
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            if (EnumDisplaySettings(null, -1, ref dm) != 0 && dm.dmPelsWidth > 0)
                return dm.dmPelsWidth + "x" + dm.dmPelsHeight + " @" + dm.dmDisplayFrequency + "Hz";
            return "n/a";
        }
    }
}