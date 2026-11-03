using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AutoNetSwitcher
{
    /// <summary>
    /// 윈도우 시작 프로그램 등록 및 관리 헬퍼
    /// 다른 PC에서 최초 실행 시에도 계정·언어·배터리 환경에 무관하게 100% 자동 등록되도록
    /// 작업 스케줄러(Universal SID S-1-5-32-545) + HKCU Run + HKLM Run 3중 등록을 수행합니다.
    /// </summary>
    public static class StartupHelper
    {
        private const string TaskName = "AutoNet";
        private const string RegKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RegValueName = "AutoNet";
        private const string DisabledFlagFile = "autonet_startup_disabled.flag";

        /// <summary>
        /// AutoNet.exe 실행 파일의 정확한 절대 경로 반환
        /// </summary>
        public static string GetSafeExecutablePath()
        {
            try
            {
                string appExe = Application.ExecutablePath;
                if (!string.IsNullOrWhiteSpace(appExe) &&
                    File.Exists(appExe) &&
                    Path.GetFileName(appExe).Equals("AutoNet.exe", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFullPath(appExe);
                }
            }
            catch { }

            string baseExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoNet.exe");
            if (File.Exists(baseExe))
            {
                return Path.GetFullPath(baseExe);
            }

            try
            {
                string asmLocation = typeof(StartupHelper).Assembly.Location;
                if (!string.IsNullOrWhiteSpace(asmLocation) &&
                    File.Exists(asmLocation) &&
                    asmLocation.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFullPath(asmLocation);
                }
            }
            catch { }

            return Path.GetFullPath(baseExe);
        }

        /// <summary>
        /// UAC 권한 승격 전(일반 사용자 컨텍스트)에서도 현재 로그인 계정의 HKCU Run 키에 선제 등록
        /// </summary>
        public static void RegisterCurrentUserRunKeyIfEnabled()
        {
            try
            {
                if (IsUserExplicitlyDisabled())
                    return;

                string exePath = GetSafeExecutablePath();
                string cmdValue = string.Format("\"{0}\" --autostart", exePath);

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegKeyPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue(RegValueName, cmdValue);
                    }
                }
            }
            catch { }
        }

        private static bool IsUserExplicitlyDisabled()
        {
            try
            {
                string flagPath = SecurityHelper.GetSafeAppDataPath(DisabledFlagFile);
                return File.Exists(flagPath);
            }
            catch
            {
                return false;
            }
        }

        private static void SetUserExplicitlyDisabled(bool disabled)
        {
            try
            {
                string flagPath = SecurityHelper.GetSafeAppDataPath(DisabledFlagFile);
                if (disabled)
                {
                    File.WriteAllText(flagPath, "disabled", Encoding.UTF8);
                }
                else if (File.Exists(flagPath))
                {
                    File.Delete(flagPath);
                }
            }
            catch { }
        }

        /// <summary>
        /// 현재 경로의 AutoNet.exe가 시작 프로그램에 정상 등록되어 있는지 확인
        /// </summary>
        public static bool IsStartupRegistered()
        {
            try
            {
                string exePath = GetSafeExecutablePath();

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegKeyPath, false))
                {
                    if (key != null)
                    {
                        string val = key.GetValue(RegValueName) as string;
                        if (!string.IsNullOrWhiteSpace(val) &&
                            val.IndexOf(exePath, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                }

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegKeyPath, false))
                {
                    if (key != null)
                    {
                        string val = key.GetValue(RegValueName) as string;
                        if (!string.IsNullOrWhiteSpace(val) &&
                            val.IndexOf(exePath, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// 시작 프로그램 등록 또는 해제 (작업 스케줄러 XML + HKCU Run + HKLM Run)
        /// </summary>
        public static bool SetStartup(bool enable)
        {
            string exePath = GetSafeExecutablePath();
            string cmdValue = string.Format("\"{0}\" --autostart", exePath);
            bool taskSuccess = false;
            bool regSuccess = false;

            if (enable)
            {
                SetUserExplicitlyDisabled(false);

                // 1. 작업 스케줄러 XML 등록 (모든 PC/언어 호환 Users SID S-1-5-32-545, 배터리 제한 해제)
                string tempXmlPath = Path.Combine(Path.GetTempPath(), "AutoNet_Task_" + Guid.NewGuid().ToString("N") + ".xml");
                try
                {
                    string schtasks = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe");
                    if (File.Exists(schtasks))
                    {
                        string escapedExe = SecurityElementEscape(exePath);

                        // Windows Task Scheduler 1.2 XSD 표준 순서 준수: Triggers -> Principals -> Settings -> Actions
                        string xmlContent =
                            "<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n" +
                            "<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\r\n" +
                            "  <Triggers>\r\n" +
                            "    <LogonTrigger>\r\n" +
                            "      <Enabled>true</Enabled>\r\n" +
                            "    </LogonTrigger>\r\n" +
                            "  </Triggers>\r\n" +
                            "  <Principals>\r\n" +
                            "    <Principal id=\"Author\">\r\n" +
                            "      <GroupId>S-1-5-32-545</GroupId>\r\n" +
                            "      <RunLevel>HighestAvailable</RunLevel>\r\n" +
                            "    </Principal>\r\n" +
                            "  </Principals>\r\n" +
                            "  <Settings>\r\n" +
                            "    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>\r\n" +
                            "    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>\r\n" +
                            "    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>\r\n" +
                            "    <AllowHardTerminate>true</AllowHardTerminate>\r\n" +
                            "    <StartWhenAvailable>true</StartWhenAvailable>\r\n" +
                            "    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>\r\n" +
                            "    <IdleSettings>\r\n" +
                            "      <StopOnIdleEnd>false</StopOnIdleEnd>\r\n" +
                            "      <RestartOnIdle>false</RestartOnIdle>\r\n" +
                            "    </IdleSettings>\r\n" +
                            "    <AllowStartOnDemand>true</AllowStartOnDemand>\r\n" +
                            "    <Enabled>true</Enabled>\r\n" +
                            "    <Hidden>false</Hidden>\r\n" +
                            "    <RunOnlyIfIdle>false</RunOnlyIfIdle>\r\n" +
                            "    <WakeToRun>false</WakeToRun>\r\n" +
                            "    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>\r\n" +
                            "    <Priority>7</Priority>\r\n" +
                            "  </Settings>\r\n" +
                            "  <Actions Context=\"Author\">\r\n" +
                            "    <Exec>\r\n" +
                            "      <Command>\"" + escapedExe + "\"</Command>\r\n" +
                            "      <Arguments>--autostart</Arguments>\r\n" +
                            "    </Exec>\r\n" +
                            "  </Actions>\r\n" +
                            "</Task>";

                        File.WriteAllText(tempXmlPath, xmlContent, Encoding.Unicode);

                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = schtasks,
                            Arguments = string.Format("/create /tn \"{0}\" /xml \"{1}\" /f", TaskName, tempXmlPath),
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        using (Process p = Process.Start(psi))
                        {
                            if (p != null)
                            {
                                p.WaitForExit(5000);
                                taskSuccess = (p.ExitCode == 0);
                            }
                        }
                    }
                }
                catch { }
                finally
                {
                    try { if (File.Exists(tempXmlPath)) File.Delete(tempXmlPath); } catch { }
                }

                // 2. 현재 사용자 레지스트리 Run 키 등록 (HKCU)
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegKeyPath, true))
                    {
                        if (key != null)
                        {
                            key.SetValue(RegValueName, cmdValue);
                            regSuccess = true;
                        }
                    }
                }
                catch { }

                // 3. 로컬 머신 레지스트리 Run 키 등록 (HKLM - 다른 PC에서 계정이 분리된 환경 대응)
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegKeyPath, true))
                    {
                        if (key != null)
                        {
                            key.SetValue(RegValueName, cmdValue);
                            regSuccess = true;
                        }
                    }
                }
                catch { }

                return taskSuccess || regSuccess;
            }
            else
            {
                SetUserExplicitlyDisabled(true);

                try
                {
                    string schtasks = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe");
                    if (File.Exists(schtasks))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = schtasks,
                            Arguments = string.Format("/delete /tn \"{0}\" /f", TaskName),
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        using (Process p = Process.Start(psi))
                        {
                            if (p != null)
                            {
                                p.WaitForExit(3000);
                            }
                        }
                    }
                }
                catch { }

                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegKeyPath, true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue(RegValueName, false);
                        }
                    }
                }
                catch { }

                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegKeyPath, true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue(RegValueName, false);
                        }
                    }
                }
                catch { }

                return true;
            }
        }

        private static string SecurityElementEscape(string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            return str.Replace("&", "&amp;")
                      .Replace("<", "&lt;")
                      .Replace(">", "&gt;")
                      .Replace("\"", "&quot;")
                      .Replace("'", "&apos;");
        }

        /// <summary>
        /// 다른 PC에서 최초 실행 시(또는 실행 파일 위치 이동 시) 자동으로 윈도우 시작프로그램에 등록
        /// (단, 사용자가 트레이 메뉴에서 수동으로 해제한 경우는 존중)
        /// </summary>
        public static bool EnsureStartupRegistered()
        {
            try
            {
                if (IsUserExplicitlyDisabled())
                    return false;

                return SetStartup(true);
            }
            catch { }

            return false;
        }
    }
}
