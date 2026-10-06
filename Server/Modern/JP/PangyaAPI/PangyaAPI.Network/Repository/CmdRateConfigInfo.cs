using System;
using PangyaAPI.Network.Models;
using PangyaAPI.DataBase;

namespace PangyaAPI.Network.Repository
{
    public class CmdRateConfigInfo : Pangya_DB
    {


        int m_server_uid = -1;
        bool m_error = false;
        ServerRateInfo m_rate_info;
        public CmdRateConfigInfo(int _uid)
        {
            m_server_uid = _uid;
            m_rate_info = new ServerRateInfo();
        }
        public CmdRateConfigInfo()
        {
            m_rate_info = new ServerRateInfo();
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(16);
            try
            {
                if (short.Parse(_result.data[0].ToString()) == -1)
                    m_error = true; // Error pode ser uma um server novo que tem que criar ou passou argumentos errados para a procedure
                else
                {

                    m_rate_info.GrandZodiacEventTime = short.Parse(_result.data[0].ToString());
                    m_rate_info.Scratchy = short.Parse(_result.data[1].ToString());
                    m_rate_info.PapelShopRareItem = short.Parse(_result.data[2].ToString());
                    m_rate_info.PapelShopCookieItem = short.Parse(_result.data[3].ToString());
                    m_rate_info.Treasure = short.Parse(_result.data[4].ToString());
                    m_rate_info.Pang = short.Parse(_result.data[5].ToString());
                    m_rate_info.Experience = short.Parse(_result.data[6].ToString());
                    m_rate_info.ClubMastery = short.Parse(_result.data[7].ToString());
                    m_rate_info.Rain = short.Parse(_result.data[8].ToString());
                    m_rate_info.MemorialShop = short.Parse(_result.data[9].ToString());
                    m_rate_info.AngelEvent = short.Parse(_result.data[10].ToString());
                    m_rate_info.GrandPrixEvent = short.Parse(_result.data[11].ToString());
                    m_rate_info.GoldenTimeEvent = short.Parse(_result.data[12].ToString());
                    m_rate_info.LoginRewardEvent = short.Parse(_result.data[13].ToString());
                    m_rate_info.GMEventBot = short.Parse(_result.data[14].ToString());
                    m_rate_info.SmartCalculation = short.Parse(_result.data[15].ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        protected override Response prepareConsulta()
        {
            var r = procedure("pangya.ProcGetRateConfigInfo", m_server_uid.ToString());

            checkResponse(r, "nao conseguiu pegar o Rate Config Info do Server[UID=" + (m_server_uid) + "].");
            return r;
        }

        public ServerRateInfo getInfo()
        {
            return this.m_rate_info;
        }


        public bool isError()
        {
            return m_error;
        }
    }
}
