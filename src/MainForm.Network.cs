using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Drawing;
using System.Windows.Forms;

namespace AutoNetSwitcher
{
    public partial class MainForm
    {
        private DateTime lastAutoConnectTime = DateTime.MinValue;
        private DateTime lastAutoIpTime = DateTime.MinValue;
        private int isRefreshing = 0;

        private void StartMonitoring()
        {
            try
            {
                if (this.scanTimer != null)
                {
                    this.scanTimer.Stop();
                    this.scanTimer.Dispose();
                }

                this.scanTimer = new System.Windows.Forms.Timer();
                this.scanTimer.Interval = 3000;
                this.scanTimer.Tick += (s, e) => { this.RefreshNetworkStatus(); };
                this.scanTimer.Start();

                this.RefreshNetworkStatus();
            }
            catch (Exception ex)
            {
                Log("모니터링 타이머 시작 오류: " + ex.Message);
            }
        }

        /// <summary>
        /// 현재 SSID(또는 원래 SSID)와 일치하는 저장된 프로필 반환
        /// </summary>
        public ApProfile FindSavedProfile(string ssid)
        {
            if (string.IsNullOrWhiteSpace(ssid)) return null;

            lock (this.profileLock)
            {
                if (this.savedProfiles.ContainsKey(ssid))
                {
                    return this.savedProfiles[ssid].Clone();
                }

                foreach (KeyValuePair<string, ApProfile> kvp in this.savedProfiles)
                {
                    if (kvp.Value.MatchesSsid(ssid))
                    {
                        return kvp.Value.Clone();
                    }
                }
            }
            return null;
        }

