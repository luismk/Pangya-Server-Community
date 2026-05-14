using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertMemorialRareWinLog : Pangya_DB
    {
        public CmdInsertMemorialRareWinLog()
        {
            this.m_uid = 0;
            this.m_coin_typeid = 0;
            this.m_ci = new ctx_coin_item_ex();
        }

        public CmdInsertMemorialRareWinLog(uint _uid,
            uint _coin_typeid,
            ctx_coin_item_ex _ci)
        {
            this.m_uid = _uid;
            this.m_coin_typeid = _coin_typeid;
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

        public uint getCoinTypeid()
        {
            return (m_coin_typeid);
        }

        public void setCoinTypeid(uint _coin_typeid)
        {
            m_coin_typeid = _coin_typeid;
        }

        public ctx_coin_item_ex getInfo()
        {
            return m_ci;
        }

        public void setInfo(ctx_coin_item_ex _ci)
        {
            m_ci = _ci;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um INSERT
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdInsertMemorialRareWinLog::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_coin_typeid == 0)
            {
                throw new exception("[CmdInsertMemorialRareWinLog::prepareConsulta][Error] m_coin_typeid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_ci._typeid == 0)
            {
                throw new exception("[CmdInsertMemorialRareWinLog::prepareConsulta][Error] m_ci._typeid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta,
                Convert.ToString(m_uid) + ", " + Convert.ToString(m_coin_typeid) + ", " + Convert.ToString(m_ci._typeid) + ", " + Convert.ToString(m_ci.qntd) + ", " + Convert.ToString(m_ci.tipo) + ", " + Convert.ToString(m_ci.probabilidade));

            checkResponse(r, "nao conseguiu inserir um Memorial Shop[COIN=" + Convert.ToString(m_coin_typeid) + "] Rare Win[TYPEID=" + Convert.ToString(m_ci._typeid) + ", QNTD=" + Convert.ToString(m_ci.qntd) + ", RARIDADE=" + Convert.ToString(m_ci.tipo) + "] Log para o PLAYER[UID=" + Convert.ToString(m_uid) + "]");

            return r;
        }

        private uint m_uid = new uint();
        private uint m_coin_typeid = new uint();
        private ctx_coin_item_ex m_ci = new ctx_coin_item_ex();

        private const string m_szConsulta = "pangya.ProcInsertMemorialRareWinLog";
    }
}