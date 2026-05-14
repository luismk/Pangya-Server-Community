using System;
using Pangya_GameServer.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdAddBall : CmdAddItemBase
    {
        public CmdAddBall(uint _uid,
            WarehouseItemEx _wi,
            byte _purchase,
            byte _gift_flag

            ) : base(_uid,
                _purchase, _gift_flag)
        {
            this.m_uid = _uid;
            this.m_purchase = _purchase;
            this.m_gift_flag = _gift_flag;
            this.m_wi = _wi;
        }


        public WarehouseItemEx getInfo()
        {
            return m_wi;
        }

        public void setInfo(WarehouseItemEx _wi)
        {
            m_wi = _wi;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1);

            m_wi.id = IFNULL<int>(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {

            if (m_wi._typeid == 0)
            {
                throw new exception("[CmdAddBall::prepareConsulta][Error] ball is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString((ushort)m_gift_flag) + ", " + Convert.ToString((ushort)m_purchase) + ", " + Convert.ToString(m_wi.id) + ", " + Convert.ToString(m_wi._typeid) + ", " + Convert.ToString((ushort)m_wi.flag) + ", " + Convert.ToString(m_wi.c[3]) + ", " + Convert.ToString(m_wi.c[0]) + ", " + Convert.ToString(m_wi.c[1]) + ", " + Convert.ToString(m_wi.c[2]) + ", " + Convert.ToString(m_wi.c[3]) + ", " + Convert.ToString(m_wi.c[4]));

            checkResponse(r, "nao conseguiu adicionar Ball[TYPEID=" + Convert.ToString(m_wi._typeid) + "] para o PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }
        private WarehouseItemEx m_wi = new WarehouseItemEx();

        private const string m_szConsulta = "pangya.ProcAddBall";
    }
}