        public void RefreshNetworkStatus()
        {
            if (Interlocked.CompareExchange(ref this.isRefreshing, 1, 0) != 0)
                return;

            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    string ssid = GetConnectedSsid();
                    bool isDisconnected = string.IsNullOrEmpty(ssid) ||
                                          ssid == "연결된 Wi-Fi 없음" ||
                                          ssid == "확인 중..." ||
                                          ssid == "Wi-Fi 인터페이스 확인 필요";

                    // 1. 미연결 상태에서 주변에 저장된 AP가 있으면 자동 연결 시도
                    if (this.isAutoEnabled && isDisconnected &&
                        (DateTime.Now - this.lastAutoConnectTime).TotalSeconds >= 8)
                    {
                        int savedCount = 0;
                        lock (this.profileLock) { savedCount = this.savedProfiles.Count; }

                        if (savedCount > 0)
                        {
                            this.lastAutoConnectTime = DateTime.Now;
                            List<string> nearbyAps = ScanNearbyWifiAps();
                            foreach (string ap in nearbyAps)
                            {
                                ApProfile matched = FindSavedProfile(ap);
                                if (matched != null)
                                {
                                    Log(string.Format("저장된 주변 AP 감지됨: '{0}' -> 자동 Wi-Fi 연결 시도", ap));
                                    string escapedSsid = ap.Replace("\"", "\\\"");
                                    string outLog;
                                    SecurityHelper.ExecuteNetshDirect(string.Format("wlan connect name=\"{0}\"", escapedSsid), out outLog, 5000);
                                    Thread.Sleep(2000);
                                    ssid = GetConnectedSsid();
                                    isDisconnected = string.IsNullOrEmpty(ssid) || ssid == "연결된 Wi-Fi 없음";
                                    break;
                                }
                            }
                        }
                    }

                    NetworkDetails net = GetActiveNetworkDetails();

                    SafeInvoke(() =>
                    {
                        this.currentSsid = ssid;
                        this.currentIp = net.Ip;
                        this.currentGateway = net.Gateway;
                        this.currentDns = net.Dns;

                        // UI 라벨 갱신 (별칭이 있는 프로필이면 함께 표시 가능)
                        ApProfile currentProf = isDisconnected ? null : FindSavedProfile(ssid);
                        if (currentProf != null && !string.Equals(currentProf.Ssid, ssid, StringComparison.OrdinalIgnoreCase))
                        {
                            this.lblSsid.Text = string.Format("{0} ({1})", currentProf.Ssid, ssid);
                        }
                        else
                        {
                            this.lblSsid.Text = string.IsNullOrEmpty(ssid) ? "연결된 Wi-Fi 없음" : ssid;
                        }

                        UpdateConnectionStatus(!isDisconnected && !string.IsNullOrEmpty(ssid), currentProf);

                        this.lblCurrentIp.Text = string.IsNullOrEmpty(net.Ip) ? "0.0.0.0" : net.Ip;
                        this.lblGateway.Text = string.IsNullOrEmpty(net.Gateway) ? "-" : net.Gateway;
                        this.lblDns.Text = string.IsNullOrEmpty(net.Dns) ? "-" : net.Dns;

                        // 툴팁 상세 정보 갱신
                        string subnetInfo = string.IsNullOrEmpty(net.Subnet) ? "" : string.Format(" (서브넷: {0})", net.Subnet);
                        this.toolTip.SetToolTip(this.lblCurrentIp, "IPv4: " + (net.Ip ?? "-") + subnetInfo);
                        this.toolTip.SetToolTip(this.lblGateway, "기본 게이트웨이: " + (net.Gateway ?? "-"));
                        this.toolTip.SetToolTip(this.lblDns, "기본 DNS: " + (net.Dns ?? "-"));

                        // 트레이 텍스트 갱신 (최대 63자 제한)
                        string trayDesc = string.Format("AutoNet: {0} ({1}) GW:{2}", this.currentSsid, this.currentIp, this.currentGateway);
                        if (trayDesc.Length >= 63)
                        {
                            trayDesc = trayDesc.Substring(0, 59) + "...";
                        }
                        if (this.trayIcon != null)
                        {
                            try
                            {
                                this.trayIcon.Text = trayDesc;
                            }
                            catch { }
                        }

                        // ==================== 2. 저장된 AP 자동 IP 할당 로직 ====================
                        if (this.isAutoEnabled && !isDisconnected && currentProf != null)
                        {
                            if (currentProf.Mode == "static")
                            {
                                bool ipMatches = string.Equals(this.currentIp, currentProf.Ip, StringComparison.OrdinalIgnoreCase);
                                bool gwMatches = string.IsNullOrEmpty(currentProf.Gateway) ||
                                                 currentProf.Gateway == "-" ||
                                                 string.Equals(this.currentGateway, currentProf.Gateway, StringComparison.OrdinalIgnoreCase);

                                // 현재 IP 또는 게이트웨이가 저장된 고정 IP와 다르면 즉시 자동 할당 (0.0.0.0 상태 포함)
                                if ((!ipMatches || !gwMatches) &&
                                    (DateTime.Now - this.lastAutoIpTime).TotalSeconds >= 7)
                                {
                                    this.lastAutoIpTime = DateTime.Now;
                                    this.lastConnectedSsid = ssid;
                                    Log(string.Format("저장된 AP('{0}') 연결 확인 -> 고정 IP({1}) 자동 할당 실행", ssid, currentProf.Ip));
                                    ApplyStaticIp(currentProf.Ip, currentProf.Gateway, currentProf.Dns, currentProf.Subnet);
                                }
                                else if (ipMatches)
                                {
                                    this.lastConnectedSsid = ssid;
                                }
                            }
                            else if (currentProf.Mode == "dhcp")
                            {
                                if (!string.Equals(ssid, this.lastConnectedSsid, StringComparison.OrdinalIgnoreCase))
                                {
                                    this.lastConnectedSsid = ssid;
                                    Log(string.Format("저장된 AP('{0}') 연결 확인 -> DHCP 자동 전환 실행", ssid));
                                    ApplyDhcpSilent();
                                }
                            }
                        }
                        else if (isDisconnected)
                        {
                            this.lastConnectedSsid = string.Empty;
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log("네트워크 상태 갱신 백그라운드 예외: " + ex.Message);
                }
                finally
                {
                    Interlocked.Exchange(ref this.isRefreshing, 0);
                }
            });
        }

