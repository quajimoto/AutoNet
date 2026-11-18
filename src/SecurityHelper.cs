using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

namespace AutoNetSwitcher
{
    /// <summary>
    /// 시큐어코딩 가이드(KISA 2023 / CWE) 기준 입력 검증 및 안전한 시스템 실행 유틸리티
    /// </summary>
    public static class SecurityHelper
    {
        // CWE-78: 인터페이스 명 화이트리스트 (영문, 숫자, 한글, 공백, 하이픈, 언더스코어, 괄호)
        private static readonly Regex SafeInterfaceRegex = new Regex(@"^[a-zA-Z0-9가-힣\s\-_\(\)]{1,64}$", RegexOptions.Compiled);

        // CWE-78: SSID 허용 패턴 (제어문자 및 셸 메타문자 제외, 최대 32글자)
        private static readonly Regex SafeSsidRegex = new Regex(@"^[^\x00-\x1F\x7F""&|;`$><\\]{1,32}$", RegexOptions.Compiled);

        /// <summary>
        /// CWE-20, CWE-807: 유효한 IPv4 유니캐스트 호스트 주소인지 엄격 검증
        /// </summary>
        public static bool IsValidIpv4(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;

            string trimmed = ip.Trim();
            string[] parts = trimmed.Split('.');
            if (parts.Length != 4)
                return false;

            IPAddress parsed;
            if (!IPAddress.TryParse(trimmed, out parsed))
                return false;

            if (parsed.AddressFamily != AddressFamily.InterNetwork)
                return false;

            byte[] bytes = parsed.GetAddressBytes();

            // 0.0.0.0 또는 255.255.255.255 브로드캐스트/미지정 주소 배제
            if (bytes[0] == 0 || bytes[0] == 255)
                return false;

            // 127.0.0.0/8 루프백 주소 배제
            if (bytes[0] == 127)
                return false;

            // 224.0.0.0 ~ 239.255.255.255 멀티캐스트 배제
            if (bytes[0] >= 224 && bytes[0] <= 239)
                return false;

            return true;
        }

        /// <summary>
        /// 서브넷 마스크 유효성 검증
        /// </summary>
        public static bool IsValidSubnetMask(string mask)
        {
            if (string.IsNullOrWhiteSpace(mask))
                return false;

            IPAddress parsed;
            if (!IPAddress.TryParse(mask.Trim(), out parsed))
                return false;

            if (parsed.AddressFamily != AddressFamily.InterNetwork)
                return false;

            byte[] bytes = parsed.GetAddressBytes();
            uint maskVal = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | (uint)bytes[3];

            // 서브넷 마스크는 상위 비트가 연속된 1이고 하위 비트가 연속된 0이어야 함
            if (maskVal == 0) return false;
            uint inverted = ~maskVal;
            return ((inverted + 1) & inverted) == 0;
        }

