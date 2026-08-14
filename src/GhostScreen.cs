using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Security.Principal;

namespace GhostScreen {
    static class Program {
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

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
            fnt = new Font("MS Sans Serif", 8.25F);
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
            ok.Text = "OK";
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
        Button btnFile, btnHelp;
        ContextMenuStrip cmFile, cmHelp;
        PictureBox pbHead;
        Rectangle minRect, closeRect;
        bool dragging;
        Point dragOff;
        Icon appIcon;
        Bitmap banner, logo;

        // ---------- worker ----------
        Thread worker;
        volatile bool busy;
        string LogFile;
        readonly object logLock = new object();

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

            Text = "GhostScreen 95";
            FormBorderStyle = FormBorderStyle.None;
            BackColor = W95.Face;
            Font = new Font("MS Sans Serif", 8F);
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

            btnFile = new Button();
            btnFile.Text = "&File";
            btnFile.FlatStyle = FlatStyle.Flat;
            btnFile.BackColor = W95.Face;
            btnFile.ForeColor = W95.Dark;
            btnFile.Font = Font;
            btnFile.Location = new Point(4, 2);
            btnFile.Size = new Size(36, 18);
            btnFile.Click += delegate {
                cmFile.Show(menuBar, new Point(btnFile.Left, btnFile.Bottom + 2));
            };

            btnHelp = new Button();
            btnHelp.Text = "&?";
            btnHelp.FlatStyle = FlatStyle.Flat;
            btnHelp.BackColor = W95.Face;
            btnHelp.ForeColor = W95.Dark;
            btnHelp.Font = Font;
            btnHelp.Location = new Point(42, 2);
            btnHelp.Size = new Size(22, 18);
            btnHelp.Click += delegate {
                cmHelp.Show(menuBar, new Point(btnHelp.Left, btnHelp.Bottom + 2));
            };

            cmFile = new ContextMenuStrip();
            cmFile.RenderMode = ToolStripRenderMode.Professional;
            cmFile.Renderer = new W95MenuRenderer();
            cmFile.Font = Font;
            cmFile.Items.Add("Esci", null, delegate { Close(); });

            cmHelp = new ContextMenuStrip();
            cmHelp.RenderMode = ToolStripRenderMode.Professional;
            cmHelp.Renderer = new W95MenuRenderer();
            cmHelp.Font = Font;
            cmHelp.Items.Add("Informazioni su GhostScreen", null, delegate { ShowAbout(); });

            menuBar.Controls.Add(btnFile);
            menuBar.Controls.Add(btnHelp);

            // ---- resolution group ----
            GroupBox gRes = new GroupBox();
            gRes.Text = "Risoluzione di destinazione";
            gRes.Font = Font;
            gRes.ForeColor = W95.Dark;
            gRes.BackColor = W95.Face;
            gRes.Location = new Point(12, 192);
            gRes.Size = new Size(352, 166);

            rbQ = NewRadio("2560x1440  (consigliata)", 16, 28); rbQ.Checked = true;
            rbF = NewRadio("1920x1080", 16, 56);
            rbH = NewRadio("1366x768", 16, 84);
            rbV = NewRadio("1280x720", 16, 112);
            Label note = new Label();
            note.Text = "In modalita' headless la risoluzione\nviene impostata istantaneamente.";
            note.Font = Font;
            note.ForeColor = Color.FromArgb(80, 80, 80);
            note.BackColor = W95.Face;
            note.Location = new Point(16, 140);
            note.AutoSize = true;
            gRes.Controls.Add(rbQ); gRes.Controls.Add(rbF); gRes.Controls.Add(rbH); gRes.Controls.Add(rbV); gRes.Controls.Add(note);

            // ---- actions ----
            btnInstall = NewButton("Installa e Applica", 380, 210, 124, 28);
            btnInstall.DefaultButton = true;
            btnInstall.Click += delegate { StartInstall(); };
            btnApply = NewButton("Solo risoluzione", 380, 246, 124, 28);
            btnApply.Click += delegate { StartApply(); };
            btnRestart = NewButton("Riavvia display", 380, 282, 124, 28);
            btnRestart.Click += delegate { StartRestart(); };
            btnAbout = NewButton("About...", 380, 318, 124, 28);
            btnAbout.Click += delegate { ShowAbout(); };

            // ---- log ----
            GroupBox gLog = new GroupBox();
            gLog.Text = "Log di avanzamento";
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
            lblStatus.Text = "Pronto";
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

            Log("=== GhostScreen 95 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===");
            Log("Admin: " + IsAdmin());
            if (!IsAdmin()) Log("!! NON eseguito come amministratore: i passi driver falliranno.");
            Log("Risoluzione attuale: " + CurrentResolution());
            if (IsVddInstalled()) {
                Log("Driver Virtual Display: gia' installato. Applico la risoluzione selezionata...");
                StartApply();
            } else {
                Log("Driver Virtual Display NON installato: avvio installazione completa...");
                StartInstall();
            }
        }

