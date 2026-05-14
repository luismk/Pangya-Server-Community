//Convertion By LuisMK
using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdAddCard : Pangya_DB
    {
        public CmdAddCard(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_gift_flag = 0;
            this.m_purchase = 0;
            this.m_ci = new CardInfo();
        }

        public CmdAddCard(uint _uid,
            CardInfo _ci, byte _purchase,
            byte _gift_flag,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_purchase = _purchase;
            this.m_gift_flag = _gift_flag;
            this.m_ci = (_ci);
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public CardInfo getInfo()
        {
            return m_ci;
        }

        public void setInfo(CardInfo _ci)
        {
            m_ci = _ci;
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

            checkColumnNumber(1);
            m_ci.id = IFNULL<int>(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {

            if (m_ci._typeid == 0)
            {
                throw new exception("[CmdAddCard::prepareConsulta][Error] Card Info is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            // Ignora as flags purchase e gift por hora, para usar a tabela antiga que eu fiz de card
            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_ci._typeid) + ", " + Convert.ToString(m_ci.qntd) + ", " + Convert.ToString((ushort)m_ci.type));

            checkResponse(r, "nao conseguiu adicionar o card[TYPEID=" + Convert.ToString(m_ci._typeid) + "] para o PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private byte m_gift_flag;
        private byte m_purchase;
        private CardInfo m_ci = new CardInfo();

        private const string m_szConsulta = "pangya.ProcInsertCard";
    }
}