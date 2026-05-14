using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdRemoveEquipedCard : Pangya_DB
    {
        public CmdRemoveEquipedCard(bool _waiter = false) : base(_waiter)
        {
            this.m_uid = 0;
            this.m_cei = new CardEquipInfo();
        }

        public CmdRemoveEquipedCard(uint _uid,
            CardEquipInfo _cei,
            bool _waiter = false) : base(_waiter)
        {
            this.m_uid = _uid;
            this.m_cei = (_cei);
        }

        public virtual void Dispose()
        {
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public CardEquipInfo getInfo()
        {
            return m_cei;
        }

        public void setInfo(CardEquipInfo _cei)
        {
            m_cei = _cei;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdRemoveEquipedCard::prepareConsulta][Error] m_uid is invalid(zero)", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_cei._typeid == 0)
            {
                throw new exception("[CmdRemoveEquipedCard::prepareConsulta][Error] CardEquipedInfo is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_cei.parts_id) + ", " + Convert.ToString(m_cei.parts_typeid) + ", " + Convert.ToString(m_cei._typeid) + ", " + Convert.ToString(m_cei.slot));

            checkResponse(r, "nao conseguiu remover card[TYPEID=" + Convert.ToString(m_cei._typeid) + "] equipado no Character[TYPEID=" + Convert.ToString(m_cei.parts_typeid) + ", ID=" + Convert.ToString(m_cei.parts_id) + "] do PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private CardEquipInfo m_cei = new CardEquipInfo();

        private const string m_szConsulta = "pangya.ProcRemoveEquipedCard";
    }
}