        /// <summary>
        /// netsh를 통하여 현재 연결된 Wi-Fi 어댑터 이름 및 SSID 파싱
        /// </summary>
        private string GetConnectedSsid()
        {
            try
            {
                string output;
                if (!SecurityHelper.ExecuteNetshDirect("wlan show interfaces", out output, 4000))
                {
                    return "연결된 Wi-Fi 없음";
                }

                string parsedSsid = string.Empty;
                using (StringReader reader = new StringReader(output))
                {
                    string line;
                    int lineCount = 0;
                    // CWE-835: 루프 상한선 설정
                    while ((line = reader.ReadLine()) != null && lineCount++ < 300)
                    {
                        string trimmed = line.Trim();
                        if ((trimmed.StartsWith("Name", StringComparison.OrdinalIgnoreCase) ||
                             trimmed.StartsWith("이름", StringComparison.OrdinalIgnoreCase)) &&
                            trimmed.Contains(":"))
                        {
                            int colonIdx = trimmed.IndexOf(':');
                            string ifName = trimmed.Substring(colonIdx + 1).Trim();
                            if (!string.IsNullOrEmpty(ifName))
                            {
                                this.activeInterfaceName = SecurityHelper.SanitizeInterfaceName(ifName);
                            }
                        }
                        else if ((trimmed.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) ||
                                  trimmed.StartsWith("SSID ", StringComparison.OrdinalIgnoreCase)) &&
                                 !trimmed.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase) &&
                                 !trimmed.Contains("BSSID"))
                        {
                            int colonIdx = trimmed.IndexOf(':');
                            if (colonIdx >= 0)
                            {
                                parsedSsid = trimmed.Substring(colonIdx + 1).Trim();
                            }
                        }
                    }
                }

