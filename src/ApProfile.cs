using System;

namespace AutoNetSwitcher
{
    /// <summary>
    /// Wi-Fi AP별 네트워크 프로필 정보 (캡슐화 및 유효성 검증 적용)
    /// </summary>
    public class ApProfile
    {
        private string _ssid = string.Empty;
        private string _originalSsid = string.Empty;
        private string _mode = "dhcp";
        private string _ip = "0.0.0.0";
        private string _subnet = "255.255.255.0";
        private string _gateway = string.Empty;
        private string _dns = "8.8.8.8";

        public string Ssid
        {
            get { return _ssid; }
            set { _ssid = (value ?? string.Empty).Trim(); }
        }

        public string OriginalSsid
        {
            get { return string.IsNullOrEmpty(_originalSsid) ? _ssid : _originalSsid; }
            set { _originalSsid = (value ?? string.Empty).Trim(); }
        }

        public string Mode
        {
            get { return _mode; }
            set
            {
                string lower = (value ?? string.Empty).Trim().ToLowerInvariant();
                _mode = (lower == "static") ? "static" : "dhcp";
            }
        }

        public string Ip
        {
            get { return _ip; }
            set { _ip = (value ?? string.Empty).Trim(); }
        }

        public string Subnet
        {
            get { return _subnet; }
            set { _subnet = string.IsNullOrWhiteSpace(value) ? "255.255.255.0" : value.Trim(); }
        }

        public string Gateway
        {
            get { return _gateway; }
            set { _gateway = (value ?? string.Empty).Trim(); }
        }

        public string Dns
        {
            get { return _dns; }
            set { _dns = string.IsNullOrWhiteSpace(value) ? "8.8.8.8" : value.Trim(); }
        }

        public ApProfile()
        {
        }

        public ApProfile(string ssid, string mode, string ip, string subnet, string gateway, string dns, string originalSsid = null)
        {
            this.Ssid = ssid;
            this.OriginalSsid = string.IsNullOrWhiteSpace(originalSsid) ? ssid : originalSsid;
            this.Mode = mode;
            this.Ip = ip;
            this.Subnet = subnet;
            this.Gateway = gateway;
            this.Dns = dns;
        }

        public bool MatchesSsid(string targetSsid)
        {
            if (string.IsNullOrWhiteSpace(targetSsid)) return false;
            string t = targetSsid.Trim();
            return string.Equals(_ssid, t, StringComparison.OrdinalIgnoreCase) ||
                   (!string.IsNullOrEmpty(_originalSsid) && string.Equals(_originalSsid, t, StringComparison.OrdinalIgnoreCase));
        }

        public ApProfile Clone()
        {
            return new ApProfile(_ssid, _mode, _ip, _subnet, _gateway, _dns, _originalSsid);
        }
    }
}
