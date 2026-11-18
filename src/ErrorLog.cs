using System;
using System.IO;
using System.Text;

namespace AutoNetSwitcher
{
    /// <summary>
    /// 오류 전용 로그 (AutoNet_Error.log): 날짜·시간, 발생 위치, 원인, 상세 스택을 기록
    /// </summary>
    internal static class ErrorLog
    {
        public const string FileName = "AutoNet_Error.log";
        private static readonly object sync = new object();

        public static string FilePath
        {
            get { return SecurityHelper.GetSafeAppDataPath(FileName); }
        }

        /// <summary>예외 기록: 가장 안쪽 예외를 원인으로 표시하고 전체 스택을 함께 남김</summary>
        public static void Write(string where, Exception ex)
        {
            if (ex == null) { Write(where, "알 수 없는 오류", null); return; }
            Exception root = ex;
            while (root.InnerException != null) root = root.InnerException;
            Write(where, root.GetType().FullName + ": " + root.Message, ex.ToString());
        }

        // Environment.OSVersion은 매니페스트 없는 앱에서 6.2(Win8)로 고정되므로 레지스트리에서 실제 빌드를 읽음
        private static string WindowsVersion()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (k != null)
                        return string.Format("Windows {0} (빌드 {1})", k.GetValue("DisplayVersion", ""), k.GetValue("CurrentBuild", ""));
                }
            }
            catch { }
            return Environment.OSVersion.ToString();
        }

        /// <summary>예외가 아닌 실패(명령 실패, 연결 실패 등) 기록</summary>
        public static void Write(string where, string cause, string detail)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("[{0:yyyy-MM-dd HH:mm:ss.fff}] 오류 발생\r\n", DateTime.Now);
                sb.AppendFormat("  위치   : {0}\r\n", where);
                sb.AppendFormat("  원인   : {0}\r\n", cause);
                sb.AppendFormat("  환경   : {0} / .NET {1} / 관리자 {2}\r\n",
                    WindowsVersion(), Environment.Version, Program.IsAdministrator() ? "예" : "아니오");
                if (!string.IsNullOrEmpty(detail))
                    sb.Append("  상세   :\r\n    ").Append(detail.Replace("\r\n", "\n").Replace("\n", "\r\n    ")).Append("\r\n");
                sb.Append("------------------------------------------------------------\r\n");

                lock (sync)
                {
                    File.AppendAllText(FilePath, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // 로깅 실패로 2차 장애가 나지 않도록 무시
            }
        }
    }
}
