//using Npgsql;
//using PangyaAPI.Utilities;
//using PangyaAPI.Utilities.Log;
//using System;
//using System.Data;
//using System.Data.SqlClient;
//using System.Diagnostics;
//using System.Linq;
//using response = PangyaAPI.SQL.Response;
//using result_set = PangyaAPI.SQL.Result_Set;
//namespace PangyaAPI.SQL.Manager
//{
//    public class postgresql : database
//    {
//        public virtual void destroy()
//        {

//            if (is_connected())
//                disconnect();

//            if (m_ctx_db._postgresql.hDbc != null)
//                m_ctx_db._postgresql.hDbc = null;

//            if (m_ctx_db._postgresql.hEnv != null)
//                m_ctx_db._postgresql.hEnv = null;

//            m_state = false;
//        }


//        public override bool hasGoneAway()
//        {
//            return false;
//        }


//        public override void connect()
//        {
//            try
//            {
//                init();

//                if (is_connected())
//                    return;

//                if (m_error)
//                    throw new exception(m_error_string);

//                if (m_ctx_db._postgresql  == null)
//                {
//                    m_ctx_db._postgresql.hDbc = new NpgsqlConnection(m_ctx_db.CreateStrConnection());

//                    m_ctx_db._postgresql.hDbc.Open();
//                }

//                m_connected = true;
//            }
//            catch (Exception ex)
//            {
//                _smp.message_pool.getInstance().push(new message("[mssql::Connect][Error] " + ex.Message + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
//                m_connected = false;
//            }
//        }

//        public override void reconnect()
//        {
//            disconnect();
//            connect();
//        }

//        public override void disconnect()
//        {
//            if (is_connected())
//            {
//                if (m_ctx_db._postgresql.hDbc != null)
//                    m_ctx_db._postgresql.hDbc.Close();
//            }

//            m_connected = false;
//        }


//        public override response ExecQuery(string _query)
//        {
//            response res = new response();
//            uint numResults = 0;
//            int numRows;
//            try
//            {
//                HandleDiagnosticRecord(_query);
//                if (m_ctx_db._postgresql.hStmt != null)
//                {
//                    var _data = m_ctx_db._postgresql.hStmt.Tables[m_ctx_db.db_name];
//                    if (_data == null)
//                    {
//                        res = new response();
//                        res.addResultSet(new result_set((uint)result_set.STATE_TYPE._NO_DATA, 0, 0, null));
//                        return res;
//                    }
//                    if (_data.Rows.Count == 1)
//                    {
//                        numResults = 1;
//                    }
//                    if (_data.Rows.Count > 1)
//                    {
//                        numResults = (uint)_data.Rows.Count - 1;
//                    }
//                    numRows = _data.Columns.Count;
//                    res.setRowsAffected(numRows);
//                    if (numResults > 0)
//                    {
//                        foreach (DataRow item in _data.Rows)
//                            res.addResultSet(new result_set((uint)result_set.STATE_TYPE.HAVE_DATA, numResults, (uint)numRows, item));
//                    }

//                    m_ctx_db._postgresql.hStmt.Clear();
//                }
//                return res;
//            }
//            catch (Exception ex)
//            {

//                // Montar a string de comando para execução do procedimento
//                var commandText = $"{_query}";

//                // A mensagem completa da exceção
//                string mensagemErro = string.Format(
//                    "[mssql::ExecQuery][Error]: {0}, [Query]: {1}",
//                    ex.Message, commandText
//                );

//                // Enviar a mensagem para o message_pool
//                _smp.message_pool.getInstance().push(new message(mensagemErro, 0)); return res;
//            }
//        }
//        public override response ExecProc(string _proc_name, string valor = null)
//        {
//            response res = new response();
//            uint numResults = 0;
//            int numRows = 0;
//            try
//            {
//                HandleDiagnosticRecord(_proc_name, valor);
//                if (m_ctx_db._postgresql.hStmt != null && m_ctx_db._postgresql.hStmt.Tables[m_ctx_db.db_name] != null)
//                {
//                    var _data = m_ctx_db._postgresql.hStmt.Tables[m_ctx_db.db_name];
//                    if (_data != null && _data.Rows.Count == 1)
//                    {
//                        numResults = 1;
//                    }
//                    if (_data.Rows.Count > 1)
//                    {
//                        numResults = (uint)_data.Rows.Count - 1;
//                    }
//                    numRows = _data.Columns.Count;
//                    res.setRowsAffected(numRows);
//                    if (numResults > 0)
//                    {
//                        foreach (DataRow item in _data.Rows)
//                            res.addResultSet(new result_set((uint)result_set.STATE_TYPE.HAVE_DATA, numResults, (uint)numRows, item));
//                    }
//                    m_ctx_db._postgresql.hStmt.Clear();
//                }
//                return res;
//            }
//            catch (Exception ex)
//            {
//                // Montar a string de comando para execução do procedimento
//                var commandText = $"EXEC {m_ctx_db.db_name}.{_proc_name} ";

