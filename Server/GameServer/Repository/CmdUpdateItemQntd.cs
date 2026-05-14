using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdUpdateItemQntd : Pangya_DB
    {
        public CmdUpdateItemQntd()
        {
            this.m_uid = 0;
            this.m_id = -1;
            this.m_qntd = 0;
        }

        public CmdUpdateItemQntd(uint _uid,
            int _id, int _qntd)
        {
            this.m_uid = _uid;
            this.m_id = _id;
            this.m_qntd = _qntd;
        }
        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;

        }

        public int getID()
        {
            return (m_id);
        }

        public void setID(int _id)
        {
            m_id = _id;
        }

        public int getQntd()
        {
            return m_qntd;
        }

        public void setQntd(int _qntd)
        {
            m_qntd = _qntd;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
        }

        protected override Response prepareConsulta()
        {

            if (m_id <= 0)
            {
                throw new exception("[CmdUpdateItemQntd::prepareConsulta][Error] Item id[value=" + Convert.ToString(m_id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = _update(m_szConsulta[0] + Convert.ToString(m_qntd) + m_szConsulta[1] + Convert.ToString(m_uid) + m_szConsulta[2] + Convert.ToString(m_id));

            checkResponse(r, "nao consiguiu atualizar quantidade[value=" + Convert.ToString(m_qntd) + "] do Item[ID=" + Convert.ToString(m_id) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private int m_id = new int();
        private int m_qntd = new int();

        private string[] m_szConsulta = { "UPDATE pangya.pangya_item_warehouse SET C0 = ", " WHERE UID = ", " AND item_id = " };
    }
}
