using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using PangyaAPI.Utilities;

namespace PangyaAPI.Network.PangyaUtil
{
    public class AntiDdosConfig
    {
        public bool EnableIpRules { get; set; } = true;
        public int LimitConnectionPerIp { get; set; } = 10;
        public string Order { get; set; } = "deny,allow";
        public List<string> Allow { get; set; } = new List<string>();
        public List<string> Deny { get; set; } = new List<string>();
        public int DdosInterval { get; set; } = 3000;
        public int DdosCount { get; set; } = 5;
        public int DdosAutoReset { get; set; } = 3000;
    }
    public class IpDdosFilter
    {
        private readonly AntiDdosConfig config;
        private readonly Dictionary<string, List<DateTime>> ipLog = new Dictionary<string, List<DateTime>>();
        private readonly Dictionary<string, int> ipConnCount = new Dictionary<string, int>();
        private readonly Dictionary<string, DateTime> blockedUntil = new Dictionary<string, DateTime>();

        public IpDdosFilter()
        {
            var ini = new IniHandle("config/socket_config.ini");
            config = new AntiDdosConfig();
            try
            {
                config.EnableIpRules = ini.ReadInt32("IPRULES", "enable_ip_rules", 1) == 1;
                config.LimitConnectionPerIp = ini.ReadInt32("IPRULES", "limit_connection_per_ip", 10);
                config.Order = ini.ReadString("IPRULES", "order", "deny,allow");
                config.DdosInterval = ini.ReadInt32("DDOS", "ddos_interval", 3000);
                config.DdosCount = ini.ReadInt32("DDOS", "ddos_count", 5);
                config.DdosAutoReset = ini.ReadInt32("DDOS", "ddos_autoreset", 600000);
                config.Allow = ini.ReadAllValues("IPRULES", "allow");
                config.Deny = ini.ReadAllValues("IPRULES", "deny");
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public bool IsBlocked(string ip)
        {
            if (!config.EnableIpRules)
                return false;

            if (blockedUntil.TryGetValue(ip, out var until))
            {
                if (DateTime.UtcNow < until)
                    return true;
                blockedUntil.Remove(ip);
            }

            if (!CheckOrder(ip))
                return true;

            if (!CheckDdos(ip))
                return true;

            return false;
        }

        public bool setBlocked(string ip)
        {
            if (!config.EnableIpRules)
                return false;

            blockedUntil[ip] = DateTime.Now.AddMilliseconds(config.DdosAutoReset);

            return false;
        }

        public void OnConnect(string ip)
        {
            if (!ipConnCount.ContainsKey(ip))
                ipConnCount[ip] = 0;
            ipConnCount[ip]++;
        }

        public void OnDisconnect(string ip)
        {
            if (ipConnCount.ContainsKey(ip))
            {
                ipConnCount[ip] = Math.Max(0, ipConnCount[ip] - 1);
                if (ipConnCount[ip] == 0)
                    ipConnCount.Remove(ip);
            }
        }

        private bool CheckOrder(string ip)
        {
            bool allow = config.Allow.Any(r => IpMatches(ip, r));
            bool deny = config.Deny.Any(r => IpMatches(ip, r));

            bool result;

            switch (config.Order)
            {
                case "deny,allow":
                    result = !deny || allow;
                    break;

                case "allow,deny":
                    result = allow && !deny;
                    break;

                case "mutual-failure":
                    result = allow && !deny;
                    break;

                default:
                    result = true;
                    break;
            }

            return result;

        }

        private bool IpMatches(string ip, string rule)
        {
            if (rule == "all") return true;
            if (rule.Contains('/'))
            {
                var parts = rule.Split('/');
                return (ToUInt(ip) & ToUInt(parts[0])) == (ToUInt(parts[1]) & ToUInt(parts[1]));
            }
            return ip == rule;
        }

        private uint ToUInt(string ip)
        {
            return BitConverter.ToUInt32(IPAddress.Parse(ip).GetAddressBytes().Reverse().ToArray(), 0);
        }

        private bool CheckDdos(string ip)
        {
            var now = DateTime.UtcNow;

            if (!ipLog.TryGetValue(ip, out var list))
                ipLog[ip] = list = new List<DateTime>();

            list.Add(now);
            list.RemoveAll(t => (now - t).TotalMilliseconds > config.DdosInterval);

            if (list.Count >= config.DdosCount)
            {
                blockedUntil[ip] = now.AddMilliseconds(config.DdosAutoReset);
                return false;
            }

            if (ipConnCount.ContainsKey(ip) && ipConnCount[ip] > config.LimitConnectionPerIp)
                return false;

            return true;
        }
    }

}