//                if (!string.IsNullOrEmpty(valor))
//                {
//                    // Divide os valores com base na vírgula
//                    var valorArray = valor.Split(',')
//                                            .Select(v => v.Trim()) // Remove espaços em branco
//                                            .Select(v => $"'{v}'") // Adiciona aspas simples
//                                            .ToArray();

//                    // Junta os valores formatados de volta em uma string
//                    commandText += string.Join(", ", valorArray);
//                }

//                // A mensagem completa da exceção
//                string mensagemErro = string.Format(
//                    "[mssql::ExecProc][Error]: {0}, [Query]: {1}",
//                    ex.Message, commandText
//                );

//                // Enviar a mensagem para o message_pool
//                _smp.message_pool.getInstance().push(new message(mensagemErro, type_msg.CL_FILE_LOG_AND_CONSOLE));
//                return res;
//            }
//        }

//        public override response ExecQueryWithParams(string _proc_name, string[] parameter = null, SqlDbType[] tipo = null, object[] valor = null, ParameterDirection Direcao = ParameterDirection.Input)
//        {
//            response res = new response();
//            uint numResults = 0;
//            int numRows = 0;
//            try
//            {
//                HandleDiagnosticRecord(_proc_name, parameter, tipo, valor, Direcao, CommandType.Text);
//                if (m_ctx_db._postgresql.hStmt != null && m_ctx_db._postgresql.hStmt.Tables[m_ctx_db.db_name] != null)
//                {
//                    var _data = m_ctx_db._postgresql.hStmt.Tables[m_ctx_db.db_name];
//                    if (_data != null && _data.Rows.Count == 1)
//                    {
//                        numResults = 1;
//                    }
//                    if (_data.Rows.Count > 1)
//                    {
//                        numResults = (uint)_data.Rows.Count - 1;
//                    }
//                    numRows = _data.Columns.Count;
//                    res.setRowsAffected(numRows);
//                    if (numResults > 0)
//                    {
//                        foreach (DataRow item in _data.Rows)
//                        {
//                            res.addResultSet(new result_set((uint)result_set.STATE_TYPE.HAVE_DATA, numResults, (uint)numRows, item));
//                        }
//                    }
//                    m_ctx_db._postgresql.hStmt.Clear();
//                }
//                return res;
//            }
//            catch (exception ex)
//            {

//                _smp.message_pool.getInstance().push(new message("[mssql::ExecProcWithParams][Error] " + ex.getFullMessageError() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
//                return res;

//            }
//        }
//        public override response ExecProcWithParams(string _proc_name, string[] parameter = null, SqlDbType[] tipo = null, object[] valor = null, ParameterDirection Direcao = ParameterDirection.Input)
//        {
//            response res = new response();
//            uint numResults = 0;
//            int numRows = 0;
//            try
//            {
//                HandleDiagnosticRecord(_proc_name, parameter, tipo, valor, Direcao);
//                if (m_ctx_db._postgresql.hStmt != null && m_ctx_db._postgresql.hStmt.Tables[m_ctx_db.db_name] != null)
//                {
//                    var _data = m_ctx_db._postgresql.hStmt.Tables[m_ctx_db.db_name];
//                    if (_data != null && _data.Rows.Count == 1)
//                    {
//                        numResults = 1;
//                    }
//                    if (_data.Rows.Count > 1)
//                    {
//                        numResults = (uint)_data.Rows.Count - 1;
//                    }
//                    numRows = _data.Columns.Count;
//                    res.setRowsAffected(numRows);
//                    if (numResults > 0)
//                    {
//                        foreach (DataRow item in _data.Rows)
//                            res.addResultSet(new result_set((uint)result_set.STATE_TYPE.HAVE_DATA, numResults, (uint)numRows, item));
//                    }
//                    m_ctx_db._postgresql.hStmt.Clear();
//                }
//                return res;
//            }
//            catch (exception ex)
//            {

//                _smp.message_pool.getInstance().push(new message("[mssql::ExecProcWithParams][Error] " + ex.getFullMessageError() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
//                return res;

//            }
//        }
////       public override string makeEscapeKeyword(string value)
//{
//    return $"\"{value}\"";
//}

//        public postgresql(ctx_db _m_ctx_db) : base(_m_ctx_db)
//        {
//            connect();
//        }
//        protected void HandleDiagnosticRecord(string query)
//        {

//            Stopwatch stopwatch = Stopwatch.StartNew();
//            try
//            {
//                if (m_ctx_db._postgresql.hDbc != null)
//                {
//                    if (string.IsNullOrEmpty(m_ctx_db._postgresql.hDbc.ConnectionString))
//                        m_ctx_db._postgresql.hDbc.ConnectionString = m_ctx_db.CreateStrConnection();

//                    // RECRIAR o DataSet antes de popular
//                    m_ctx_db._postgresql.hStmt = new DataSet();

