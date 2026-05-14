using System;
using System.Collections.Generic;
using Pangya_GameServer.Models.golden_time_type;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdGoldenTimeItem : Pangya_DB
    {
        public CmdGoldenTimeItem(uint _id, bool _waiter = false) : base(_waiter)
        {
            this.m_id = _id;
            this.m_item = new List<stItemReward>();
        }
        public List<stItemReward> getInfo()
        {
            return new List<stItemReward>(m_item);
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(4, _result.cols);

            stItemReward item = new stItemReward() { _typeid = 0u };


            item._typeid = (uint)IFNULL(_result.data[0]);

            item.qntd = (uint)IFNULL(_result.data[1]);

            item.qntd_time = (uint)IFNULL(_result.data[2]);

            item.rate = (uint)IFNULL(_result.data[3]);

            m_item.Add(item);
        }

        protected override Response prepareConsulta()
        {

            if (m_id == 0u)
            {
                throw new exception("[CmdGoldenTimeItem::prepareConsulta][Error] m_id is invalid(" + Convert.ToString(m_id) + ")", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_item.Count > 0)
            {
                m_item.Clear();
            }

            var r = consulta(
                m_szConsulta + m_id);

            checkResponse(r, "nao conseguiu pegar o Golden Time Info");

            return r;
        }


        private uint m_id = new uint();

        private List<stItemReward> m_item = new List<stItemReward>();


        private const string m_szConsulta = "SELECT typeid, qntd, qntd_time, rate FROM pangya.pangya_golden_time_item WHERE golden_time_id = ";
    }
}