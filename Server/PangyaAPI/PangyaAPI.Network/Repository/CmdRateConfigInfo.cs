using System;
using PangyaAPI.Network.Models;
using PangyaAPI.SQL;

namespace PangyaAPI.Network.Repository
{
    public class CmdRateConfigInfo : Pangya_DB
    {


        int m_server_uid = -1;
        bool m_error = false;
        RateConfigInfo m_rate_info;
        public CmdRateConfigInfo(int _uid)
        {
            m_server_uid = _uid;
            m_rate_info = new RateConfigInfo();
        }
        public CmdRateConfigInfo()
        {
            m_rate_info = new RateConfigInfo();
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

                    m_rate_info.grand_zodiac_event_time = short.Parse(_result.data[0].ToString());
                    m_rate_info.scratchy = short.Parse(_result.data[1].ToString());
                    m_rate_info.papel_shop_rare_item = short.Parse(_result.data[2].ToString());
                    m_rate_info.papel_shop_cookie_item = short.Parse(_result.data[3].ToString());
                    m_rate_info.treasure = short.Parse(_result.data[4].ToString());
                    m_rate_info.pang = short.Parse(_result.data[5].ToString());
                    m_rate_info.exp = short.Parse(_result.data[6].ToString());
                    m_rate_info.club_mastery = short.Parse(_result.data[7].ToString());
                    m_rate_info.chuva = short.Parse(_result.data[8].ToString());
                    m_rate_info.memorial_shop = short.Parse(_result.data[9].ToString());
                    m_rate_info.angel_event = short.Parse(_result.data[10].ToString());
                    m_rate_info.grand_prix_event = short.Parse(_result.data[11].ToString());
                    m_rate_info.golden_time_event = short.Parse(_result.data[12].ToString());
                    m_rate_info.login_reward_event = short.Parse(_result.data[13].ToString());
                    m_rate_info.bot_gm_event = short.Parse(_result.data[14].ToString());
                    m_rate_info.smart_calculator = short.Parse(_result.data[15].ToString());
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

        public RateConfigInfo getInfo()
        {
            return this.m_rate_info;
        }


        public bool isError()
        {
            return m_error;
        }
    }
}
