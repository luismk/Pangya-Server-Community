using System;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertSpinningCubeSuperRareWinBroadcast : Pangya_DB
    {
        public CmdInsertSpinningCubeSuperRareWinBroadcast(bool _waiter = false) : base(_waiter)
        {
            this.m_message = "";
            this.m_opt = 0;
        }

        public CmdInsertSpinningCubeSuperRareWinBroadcast(string _message,
            byte _opt,
            bool _waiter = false) : base(_waiter)
        {
            this.m_message = _message;
            this.m_opt = _opt;
        }

        public string getMessage()
        {
            return m_message;
        }

        public void setMessage(string _message)
        {
            m_message = _message;
        }

        public byte getOpt()
        {
            return m_opt;
        }

        public void setOpt(byte _opt)
        {
            m_opt = _opt;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {


            // N�o usa por que � um INSERT
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_message.Length == 0)
            {
                throw new exception("[CmdInsertSpinningCubeSuperRareWinBroadcast::prepareConsulta][Error] m_message is invalid(empty)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var r = procedure(m_szConsulta, makeText(m_message) + ", " + Convert.ToString((ushort)m_opt));

            checkResponse(r, "nao conseguiu inserir Spinning Cube Super Rare Win Broadcast[MSG=" + m_message + ", OPT=" + Convert.ToString((ushort)m_opt) + "]");

            return r;
        }

        private string m_message = "";
        private byte m_opt;

        private const string m_szConsulta = "pangya.ProcInsertSpinningCubeSuperRareWinBroadCast";
    }
}