using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AutoNetSwitcher
{
    public partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel pnlHeader;
        private Label lblTitle;
        private Button btnAutoToggle;
        private Button btnHeaderMin;
        private Button btnHeaderClose;

        private Panel pnlSsidCard;
        private Label lblWifiIcon;
        private Label lblSsid;
        private Button btnChangeAp;

        private Panel pnlIpCard;
        private Label lblIpHeader;
        private Button btnCopyIp;
        private Label lblCurrentIp;
        private Panel pnlNetMeta;
        private Label lblGwTitle;
        private Label lblGateway;
        private Label lblDnsTitle;
        private Label lblDns;

        private Button btnSetIp;
        private Button btnDhcp;
        private Button btnHistory;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private ToolTip toolTip;
        private System.Windows.Forms.Timer scanTimer;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (scanTimer != null)
                {
                    try
                    {
                        scanTimer.Stop();
                        scanTimer.Dispose();
                    }
                    catch { }
                    scanTimer = null;
                }

                if (trayIcon != null)
                {
                    try
                    {
                        trayIcon.Visible = false;
                        trayIcon.Icon = null;
                        trayIcon.Dispose();
                    }
                    catch { }
                    trayIcon = null;
                }

                if (trayMenu != null)
                {
                    try
                    {
                        trayMenu.Dispose();
                    }
                    catch { }
                    trayMenu = null;
                }

                if (toolTip != null)
                {
                    try
                    {
                        toolTip.Dispose();
                    }
                    catch { }
                    toolTip = null;
                }

                if (components != null)
                {
                    try
                    {
                        components.Dispose();
                    }
                    catch { }
                    components = null;
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.Text = "AutoNet - Wi-Fi 자동 고정 IP 전환기";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(320, 316);
            this.BackColor = Color.FromArgb(11, 19, 43);
            this.ForeColor = Color.FromArgb(248, 250, 252);
            this.ShowInTaskbar = true;
            this.TopMost = true;
            this.DoubleBuffered = true;

            this.toolTip = new ToolTip();
            this.Paint += new PaintEventHandler(this.MainForm_Paint);

            // ==================== 1. 헤더 패널 ====================
            this.pnlHeader = new Panel();
            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Height = 38;
            this.pnlHeader.BackColor = Color.FromArgb(11, 19, 43);
            this.pnlHeader.MouseDown += new MouseEventHandler(this.DragWindow);
            this.pnlHeader.Paint += new PaintEventHandler(this.PnlHeader_Paint);

            this.lblTitle = new Label();
            this.lblTitle.Text = "AutoNet";
            this.lblTitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.FromArgb(241, 245, 249);
            this.lblTitle.Location = new Point(12, 9);
            this.lblTitle.AutoSize = true;
            this.lblTitle.MouseDown += new MouseEventHandler(this.DragWindow);

            this.btnAutoToggle = new Button();
            this.btnAutoToggle.Text = " AUTO";
            this.btnAutoToggle.Image = UiIcons.Get("power", 11, Color.FromArgb(52, 211, 153));
            this.btnAutoToggle.ImageAlign = ContentAlignment.MiddleLeft;
            this.btnAutoToggle.TextImageRelation = TextImageRelation.ImageBeforeText;
            this.btnAutoToggle.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            this.btnAutoToggle.ForeColor = Color.FromArgb(52, 211, 153);
            this.btnAutoToggle.BackColor = Color.FromArgb(6, 78, 59);
            this.btnAutoToggle.FlatStyle = FlatStyle.Flat;
            this.btnAutoToggle.Size = new Size(68, 22);
            this.btnAutoToggle.Location = new Point(82, 8);
            this.btnAutoToggle.Cursor = Cursors.Hand;
            this.btnAutoToggle.FlatAppearance.BorderColor = Color.FromArgb(16, 185, 129);
            this.btnAutoToggle.FlatAppearance.BorderSize = 1;
            this.toolTip.SetToolTip(this.btnAutoToggle, "AP 변경 시 고정 IP 자동 전환 활성화/비활성화");
            this.btnAutoToggle.Click += new EventHandler(this.BtnAutoToggle_Click);

            this.btnHeaderMin = new Button();
            this.btnHeaderMin.Image = UiIcons.Get("minus", 13, Color.FromArgb(148, 163, 184));
            this.btnHeaderMin.ImageAlign = ContentAlignment.MiddleCenter;
            this.btnHeaderMin.BackColor = Color.Transparent;
            this.btnHeaderMin.FlatStyle = FlatStyle.Flat;
            this.btnHeaderMin.Size = new Size(26, 24);
            this.btnHeaderMin.Location = new Point(256, 7);
            this.btnHeaderMin.Cursor = Cursors.Hand;
            this.btnHeaderMin.FlatAppearance.BorderSize = 0;
            this.toolTip.SetToolTip(this.btnHeaderMin, "트레이로 최소화 (백그라운드 동작 유지)");
            this.btnHeaderMin.MouseEnter += (s, e) => { this.btnHeaderMin.Image = UiIcons.Get("minus", 13, Color.FromArgb(248, 250, 252)); };
            this.btnHeaderMin.MouseLeave += (s, e) => { this.btnHeaderMin.Image = UiIcons.Get("minus", 13, Color.FromArgb(148, 163, 184)); };
            this.btnHeaderMin.Click += (s, e) => { this.MinimizeToTray(); };

            this.btnHeaderClose = new Button();
            this.btnHeaderClose.Image = UiIcons.Get("x", 13, Color.FromArgb(148, 163, 184));
            this.btnHeaderClose.ImageAlign = ContentAlignment.MiddleCenter;
            this.btnHeaderClose.BackColor = Color.Transparent;
            this.btnHeaderClose.FlatStyle = FlatStyle.Flat;
            this.btnHeaderClose.Size = new Size(26, 24);
            this.btnHeaderClose.Location = new Point(284, 7);
            this.btnHeaderClose.Cursor = Cursors.Hand;
            this.btnHeaderClose.FlatAppearance.BorderSize = 0;
            this.toolTip.SetToolTip(this.btnHeaderClose, "트레이로 숨기기 (백그라운드 실행 유지)");
            this.btnHeaderClose.MouseEnter += (s, e) => { this.btnHeaderClose.Image = UiIcons.Get("x", 13, Color.FromArgb(244, 63, 94)); };
            this.btnHeaderClose.MouseLeave += (s, e) => { this.btnHeaderClose.Image = UiIcons.Get("x", 13, Color.FromArgb(148, 163, 184)); };
            this.btnHeaderClose.Click += (s, e) => { this.MinimizeToTray(); };

            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.btnAutoToggle);
            this.pnlHeader.Controls.Add(this.btnHeaderMin);
            this.pnlHeader.Controls.Add(this.btnHeaderClose);

            // ==================== 2. SSID 카드 ====================
            this.pnlSsidCard = new Panel();
            this.pnlSsidCard.Location = new Point(12, 46);
            this.pnlSsidCard.Size = new Size(296, 40);
            this.pnlSsidCard.BackColor = Color.FromArgb(15, 23, 42);
            this.pnlSsidCard.Paint += new PaintEventHandler((s, e) => { DrawCardBorder(e.Graphics, this.pnlSsidCard.ClientRectangle); });

            this.lblWifiIcon = new Label();
            this.lblWifiIcon.Text = "";
            this.lblWifiIcon.Image = UiIcons.Get("wifi", 18, Color.FromArgb(34, 211, 238));
            this.lblWifiIcon.ImageAlign = ContentAlignment.MiddleCenter;
            this.lblWifiIcon.Location = new Point(8, 10);
            this.lblWifiIcon.Size = new Size(22, 20);

            this.lblSsid = new Label();
            this.lblSsid.Text = "확인 중...";
            this.lblSsid.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            this.lblSsid.ForeColor = Color.FromArgb(248, 250, 252);
            this.lblSsid.Location = new Point(32, 10);
            this.lblSsid.Size = new Size(198, 20);
            this.lblSsid.AutoEllipsis = true;

            this.btnChangeAp = new Button();
            this.btnChangeAp.Text = "변경 ";
            this.btnChangeAp.Image = UiIcons.Get("chevron-down", 11, Color.FromArgb(56, 189, 248));
            this.btnChangeAp.ImageAlign = ContentAlignment.MiddleRight;
            this.btnChangeAp.TextImageRelation = TextImageRelation.TextBeforeImage;
            this.btnChangeAp.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            this.btnChangeAp.ForeColor = Color.FromArgb(56, 189, 248);
            this.btnChangeAp.BackColor = Color.Transparent;
            this.btnChangeAp.FlatStyle = FlatStyle.Flat;
            this.btnChangeAp.Size = new Size(58, 26);
            this.btnChangeAp.Location = new Point(234, 7);
            this.btnChangeAp.Cursor = Cursors.Hand;
            this.btnChangeAp.FlatAppearance.BorderSize = 0;
            this.toolTip.SetToolTip(this.btnChangeAp, "주변 Wi-Fi AP 선택 및 새로고침");
            this.btnChangeAp.Click += new EventHandler((s, e) => { this.ShowApPopupMenu(this.btnChangeAp); });

            this.pnlSsidCard.Controls.Add(this.lblWifiIcon);
            this.pnlSsidCard.Controls.Add(this.lblSsid);
            this.pnlSsidCard.Controls.Add(this.btnChangeAp);

            // ==================== 3. IP 정보 카드 ====================
            this.pnlIpCard = new Panel();
            this.pnlIpCard.Location = new Point(12, 94);
            this.pnlIpCard.Size = new Size(296, 126);
            this.pnlIpCard.BackColor = Color.FromArgb(15, 23, 42);
            this.pnlIpCard.Paint += new PaintEventHandler((s, e) => { DrawCardBorder(e.Graphics, this.pnlIpCard.ClientRectangle); });

            this.lblIpHeader = new Label();
            this.lblIpHeader.Text = "할당된 IPv4";
            this.lblIpHeader.Image = UiIcons.Get("network", 14, Color.FromArgb(148, 163, 184));
            this.lblIpHeader.ImageAlign = ContentAlignment.MiddleLeft;
            this.lblIpHeader.Padding = new Padding(18, 0, 0, 0);
            this.lblIpHeader.Font = new Font("Segoe UI", 8f);
            this.lblIpHeader.ForeColor = Color.FromArgb(148, 163, 184);
            this.lblIpHeader.Location = new Point(10, 8);
            this.lblIpHeader.AutoSize = true;

            this.btnCopyIp = new Button();
            this.btnCopyIp.Text = "";
            this.btnCopyIp.Image = UiIcons.Get("copy", 13, Color.FromArgb(148, 163, 184));
            this.btnCopyIp.ImageAlign = ContentAlignment.MiddleCenter;
            this.btnCopyIp.BackColor = Color.Transparent;
            this.btnCopyIp.FlatStyle = FlatStyle.Flat;
            this.btnCopyIp.Size = new Size(24, 22);
            this.btnCopyIp.Location = new Point(264, 5);
            this.btnCopyIp.Cursor = Cursors.Hand;
            this.btnCopyIp.FlatAppearance.BorderSize = 0;
            this.toolTip.SetToolTip(this.btnCopyIp, "현재 IPv4 주소 클립보드에 복사");
            this.btnCopyIp.MouseEnter += (s, e) => { this.btnCopyIp.Image = UiIcons.Get("copy", 13, Color.FromArgb(56, 189, 248)); };
            this.btnCopyIp.MouseLeave += (s, e) => { this.btnCopyIp.Image = UiIcons.Get("copy", 13, Color.FromArgb(148, 163, 184)); };
            this.btnCopyIp.Click += new EventHandler(this.BtnCopyIp_Click);

            this.lblCurrentIp = new Label();
            this.lblCurrentIp.Text = "0.0.0.0";
            this.lblCurrentIp.Font = new Font("Consolas", 16f, FontStyle.Bold);
            this.lblCurrentIp.ForeColor = Color.FromArgb(34, 211, 238);
            this.lblCurrentIp.Location = new Point(6, 31);
            this.lblCurrentIp.Size = new Size(284, 34);
            this.lblCurrentIp.TextAlign = ContentAlignment.MiddleCenter;
            this.lblCurrentIp.AutoEllipsis = false;

            this.pnlNetMeta = new Panel();
            this.pnlNetMeta.Location = new Point(8, 68);
            this.pnlNetMeta.Size = new Size(280, 52);
            this.pnlNetMeta.BackColor = Color.FromArgb(2, 6, 23);
            this.pnlNetMeta.Paint += new PaintEventHandler((s, e) => { DrawCardBorder(e.Graphics, this.pnlNetMeta.ClientRectangle); });

            this.lblGwTitle = new Label();
            this.lblGwTitle.Text = "기본 GW";
            this.lblGwTitle.Font = new Font("Segoe UI", 7.5f);
            this.lblGwTitle.ForeColor = Color.FromArgb(148, 163, 184);
            this.lblGwTitle.Location = new Point(8, 6);
            this.lblGwTitle.AutoSize = true;

            this.lblGateway = new Label();
            this.lblGateway.Text = "-";
            this.lblGateway.Font = new Font("Consolas", 9f, FontStyle.Bold);
            this.lblGateway.ForeColor = Color.FromArgb(52, 211, 153);
            this.lblGateway.Location = new Point(62, 5);
            this.lblGateway.AutoSize = true;

            this.lblDnsTitle = new Label();
            this.lblDnsTitle.Text = "기본 DNS";
            this.lblDnsTitle.Font = new Font("Segoe UI", 7.5f);
            this.lblDnsTitle.ForeColor = Color.FromArgb(148, 163, 184);
            this.lblDnsTitle.Location = new Point(8, 30);
            this.lblDnsTitle.AutoSize = true;

            this.lblDns = new Label();
            this.lblDns.Text = "-";
            this.lblDns.Font = new Font("Consolas", 8.5f, FontStyle.Bold);
            this.lblDns.ForeColor = Color.FromArgb(165, 180, 252);
            this.lblDns.Location = new Point(62, 29);
            this.lblDns.AutoSize = true;

            this.pnlNetMeta.Controls.Add(this.lblGwTitle);
            this.pnlNetMeta.Controls.Add(this.lblGateway);
            this.pnlNetMeta.Controls.Add(this.lblDnsTitle);
            this.pnlNetMeta.Controls.Add(this.lblDns);

            this.pnlIpCard.Controls.Add(this.lblIpHeader);
            this.pnlIpCard.Controls.Add(this.btnCopyIp);
            this.pnlIpCard.Controls.Add(this.lblCurrentIp);
            this.pnlIpCard.Controls.Add(this.pnlNetMeta);

            // ==================== 4. 핵심 동작 버튼 ====================
            this.btnSetIp = new Button();
            this.btnSetIp.Text = " 고정 IP 설정";
            this.btnSetIp.Image = UiIcons.Get("sliders", 14, Color.FromArgb(15, 23, 42));
            this.btnSetIp.ImageAlign = ContentAlignment.MiddleCenter;
            this.btnSetIp.TextImageRelation = TextImageRelation.ImageBeforeText;
            this.btnSetIp.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            this.btnSetIp.ForeColor = Color.FromArgb(15, 23, 42);
            this.btnSetIp.BackColor = Color.FromArgb(6, 182, 212);
            this.btnSetIp.FlatStyle = FlatStyle.Flat;
            this.btnSetIp.Size = new Size(145, 36);
            this.btnSetIp.Location = new Point(12, 230);
            this.btnSetIp.Cursor = Cursors.Hand;
            this.btnSetIp.FlatAppearance.BorderSize = 0;
            this.toolTip.SetToolTip(this.btnSetIp, "현재 Wi-Fi에 고정 IP / 게이트웨이 / DNS 지정");
            this.btnSetIp.Click += new EventHandler((s, e) => { this.ShowSetIpDialog(); });

            this.btnDhcp = new Button();
            this.btnDhcp.Text = " DHCP 자동";
            this.btnDhcp.Image = UiIcons.Get("refresh-cw", 13, Color.FromArgb(203, 213, 225));
            this.btnDhcp.ImageAlign = ContentAlignment.MiddleCenter;
            this.btnDhcp.TextImageRelation = TextImageRelation.ImageBeforeText;
            this.btnDhcp.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            this.btnDhcp.ForeColor = Color.FromArgb(203, 213, 225);
            this.btnDhcp.BackColor = Color.FromArgb(30, 41, 59);
            this.btnDhcp.FlatStyle = FlatStyle.Flat;
            this.btnDhcp.Size = new Size(145, 36);
            this.btnDhcp.Location = new Point(163, 230);
            this.btnDhcp.Cursor = Cursors.Hand;
            this.btnDhcp.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            this.btnDhcp.FlatAppearance.BorderSize = 1;
            this.toolTip.SetToolTip(this.btnDhcp, "현재 Wi-Fi를 공유기 자동 할당(DHCP)으로 전환");
            this.btnDhcp.MouseEnter += (s, e) => { this.btnDhcp.BackColor = Color.FromArgb(51, 65, 85); };
            this.btnDhcp.MouseLeave += (s, e) => { this.btnDhcp.BackColor = Color.FromArgb(30, 41, 59); };
            this.btnDhcp.Click += new EventHandler((s, e) => { this.ApplyDhcp(); });

            this.btnHistory = new Button();
            this.btnHistory.Text = " 저장된 AP 프로필 관리 (0개)";
            this.btnHistory.Image = UiIcons.Get("list", 13, Color.FromArgb(148, 163, 184));
            this.btnHistory.ImageAlign = ContentAlignment.MiddleCenter;
            this.btnHistory.TextImageRelation = TextImageRelation.ImageBeforeText;
            this.btnHistory.Font = new Font("Segoe UI", 8f);
            this.btnHistory.ForeColor = Color.FromArgb(203, 213, 225);
            this.btnHistory.BackColor = Color.FromArgb(15, 23, 42);
            this.btnHistory.FlatStyle = FlatStyle.Flat;
            this.btnHistory.Size = new Size(296, 30);
            this.btnHistory.Location = new Point(12, 274);
            this.btnHistory.Cursor = Cursors.Hand;
            this.btnHistory.FlatAppearance.BorderColor = Color.FromArgb(30, 41, 59);
            this.btnHistory.FlatAppearance.BorderSize = 1;
            this.btnHistory.MouseEnter += (s, e) => { this.btnHistory.BackColor = Color.FromArgb(30, 41, 59); };
            this.btnHistory.MouseLeave += (s, e) => { this.btnHistory.BackColor = Color.FromArgb(15, 23, 42); };
            this.btnHistory.Click += new EventHandler((s, e) => { this.ShowSavedProfilesDialog(); });

            // ==================== 5. 트레이 아이콘 및 메뉴 ====================
            this.trayMenu = new ContextMenuStrip();
            this.trayMenu.Renderer = new DarkMenuRenderer();
            this.trayMenu.ShowImageMargin = false;
            this.trayMenu.BackColor = Color.FromArgb(15, 23, 42);
            this.trayMenu.ForeColor = Color.FromArgb(241, 245, 249);
            this.trayMenu.Font = new Font("Segoe UI", 9f);

            this.trayMenu.Items.Add("AutoNet 열기", null, (s, e) => { this.RestoreFromTray(); });
            this.trayMenu.Items.Add("상태 새로고침", null, (s, e) => { this.RefreshNetworkStatus(); });
            this.trayMenu.Items.Add("저장된 AP 목록 관리", null, (s, e) => { this.RestoreFromTray(); this.ShowSavedProfilesDialog(); });
            this.trayMenu.Items.Add("-");
            ToolStripMenuItem mnuStartup = new ToolStripMenuItem("윈도우 시작 시 자동 실행");
            mnuStartup.Click += (s, e) =>
            {
                bool newState = !StartupHelper.IsStartupRegistered();
                StartupHelper.SetStartup(newState);
                mnuStartup.Checked = newState;
                this.Log(newState ? "시작 프로그램 등록 활성화" : "시작 프로그램 등록 해제");
            };
            this.trayMenu.Opening += (s, e) =>
            {
                mnuStartup.Checked = StartupHelper.IsStartupRegistered();
            };
            this.trayMenu.Items.Add(mnuStartup);
            this.trayMenu.Items.Add("-");
            this.trayMenu.Items.Add("종료", null, (s, e) => { this.allowExit = true; this.Close(); });

            Icon appIcon = null;
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (System.IO.File.Exists(icoPath))
                {
                    appIcon = new Icon(icoPath);
                }
                else
                {
                    appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            }
            catch
            {
                try { appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            }

            Icon trayRunningIcon = null;
            try
            {
                string trayPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tray_running.ico");
                if (System.IO.File.Exists(trayPath))
                {
                    trayRunningIcon = new Icon(trayPath);
                }
            }
            catch { }

            if (appIcon != null)
            {
                this.Icon = appIcon;
            }

            this.trayIcon = new NotifyIcon();
            this.trayIcon.Text = "AutoNet Switcher (Wi-Fi 자동 전환 실행 중)";
            this.trayIcon.Icon = trayRunningIcon ?? appIcon ?? SystemIcons.Application;
            this.trayIcon.ContextMenuStrip = this.trayMenu;
            this.trayIcon.Visible = true;
            this.trayIcon.DoubleClick += new EventHandler((s, e) => { this.RestoreFromTray(); });

            // 폼 컨트롤 추가
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlSsidCard);
            this.Controls.Add(this.pnlIpCard);
            this.Controls.Add(this.btnSetIp);
            this.Controls.Add(this.btnDhcp);
            this.Controls.Add(this.btnHistory);
        }

        private static void DrawCardBorder(Graphics g, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(Color.FromArgb(30, 41, 59), 1f))
            {
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            }
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(Color.FromArgb(30, 41, 59), 1.5f))
            {
                e.Graphics.DrawRectangle(pen, 1, 1, this.Width - 2, this.Height - 2);
            }
        }

        private void PnlHeader_Paint(object sender, PaintEventArgs e)
        {
            using (Pen p = new Pen(Color.FromArgb(30, 41, 59), 1f))
            {
                e.Graphics.DrawLine(p, 0, this.pnlHeader.Height - 1, this.pnlHeader.Width, this.pnlHeader.Height - 1);
            }
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(this.Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HTCAPTION, 0);
            }
        }

        public void MinimizeToTray()
        {
            this.ShowInTaskbar = false;
            this.Hide();
            if (this.trayIcon != null)
            {
                this.trayIcon.ShowBalloonTip(
                    1200,
                    "AutoNet",
                    "트레이로 최소화되어 백그라운드에서 계속 실행 중입니다.",
                    ToolTipIcon.Info);
            }
        }

        public void RestoreFromTray()
        {
            this.ShowInTaskbar = true;
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
        }
    }
}
