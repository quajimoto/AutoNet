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

        private const int FormWidth = 340;
        private const int FormHeight = 366;
        private bool nativeChrome;

        private Panel pnlHeader;
        private RoundedPanel pnlLogo;
        private Label lblLogo;
        private Label lblTitle;
        private Label lblSubtitle;
        private ModernButton btnAutoToggle;
        private ModernButton btnHeaderMin;
        private ModernButton btnHeaderClose;

        private RoundedPanel pnlConnCard;
        private RoundedPanel pnlWifiBadge;
        private Label lblWifiIcon;
        private Label lblSsid;
        private StatusDot dotStatus;
        private Label lblStatus;
        private ModernButton btnChangeAp;

        private Label lblIpHeader;
        private ModernButton btnCopyIp;
        private Label lblCurrentIp;
        private RoundedPanel pnlGwTile;
        private Label lblGwTitle;
        private Label lblGateway;
        private RoundedPanel pnlDnsTile;
        private Label lblDnsTitle;
        private Label lblDns;

        private ModernButton btnSetIp;
        private ModernButton btnDhcp;
        private ModernButton btnHistory;

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

        /// <summary>
        /// 테두리 없는 창에 그림자 적용
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            this.nativeChrome = Theme.ApplyWindowChrome(this);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.Text = "AutoNet - Wi-Fi 자동 고정 IP 전환기";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(FormWidth, FormHeight);
            this.BackColor = Theme.Bg;
            this.ForeColor = Theme.Text;
            this.Font = Theme.Ui(9f);
            this.ShowInTaskbar = true;
            this.TopMost = true;
            this.DoubleBuffered = true;

            this.toolTip = new ToolTip();
            this.Paint += new PaintEventHandler(this.MainForm_Paint);

            const int pad = 14;
            const int innerW = FormWidth - pad * 2; // 312

            // ==================== 1. 헤더 ====================
            this.pnlHeader = new Panel();
            this.pnlHeader.Location = new Point(0, 0);
            this.pnlHeader.Size = new Size(FormWidth, 52);
            this.pnlHeader.BackColor = Theme.Bg;
            this.pnlHeader.MouseDown += new MouseEventHandler(this.DragWindow);

            this.pnlLogo = new RoundedPanel();
            this.pnlLogo.Location = new Point(pad, 13);
            this.pnlLogo.Size = new Size(28, 28);
            this.pnlLogo.Radius = 8;
            this.pnlLogo.BackColor = Theme.AccentStrong;
            this.pnlLogo.GradientEnd = Theme.AccentBlue;
            this.pnlLogo.BorderColor = Color.Empty;

            this.lblLogo = new Label();
            this.lblLogo.Location = new Point(5, 5);
            this.lblLogo.Size = new Size(18, 18);
            this.lblLogo.BackColor = Color.Transparent;
            this.lblLogo.Image = UiIcons.Get("wifi", 16, Color.White);
            this.lblLogo.ImageAlign = ContentAlignment.MiddleCenter;
            this.lblLogo.MouseDown += new MouseEventHandler(this.DragWindow);
            this.pnlLogo.Controls.Add(this.lblLogo);
            this.pnlLogo.MouseDown += new MouseEventHandler(this.DragWindow);

            this.lblTitle = new Label();
            this.lblTitle.Text = "AutoNet";
            this.lblTitle.Font = Theme.Ui(10.5f, FontStyle.Bold);
            this.lblTitle.ForeColor = Theme.Text;
            this.lblTitle.Location = new Point(48, 9);
            this.lblTitle.AutoSize = true;
            this.lblTitle.MouseDown += new MouseEventHandler(this.DragWindow);

            this.lblSubtitle = new Label();
            this.lblSubtitle.Text = "Wi-Fi 자동 IP 전환";
            this.lblSubtitle.Font = Theme.Ui(7.5f);
            this.lblSubtitle.ForeColor = Theme.TextSubtle;
            this.lblSubtitle.Location = new Point(49, 28);
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.MouseDown += new MouseEventHandler(this.DragWindow);

            this.btnAutoToggle = new ModernButton();
            this.btnAutoToggle.Size = new Size(74, 26);
            this.btnAutoToggle.Location = new Point(FormWidth - 10 - 30 - 30 - 8 - 74, 13);
            this.btnAutoToggle.Radius = 13;
            this.btnAutoToggle.IconName = "power";
            this.btnAutoToggle.IconSize = 11;
            this.btnAutoToggle.Font = Theme.Ui(8f, FontStyle.Bold);
            this.btnAutoToggle.TabStop = false;
            this.btnAutoToggle.Click += new EventHandler(this.BtnAutoToggle_Click);
            this.ApplyAutoToggleStyle();

            this.btnHeaderMin = CreateHeaderIconButton("minus", Theme.SurfaceHover, Theme.Text);
            this.btnHeaderMin.Location = new Point(FormWidth - 10 - 30 - 30, 12);
            this.toolTip.SetToolTip(this.btnHeaderMin, "트레이로 최소화 (백그라운드 동작 유지)");
            this.btnHeaderMin.Click += (s, e) => { this.MinimizeToTray(); };

            this.btnHeaderClose = CreateHeaderIconButton("x", Theme.DangerTint, Theme.Danger);
            this.btnHeaderClose.Location = new Point(FormWidth - 10 - 30, 12);
            this.toolTip.SetToolTip(this.btnHeaderClose, "트레이로 숨기기 (백그라운드 실행 유지)");
            this.btnHeaderClose.Click += (s, e) => { this.MinimizeToTray(); };

            this.pnlHeader.Controls.Add(this.pnlLogo);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblSubtitle);
            this.pnlHeader.Controls.Add(this.btnAutoToggle);
            this.pnlHeader.Controls.Add(this.btnHeaderMin);
            this.pnlHeader.Controls.Add(this.btnHeaderClose);

            // ==================== 2. 연결 정보 카드 ====================
            this.pnlConnCard = new RoundedPanel();
            this.pnlConnCard.Location = new Point(pad, 56);
            this.pnlConnCard.Size = new Size(innerW, 200);
            this.pnlConnCard.Radius = 14;
            this.pnlConnCard.BackColor = Theme.Surface;
            this.pnlConnCard.BorderColor = Theme.Border;
            this.pnlConnCard.Paint += new PaintEventHandler(this.PnlConnCard_Paint);

            // 2-1. SSID 행
            this.pnlWifiBadge = new RoundedPanel();
            this.pnlWifiBadge.Location = new Point(14, 14);
            this.pnlWifiBadge.Size = new Size(38, 38);
            this.pnlWifiBadge.Radius = 11;
            this.pnlWifiBadge.BackColor = Theme.AccentTint;
            this.pnlWifiBadge.BorderColor = Color.Empty;

            this.lblWifiIcon = new Label();
            this.lblWifiIcon.Dock = DockStyle.Fill;
            this.lblWifiIcon.BackColor = Theme.AccentTint;
            this.lblWifiIcon.Image = UiIcons.Get("wifi", 20, Theme.Accent);
            this.lblWifiIcon.ImageAlign = ContentAlignment.MiddleCenter;
            this.pnlWifiBadge.Controls.Add(this.lblWifiIcon);

            this.lblSsid = new Label();
            this.lblSsid.Text = "확인 중...";
            this.lblSsid.Font = Theme.Ui(10f, FontStyle.Bold);
            this.lblSsid.ForeColor = Theme.Text;
            this.lblSsid.Location = new Point(60, 13);
            this.lblSsid.Size = new Size(168, 22);
            this.lblSsid.AutoEllipsis = true;

            this.dotStatus = new StatusDot();
            this.dotStatus.Location = new Point(62, 38);
            this.dotStatus.Size = new Size(10, 10);
            this.dotStatus.DotColor = Theme.TextSubtle;

            this.lblStatus = new Label();
            this.lblStatus.Text = "상태 확인 중";
            this.lblStatus.Font = Theme.Ui(7.5f);
            this.lblStatus.ForeColor = Theme.TextMuted;
            this.lblStatus.Location = new Point(75, 35);
            this.lblStatus.Size = new Size(152, 16);
            this.lblStatus.AutoEllipsis = true;

            this.btnChangeAp = new ModernButton();
            this.btnChangeAp.Text = "변경";
            this.btnChangeAp.Font = Theme.Ui(8.5f, FontStyle.Bold);
            this.btnChangeAp.ForeColor = Theme.Accent;
            this.btnChangeAp.BackColor = Theme.SurfaceHover;
            this.btnChangeAp.HoverBackColor = Theme.SurfacePressed;
            this.btnChangeAp.Radius = 8;
            this.btnChangeAp.TextAlign = ContentAlignment.MiddleLeft;
            this.btnChangeAp.ContentPadding = 11;
            this.btnChangeAp.TrailingIcon = "chevron-down";
            this.btnChangeAp.Size = new Size(66, 28);
            this.btnChangeAp.Location = new Point(innerW - 14 - 66, 19);
            this.toolTip.SetToolTip(this.btnChangeAp, "주변 Wi-Fi AP 선택 및 새로고침");
            this.btnChangeAp.Click += new EventHandler((s, e) => { this.ShowApPopupMenu(this.btnChangeAp); });

            // 2-2. IPv4 영역 (구분선은 PnlConnCard_Paint 에서 y=66 에 그림)
            this.lblIpHeader = new Label();
            this.lblIpHeader.Text = "할당된 IPv4";
            this.lblIpHeader.Font = Theme.Ui(8f);
            this.lblIpHeader.ForeColor = Theme.TextMuted;
            this.lblIpHeader.Location = new Point(13, 78);
            this.lblIpHeader.AutoSize = true;

            this.btnCopyIp = new ModernButton();
            this.btnCopyIp.IconName = "copy";
            this.btnCopyIp.IconSize = 13;
            this.btnCopyIp.IconColor = Theme.TextMuted;
            this.btnCopyIp.IconHoverColor = Theme.Accent;
            this.btnCopyIp.BackColor = Color.Transparent;
            this.btnCopyIp.HoverBackColor = Theme.SurfaceHover;
            this.btnCopyIp.Radius = 7;
            this.btnCopyIp.Size = new Size(28, 26);
            this.btnCopyIp.Location = new Point(innerW - 12 - 28, 80);
            this.btnCopyIp.TabStop = false;
            this.toolTip.SetToolTip(this.btnCopyIp, "현재 IPv4 주소 클립보드에 복사");
            this.btnCopyIp.Click += new EventHandler(this.BtnCopyIp_Click);

            this.lblCurrentIp = new Label();
            this.lblCurrentIp.Text = "0.0.0.0";
            this.lblCurrentIp.Font = Theme.Mono(19f, FontStyle.Bold);
            this.lblCurrentIp.ForeColor = Theme.Accent;
            this.lblCurrentIp.Location = new Point(10, 96);
            this.lblCurrentIp.Size = new Size(innerW - 60, 36);
            this.lblCurrentIp.TextAlign = ContentAlignment.MiddleLeft;
            this.lblCurrentIp.AutoEllipsis = false;

            // 2-3. 게이트웨이 / DNS 타일
            int tileW = (innerW - 28 - 8) / 2;
            this.pnlGwTile = CreateMetaTile(new Point(14, 138), tileW, "게이트웨이", out this.lblGwTitle, out this.lblGateway, Theme.Success);
            this.pnlDnsTile = CreateMetaTile(new Point(14 + tileW + 8, 138), tileW, "DNS", out this.lblDnsTitle, out this.lblDns, Theme.Indigo);

            this.pnlConnCard.Controls.Add(this.pnlWifiBadge);
            this.pnlConnCard.Controls.Add(this.lblSsid);
            this.pnlConnCard.Controls.Add(this.dotStatus);
            this.pnlConnCard.Controls.Add(this.lblStatus);
            this.pnlConnCard.Controls.Add(this.btnChangeAp);
            this.pnlConnCard.Controls.Add(this.lblIpHeader);
            this.pnlConnCard.Controls.Add(this.btnCopyIp);
            this.pnlConnCard.Controls.Add(this.lblCurrentIp);
            this.pnlConnCard.Controls.Add(this.pnlGwTile);
            this.pnlConnCard.Controls.Add(this.pnlDnsTile);

            // ==================== 3. 동작 버튼 ====================
            int halfW = (innerW - 8) / 2;

            this.btnSetIp = new ModernButton();
            this.btnSetIp.Text = "고정 IP 설정";
            this.btnSetIp.IconName = "sliders";
            this.btnSetIp.IconSize = 14;
            this.btnSetIp.IconColor = Color.White;
            this.btnSetIp.Font = Theme.Ui(9f, FontStyle.Bold);
            this.btnSetIp.ForeColor = Color.White;
            this.btnSetIp.BackColor = Theme.AccentStrong;
            this.btnSetIp.GradientEnd = Theme.AccentBlue;
            this.btnSetIp.Radius = 10;
            this.btnSetIp.Size = new Size(halfW, 42);
            this.btnSetIp.Location = new Point(pad, 266);
            this.toolTip.SetToolTip(this.btnSetIp, "현재 Wi-Fi에 고정 IP / 게이트웨이 / DNS 지정");
            this.btnSetIp.Click += new EventHandler((s, e) => { this.ShowSetIpDialog(); });

            this.btnDhcp = new ModernButton();
            this.btnDhcp.Text = "DHCP 자동";
            this.btnDhcp.IconName = "refresh-cw";
            this.btnDhcp.IconSize = 13;
            this.btnDhcp.IconColor = Theme.TextSecondary;
            this.btnDhcp.Font = Theme.Ui(9f, FontStyle.Bold);
            this.btnDhcp.ForeColor = Theme.TextSecondary;
            this.btnDhcp.BackColor = Theme.Surface;
            this.btnDhcp.HoverBackColor = Theme.SurfaceHover;
            this.btnDhcp.BorderColor = Theme.Border;
            this.btnDhcp.Radius = 10;
            this.btnDhcp.Size = new Size(halfW, 42);
            this.btnDhcp.Location = new Point(pad + halfW + 8, 266);
            this.toolTip.SetToolTip(this.btnDhcp, "현재 Wi-Fi를 공유기 자동 할당(DHCP)으로 전환");
            this.btnDhcp.Click += new EventHandler((s, e) => { this.ApplyDhcp(); });

            this.btnHistory = new ModernButton();
            this.btnHistory.Text = "저장된 AP 프로필 관리 (0개)";
            this.btnHistory.IconName = "list";
            this.btnHistory.IconSize = 14;
            this.btnHistory.IconColor = Theme.TextMuted;
            this.btnHistory.Font = Theme.Ui(8.5f);
            this.btnHistory.ForeColor = Theme.TextSecondary;
            this.btnHistory.BackColor = Color.Transparent;
            this.btnHistory.HoverBackColor = Theme.Surface;
            this.btnHistory.Radius = 10;
            this.btnHistory.TextAlign = ContentAlignment.MiddleLeft;
            this.btnHistory.ContentPadding = 14;
            this.btnHistory.TrailingIcon = "chevron-right";
            this.btnHistory.Size = new Size(innerW, 38);
            this.btnHistory.Location = new Point(pad, 316);
            this.toolTip.SetToolTip(this.btnHistory, "기억된 AP 목록 확인 · 이름 변경 · 삭제");
            this.btnHistory.Click += new EventHandler((s, e) => { this.ShowSavedProfilesDialog(); });

            // ==================== 4. 트레이 아이콘 및 메뉴 ====================
            this.trayMenu = new ContextMenuStrip();
            this.trayMenu.Renderer = new DarkMenuRenderer();
            this.trayMenu.ShowImageMargin = true; // 체크 표시(자동 실행 여부)와 아이콘이 보이도록 여백 사용
            this.trayMenu.BackColor = Theme.Surface;
            this.trayMenu.ForeColor = Theme.Text;
            this.trayMenu.Font = Theme.Ui(9f);

            this.trayMenu.Items.Add("AutoNet 열기", UiIcons.Get("wifi", 14, Theme.Accent), (s, e) => { this.RestoreFromTray(); });
            this.trayMenu.Items.Add("상태 새로고침", UiIcons.Get("refresh-cw", 14, Theme.TextMuted), (s, e) => { this.RefreshNetworkStatus(); });
            this.trayMenu.Items.Add("저장된 AP 목록 관리", UiIcons.Get("list", 14, Theme.TextMuted), (s, e) => { this.RestoreFromTray(); this.ShowSavedProfilesDialog(); });
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
            this.trayMenu.Items.Add("종료", UiIcons.Get("power", 14, Theme.Danger), (s, e) => { this.allowExit = true; this.Close(); });

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
            this.Controls.Add(this.pnlConnCard);
            this.Controls.Add(this.btnSetIp);
            this.Controls.Add(this.btnDhcp);
            this.Controls.Add(this.btnHistory);
        }

        private ModernButton CreateHeaderIconButton(string icon, Color hoverBack, Color hoverIcon)
        {
            ModernButton b = new ModernButton();
            b.IconName = icon;
            b.IconSize = 13;
            b.IconColor = Theme.TextMuted;
            b.IconHoverColor = hoverIcon;
            b.BackColor = Color.Transparent;
            b.HoverBackColor = hoverBack;
            b.Radius = 8;
            b.Size = new Size(30, 28);
            b.TabStop = false;
            return b;
        }

        private static RoundedPanel CreateMetaTile(Point location, int width, string title, out Label titleLabel, out Label valueLabel, Color accent)
        {
            RoundedPanel tile = new RoundedPanel();
            tile.Location = location;
            tile.Size = new Size(width, 48);
            tile.Radius = 10;
            tile.BackColor = Theme.SurfaceInset;
            tile.BorderColor = Color.Empty;

            titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.Font = Theme.Ui(7.5f);
            titleLabel.ForeColor = Theme.TextSubtle;
            titleLabel.Location = new Point(9, 6);
            titleLabel.AutoSize = true;

            valueLabel = new Label();
            valueLabel.Text = "-";
            valueLabel.Font = Theme.Mono(9f, FontStyle.Bold);
            valueLabel.ForeColor = accent;
            valueLabel.Location = new Point(8, 24);
            valueLabel.Size = new Size(width - 14, 18);
            valueLabel.AutoEllipsis = true;

            tile.Controls.Add(titleLabel);
            tile.Controls.Add(valueLabel);
            return tile;
        }

        /// <summary>
        /// AUTO / MANUAL 모드에 맞게 토글 버튼 모양을 갱신
        /// </summary>
        private void ApplyAutoToggleStyle()
        {
            if (this.btnAutoToggle == null) return;
            if (this.isAutoEnabled)
            {
                this.btnAutoToggle.Text = "AUTO";
                this.btnAutoToggle.ForeColor = Theme.Success;
                this.btnAutoToggle.IconColor = Theme.Success;
                this.btnAutoToggle.BackColor = Theme.SuccessTint;
                this.btnAutoToggle.HoverBackColor = Theme.Blend(Theme.SuccessTint, Theme.Success, 0.18f);
                this.btnAutoToggle.BorderColor = Theme.Blend(Theme.SuccessTint, Theme.Success, 0.45f);
                this.toolTip.SetToolTip(this.btnAutoToggle, "AP 변경 시 고정 IP 자동 전환 활성화됨 (클릭 시 수동 모드)");
            }
            else
            {
                this.btnAutoToggle.Text = "MANUAL";
                this.btnAutoToggle.ForeColor = Theme.TextMuted;
                this.btnAutoToggle.IconColor = Theme.TextMuted;
                this.btnAutoToggle.BackColor = Theme.Surface;
                this.btnAutoToggle.HoverBackColor = Theme.SurfaceHover;
                this.btnAutoToggle.BorderColor = Theme.BorderStrong;
                this.toolTip.SetToolTip(this.btnAutoToggle, "자동 전환 비활성화됨 (클릭 시 자동 모드)");
            }
        }

        /// <summary>
        /// SSID 아래 연결 상태 표시(점 + 문구) 갱신
        /// </summary>
        private void UpdateConnectionStatus(bool connected, ApProfile profile)
        {
            if (this.dotStatus == null || this.lblStatus == null) return;

            if (!connected)
            {
                this.dotStatus.DotColor = Theme.TextSubtle;
                this.lblStatus.Text = "Wi-Fi 연결 안 됨";
                this.lblStatus.ForeColor = Theme.TextMuted;
                this.lblWifiIcon.Image = UiIcons.Get("wifi", 20, Theme.TextSubtle);
                return;
            }

            this.lblWifiIcon.Image = UiIcons.Get("wifi", 20, Theme.Accent);
            if (profile == null)
            {
                this.dotStatus.DotColor = Theme.Warning;
                this.lblStatus.Text = "연결됨 · 미등록 AP";
            }
            else
            {
                this.dotStatus.DotColor = Theme.Success;
                this.lblStatus.Text = (profile.Mode == "static") ? "연결됨 · 고정 IP 프로필" : "연결됨 · DHCP 프로필";
            }
            this.lblStatus.ForeColor = Theme.TextSecondary;
        }

        private void PnlConnCard_Paint(object sender, PaintEventArgs e)
        {
            using (Pen p = new Pen(Theme.Border, 1f))
            {
                e.Graphics.DrawLine(p, 14, 66, this.pnlConnCard.Width - 15, 66);
            }
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            // Windows 11 은 DWM 이 둥근 테두리를 그리므로, 그 외 환경에서만 직접 테두리를 그림
            if (!this.nativeChrome)
            {
                using (Pen pen = new Pen(Theme.BorderStrong, 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
                }
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
