using System;

namespace AutoNetSwitcher
{
    /// <summary>
    /// 현재 활성 네트워크 인터페이스의 세부 정보 모델
    /// </summary>
    public class NetworkDetails
    {
        private string _ip = "0.0.0.0";
        private string _subnet = "255.255.255.0";
        private string _gateway = "-";
        private string _dns = "-";

        public string Ip
        {
            get { return _ip; }
            set { _ip = string.IsNullOrWhiteSpace(value) ? "0.0.0.0" : value.Trim(); }
        }

        public string Subnet
        {
            get { return _subnet; }
            set { _subnet = string.IsNullOrWhiteSpace(value) ? "255.255.255.0" : value.Trim(); }
        }

        public string Gateway
        {
            get { return _gateway; }
            set { _gateway = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim(); }
        }

        public string Dns
        {
            get { return _dns; }
            set { _dns = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim(); }
        }

        public NetworkDetails()
        {
        }

        public NetworkDetails(string ip, string subnet, string gateway, string dns)
        {
            this.Ip = ip;
            this.Subnet = subnet;
            this.Gateway = gateway;
            this.Dns = dns;
        }

        public NetworkDetails Clone()
        {
            return new NetworkDetails(_ip, _subnet, _gateway, _dns);
        }
    }
}
