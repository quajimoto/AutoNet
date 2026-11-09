using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AutoNetSwitcher
{
    public partial class MainForm : Form
    {
        // 네트워크 상태 필드
        private string currentSsid = "확인 중...";
        private string currentIp = "0.0.0.0";
        private string currentGateway = "-";
        private string currentDns = "-";
        private string lastConnectedSsid = string.Empty;
        private string activeInterfaceName = "Wi-Fi";

        private bool isAutoEnabled = true;
        private bool allowExit = false;

        // CWE-367 (TOCTOU 및 동시성 보호): 프로필 딕셔너리 동기화 객체
        private readonly object profileLock = new object();
        private readonly Dictionary<string, ApProfile> savedProfiles = new Dictionary<string, ApProfile>(StringComparer.OrdinalIgnoreCase);

        // CWE-22, CWE-99: 안전하게 검증된 경로
        private readonly string configPath;
        private readonly string logPath;
        private readonly bool isAutoStart = false;

        public MainForm()
        {
            // 명령행 인수 확인 (--autostart 또는 --minimized)
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 1; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--autostart", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--minimized", StringComparison.OrdinalIgnoreCase))
                    {
                        this.isAutoStart = true;
                        break;
                    }
                }
            }
            catch { }

            // 시큐어코딩: 안전한 AppData 디렉터리 경로 획득
            this.configPath = SecurityHelper.GetSafeAppDataPath("autonet_profiles.txt");
            this.logPath = SecurityHelper.GetSafeAppDataPath("autonet.log");

            InitializeComponent();

            this.Load += new EventHandler(this.MainForm_Load);
            this.Shown += new EventHandler(this.MainForm_Shown);
            this.FormClosing += new FormClosingEventHandler(this.MainForm_FormClosing);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                LoadProfiles();
                UpdateSavedApCount();
                Log("AutoNet 시큐어 버전 시작 완료");

                // 첫 실행 시 윈도우 시작 프로그램에 자동 등록 (작업 스케줄러 & 레지스트리)
                if (StartupHelper.EnsureStartupRegistered())
                {
                    Log("윈도우 시작 프로그램 자동 등록 완료 (재부팅 시 자동 실행)");
                }
            }
            catch (Exception ex)
            {
                Log("초기 로드 중 예외: " + ex.Message);
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            // 백그라운드 모니터링 시작
            StartMonitoring();

            if (this.isAutoStart)
            {
                // 재부팅 자동 실행 모드: 트레이로 바로 숨김
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Hide();

                if (this.trayIcon != null)
                {
                    try
                    {
                        this.trayIcon.ShowBalloonTip(
                            2500,
                            "AutoNet 자동 시작됨",
                            "시작 프로그램으로 등록되어 트레이 백그라운드에서 동작 중입니다.",
                            ToolTipIcon.Info);
                    }
                    catch { }
                }
            }
            else
            {
                this.Visible = true;
                this.BringToFront();
                this.Activate();

                if (this.trayIcon != null)
                {
                    try
                    {
                        this.trayIcon.ShowBalloonTip(
                            1800,
                            "AutoNet 실행됨",
                            "Wi-Fi AP별 자동 IP/게이트웨이/DNS 전환기가 백그라운드에서 동작 중입니다.",
                            ToolTipIcon.Info);
                    }
                    catch { }
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 사용자가 닫기(X 버튼, Alt+F4, 작업표시줄 닫기)를 누른 경우 종료하지 않고 트레이로 최소화
            if (!this.allowExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.MinimizeToTray();
                return;
            }

            // 1. 주기적 스캔 타이머 즉시 정지
            try
            {
                if (this.scanTimer != null)
                {
                    this.scanTimer.Stop();
                }
            }
            catch { }

            // 2. 트레이 아이콘 가시성 즉시 해제 (종료 시 트레이 아이콘 잔상 방지 및 오류 방어)
            try
            {
                if (this.trayIcon != null)
                {
                    this.trayIcon.Visible = false;
                }
            }
            catch { }

            // 3. 프로필 저장 및 종료 로그 기록
            try
            {
                SaveProfiles();
                Log("AutoNet 안전하게 종료됨");
            }
            catch
            {
                // 종료 시 예외 억제
            }
        }

        /// <summary>
        /// CWE-404, CWE-476: 스레드 안전 UI 업데이트 헬퍼
        /// </summary>
        public void SafeInvoke(Action action)
        {
            if (action == null) return;
            if (this.IsDisposed || this.Disposing) return;

            try
            {
                if (this.InvokeRequired)
                {
                    if (this.IsHandleCreated && !this.IsDisposed && !this.Disposing)
                    {
                        this.BeginInvoke(action);
                    }
                }
                else
                {
                    action();
                }
            }
            catch
            {
                // 폼 종료 중 Invoke 예외 방어
            }
        }

        private static readonly uint WM_SHOW_AUTONET = NativeMethods.RegisterWindowMessage("AutoNet_ShowExistingInstance");

        /// <summary>
        /// 중복 실행 시 기존 실행 중인 인스턴스를 복원 및 전면 활성화 (CWE-404)
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (WM_SHOW_AUTONET != 0 && m.Msg == (int)WM_SHOW_AUTONET)
            {
                SafeInvoke(() =>
                {
                    this.RestoreFromTray();
                });
                return;
            }
            base.WndProc(ref m);
        }

        /// <summary>
        /// 안전한 타임스탬프 로그 기록 (동시성 락 및 오류 처리 적용)
        /// </summary>
        public void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                string line = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}\r\n", DateTime.Now, message.Trim());
                lock (this.logPath)
                {
                    File.AppendAllText(this.logPath, line);
                }
            }
            catch
            {
                // 로깅 실패로 애플리케이션이 비정상 종료되지 않도록 방어
            }
        }

        private void BtnAutoToggle_Click(object sender, EventArgs e)
        {
            this.isAutoEnabled = !this.isAutoEnabled;
            this.ApplyAutoToggleStyle();
            if (this.isAutoEnabled)
            {
                Log("자동 감지 및 전환 기능 활성화됨");
                this.RefreshNetworkStatus();
            }
            else
            {
                Log("자동 감지 및 전환 기능 비활성화됨 (수동 모드)");
            }
        }

        private void BtnCopyIp_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(this.currentIp) && this.currentIp != "0.0.0.0")
                {
                    Clipboard.SetText(this.currentIp);
                    this.toolTip.Show("IP 주소가 복사되었습니다.", this.btnCopyIp, 0, -25, 1200);
                }
            }
            catch (Exception ex)
            {
                Log("클립보드 복사 오류: " + ex.Message);
            }
        }
    }
}
