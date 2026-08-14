using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Security.Principal;
using Microsoft.Win32;

namespace GhostScreen {
    static class Program {
        public static bool Quiet;

        [STAThread]
        static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            for (int i = 0; i < args.Length; i++) {
                string a = args[i].ToLowerInvariant();
                if (a.StartsWith("/lang:")) {
                    string c = a.Substring(6);
                    if (c == "it" || c == "es" || c == "fr" || c == "de" || c == "en" || c == "zh" || c == "ja")
                        L.OverrideLang = c;
                } else if (a == "/nosound") {
                    Chiptune.Muted = true;
                } else if (a == "/quiet") {
                    Quiet = true;
                }
            }
            Application.Run(new MainForm());
        }
    }

    // ============================================================
    // L: translation engine (it, es, fr, de, en, zh, ja)
    // ============================================================
    static class L {
        public static string OverrideLang = null;
        static Dictionary<string, Dictionary<string, string>> T = new Dictionary<string, Dictionary<string, string>>();
        static string[] langs = { "it", "es", "fr", "de", "en", "zh", "ja" };
        static string[] nativeNames = { "Italiano", "Español", "Français", "Deutsch", "English", "中文", "日本語" };
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
                        using (StreamReader r = new StreamReader(s, System.Text.Encoding.UTF8)) {
                            string line;
                            while ((line = r.ReadLine()) != null) {
                                if (line.Length == 0) continue;
                                string[] p = line.Split('\t');
                                if (p.Length < 8) continue;
                                Dictionary<string, string> d = new Dictionary<string, string>();
                                for (int i = 0; i < 7; i++) d[langs[i]] = p[i + 1].Replace("\\n", "\n");
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
                if (c == "it" || c == "es" || c == "fr" || c == "de" || c == "zh" || c == "ja") return c;
            } catch { }
            return "en";
        }

        public static void Set(string code) {
            EnsureLoaded();
            Code = code;
            string fam = "MS Sans Serif";
            if (code == "zh") fam = "Microsoft YaHei";
            else if (code == "ja") fam = "Yu Gothic UI";
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
        const int SR = 44100;

        [DllImport("winmm.dll")]
        static extern bool PlaySound(byte[] pszSound, IntPtr hmod, uint fdwSound);
        [DllImport("winmm.dll")]
        static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

        const uint SND_ASYNC = 0x0001;
        const uint SND_NODEFAULT = 0x0002;
        const uint SND_MEMORY = 0x0004;
        const uint SND_LOOP = 0x0008;

        public static void Start() {
            if (Muted || playing) return;
            try {
                if (wav == null) wav = Build();
PlaySound((string)null, IntPtr.Zero, 0);
                playing = PlaySound(wav, IntPtr.Zero, SND_ASYNC | SND_MEMORY | SND_LOOP | SND_NODEFAULT);
            } catch { }
        }

        public static void Stop() {
            try { PlaySound((string)null, IntPtr.Zero, 0); } catch { }
            playing = false;
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

        static byte[] Build() {
            // 140 BPM, 4 bars of 8 eighths
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
                AddSquare(mix, t0, (int)(eighthS * 0.88), lead[i], 0.32, 0.5);
                if (i % 2 == 1) AddNoise(mix, t0, (int)(0.05 * SR), 0.09);
            }
            for (int i = 0; i < 8; i++) {
                int t0 = i * 4 * eighthS;
                AddSquare(mix, t0, 4 * eighthS - 2, bass[i], 0.42, 0.5);
            }

            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms, System.Text.Encoding.ASCII);
            int dataLen = n * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + dataLen);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            w.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            w.Write(16);
            w.Write((short)1);       // PCM
            w.Write((short)1);       // mono
            w.Write(SR);
            w.Write(SR * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
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
    // Win95 styling
    // ============================================================
    static class W95 {
        public static readonly Color Face = Color.FromArgb(192, 192, 192);
        public static readonly Color Light = Color.White;
        public static readonly Color Shadow = Color.FromArgb(128, 128, 128);
        public static readonly Color Dark = Color.Black;
        public static readonly Color Title1 = Color.FromArgb(0, 0, 128);
        public static readonly Color Title2 = Color.FromArgb(16, 132, 208);
        public static readonly Color Navy = Color.FromArgb(0, 0, 64);
        public static readonly Color Green = Color.FromArgb(0, 96, 0);
        public static readonly Color Teal = Color.FromArgb(0, 128, 128);
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
            W95MsgBox box = new W95MsgBox(text, caption, appIcon, error);
            box.ShowDialog();
        }

        Rectangle closeRect;
        Font fnt;

        W95MsgBox(string text, string caption, Icon appIcon, bool error) {
            fnt = L.UIFont;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = W95.Face;
            StartPosition = FormStartPosition.CenterParent;
            DoubleBuffered = true;
            Text = caption;

            int tw = TextRenderer.MeasureText(text, fnt).Width;
            int w = Math.Max(300, Math.Min(tw + 130, 560));
            int h = 150;
            ClientSize = new Size(w, h);

            PictureBox pic = new PictureBox();
            pic.Location = new Point(18, 40);
            pic.Size = new Size(32, 32);
            pic.BackColor = W95.Face;
            if (appIcon != null) pic.Image = new Icon(appIcon, 32, 32).ToBitmap();

            Label lb = new Label();
            lb.Text = text;
            lb.Font = fnt;
            lb.BackColor = W95.Face;
            lb.ForeColor = W95.Dark;
            lb.Location = new Point(62, 38);
            lb.AutoSize = false;
            lb.Size = new Size(w - 90, 70);
            lb.TextAlign = ContentAlignment.MiddleLeft;

            W95Button ok = new W95Button();
            ok.Text = L.Get("msg_ok");
            ok.Font = fnt;
            ok.Size = new Size(90, 26);
            ok.Location = new Point((w - 90) / 2, h - 40);
            ok.DialogResult = DialogResult.OK;

            Controls.Add(pic);
            Controls.Add(lb);
            Controls.Add(ok);
            closeRect = new Rectangle(w - 22, 3, 18, 15);
            AcceptButton = ok;
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

    class MainForm : Form {
        // ---------- UI ----------
        RadioButton rbQ, rbF, rbH, rbV;
        W95Button btnInstall, btnApply, btnRestart, btnAbout;
        TextBox txtLog;
        Label lblStatus, lblSeg;
        Button btnFile, btnLang, btnHelp;
        ContextMenuStrip cmFile, cmLang, cmHelp;
        PictureBox pbHead;
        Rectangle minRect, closeRect;
        bool dragging;
        Point dragOff;
        Icon appIcon;
        Bitmap banner, logo;

        // ---------- state ----------
        Thread worker;
        volatile bool busy;
        string LogFile;
        readonly object logLock = new object();
        int selW, selH;
        bool musicOn;

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

            // ---- language ----
            string code = L.Detect();
            try {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\GhostScreen")) {
                    if (k != null) {
                        string v = k.GetValue("Lang") as string;
                        if (v != null && v.Length == 2) code = v;
                    }
                }
            } catch { }
            if (L.OverrideLang != null) code = L.OverrideLang;
            L.Set(code);
            Font = L.UIFont;

            Text = "GhostScreen 95";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = W95.Face;
            ClientSize = new Size(640, 576);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            Icon = appIcon;

            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            LogFile = Path.Combine(win, "Temp", "GhostScreen.log");

            // ---- header banner ----
            if (banner != null) {
                int bh = banner.Width / 2;
                int by = Math.Max(0, (banner.Height - bh) / 2);
                if (by + bh > banner.Height) { bh = banner.Height; by = 0; }
                Rectangle src = new Rectangle(0, by, banner.Width, bh);
                Bitmap head = new Bitmap(624, 140);
                using (Graphics g = Graphics.FromImage(head))
                    g.DrawImage(banner, new Rectangle(0, 0, 624, 140), src, GraphicsUnit.Pixel);
                pbHead = new PictureBox();
                pbHead.Image = head;
                pbHead.Location = new Point(8, 46);
                pbHead.Size = new Size(624, 140);
                pbHead.BackColor = W95.Teal;
                pbHead.SizeMode = PictureBoxSizeMode.Normal;
            }

            // ---- menu bar ----
            Panel menuBar = new Panel();
            menuBar.Location = new Point(0, 22);
            menuBar.Size = new Size(640, 22);
            menuBar.BackColor = W95.Face;

            btnFile = NewMenuBtn(4, 2, 36);
            btnFile.Click += delegate { cmFile.Show(menuBar, new Point(btnFile.Left, btnFile.Bottom + 2)); };
            btnLang = NewMenuBtn(42, 2, 52);
            btnLang.Click += delegate { cmLang.Show(menuBar, new Point(btnLang.Left, btnLang.Bottom + 2)); };
            btnHelp = NewMenuBtn(96, 2, 22);
            btnHelp.Click += delegate { cmHelp.Show(menuBar, new Point(btnHelp.Left, btnHelp.Bottom + 2)); };

            cmFile = NewMenu();
            cmLang = NewMenu();
            cmHelp = NewMenu();

            menuBar.Controls.Add(btnFile);
            menuBar.Controls.Add(btnLang);
            menuBar.Controls.Add(btnHelp);

            // ---- resolution group ----
            GroupBox gRes = new GroupBox();
            gResBox = gRes;
            gRes.Text = L.Get("grp_res");
            gRes.Font = Font;
            gRes.ForeColor = W95.Dark;
            gRes.BackColor = W95.Face;
            gRes.Location = new Point(12, 192);
            gRes.Size = new Size(352, 166);

            rbQ = NewRadio(16, 28); rbQ.Checked = true;
            rbF = NewRadio(16, 56);
            rbH = NewRadio(16, 84);
            rbV = NewRadio(16, 112);
            Label note = new Label();
            noteText = note;
            note.Font = Font;
            note.ForeColor = Color.FromArgb(80, 80, 80);
            note.BackColor = W95.Face;
            note.Location = new Point(16, 140);
            note.AutoSize = true;
            gRes.Controls.Add(rbQ); gRes.Controls.Add(rbF); gRes.Controls.Add(rbH); gRes.Controls.Add(rbV); gRes.Controls.Add(note);

            // ---- actions ----
            btnInstall = NewButton(380, 210, 124, 28);
            btnInstall.DefaultButton = true;
            btnInstall.Click += delegate { StartInstall(); };
            btnApply = NewButton(380, 246, 124, 28);
            btnApply.Click += delegate { StartApply(); };
            btnRestart = NewButton(380, 282, 124, 28);
            btnRestart.Click += delegate { StartRestart(); };
            btnAbout = NewButton(380, 318, 124, 28);
            btnAbout.Click += delegate { ShowAbout(); };

            // ---- log ----
            GroupBox gLog = new GroupBox();
            gLogBox = gLog;
            gLog.Text = L.Get("grp_log");
            gLog.Font = Font;
            gLog.ForeColor = W95.Dark;
            gLog.BackColor = W95.Face;
            gLog.Location = new Point(12, 366);
            gLog.Size = new Size(616, 172);

            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.BorderStyle = BorderStyle.Fixed3D;
            txtLog.BackColor = Color.White;
            txtLog.Font = new Font("Courier New", 8.5F);
            txtLog.Location = new Point(16, 36);
            txtLog.Size = new Size(584, 122);
            gLog.Controls.Add(txtLog);

            // ---- status bar ----
            Panel statusBar = new Panel();
            statusBar.Location = new Point(0, 548);
            statusBar.Size = new Size(640, 28);
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
            lblSeg.Location = new Point(538, 7);
            lblSeg.AutoSize = true;

            statusBar.Controls.Add(lblStatus);
            statusBar.Controls.Add(lblSeg);

            if (pbHead != null) Controls.Add(pbHead);
            Controls.Add(menuBar);
            Controls.Add(gRes);
            Controls.Add(gLog);
            Controls.Add(btnInstall); Controls.Add(btnApply); Controls.Add(btnRestart); Controls.Add(btnAbout);
            Controls.Add(statusBar);

            minRect = new Rectangle(ClientSize.Width - 42, 3, 18, 15);
            closeRect = new Rectangle(ClientSize.Width - 22, 3, 18, 15);
            AcceptButton = btnInstall;

            ApplyLanguage();
            Chiptune.Start();
            musicOn = !Chiptune.Muted;

            Log(L.Get("log_lang", L.Code));
            Log(L.Get("log_admin", IsAdmin()));
            if (!IsAdmin()) Log(L.Get("log_not_admin"));
            Log(L.Get("log_cur_res", CurrentResolution()));
            if (IsVddInstalled()) {
                Log(L.Get("log_drv_inst"));
                StartApply();
            } else {
                Log(L.Get("log_drv_miss"));
                StartInstall();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            base.OnFormClosed(e);
            Chiptune.Stop();
            try {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\GhostScreen")) {
                    k.SetValue("Lang", L.Code);
                }
            } catch { }
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
            noteText.Text = L.Get("res_note");
            ((GroupBox)gResBox).Text = L.Get("grp_res");
            ((GroupBox)gLogBox).Text = L.Get("grp_log");
            btnInstall.Text = L.Get("btn_install");
            btnApply.Text = L.Get("btn_apply");
            btnRestart.Text = L.Get("btn_restart");
            btnAbout.Text = L.Get("btn_about");
            lblStatus.Text = L.Get("st_ready");
            lblSeg.Text = "GhostScreen 95";

            // ---- rebuild menus ----
            cmFile.Items.Clear();
            ToolStripMenuItem miMusic = new ToolStripMenuItem(L.Get("music"));
            miMusic.CheckOnClick = true;
            miMusic.Checked = musicOn;
            miMusic.Click += delegate {
                musicOn = miMusic.Checked;
                if (musicOn) Chiptune.Start(); else Chiptune.Stop();
            };
            cmFile.Items.Add(miMusic);
            cmFile.Items.Add(new ToolStripSeparator());
            cmFile.Items.Add(L.Get("exit"), null, delegate { Close(); });

            cmLang.Items.Clear();
            for (int i = 0; i < L.Codes.Length; i++) {
                string code = L.Codes[i];
                ToolStripMenuItem it = new ToolStripMenuItem(L.NativeNames[i]);
                it.Checked = (L.Code == code);
                it.Click += delegate { SwitchLang(code); };
                cmLang.Items.Add(it);
            }

            cmHelp.Items.Clear();
            cmHelp.Items.Add(L.Get("about"), null, delegate { ShowAbout(); });

            Invalidate();
        }

        void SetFontRecursive(Control parent, Font f) {
            foreach (Control c in parent.Controls) {
                try { c.Font = f; } catch { }
                SetFontRecursive(c, f);
            }
        }

        void SwitchLang(string code) {
            if (L.Code == code) return;
            L.Set(code);
            ApplyLanguage();
            Log(L.Get("log_lang", code));
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
            ControlPaint.DrawBorder3D(g, ClientRectangle, Border3DStyle.Raised, Border3DSide.All);
            Rectangle tr = new Rectangle(2, 2, ClientSize.Width - 4, 20);
            using (LinearGradientBrush b = new LinearGradientBrush(tr, W95.Title1, W95.Title2, 90F))
                g.FillRectangle(b, tr);
            if (appIcon != null) g.DrawIcon(appIcon, new Rectangle(7, 4, 16, 16));
            g.DrawString(L.Get("title"), new Font(Font, FontStyle.Bold), Brushes.White, 27, 5);
            ControlPaint.DrawButton(g, closeRect, ButtonState.Normal);
            ControlPaint.DrawButton(g, minRect, ButtonState.Normal);
            using (Pen p = new Pen(W95.Dark)) {
                int cx = closeRect.Left + 4, cy = closeRect.Top + 3;
                g.DrawLine(p, cx, cy, cx + 9, cy + 9);
                g.DrawLine(p, cx + 9, cy, cx, cy + 9);
                g.DrawLine(p, minRect.Left + 3, minRect.Top + 10, minRect.Left + 13, minRect.Top + 10);
            }
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

        void ApplySelectedResolution() {
            Step(L.Get("st_applying", selW, selH));
            string res = ApplyResolution(selW, selH, 60);
            Log("Result: " + res);
            Thread.Sleep(3000);
            Log(L.Get("log_cur_res", CurrentResolution()));
        }

        void Finish(string msg) {
            busy = false;
            bool ok = !msg.StartsWith("ERRORE") && !msg.StartsWith("ERROR") && !msg.StartsWith("ERREUR") && !msg.StartsWith("FEHLER") && !msg.StartsWith("错误") && !msg.StartsWith("エラー");
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