using PangyaAPI.DataBase.Engine;
using PangyaAPI.DataBase.Manager;
using PangyaAPI.DataBase.Models;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log; 
using System.Globalization; 

namespace PangyaAPI.DataBase
{  
    public abstract class Pangya_DB
    {
        private static readonly CmdExecuteLog cdcs = new CmdExecuteLog();

        private Connector _db;
        public virtual string FileConnection { get; set; } = "db.ini";
        private bool _wait = false;
        private bool executed;
        public Pangya_DB()
        {
            _wait = true;
            executed = false;
            loadIni();

            if (_wait)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000); // aguarda 1 segundo sem bloquear
                });
            }
            _wait = false;
        }


        private DatabaseContext m_ctx_db = new DatabaseContext();
        public bool loadIni()
        {
            if (!string.IsNullOrEmpty(m_ctx_db.ip))
            {
                return false;
            }
            IniHandle ini = new IniHandle(FileConnection);
            try
            {

                m_ctx_db.engine = ini.ReadString("NORMAL_DB", "DBENGINE");//tipo de conexao 
                m_ctx_db.ip = ini.ReadString("NORMAL_DB", "DBIP");
                m_ctx_db.db_name = ini.ReadString("NORMAL_DB", "DBNAME");
                m_ctx_db.user = ini.ReadString("NORMAL_DB", "DBUSER");
                m_ctx_db.pass = ini.ReadString("NORMAL_DB", "DBPASS");
                m_ctx_db.port = ini.ReadUInt32("NORMAL_DB", "DBPORT", 1433);
                m_ctx_db.cmd_log = ini.ReadBool("NORMAL_DB", "DBLOG", false);

                _db = DbFactory.Create(m_ctx_db);
            }
            catch (exception ex)
            {
                throw ex;
            }
            return true;
        }
         
        private bool logExecuteCmds(string _name)
        {
            var v_cmds = cdcs.loadCmds();

            if (!v_cmds.TryGetValue(_name, out var value))
            {
                v_cmds[_name] = "yes";
                cdcs.saveCmds(v_cmds);
                return true; // show log
            }
            else if (value == "no")
            {
                v_cmds[_name] = "yes";
                cdcs.saveCmds(v_cmds);
                return true; // show log
            }

            return false;
        }

        public void exec()
        {
            if (_wait)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000); // aguarda 1 segundo sem bloquear
                    _wait = false;          // depois de esperar, marca como falso
                });
            }

            uint num_result = 0;
            try
            {
                Response r = null;
                if ((r = prepareConsulta()) != null)
                {
                    var results = r.getResultSet();
                    foreach (var _result in results)
                    {
                        var line = _result.getFirstLine();
                        if (line != null)
                        {
                            lineResult(line, num_result++);
                        }
                    }
                    if (results.Count == 0)
                    {
                        lineResultNull();
                    }
                    executed =true;
                    r = null;
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[Pangya_DB::" + _getName + "::exec][Error] return prepareConsulta is null.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }
            catch (exception e)
            {
                m_exception = e;
                _smp.LogManager.Instance.push(new AppMessage("[pangya_db::" + _getName + "::exec][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }


            if (logExecuteCmds(_getName))
                _smp.LogManager.Instance.push(new AppMessage($"[{_getName}::exec][Sucess] {(this.executed == true ? "was" : "No")} Executed.", type_msg.CL_ONLY_CONSOLE_DEBUG));

            _db.disconnect();
        }

        public virtual exception getException() => m_exception ?? new exception("");

        public virtual Response _update(string _query) { return _db.ExecQuery(_query); }

        public virtual Response _delete(string _query) { return _db.ExecQuery(_query); }

        public virtual Response consulta(string _query) { return _db.ExecQuery(_query); }
        // MYSQL USA PARENTESES MAS O MSSQL server não usa
        //EXEMPLE: L"exec pangya.ProcGetGuildInfo 4218, 1"
        public virtual Response procedure(string _name, string values = null) { return _db.ExecProc(_name, values); }
        public virtual Response procedureWithParams(string _name, string values = null) { return _db.ExecQueryWithParams(_name, values); }
        //others
        public virtual Response deleteWithParams(string _proc_name, string valor = null) { return _db.ExecQueryWithParams(_proc_name, valor); }
        public virtual Response consultaeWithParams(string _proc_name, string valor = null) { return _db.ExecQueryWithParams(_proc_name, valor); }
        public virtual Response _updateWithParams(string _proc_name, string valor = null) { return _db.ExecQueryWithParams(_proc_name, valor); }

        public virtual void checkColumnNumber(uint _number_cols1)
        {
            if (_number_cols1 <= 0)
                throw new exception("[Pangya_DB::" + _getName + "::checkColumnNumber][Error] numero de colunas retornada pela consulta sao diferente do esperado.");
        }
        public virtual void checkColumnNumber(uint _number_cols1, uint _number_cols2)
        {
            if (_number_cols1 <= 0 || _number_cols1 != _number_cols2)
                throw new exception("[Pangya_DB::" + _getName + "::checkColumnNumber][Error] numero de colunas retornada pela consulta sao diferente do esperado.");
        }

        public virtual void checkResponse(Response r, string _exception_msg)
        {
            if (r == null || (r.getNumResultSet() <= 0 && r.getRowsAffected() == -1))
                throw new exception("[Pangya_DB::" + _getName + "::checkResponse][Error] " + _exception_msg, ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 0, 0));
        }
        public uint STDA_MAKE_ERROR(STDA_ERROR_TYPE code, uint err_code, uint _err_sys) => ExceptionError.STDA_MAKE_ERROR_TYPE(code, err_code, _err_sys);
        protected virtual void lineResult(ctx_res _result, uint _index_result) { }
        protected virtual void lineResultNull() { }//nao em dados, so confirma a execucao
        protected abstract Response prepareConsulta();

        protected virtual string _getName { get => GetType().Name; }
        public static string _formatDate(SystemTime date)
        {
            return UtilTime.FormatDate(date);
        }
        public static string _formatDate(DateTime date)
        {
            return UtilTime.FormatDate(date);
        }
        public static string formatDateLocal(long date)
        {
            return UtilTime.FormatDateLocal(date);
        }

        public static bool is_valid_c_string(object value)
        {
            if (value == null || value is DBNull || (value is string && string.IsNullOrEmpty((string)value)))
            {
                return false;
            }
            var _ptr_c_string = Convert.ToString(value);
            return _ptr_c_string != null && _ptr_c_string[0] != 0;
        }
          
        public T IFNULL<T>(object value, T defaultValue = default)
        {
            if (value == null || value is DBNull)
                return defaultValue;

            try
            {
                // Se for enum (comum em estados de server), trata adequadamente
                if (typeof(T).IsEnum)
                    return (T)Enum.ToObject(typeof(T), value);

                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static DateTime? _translateDate(object value)
        {
            if (value == null)
                return null;

            string[] formatos = new[] {
    "M/d/yyyy h:mm:ss tt",    // ex: 5/6/2025 12:00:00 AM
    "M/d/yyyy hh:mm:ss tt",
    "dd/MM/yyyy HH:mm:ss",
    "HH:mm:ss"                // apenas hora
};

            if (DateTime.TryParseExact(value.ToString(), formatos,
                                       System.Globalization.CultureInfo.InvariantCulture,
                                       System.Globalization.DateTimeStyles.None,
                                       out DateTime result))
            {
                // Se for apenas horário (sem data), assume o dia de hoje
                if (value.ToString().Length == 8 && value.ToString().Count(c => c == ':') == 2)
                {
                    return DateTime.Today.Add(result.TimeOfDay);
                }

                return result;
            }

            return null;
        }


        public string makeEscapeKeyword(string value)
        {
            return _db.makeEscapeKeyword(value);
        }

        public string SQLDATE()
        {
            switch (_db.m_ctx_db.engine.ToUpper())
            {
                case "MSSQL":
                    // GETDATE() é uma função.
                    return "GETDATE()";

                case "MYSQL":
                    // MySQL usa NOW() para data e hora ou CURDATE() apenas para data.
                    return "NOW()";

                case "POSTGRESQL":
                    return "CURRENT_TIMESTAMP";

                default:
                    return "CURRENT_TIMESTAMP";
            }
        }

        public string SQL_LIMIT(string sql, int quantidade)
        {
            switch (_db.m_ctx_db.engine.ToUpper())
            {
                case "MSSQL":
                    return sql.Replace("SELECT", $"SELECT TOP {quantidade}");
                case "MYSQL":
                case "POSTGRESQL":
                default:
                    return $"{sql} LIMIT {quantidade}";
            }
        }

        public string makeText(string value)
        {
            if (value == null) return "NULL";

            string rawValue = value;
            // Remove o N' inicial e a ' final se existirem (Padrão MSSQL)
            if (rawValue.StartsWith("N'") && rawValue.EndsWith("'"))
            {
                rawValue = rawValue.Substring(2, rawValue.Length - 3);
            }
            // Remove aspas simples iniciais e finais (Padrão Geral)
            else if (rawValue.StartsWith("'") && rawValue.EndsWith("'"))
            {
                rawValue = rawValue.Substring(1, rawValue.Length - 2);
            }

            // 2. Agora sim, aplicamos o escape padrão ANSI
            string safeValue = rawValue.Replace("'", "''");

            switch (_db.m_ctx_db.engine.ToUpper())
            {
                case "MSSQL":
                    return $"N'{safeValue}'";

                case "MYSQL":
                    string mysqlSafe = safeValue.Replace("\\", "\\\\");
                    return $"'{mysqlSafe}'";

                case "POSTGRESQL":
                    return $"'{safeValue}'";
                default:
                    return $"'{safeValue}'";
            }
        }

        public string ToString(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public string ToString(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public string makeText(object value)
        {
            return $"'{value.ToString()}'";
        }

        protected exception m__exception { get; set; }
        public exception m_exception { get => m__exception; set => m__exception = value; }
    }
}