        void ShowAbout() {
            W95MsgBox.Show(
                "GhostScreen 95 v1.0.0\nVirtual Display Suite per PC headless.\n\n" +
                "Installa un display virtuale e sblocca\nrisoluzioni reali fino a 4K senza\nmonitor fisico.\n\n" +
                "Realizzato da Luigi Strazzullo\nper Culture Digitali Srl\n\n" +
                "Driver: Virtual-Display-Driver (MikeTheTech)\nMIT License - GhostScreen Project",
                "Informazioni su GhostScreen", appIcon, false);
        }

        RadioButton NewRadio(string text, int x, int y) {
            RadioButton r = new RadioButton();
            r.Text = text;
            r.Font = Font;
            r.ForeColor = W95.Dark;
            r.BackColor = W95.Face;
            r.Location = new Point(x, y);
            r.AutoSize = true;
            return r;
        }

        W95Button NewButton(string text, int x, int y, int w, int h) {
            W95Button b = new W95Button();
            b.Text = text;
            b.Font = Font;
            b.Size = new Size(w, h);
            b.Location = new Point(x, y);
            return b;
        }

        // ---------- title bar ----------
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            ControlPaint.DrawBorder3D(g, ClientRectangle, Border3DStyle.Raised, Border3DSide.All);
            Rectangle tr = new Rectangle(2, 2, ClientSize.Width - 4, 20);
            using (LinearGradientBrush b = new LinearGradientBrush(tr, W95.Title1, W95.Title2, 90F))
                g.FillRectangle(b, tr);
            if (appIcon != null) g.DrawIcon(appIcon, new Rectangle(7, 4, 16, 16));
            g.DrawString("GhostScreen 95 - Virtual Display Suite", new Font(Font, FontStyle.Bold), Brushes.White, 27, 5);
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
        protected override void OnDoubleClick(EventArgs e) {
            base.OnDoubleClick(e);
            if (dragging) WindowState = FormWindowState.Minimized;
        }

        // ---------- actions ----------
        void StartInstall() {
            if (busy) return;
            busy = true;
            SetBusyUI(true);
            Step("Installazione del driver in corso...");
            worker = new Thread(DoInstall);
            worker.IsBackground = true;
            worker.Start();
        }

        void StartApply() {
            if (busy) return;
            busy = true;
            SetBusyUI(true);
            Step("Applicazione risoluzione in corso...");
            worker = new Thread(DoApply);
            worker.IsBackground = true;
            worker.Start();
        }

        void StartRestart() {
            if (busy) return;
            busy = true;
            SetBusyUI(true);
            Step("Riavvio del display in corso...");
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
                Log("Estraggo i file del driver in " + work);
                Directory.CreateDirectory(work);
                Extract("Res.mttvdd.inf", work, "mttvdd.inf");
                Extract("Res.MttVDD.cat", work, "MttVDD.cat");
                Extract("Res.MttVDD.dll", work, "MttVDD.dll");
                Extract("Res.vdd_settings.xml", work, "vdd_settings.xml");
                Extract("Res.devcon.exe", work, "devcon.exe");
                Extract("Res.copy_settings.cmd", work, "copy_settings.cmd");
                string inf = Path.Combine(work, "mttvdd.inf");

                Step("Installazione del driver nel DriverStore...");
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
                    else Log("!! vdd_settings.xml -> DriverStore: NON presente");
                } else {
                    Log("!! cartella DriverStore non trovata");
                }

                string umdf = Path.Combine(win, "System32", "drivers", "UMDF");
                try {
                    File.Copy(Path.Combine(work, "vdd_settings.xml"), Path.Combine(umdf, "vdd_settings.xml"), true);
                    Log("vdd_settings.xml -> UMDF: OK");
                } catch (Exception ex) {
                    Log("!! vdd_settings.xml -> UMDF: " + ex.Message);
                }

                Step("Creazione del dispositivo virtuale...");
                if (DeviceMissing()) {
                    Run(Path.Combine(work, "devcon.exe"), "install \"" + inf + "\" \"Root\\MttVDD\"");
                } else {
                    Log("Device Virtual Display: gia' presente");
                }

                Thread.Sleep(4000);
                Step("Riavvio dei dispositivi display...");
                Run(pnputil, "/restart-device \"ROOT\\MttVDD\"");
                Run(pnputil, "/restart-device \"ROOT\\DISPLAY\\0000\"");
                Thread.Sleep(5000);