//                    var da = new NpgsqlDataAdapter(query, m_ctx_db._postgresql.hDbc);
//                    da.Fill(m_ctx_db._postgresql.hStmt, m_ctx_db.db_name);
//                }
//            }
//            catch (exception ex)
//            {
//                _smp.message_pool.getInstance().push(new message("[mssql::HandleDiagnosticQuery][Error] " + ex.getFullMessageError() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
//            }
//            finally
//            {
//                stopwatch.Stop();
//                //Debug.WriteLine($"[HandleDiagnosticRecord][Log] Tempo de execução: {stopwatch.ElapsedMilliseconds}ms");

//            }

//        }

//        protected void HandleDiagnosticRecord(string _proc_name, string valores = null)
//        {
//            Stopwatch stopwatch = Stopwatch.StartNew();

//            try
//            {

//                if (string.IsNullOrEmpty(m_ctx_db._postgresql.hDbc.ConnectionString))
//                {
//                    m_ctx_db._postgresql.hDbc.Close();
//                    m_ctx_db._postgresql.hDbc = new NpgsqlConnection(m_ctx_db.CreateStrConnection());
//                    m_ctx_db._postgresql.hDbc.Open();
//                }

//                // RECRIAR o DataSet antes de popular
//                m_ctx_db._postgresql.hStmt = new DataSet();

//                // Montar a string de comando para execução do procedimento
//                var commandText = $"EXEC {m_ctx_db.db_name}.{_proc_name} ";

//                if (!string.IsNullOrEmpty(valores))
//                {
//                    // Verifica se os valores estão no formato de uma sequência separada por '|'
//                    if (valores.Contains("|"))
//                    {
//                        // Dividindo corretamente pelos pipes '|'
//                        var valorArray = valores.Split('|')
//                                                .Select(v => v.Trim()) // Remover espaços em branco
//                                                .Select(v => v.ToUpper() == "NULL" ? "NULL" : $"N'{v.Replace("'", "''")}'")
//                                                .ToArray();

//                        commandText += string.Join(", ", valorArray);
//                    }
//                    else
//                    {
//                        // Dividindo por vírgula ',' caso não contenha pipe
//                        var valorArray = valores.Split(',')
//                                                .Select(v => v.Trim())
//                                                .Select(v => v.ToUpper() == "NULL" ? "NULL" : $"N'{v.Replace("'", "''")}'")
//                                                .ToArray();

//                        commandText += string.Join(", ", valorArray);
//                    }

//                }
//                m_ctx_db._postgresql.hEnv = new NpgsqlCommand(commandText, m_ctx_db._postgresql.hDbc);
//                m_ctx_db._postgresql.hEnv.CommandTimeout = 300;
//                var da = new NpgsqlDataAdapter(m_ctx_db._postgresql.hEnv);
//                da.Fill(m_ctx_db._postgresql.hStmt, m_ctx_db.db_name);
//            }
//            catch (exception ex)
//            {
//                _smp.message_pool.getInstance().push(new message("[mssql::HandleDiagnosticQuery][Error] " + ex.getFullMessageError() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
//            }

//            finally
//            {
//                stopwatch.Stop();
//                //Debug.WriteLine($"[HandleDiagnosticRecord1][Log] Tempo de execução: {stopwatch.ElapsedMilliseconds}ms");

//            }
//        }
//        public void HandleDiagnosticRecord(string _proc_name, string[] parameter = null, SqlDbType[] tipo = null, object[] valor = null, ParameterDirection Direcao = ParameterDirection.Input, CommandType command = CommandType.StoredProcedure)
//        {
//            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
//            try
//            {

//                m_ctx_db._postgresql.hDbc = new NpgsqlConnection(m_ctx_db.CreateStrConnection());
//                m_ctx_db._postgresql.hDbc.Open();

//                m_ctx_db._postgresql.hStmt = new DataSet();

//                m_ctx_db._postgresql.hEnv = new NpgsqlCommand($"{m_ctx_db.db_name}.{_proc_name}", m_ctx_db._postgresql.hDbc)
//                {
//                    CommandType = command,
//                    CommandTimeout = 300
//                };

//                if (parameter != null && parameter.Length > 0)
//                {
//                    for (int i = 0; i < parameter.Length; i++)
//                    {
//                        var param = new SqlParameter
//                        {
//                            ParameterName = parameter[i],
//                            SqlDbType = tipo[i],
//                            Direction = Direcao,
//                            Value = (valor[i] is Guid g && g == Guid.Empty) ? DBNull.Value : valor[i]
//                        };


//                        if (tipo[i] == SqlDbType.NVarChar || tipo[i] == SqlDbType.VarChar)
//                            param.Size = 1024;

//                        m_ctx_db._postgresql.hEnv.Parameters.Add(param);
//                    }
//                }

//                var da = new NpgsqlDataAdapter(m_ctx_db._postgresql.hEnv);
//                da.Fill(m_ctx_db._postgresql.hStmt, m_ctx_db.db_name);
//            }
//            catch (SqlException ex)
//            {
//                throw new Exception("[HandleDiagnosticRecord][SqlException] " + ex.Message, ex);
//            }

//            finally
//            {
//                stopwatch.Stop();
//                Debug.WriteLine($"[HandleDiagnosticRecord2][Log] Tempo de execução: {stopwatch.ElapsedMilliseconds}ms");
//            }
//        }
//    }
//}