using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateCounterItem : Pangya_DB
    { 
        public CmdUpdateCounterItem(uint _uid,
            CounterItemInfo _cii)
        {

            this.m_uid = _uid;
            //this.
            this.m_cii = (_cii);
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {

            m_uid = _uid;

        }

        public CounterItemInfo getInfo()
        {
            return m_cii;
        }

        public void setInfo(CounterItemInfo _cii)
        {

            m_cii = _cii;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o aqui por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdUpdateCounterItem::prepareConsulta][Error] m_uid is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_cii.id <= 0 || m_cii._typeid == 0)
            {
                throw new exception("[CmdUpdateCounterItem::prepareConsulta][Error] CounterItemInfo m_cii is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var _str = m_szConsulta[0] + Convert.ToString(m_cii.value) + m_szConsulta[1] + Convert.ToString(m_uid) + m_szConsulta[2] + Convert.ToString(m_cii.id);
            var r = _update(_str);

            checkResponse(r, "nao conseguiu atualizar o Counter Item[ID=" + Convert.ToString(m_cii.id) + "] do player: " + Convert.ToString(m_uid));

            return r;
        }

        private uint m_uid = new uint();
        private CounterItemInfo m_cii = new CounterItemInfo();

        private string[] m_szConsulta = { "UPDATE pangya.pangya_counter_item SET count_num_item = ", " WHERE UID = ", " AND count_id = " };
    }
}