        /// <summary>
        /// IP와 게이트웨이가 동일 서브넷 대역인지 검증 (/24 기본 비교 및 마스크 기반 비교 지원)
        /// </summary>
        public static bool IsSubnetMatch(string ip1, string ip2, string subnetMask = "255.255.255.0")
        {
            if (!IsValidIpv4(ip1) || !IsValidIpv4(ip2))
                return true; // 입력 중 하나라도 아직 미완성일 때는 유효성 통과 처리

            try
            {
                IPAddress addr1 = IPAddress.Parse(ip1.Trim());
                IPAddress addr2 = IPAddress.Parse(ip2.Trim());

                IPAddress maskAddr;
                if (!IPAddress.TryParse(subnetMask, out maskAddr))
                {
                    maskAddr = IPAddress.Parse("255.255.255.0");
                }

                byte[] b1 = addr1.GetAddressBytes();
                byte[] b2 = addr2.GetAddressBytes();
                byte[] m = maskAddr.GetAddressBytes();

                for (int i = 0; i < 4; i++)
                {
                    if ((b1[i] & m[i]) != (b2[i] & m[i]))
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// CWE-78: 인터페이스 이름 안전성 검증
        /// </summary>
        public static string SanitizeInterfaceName(string interfaceName)
        {
            if (string.IsNullOrWhiteSpace(interfaceName))
                return "Wi-Fi";

            string trimmed = interfaceName.Trim();
            if (SafeInterfaceRegex.IsMatch(trimmed))
                return trimmed;

            return "Wi-Fi";
        }

        /// <summary>
        /// CWE-78: SSID 안전성 검증
        /// </summary>
        public static bool IsSafeSsid(string ssid)
        {
            if (string.IsNullOrWhiteSpace(ssid))
                return false;

            string trimmed = ssid.Trim();
            return SafeSsidRegex.IsMatch(trimmed);
        }

        /// <summary>
        /// CWE-1-3: 경로 조작 방지 및 안전한 애플리케이션 데이터 디렉터리 경로 반환
        /// </summary>
        public static string GetSafeAppDataPath(string fileName)
        {
            // 상위 디렉터리 탐색 문자열 차단
            string safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
                throw new ArgumentException("유효하지 않은 파일 이름입니다.", "fileName");

            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoNet");

            try
            {
                if (!Directory.Exists(appData))
                {
                    Directory.CreateDirectory(appData);
                }
                return Path.GetFullPath(Path.Combine(appData, safeFileName));
            }
            catch
            {
                // 로컬 앱데이터 접근 불가 시 실행 파일 기준 디렉터리로 안전 폴백
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.GetFullPath(Path.Combine(baseDir, safeFileName));
            }
        }

        /// <summary>
        /// netsh.exe의 안전한 절대 경로 반환 (System32 폴더 고정)
        /// </summary>
        public static string GetNetshPath()
        {
            string sysDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string netshPath = Path.Combine(sysDir, "netsh.exe");
            if (File.Exists(netshPath))
                return netshPath;

            return "netsh.exe";
        }

        /// <summary>
        /// netsh 출력 디코딩: 최신 Windows는 시스템 코드페이지(949)와 무관하게 UTF-8로 출력하므로
        /// 유효한 UTF-8이면 UTF-8로, 아니면 시스템 기본(구버전 CP949) 인코딩으로 해석
        /// (잘못 해석하면 "상태 : 연결됨" 같은 한글 라벨과 한글 SSID가 깨짐)
        /// </summary>
        public static string DecodeNetshOutput(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;
            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.Default.GetString(bytes);
            }
        }

        /// <summary>
        /// CWE-78, CWE-404: cmd.exe를 거치지 않고 netsh 프로세스를 직접 안전하게 실행
        /// </summary>
        public static bool ExecuteNetshDirect(string arguments, out string output, int timeoutMs = 7000)
        {
            output = string.Empty;
            string netshExe = GetNetshPath();

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = netshExe;
            psi.Arguments = arguments;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            // 원본 바이트를 그대로 받은 뒤 디코딩 (Latin-1은 바이트를 1:1로 보존)
            psi.StandardOutputEncoding = Encoding.GetEncoding(28591);

            try
            {
                using (Process proc = Process.Start(psi))
                {
                    if (proc == null)
                        return false;

                    output = DecodeNetshOutput(Encoding.GetEncoding(28591).GetBytes(proc.StandardOutput.ReadToEnd()));
                    if (!proc.WaitForExit(timeoutMs))
                    {
                        try { proc.Kill(); } catch { }
                        return false;
                    }
                    return proc.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                output = "오류: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 관리자 권한으로 netsh 프로세스 직접 단독 실행 (cmd.exe 경유 없음)
        /// </summary>
        public static bool ExecuteNetshElevated(string arguments, int timeoutMs = 10000)
        {
            string netshExe = GetNetshPath();

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = netshExe;
            psi.Arguments = arguments;
            psi.UseShellExecute = true;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                using (Process proc = Process.Start(psi))
                {
                    if (proc == null)
                        return false;

                    if (!proc.WaitForExit(timeoutMs))
                    {
                        try { proc.Kill(); } catch { }
                        ErrorLog.Write("네트워크 설정 명령 (netsh)", "응답 시간 초과 (" + timeoutMs + "ms)", "netsh " + arguments);
                        return false;
                    }
                    if (proc.ExitCode != 0)
                        ErrorLog.Write("네트워크 설정 명령 (netsh)", "명령 실패 (종료 코드 " + proc.ExitCode + ")", "netsh " + arguments);
                    return proc.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Write("네트워크 설정 명령 (netsh " + arguments + ")", ex);
                return false;
            }
        }
    }
}
