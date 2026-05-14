
using Pangya_AuthServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using System;
namespace Pangya_AuthServer.Repository
{
    public class CmdUpdateCommand : Pangya_DB
    {
        public CmdUpdateCommand(bool _waiter = false) : base(_waiter)
        {
            this.m_ci = new CommandInfo(0);
        }

        public CmdUpdateCommand(CommandInfo _ci, bool _waiter = false) : base(_waiter)
        {
            this.m_ci = _ci;
        }

        public virtual void Dispose()
        {
        }

        public CommandInfo getInfo()
        {
            return m_ci;
        }

        public void setInfo(CommandInfo _ci)
        {
            m_ci = _ci;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            // N�o usa por que � um UPDATE
            return;
        }

        protected override Response prepareConsulta()
        {

            if (m_ci.idx == 0)
            {
                throw new exception("[CmdUpdateCommand::prepareConsulta][Error] (CommandInfo)m_ci.idx is invalid(zero).", STDA_MAKE_ERROR(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            var reserveDate = (m_ci.reserveDate.Year != 0)  ? makeText(_formatDate(m_ci.reserveDate)) : "NULL";
            
            var r = procedure(
                    m_szConsulta,
                    Convert.ToString(m_ci.idx) + ", " + Convert.ToString(m_ci.id) + ", " + Convert.ToString(m_ci.arg[0]) + ", " + Convert.ToString(m_ci.arg[1]) + ", " + Convert.ToString(m_ci.arg[2]) + ", " + Convert.ToString(m_ci.arg[3]) + ", " + Convert.ToString(m_ci.arg[4]) + ", " + Convert.ToString(m_ci.target) + ", " + Convert.ToString(m_ci.flag) + ", " + Convert.ToString((ushort)m_ci.valid) + ", " + reserveDate);

            checkResponse(r, "nao conseguiu Atualizar o Command[" + m_ci.toString() + "]");

            return r;
        }

        private CommandInfo m_ci = new CommandInfo();

        private string m_szConsulta = "pangya.ProcUpdateCommand";
    }
}
