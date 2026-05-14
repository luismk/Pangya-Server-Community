using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdEquipCard : Pangya_DB
    { 
        public CmdEquipCard(uint _uid, CardEquipInfoEx _cei, uint _tempo)
        {
            this.m_uid = _uid;
            this.m_tempo = _tempo;
            this.m_cei = _cei;
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public uint getTempo()
        {
            return m_tempo;
        }

        public void setTempo(uint _tempo)
        {
            m_tempo = _tempo;
        }

        public CardEquipInfoEx getInfo()
        {
            return m_cei;
        }

        public void setInfo(CardEquipInfoEx _cei)
        {
            m_cei = _cei;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1);

            m_cei.index = IFNULL<int>(_result.data[0]);

            if (m_cei.index < 0)
            {
                throw new exception("[CmdEquipCard::lineResult][Error] m_cei[index=" + Convert.ToString(m_cei.index) + "] is invalid, nao conseguiu equipar o card[TYPEID=" + Convert.ToString(m_cei._typeid) + "] para o PLAYER[UID=" + Convert.ToString(m_uid) + "]", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    3, 0));
            }

            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdEquipCard::prepareConsulta][Error] m_uid is invalid(zero)", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_cei._typeid == 0)
            {
                throw new exception("[CmdEquipCard::prepareConsulta][Error] CardEquipInfo[TYPEID=" + Convert.ToString(m_cei._typeid) + "] is invalid", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_cei.index = -1;

            var r = procedure(m_szConsulta, Convert.ToString(m_uid) + ", " + Convert.ToString(m_cei._typeid) + ", " + Convert.ToString(m_cei.parts_id) + ", " + Convert.ToString(m_cei.parts_typeid) + ", " + Convert.ToString(m_cei.efeito) + ", " + Convert.ToString(m_cei.efeito_qntd) + ", " + Convert.ToString(m_cei.slot) + ", " + Convert.ToString(m_cei.tipo) + ", " + Convert.ToString(m_tempo));

            checkResponse(r, "nao conseguiu equipar o Card[TYPEID=" + Convert.ToString(m_cei._typeid) + "] no Character[TYPEID=" + Convert.ToString(m_cei.parts_typeid) + ", ID=" + Convert.ToString(m_cei.parts_id) + "] para o PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = 0;
        private uint m_tempo = 0;
        private CardEquipInfoEx m_cei = new CardEquipInfoEx();

        private const string m_szConsulta = "pangya.ProcEquipCard";
    }
}