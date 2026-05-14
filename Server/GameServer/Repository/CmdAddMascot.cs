//Convertion By LuisMK      
using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdAddMascot : Pangya_DB
    {
        public CmdAddMascot()
        {
            this.m_uid = 0;
            this.m_purchase = 0;
            this.m_gift_flag = 0;
            this.m_time = 0;
            this.m_mi = new MascotInfoEx();
        }

        public CmdAddMascot(uint _uid,
            MascotInfoEx _mi,
            int _time, byte _purchase,
            byte _gift_flag)
        {
            this.m_uid = _uid;
            this.m_purchase = _purchase;
            this.m_gift_flag = _gift_flag;
            this.m_time = _time;
            this.m_mi = (_mi);
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public MascotInfoEx getInfo()
        {
            return m_mi;
        }

        public void setInfo(MascotInfoEx _mi)
        {
            m_mi = _mi;
        }

        public int getTime()
        {
            return (m_time);
        }

        public void setTime(int _time)
        {
            m_time = _time;
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

            m_mi.id = IFNULL<int>(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {

            if (m_mi._typeid == 0)
            {
                throw new exception("[CmdAddMascot::prepareConsulta][Error] Mascot info is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            // Ignora as PCBangMascot gift e purchase para usar minha nova proc de add mascot
            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_mi._typeid) + ", " + Convert.ToString(m_mi.tipo) + ", " + Convert.ToString((ushort)m_mi.is_cash) + ", " + Convert.ToString(m_time) + ", " + makeText(m_mi.message) + ", " + Convert.ToString(m_mi.price));

            checkResponse(r, "nao conseguiu adicionar o Mascot[TYPEID=" + Convert.ToString(m_mi._typeid) + "] para o PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private byte m_purchase;
        private byte m_gift_flag;
        private int m_time = new int();
        private MascotInfoEx m_mi = new MascotInfoEx();

        private const string m_szConsulta = "pangya.ProcInsertMascot";
    }
}