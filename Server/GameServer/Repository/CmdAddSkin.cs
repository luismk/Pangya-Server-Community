//Convertion By LuisMK
using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    internal class CmdAddSkin : Pangya_DB
    {
        public CmdAddSkin(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_gift_flag = 0;
            this.m_purchase = 0;
            this.m_wi = new WarehouseItemEx();
        }

        public CmdAddSkin(uint _uid,
            WarehouseItemEx _wi,
            byte _purchase,
            byte _gift_flag,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_gift_flag = _gift_flag;
            this.m_purchase = _purchase;
            this.m_wi = (_wi);
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public WarehouseItemEx getInfo()
        {
            return m_wi;
        }

        public void setInfo(WarehouseItemEx _wi)
        {
            m_wi = _wi;
        }

        public byte getGiftFlag()
        {
            return m_gift_flag;
        }

        public void setGiftFlag(byte _gift_flag)
        {
            m_gift_flag = _gift_flag;
        }

        public byte getPurchase()
        {
            return m_purchase;
        }

        public void setPurchase(byte _purchase)
        {
            m_purchase = _purchase;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1, (uint)_result.cols);

            m_wi.id = IFNULL<int>(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {

            if (m_wi._typeid == 0)
            {
                throw new exception("[CmdAddSkin::prepareConsulta][Error] Skin is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_wi._typeid) + ", " + Convert.ToString((ushort)m_wi.flag) + ", " + Convert.ToString((ushort)m_gift_flag) + ", " + Convert.ToString((ushort)m_purchase) + ", " + Convert.ToString(m_wi.c[3]));

            checkResponse(r, "nao conseguiu adicionar Skin[TYPEID=" + Convert.ToString(m_wi._typeid) + "] para o PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private byte m_purchase;
        private byte m_gift_flag;
        private WarehouseItemEx m_wi = new WarehouseItemEx();

        private const string m_szConsulta = "pangya.ProcInsertSkin";

    }
}