                Step("Attesa del display virtuale...");
                if (!WaitForDisplayReady(25)) Log("!! display non pronto dopo 25s, procedo comunque");
                ApplySelectedResolution();
                Finish("Installazione completata.");
            } catch (Exception ex) {
                Log("FATAL: " + ex);
                Finish("ERRORE: " + ex.Message);
            }
        }

        void DoApply() {
            try {
                Step("Attesa del display virtuale...");
                if (!WaitForDisplayReady(25)) Log("!! display non pronto dopo 25s, procedo comunque");
                ApplySelectedResolution();
                Finish("Risoluzione applicata.");
            } catch (Exception ex) {
                Log("FATAL: " + ex);
                Finish("ERRORE: " + ex.Message);
            }
        }

        void DoRestart() {
            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string pnputil = Path.Combine(win, "System32", "pnputil.exe");
            try {
                Step("Riavvio dei dispositivi display...");
                Run(pnputil, "/restart-device \"ROOT\\MttVDD\"");
                Run(pnputil, "/restart-device \"ROOT\\DISPLAY\\0000\"");
                Thread.Sleep(6000);
                Step("Attesa del display virtuale...");
                if (!WaitForDisplayReady(20)) Log("!! display non pronto dopo 20s");
                ApplySelectedResolution();
                Finish("Display riavviato.");
            } catch (Exception ex) {
                Log("FATAL: " + ex);
                Finish("ERRORE: " + ex.Message);
            }
        }

        void ApplySelectedResolution() {
            int x = 2560, y = 1440;
            if (rbF.Checked) { x = 1920; y = 1080; }
            else if (rbH.Checked) { x = 1366; y = 768; }
            else if (rbV.Checked) { x = 1280; y = 720; }
            Step("Applicazione della risoluzione " + x + "x" + y + "...");
            string res = ApplyResolution(x, y, 60);
            Log("Esito: " + res);
            Thread.Sleep(3000);
            Log("Risoluzione finale: " + CurrentResolution());
        }

        void Finish(string msg) {
            busy = false;
            bool ok = !msg.StartsWith("ERRORE");
            if (ok) {
                string cur = CurrentResolution();
                Log(msg + " Risoluzione attuale: " + cur);
                SetStatus(msg + "  Risoluzione: " + cur, W95.Green);
                string fmsg = msg + "\r\n\r\nRisoluzione attuale: " + cur + "\r\n\r\nLog completo: " + LogFile;
                Invoke(new Action(delegate { W95MsgBox.Show(fmsg, "GhostScreen 95", appIcon, false); }));
            } else {
                SetStatus(msg, Color.Firebrick);
                string fmsg = msg + "\r\n\r\nLog completo: " + LogFile;
                Invoke(new Action(delegate { W95MsgBox.Show(fmsg, "GhostScreen 95", appIcon, true); }));
            }
            SetBusyUI(false);
        }

        void SetBusyUI(bool b) {
            try {
                if (InvokeRequired) { Invoke(new Action<bool>(SetBusyUI), b); return; }
                btnInstall.Enabled = !b;
                btnApply.Enabled = !b;
                btnRestart.Enabled = !b;
                btnFile.Enabled = !b;
                btnHelp.Enabled = !b;
            } catch { }
        }

        void SetStatus(string text, Color c) {
            try {
                if (InvokeRequired) { Invoke(new Action<string, Color>(SetStatus), text, c); return; }
                lblStatus.Text = text;
                lblStatus.ForeColor = c;
            } catch { }
        }

        void Log(string m) {
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + m;
            if (InvokeRequired) { Invoke(new Action<string>(Log), m); return; }
            lock (logLock) {
                try { File.AppendAllText(LogFile, line + "\r\n"); } catch { }
            }
            txtLog.AppendText(line + "\r\n");
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        void Extract(string resName, string dir, string fileName) {
            string dest = Path.Combine(dir, fileName);
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName)) {
                if (s == null) throw new Exception("Risorsa incorporata mancante: " + resName);
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
                Log("!! comando fallito (" + exe + " " + args + "): " + ex.Message);
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
                        if (r == DISP_CHANGE_SUCCESSFUL) return "OK (modalita' #" + i + ", " + w + "x" + h + ")";
                        return "modo trovato ma rifiutato (codice " + r + ")";
                    }
                    i++;
                }
                if (!any && attempt == 1) {
                    Log("  enumerazione vuota: reset della configurazione display...");
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
            return "FAIL: codice " + r2 + " (0=ok, -2=non supportata, -5=non aggiornata). Riavvia il PC e riprova.";
        }

        string CurrentResolution() {
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            if (EnumDisplaySettings(null, -1, ref dm) != 0 && dm.dmPelsWidth > 0)
                return dm.dmPelsWidth + "x" + dm.dmPelsHeight + " @" + dm.dmDisplayFrequency + "Hz";
            return "n/d";
        }
    }
}