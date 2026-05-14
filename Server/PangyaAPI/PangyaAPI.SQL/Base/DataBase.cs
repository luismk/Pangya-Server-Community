using PangyaAPI.SQL.Manager;
using PangyaAPI.Utilities.Log;
using System;
using System.Data;
using System.Data.SqlClient;
using response = PangyaAPI.SQL.Response;
using System.Runtime.InteropServices;

namespace PangyaAPI.SQL
{
    public abstract class database
    {
        public enum ERROR_TYPE : uint
        {
            INVALID_HANDLE,
            INVALID_PARAMETER,
            ALLOC_HANDLE_FAIL_ENV,
            ALLOC_HANDLE_FAIL_DBC,
            ALLOC_HANDLE_FAIL_STMT,
            SET_ATTR_ENV_FAIL,
            CONNECT_DRIVER_FAIL,
            EXEC_QUERY_FAIL,
            FETCH_QUERY_FAIL,
            MORE_RESULTS,
            GERAL_ERROR,
            HAS_CONNECT
        }
        public database()
        {
            this.m_state = false;
            this.m_connected = false;
        }
        public database(ctx_db _m_ctx_db)
        {
            m_ctx_db = _m_ctx_db;

            this.m_state = false;
            this.m_connected = false;
            init();
        }


        /// <summary>
        ///recria dados da conexao em uma unica linha de string
        /// </summary>
        /// <returns></returns>    
        public void init()
        {

            if (m_ctx_db == null || string.IsNullOrEmpty(m_ctx_db.engine))
                throw new ArgumentNullException(nameof(m_ctx_db), "ctx_db ou engine não informado");

            switch (m_ctx_db.engine.ToUpper())
            {
                case "MSSQL":
                case "SQLSERVER":
                    {
                        m_ctx_db._mssql = new ctx_db._MSSQL();
                    }
                    break;
                case "MYSQL":
                    {
                        m_ctx_db._mysql = new ctx_db._MYSQL();
                    }
                    break;
                case "POSTGRESQL":
                case "PGSQL":
                    {
                        m_ctx_db._postgresql = new ctx_db._POSTGRESQL();
                    }
                    break;
                default:
                    throw new NotSupportedException($"Engine '{m_ctx_db.engine}' não suportada");
            }
            m_state = true;
        } 

        public bool is_connected()
        {
            return m_connected;
        }

        public abstract bool hasGoneAway();

        public abstract void connect();
        public abstract void reconnect();
        public abstract void disconnect();

        public abstract response ExecQuery(string _query);
        public abstract response ExecProc(string _proc_name, string values = null);
        public abstract response ExecQueryWithParams(string _proc_name, string values = null/*, SqlDbType[] tipo = null, object[] valor = null, ParameterDirection Direcao = ParameterDirection.Input*/);
        public abstract response ExecProcWithParams(string _proc_name, string values = null/*, SqlDbType[] tipo = null, object[] valor = null, ParameterDirection Direcao = ParameterDirection.Input*/);
        public abstract response ExecProcWithParams(string _proc_name, object[] values = null/*, SqlDbType[] tipo = null, object[] valor = null, ParameterDirection Direcao = ParameterDirection.Input*/);
        public abstract string makeEscapeKeyword(string _value);

        public bool m_error = false;
        public string m_error_string = "";
        protected bool m_state;
        protected bool m_connected;
        public ctx_db m_ctx_db = new ctx_db();
    }
}
