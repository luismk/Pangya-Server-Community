
using System;
using System.Collections.Generic;
using System.Net;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdListIpBan : Pangya_DB
    {
        List<IPBan> v_list_ip_ban;
        public CmdListIpBan()
        {
            v_list_ip_ban = new List<IPBan>();
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(2);
            try
            {
                IPBan pb = new IPBan();
                string ip = "", mask = "";
                int offset = -1;
                bool error = false;

                if (_result.IsNotNull(0))
                {
                    ip = _result.data[0].ToString();
                }
                if (_result.IsNotNull(1))
                {
                    mask = _result.data[1].ToString();
                }

                // Verifica se é um IP RANGE
                if ((offset = ip.IndexOf('/')) == -1 && ip.Length > 15)
                {
                    return;
                }
                else
                {
                    // Range IP
                    if (offset != -1)
                    {
                        pb.type = IPBan._TYPE.IP_BLOCK_NORMAL;

                        mask = ip.Substring(offset + 1);
                        ip = ip.Substring(0, offset);

                        error = IPAddress.TryParse(ip, out IPAddress ipAddr);
                        pb.ip = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(ipAddr.GetAddressBytes(), 0));

                        error = IPAddress.TryParse(mask, out IPAddress maskAddr);
                        pb.mask = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(maskAddr.GetAddressBytes(), 0));
                    }
                    else
                    {
                        // IP Normal
                        error = IPAddress.TryParse(ip, out IPAddress ipAddr);
                        pb.ip = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(ipAddr.GetAddressBytes(), 0));

                        error = IPAddress.TryParse(mask, out IPAddress maskAddr);
                        pb.mask = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(maskAddr.GetAddressBytes(), 0));
                    }
                }

                // Error 1 Success tem um ip e mask válida
                if (error)
                {
                    v_list_ip_ban.Add(pb);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            v_list_ip_ban.Clear();

            var r = consulta("SELECT ip, mask FROM pangya.pangya_ip_table");

            checkResponse(r, "nao conseguiu recuperar a lista de MAC Address");
            return r;
        }


        public List<IPBan> getListIPBan()
        {
            return v_list_ip_ban;
        }

    }
}
