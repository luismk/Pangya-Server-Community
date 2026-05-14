//Convertion By LuisMK
using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdAddPart : Pangya_DB
    {
        public CmdAddPart()
        {
            this.m_uid = 0;
            this.m_purchase = 0;
            this.m_gift_flag = 0;
            this.m_type_iff = 0;
            this.m_wi = new WarehouseItemEx();
        }

        public CmdAddPart(uint _uid,
            WarehouseItemEx _wi,
            byte _purchase,
            byte _gift_flag,
            byte _type_iff)
        {
            this.m_uid = _uid;
            this.m_purchase = _purchase;
            this.m_gift_flag = _gift_flag;
            this.m_type_iff = _type_iff;
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

        public byte getPurchase()
        {
            return m_purchase;
        }

        public void setPurchase(byte _purchase)
        {
            m_purchase = _purchase;
        }

        public byte getGiftFlag()
        {
            return m_gift_flag;
        }

        public void setGiftFlag(byte _gift_flag)
        {
            m_gift_flag = _gift_flag;
        }

        public byte getTypeIFF()
        {
            return m_type_iff;
        }

        public void setTypeIFF(byte _type_iff)
        {
            m_type_iff = _type_iff;
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

            if (_index_result == 0
                && (m_type_iff == 8 || m_type_iff == 9)
                && _result.cols == 3)
            { // Part UCC [Self Design]

                m_wi.id = (int)IFNULL(_result.data[0]);

                if (is_valid_c_string(_result.data[1]))
                {
                    m_wi.ucc.idx = _result.GetString(1);
                }

                m_wi.ucc.seq = (short)IFNULL(_result.data[2]);
            }
            else if (_index_result == 0 && !(m_type_iff == 8 || m_type_iff == 9) && _result.cols == 3)
            { // Parts Normal

                m_wi.id = _result.GetInt32(0);
            }
            else
            {
                checkColumnNumber(1); // S� para lan�a a exception, por que eu verificou em cima o n�mero das colunas retornadas
            }
        }

        protected override Response prepareConsulta()
        {

            if (m_wi._typeid == 0)
            {
                throw new exception("[CmdAddPart::prepareConsulta][Error] Part is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString((ushort)m_gift_flag) + ", " + Convert.ToString((ushort)m_purchase) + ", " + Convert.ToString(m_wi.id) + ", " + Convert.ToString(m_wi._typeid) + ", " + Convert.ToString((ushort)m_wi.flag) + ", " + Convert.ToString((ushort)m_type_iff) + ", " + Convert.ToString(m_wi.c[3]) + ", " + Convert.ToString(m_wi.c[0]) + ", " + Convert.ToString(m_wi.c[1]) + ", " + Convert.ToString(m_wi.c[2]) + ", " + Convert.ToString(m_wi.c[3]) + ", " + Convert.ToString(m_wi.c[4]));

            checkResponse(r, "nao conseguiu adicionar Part[TYPEID=" + Convert.ToString(m_wi._typeid) + "] para o PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private byte m_purchase;
        private byte m_gift_flag;
        private byte m_type_iff;
        private WarehouseItemEx m_wi = new WarehouseItemEx();

        private const string m_szConsulta = "pangya.ProcAddPart";
    }
}