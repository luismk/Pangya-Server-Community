using System;
using System.Data;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdUpdateRateConfigInfo : Pangya_DB
    {
        int m_server_uid = -1;
        RateConfigInfo m_rci;

        public CmdUpdateRateConfigInfo(int _uid, RateConfigInfo _rate)
        {
            m_server_uid = _uid;
            m_rci = _rate;
        }
        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            //somente update!
        }

        protected override Response prepareConsulta()
        {

            if (m_server_uid == -1)
                throw new Exception("[CmdUpdateRateConfigInfo][Error] server_uid[VALUE=" + (m_server_uid) + "] is invalid.");


            var r = procedure("pangya.ProcUpdateRateConfigInfo", (m_server_uid) + ", " + (m_rci.grand_zodiac_event_time)
                + ", " + (m_rci.scratchy) + ", " + (m_rci.papel_shop_rare_item)
                + ", " + (m_rci.papel_shop_cookie_item) + ", " + (m_rci.treasure)
                + ", " + (m_rci.pang) + ", " + (m_rci.exp) + ", " + (m_rci.club_mastery)
                + ", " + (m_rci.chuva) + ", " + (m_rci.memorial_shop)
                + ", " + (m_rci.angel_event) + ", " + (m_rci.grand_prix_event)
                + ", " + (m_rci.golden_time_event) + ", " + (m_rci.login_reward_event)
                + ", " + (m_rci.bot_gm_event) + ", " + (m_rci.smart_calculator)
    );

            checkResponse(r, "nao conseguiu atualizar o Rate Config Info[SERVER_UID=" + (m_server_uid) + ", " + m_rci.ToString() + "]");
            return r;
        }

        public RateConfigInfo GetInfo()
        {
            return this.m_rci;
        }

        public int getServerUID()
        {
            return this.m_server_uid;
        }
    }
}
