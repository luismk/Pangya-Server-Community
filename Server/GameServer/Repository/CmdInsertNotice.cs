using System;
using System.Data;
using PangyaAPI.SQL;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertNotice : Pangya_DB
    {
        public CmdInsertNotice()
        {
            this.m_msg = "";
            this.replay_count_in = 1;
            this.refresh_time_min_in = 1;
            this.target_in = 1;
            this.reserveDate_in = DateTime.Now;
        }
        public CmdInsertNotice(string msg)
        {
            m_msg = msg;
        }
        public CmdInsertNotice(string msg,
            uint replay_count,
            uint refresh_time)
        {
            m_msg = msg;
            replay_count_in = replay_count;
            refresh_time_min_in = refresh_time;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N o usa por que   um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {
            if (target_in == 0)
            {
                target_in = 1; //antes coloca o uid do server, agora coloco o tipo, que é o tipo 1 game server
            }
            else if (refresh_time_min_in == 0)
            {
                refresh_time_min_in = 5; //daqui uns 5 minutos repete novamente o comando
            }
            else if (replay_count_in == 0)
            {
                replay_count_in = 1; //aqui e um contador de replay da mensagem
            }
            reserveDate_in = DateTime.Now;

            var str_date = makeText(_formatDate(reserveDate_in));
            var r = procedure(m_szConsulta, makeText(m_msg) + ", " +  replay_count_in + ", " +  refresh_time_min_in + ", " +  target_in + ", " +  str_date);

            checkResponse(r, "nao conseguiu adicionar um Notice[MESSAGE=" + m_msg + "] para o server[UID=" + Convert.ToString(target_in) + "]");

            return r;
        }
        public uint getServerUID()
        {
            return target_in;
        }
        public void setServerUID(uint _server_uid)
        {
            target_in = _server_uid;
        }
        public string getMessage()
        {
            return m_msg;
        }
        public void setMessage(string _msg)
        {
            m_msg = _msg;
        }
        private string m_msg = "";
        private uint replay_count_in;
        private uint refresh_time_min_in;
        private uint target_in;
        private DateTime reserveDate_in = DateTime.Now;
        private string m_szConsulta = "pangya.ProcRegisterNoticeBroadcast";
    }
}