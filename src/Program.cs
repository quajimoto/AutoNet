using System;
using System.IO;
using System.Security.Principal;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;

namespace AutoNetSwitcher
{
    internal static class Program
    {
        private const string MutexName = @"Local\AutoNet_SingleInstance_Mutex";
        private static Mutex appMutex;

        [STAThread]
        private static void Main(string[] args)
        {
            bool isAutoStart = false;
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--autostart", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--minimized", StringComparison.OrdinalIgnoreCase))
                    {
                        isAutoStart = true;
                        break;
                    }
                }
            }

            // 1. 최초 실행 시 현재 로그인 계정의 HKCU 시작프로그램 선제 등록 (타 PC/표준 계정 호환)
            StartupHelper.RegisterCurrentUserRunKeyIfEnabled();

            // 2. 이미 다른 인스턴스가 실행 중인지 선제 검사 (중복 실행 방지)
            if (IsAnotherInstanceRunning())
            {
                if (!isAutoStart)
                {
                    NotifyExistingInstanceAndExit();
                }
                return;
            }

            // 2. 관리자 권한 확인 및 자동 승격 요청
            if (!IsAdministrator())
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = Application.ExecutablePath;
                    if (isAutoStart)
                    {
                        psi.Arguments = "--autostart";
                    }
                    psi.Verb = "runas";
                    psi.UseShellExecute = true;
                    Process.Start(psi);
                    return;
                }
                catch (Exception ex)
                {
                    // UAC 거부 또는 취소 시 친절한 안내 후 종료
                    LogCrash("관리자 권한 요청 (UAC 거부)", ex);
                    if (!isAutoStart)
                    {
                        MessageBox.Show(
                            "AutoNet은 네트워크 어댑터 IP 및 DNS 설정을 제어하기 위해 관리자 권한이 반드시 필요합니다.",
                            "권한 필요 안내",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    return;
                }
            }

            // 3. 관리자 권한 프로세스에서 단일 인스턴스 뮤텍스 생성 및 점유
            bool createdNew = false;
            try
            {
                appMutex = new Mutex(true, MutexName, out createdNew);
                if (!createdNew)
                {
                    if (!isAutoStart)
                    {
                        NotifyExistingInstanceAndExit();
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                LogCrash("중복 실행 검사 (뮤텍스 생성 실패)", ex);
                if (!isAutoStart)
                {
                    NotifyExistingInstanceAndExit();
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // CWE-209, CWE-4-1: 상세 스택트레이스 화면 비노출 및 안전한 파일 로깅
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.ThreadException += OnThreadException;

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                LogCrash("애플리케이션 메인 루프 (치명적 오류)", ex);
                MessageBox.Show(
                    "AutoNet 실행 중 예기치 않은 오류가 발생했습니다.\n자세한 기술 정보는 로그(AutoNet_Error.log)에 날짜·시간과 함께 기록되었습니다.",
                    "AutoNet 오류 안내",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // 종료 시 뮤텍스 안전 해제 (CWE-404)
                if (appMutex != null)
                {
                    try
                    {
                        appMutex.ReleaseMutex();
                        appMutex.Dispose();
                    }
                    catch { }
                    appMutex = null;
                }
            }
        }

        /// <summary>
        /// 이미 실행 중인 AutoNet 프로세스가 있는지 검사
        /// </summary>
        private static bool IsAnotherInstanceRunning()
        {
            try
            {
                using (Mutex existing = Mutex.OpenExisting(MutexName))
                {
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 다른 권한의 프로세스가 이미 뮤텍스를 점유 중인 경우
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 기존 실행 중인 인스턴스에 복원 메시지를 전송하고 안내 팝업을 띄운 뒤 새 인스턴스 종료
        /// </summary>
        private static void NotifyExistingInstanceAndExit()
        {
            try
            {
                uint msg = NativeMethods.RegisterWindowMessage("AutoNet_ShowExistingInstance");
                if (msg != 0)
                {
                    NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, msg, IntPtr.Zero, IntPtr.Zero);
                }
            }
            catch { }

            MessageBox.Show(
                "AutoNet 프로그램이 이미 실행 중입니다.\n기존에 실행 중인 프로그램을 계속 사용합니다.",
                "AutoNet 알림",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        public static bool IsAdministrator()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    if (identity == null) return false;
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogCrash("미처리 예외 (Domain UnhandledException)", e.ExceptionObject as Exception);
            MessageBox.Show(
                "시스템 오류가 발생하여 안전하게 복구되었습니다.\n네트워크 상태를 다시 확인해 주십시오.",
                "AutoNet 시스템 알림",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogCrash("UI 스레드 예외 (ThreadException)", e.Exception);
            MessageBox.Show(
                "화면 표시 중 오류가 발생했습니다.\n작업을 계속 진행합니다.",
                "AutoNet UI 알림",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private static void LogCrash(string where, Exception ex)
        {
            ErrorLog.Write(where, ex);
        }
    }
}
