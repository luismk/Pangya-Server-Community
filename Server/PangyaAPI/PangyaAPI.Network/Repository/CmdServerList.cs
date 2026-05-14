using System;
using System.Collections.Generic;
using System.Linq;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public enum TYPE_SERVER : byte
    {
        GAME,
        MSN,
        LOGIN,
        RANK,
        AUTH,
    }
    public class CmdServerList : Pangya_DB
    {
        TYPE_SERVER m_type;
        List<ServerInfo> v_server_list;

        public CmdServerList(TYPE_SERVER _type)
        {
            v_server_list = new List<ServerInfo>();
            m_type = _type;
        }

        public CmdServerList()
        {
            v_server_list = new List<ServerInfo>();
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(13, _result.cols);//melhorar depois
            try
            {
                ServerInfo si = new ServerInfo();

                if (!string.IsNullOrEmpty(_result.data[0].ToString()))
                    si.nome = (_result.data[0].ToString());
                si.uid = int.Parse(_result.data[1].ToString());
                if (!string.IsNullOrEmpty(_result.data[2].ToString()))
                    si.ip = _result.data[2].ToString();
                si.port = int.Parse(_result.data[3].ToString());
                si.max_user = int.Parse(_result.data[4].ToString());
                si.curr_user = int.Parse(_result.data[5].ToString());
                si.propriedade = new uProperty(uint.Parse(_result.data[6].ToString()));
                si.angelic_wings_num = int.Parse(_result.data[7].ToString());
                si.event_flag = new uEventFlag(ushort.Parse(_result.data[8].ToString()));
                si.event_map = short.Parse(_result.data[9].ToString());
                si.img_no = short.Parse(_result.data[10].ToString());
                si.app_rate = short.Parse(_result.data[11].ToString());
                si.scratch_rate = short.Parse(_result.data[12].ToString());    // Estava o rate_scratchy mas realoquei ele para o ServerInfoEx::Rate
                if (!v_server_list.Any(c => c.uid == si.uid))
                    v_server_list.Add(si);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            v_server_list.Clear();

            var @tipo = Convert.ToByte(m_type);
            var r = procedure("pangya.ProcGetServerList", tipo.ToString());

            checkResponse(r, "nao conseguiu pegar o server list");
            return r;
        }

        public List<ServerInfo> getServerList()
        {
            return this.v_server_list;
        }


        public TYPE_SERVER getType()
        {
            return m_type;
        }

        public void setType(TYPE_SERVER _type)
        {
            m_type = _type;
        }
    }
}
