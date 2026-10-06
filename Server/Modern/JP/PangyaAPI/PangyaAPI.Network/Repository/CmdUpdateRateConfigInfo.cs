using System;
using System.Data;
using PangyaAPI.Network.Models;
using PangyaAPI.DataBase;

namespace PangyaAPI.Network.Repository
{
    public class CmdUpdateRateConfigInfo : Pangya_DB
    {
        int m_server_uid = -1;
        ServerRateInfo m_rci;

        public CmdUpdateRateConfigInfo(int _uid, ServerRateInfo _rate)
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
                throw new Exception("[CmdUpdateRateConfigInfo][Error] ServerIndex[VALUE=" + (m_server_uid) + "] is invalid.");


            var r = procedure("pangya.ProcUpdateRateConfigInfo", (m_server_uid) + ", " + (m_rci.GrandZodiacEventTime)
                + ", " + (m_rci.Scratchy) + ", " + (m_rci.PapelShopRareItem)
                + ", " + (m_rci.PapelShopCookieItem) + ", " + (m_rci.Treasure)
                + ", " + (m_rci.Pang) + ", " + (m_rci.Experience) + ", " + (m_rci.ClubMastery)
                + ", " + (m_rci.Rain) + ", " + (m_rci.MemorialShop)
                + ", " + (m_rci.AngelEvent) + ", " + (m_rci.GrandPrixEvent)
                + ", " + (m_rci.GoldenTimeEvent) + ", " + (m_rci.LoginRewardEvent)
                + ", " + (m_rci.GMEventBot) + ", " + (m_rci.SmartCalculation)
    );

            checkResponse(r, "nao conseguiu atualizar o Rate Config Info[SERVER_UID=" + (m_server_uid) + ", " + m_rci.ToString() + "]");
            return r;
        }

        public ServerRateInfo GetInfo()
        {
            return this.m_rci;
        }

        public int getServerUID()
        {
            return this.m_server_uid;
        }
    }
}