                return string.IsNullOrEmpty(parsedSsid) ? "연결된 Wi-Fi 없음" : parsedSsid;
            }
            catch (Exception ex)
            {
                Log("GetConnectedSsid 예외: " + ex.Message);
                return "Wi-Fi 인터페이스 확인 필요";
            }
        }

        /// <summary>
        /// .NET 네트워크 인터페이스 API를 사용하여 활성 네트워크 어댑터 IP/서브넷/게이트웨이/DNS 조회
        /// </summary>
        private NetworkDetails GetActiveNetworkDetails()
        {
            NetworkDetails details = new NetworkDetails();
            try
            {
                NetworkInterface[] allInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                NetworkInterface targetInterface = null;

                // 1순위: activeInterfaceName과 이름이 일치하는 인터페이스
                if (!string.IsNullOrEmpty(this.activeInterfaceName))
                {
                    foreach (NetworkInterface nic in allInterfaces)
                    {
                        if (nic.OperationalStatus == OperationalStatus.Up &&
                            nic.Name.Equals(this.activeInterfaceName, StringComparison.OrdinalIgnoreCase))
                        {
                            targetInterface = nic;
                            break;
                        }
                    }
                }

                // 2순위: 무선(Wireless80211) 인터페이스
                if (targetInterface == null)
                {
                    foreach (NetworkInterface nic in allInterfaces)
                    {
                        if (nic.OperationalStatus == OperationalStatus.Up &&
                            nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        {
                            targetInterface = nic;
                            break;
                        }
                    }
                }

                // 3순위: 루프백이 아닌 일반 활성 인터페이스
                if (targetInterface == null)
                {
                    foreach (NetworkInterface nic in allInterfaces)
                    {
                        if (nic.OperationalStatus == OperationalStatus.Up &&
                            nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        {
                            targetInterface = nic;
                            break;
                        }
                    }
                }

                if (targetInterface != null)
                {
                    IPInterfaceProperties ipProps = targetInterface.GetIPProperties();
                    if (ipProps != null)
                    {
                        // IPv4 주소 및 서브넷 마스크 추출
                        if (ipProps.UnicastAddresses != null)
                        {
                            foreach (UnicastIPAddressInformation u in ipProps.UnicastAddresses)
                            {
                                if (u.Address != null && u.Address.AddressFamily == AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(u.Address))
                                {
                                    details.Ip = u.Address.ToString();
                                    if (u.IPv4Mask != null)
                                    {
                                        details.Subnet = u.IPv4Mask.ToString();
                                    }
                                    break;
                                }
                            }
                        }

                        // 기본 게이트웨이 추출 (0.0.0.0 제외)
                        if (ipProps.GatewayAddresses != null)
                        {
                            foreach (GatewayIPAddressInformation g in ipProps.GatewayAddresses)
                            {
                                if (g.Address != null && g.Address.AddressFamily == AddressFamily.InterNetwork && g.Address.ToString() != "0.0.0.0")
                                {
                                    details.Gateway = g.Address.ToString();
                                    break;
                                }
                            }
                        }

                        // DNS 주소 목록 추출
                        if (ipProps.DnsAddresses != null)
                        {
                            List<string> dnsList = new List<string>();
                            foreach (System.Net.IPAddress d in ipProps.DnsAddresses)
                            {
                                if (d.AddressFamily == AddressFamily.InterNetwork && d.ToString() != "0.0.0.0")
                                {
                                    dnsList.Add(d.ToString());
                                }
                            }
                            if (dnsList.Count > 0)
                            {
                                details.Dns = string.Join(", ", dnsList.ToArray());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("GetActiveNetworkDetails 오류: " + ex.Message);
            }

            return details;
        }

        /// <summary>
        /// 주변 Wi-Fi 네트워크 목록 스캔 (CWE-78 방어: netsh 직접 실행)
        /// </summary>
        public List<string> ScanNearbyWifiAps()
        {
            List<string> apList = new List<string>();
            try
            {
                string output;
                if (SecurityHelper.ExecuteNetshDirect("wlan show networks mode=bssid", out output, 4000))
                {
                    string[] lines = output.Split('\n');
                    for (int i = 0; i < lines.Length && i < 500; i++)
                    {
                        string line = lines[i].Trim();
                        if (line.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) && line.Contains(":"))
                        {
                            int colonIdx = line.IndexOf(':');
                            string ssid = line.Substring(colonIdx + 1).Trim();
                            if (!string.IsNullOrEmpty(ssid) && !apList.Contains(ssid) && SecurityHelper.IsSafeSsid(ssid))
                            {
                                apList.Add(ssid);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("ScanNearbyWifiAps 예외: " + ex.Message);
            }
            return apList;
        }

        /// <summary>
        /// 지정한 SSID로 연결 후 저장된 프로필이 있으면 즉시 IP 할당
        /// </summary>
        public void ConnectToSsid(string ssid)
        {
            if (string.IsNullOrWhiteSpace(ssid) || !SecurityHelper.IsSafeSsid(ssid))
            {
                Log("연결 요청 거부 (유효하지 않은 SSID): " + ssid);
                return;
            }

            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    // 프로필 별칭이 전달된 경우 실제 무선 SSID 확인
                    string actualSsid = ssid;
                    ApProfile prof = FindSavedProfile(ssid);
                    if (prof != null && !string.IsNullOrEmpty(prof.OriginalSsid))
                    {
                        List<string> nearby = ScanNearbyWifiAps();
                        if (!nearby.Contains(ssid) && nearby.Contains(prof.OriginalSsid))
                        {
                            actualSsid = prof.OriginalSsid;
                        }
                    }

                    Log("Wi-Fi 연결 시도: " + actualSsid);
                    string escapedSsid = actualSsid.Replace("\"", "\\\"");
                    string args = string.Format("wlan connect name=\"{0}\"", escapedSsid);

                    string output;
                    SecurityHelper.ExecuteNetshDirect(args, out output, 5000);

                    // 즉시 자동 IP 할당이 동작하도록 쿨다운 초기화
                    this.lastAutoIpTime = DateTime.MinValue;
                    this.lastConnectedSsid = string.Empty;

                    Thread.Sleep(1800);
                    RefreshNetworkStatus();
                }
                catch (Exception ex)
                {
                    Log("ConnectToSsid 오류: " + ex.Message);
                }
            });
        }

        /// <summary>
        /// 고정 IP 적용 (CWE-78 명령어 삽입 방어: cmd.exe 미사용, netsh 직접 단독 실행)
        /// </summary>
        public void ApplyStaticIp(string ip, string gw, string dns, string subnet)
        {
            // CWE-20, CWE-807: 파라미터 엄격 검증
            if (!SecurityHelper.IsValidIpv4(ip))
            {
                Log("고정 IP 적용 거부 (유효하지 않은 IP): " + ip);
                return;
            }

            string safeIf = SecurityHelper.SanitizeInterfaceName(this.activeInterfaceName);
            string safeSubnet = SecurityHelper.IsValidSubnetMask(subnet) ? subnet.Trim() : "255.255.255.0";
            string safeGw = SecurityHelper.IsValidIpv4(gw) ? gw.Trim() : string.Empty;
            string safeDns = SecurityHelper.IsValidIpv4(dns) ? dns.Trim() : string.Empty;

            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    Log(string.Format("고정 IP 적용 시작: {0} -> IP:{1}, MASK:{2}, GW:{3}, DNS:{4}",
                        safeIf, ip, safeSubnet, safeGw, safeDns));

                    // 1. IP 및 게이트웨이 설정 명령
                    string ipArgs;
                    if (!string.IsNullOrEmpty(safeGw))
                    {
                        ipArgs = string.Format("interface ipv4 set address name=\"{0}\" static {1} {2} {3} 1",
                            safeIf, ip, safeSubnet, safeGw);
                    }
                    else
                    {
                        ipArgs = string.Format("interface ipv4 set address name=\"{0}\" static {1} {2}",
                            safeIf, ip, safeSubnet);
                    }
                    SecurityHelper.ExecuteNetshElevated(ipArgs, 8000);

                    // 2. DNS 설정 명령 (validate=no 우선 적용으로 빠른 설정)
                    if (!string.IsNullOrEmpty(safeDns))
                    {
                        string dnsArgs = string.Format("interface ipv4 set dns name=\"{0}\" static {1} primary validate=no",
                            safeIf, safeDns);
                        if (!SecurityHelper.ExecuteNetshElevated(dnsArgs, 5000))
                        {
                            string fallbackDnsArgs = string.Format("interface ipv4 set dns name=\"{0}\" static {1} primary",
                                safeIf, safeDns);
                            SecurityHelper.ExecuteNetshElevated(fallbackDnsArgs, 8000);
                        }
                    }

                    Thread.Sleep(1200);
                    RefreshNetworkStatus();
                }
                catch (Exception ex)
                {
                    Log("ApplyStaticIp 예외: " + ex.Message);
                }
            });
        }

        /// <summary>
        /// DHCP(자동 IP/DNS) 모드로 복원
        /// </summary>
        public void ApplyDhcp()
        {
            if (!string.IsNullOrEmpty(this.currentSsid) &&
                this.currentSsid != "확인 중..." &&
                this.currentSsid != "연결된 Wi-Fi 없음")
            {
                lock (this.profileLock)
                {
                    ApProfile existing = FindSavedProfile(this.currentSsid);
                    string key = existing != null ? existing.Ssid : this.currentSsid;
                    string orig = existing != null ? existing.OriginalSsid : this.currentSsid;
                    ApProfile profile = new ApProfile(key, "dhcp", "0.0.0.0", "255.255.255.0", "", "8.8.8.8", orig);
                    this.savedProfiles[key] = profile;
                }
                SaveProfiles();
                UpdateSavedApCount();
            }

            ApplyDhcpSilent();

            if (this.trayIcon != null)
            {
                try
                {
                    this.trayIcon.ShowBalloonTip(
                        1500,
                        "AutoNet",
                        "DHCP(자동 IP 및 DNS)로 복원 명령을 전송했습니다.",
                        ToolTipIcon.Info);
                }
                catch { }
            }
        }

        /// <summary>
        /// 조용히 DHCP로 전환 실행 (백그라운드 스레드)
        /// </summary>
        public void ApplyDhcpSilent()
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    string safeIf = SecurityHelper.SanitizeInterfaceName(this.activeInterfaceName);
                    Log("DHCP 전환 실행: " + safeIf);

                    // IP를 DHCP로 전환
                    SecurityHelper.ExecuteNetshElevated(
                        string.Format("interface ipv4 set address name=\"{0}\" source=dhcp", safeIf), 8000);

                    // DNS를 DHCP로 전환
                    SecurityHelper.ExecuteNetshElevated(
                        string.Format("interface ipv4 set dns name=\"{0}\" source=dhcp", safeIf), 8000);

                    Thread.Sleep(1500);
                    RefreshNetworkStatus();
                }
                catch (Exception ex)
                {
                    Log("ApplyDhcpSilent 예외: " + ex.Message);
                }
            });
        }

        /// <summary>
        /// 프로필 파일 로드 (CWE-22 경로 조작 방어, CWE-367 동시성 보호, 입력값 검증)
        /// </summary>
        private void LoadProfiles()
        {
            lock (this.profileLock)
            {
                this.savedProfiles.Clear();

                if (!File.Exists(this.configPath))
                    return;

                try
                {
                    // CWE-404: using 문을 통한 자원 해제 보장
                    using (FileStream fs = new FileStream(this.configPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
                    {
                        string line;
                        int count = 0;
                        while ((line = reader.ReadLine()) != null && count++ < 1000)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            string[] parts = line.Split('|');
                            if (parts.Length < 3) continue;

                            string ssid = parts[0].Trim();
                            if (!SecurityHelper.IsSafeSsid(ssid)) continue;

                            string mode = parts[1].Trim().ToLowerInvariant();
                            string ip = parts[2].Trim();
                            string subnet = (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3])) ? parts[3].Trim() : "255.255.255.0";
                            string gw = (parts.Length > 4) ? parts[4].Trim() : "";
                            string dns = (parts.Length > 5 && !string.IsNullOrWhiteSpace(parts[5])) ? parts[5].Trim() : "8.8.8.8";
                            string origSsid = (parts.Length > 6 && !string.IsNullOrWhiteSpace(parts[6]) && SecurityHelper.IsSafeSsid(parts[6].Trim()))
                                ? parts[6].Trim()
                                : ssid;

                            ApProfile profile = new ApProfile(ssid, mode, ip, subnet, gw, dns, origSsid);
                            this.savedProfiles[ssid] = profile;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("LoadProfiles 오류: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// 프로필 파일 저장 (CWE-367 동시성 락, 원자적 저장 기법)
        /// </summary>
        private void SaveProfiles()
        {
            lock (this.profileLock)
            {
                try
                {
                    List<string> lines = new List<string>();
                    foreach (KeyValuePair<string, ApProfile> kvp in this.savedProfiles)
                    {
                        ApProfile p = kvp.Value;
                        string line = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}",
                            p.Ssid, p.Mode, p.Ip, p.Subnet, p.Gateway, p.Dns, p.OriginalSsid);
                        lines.Add(line);
                    }

                    // CWE-404: 파일 쓰기 자원 안전 해제
                    using (FileStream fs = new FileStream(this.configPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
                    {
                        foreach (string l in lines)
                        {
                            writer.WriteLine(l);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("SaveProfiles 오류: " + ex.Message);
                }
            }
        }

        private void UpdateSavedApCount()
        {
            SafeInvoke(() =>
            {
                int count;
                lock (this.profileLock)
                {
                    count = this.savedProfiles.Count;
                }
                if (this.btnHistory != null)
                {
                    this.btnHistory.Text = string.Format(" 저장된 AP 프로필 관리 ({0}개)", count);
                }
            });
        }
    }
}
