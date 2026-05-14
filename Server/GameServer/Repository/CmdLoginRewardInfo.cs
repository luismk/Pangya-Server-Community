using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace Pangya_GameServer.Repository
{
    public class CmdLoginRewardInfo : Pangya_DB
    {
        public CmdLoginRewardInfo()
        {
            this.m_lr = new List<stLoginReward>();
        }

        public List<stLoginReward> getInfo()
        {
            return new List<stLoginReward>(m_lr);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(10, (uint)_result.cols);

            stLoginReward.stItemReward item = new stLoginReward.stItemReward((uint)IFNULL(_result.data[5]),
            (uint)IFNULL(_result.data[6]),
                (uint)IFNULL(_result.data[7]));

            SYSTEMTIME end_date = new SYSTEMTIME();

            if (_result.data[9] != null)
                end_date.CreateTime(_translateDate(_result.data[9]));


            m_lr.Add(new stLoginReward((uint)IFNULL(_result.data[0]),
                (stLoginReward.eTYPE)(byte)IFNULL(_result.data[2]),
               IFNULL<string>(_result.data[1]),
                (uint)IFNULL(_result.data[3]),
                (uint)IFNULL(_result.data[4]),
                item, end_date,
                (IFNULL<bool>(_result.data[8]) ? true : false)));
        }

        protected override Response prepareConsulta()
        {

            if (m_lr.Count > 0)
            {
                m_lr.Clear();
            }

           string m_szConsulta = "SELECT " + makeEscapeKeyword("index") + ", " + makeEscapeKeyword("name") + ", " + makeEscapeKeyword("type") + ", days_to_gift, n_times_gift, item_typeid, item_qntd, item_qntd_time, is_end, end_date FROM pangya.pangya_login_reward WHERE is_end = 0";
            
            var r = consulta(m_szConsulta);

            checkResponse(r, "nao conseguiu pegar o Login Reward Info");

            return r;
        }

        private List<stLoginReward> m_lr = new List<stLoginReward>();
    }
}