using System;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdInsertCommand : Pangya_DB
    {
        public CmdInsertCommand(CommandInfo _ci)
        {
            this.m_ci = _ci;
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

            // N�o usa por que � um INSERT
            return;
        }

        protected override Response prepareConsulta()
        {
            reserveDate = (!m_ci.reserveDate.IsEmpty)
        ? UtilTime._formatDate(m_ci.reserveDate)
        : "NULL";
            var _params = (Convert.ToString(m_ci.id) + ", " +
                Convert.ToString(m_ci.arg[0]) + ", " +
                Convert.ToString(m_ci.arg[1]) + ", " +
                Convert.ToString(m_ci.arg[2]) + ", " +
                Convert.ToString(m_ci.arg[3]) + ", " +
                Convert.ToString(m_ci.arg[4]) + ", " +
                Convert.ToString(m_ci.target) + ", " +
                Convert.ToString(m_ci.flag) + ", " +
                Convert.ToString((ushort)m_ci.valid) + ", " + reserveDate);
            var r = procedure(m_szConsulta,
                _params);

            checkResponse(r, "nao conseguiu adicionar o Comando[" + m_ci.ToString() + "]");

            return r;
        }
        public static string MakeText(string value)
        {
            if (value == null || value.ToUpper() == "NULL")
                return "NULL";  // valor NULL real em SQL

            return $"N'{value.Replace("'", "''")}'"; // protege aspas simples
        }


        private CommandInfo m_ci = new CommandInfo();
        string reserveDate = "NULL"; // Default to DBNull.Value
        private const string m_szConsulta = "pangya.ProcInsertCommand";
    }
}