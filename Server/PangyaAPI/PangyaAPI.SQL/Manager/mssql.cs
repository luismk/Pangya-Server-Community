using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using static PangyaAPI.SQL.ctx_db;

namespace PangyaAPI.SQL.Manager
{
    public class mssql : database
    {
        public mssql(ctx_db ctx) : base(ctx)
        {
            connect();
        }

        public override void connect()
        {
            try
            {
                if (m_ctx_db._mssql.hDbc != null &&
               m_ctx_db._mssql.hDbc.State == ConnectionState.Open)
                    return;

                m_ctx_db._mssql.hDbc =
                    new OdbcConnection(m_ctx_db.CreateStrConnection());

                m_ctx_db._mssql.hDbc.Open();
                m_connected = true;
            }
            catch (OdbcException ex)
            {
               
                m_connected = false; 
                _smp.message_pool.getInstance().push(new message($"[mssql::Connect][Error] {ex.Message}", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

        }

        public override void disconnect()
        {
            if (m_ctx_db._mssql.hDbc != null)
            { 
                m_ctx_db._mssql.clear();
            }

            m_connected = false;
        }

        public override void reconnect()
        {
            disconnect();
            connect();
        }

        public override bool hasGoneAway()
        {
            return m_ctx_db._mssql.hDbc == null ||
                   m_ctx_db._mssql.hDbc.State != ConnectionState.Open;
        }

        public override Response ExecQuery(string query)
        { 
            var res = new Response(); 

            try
            { 
                executeReader(query);
                buildResponse(res);
            }
            catch (Exception ex)
            {
                logError("ExecQuery", ex.Message, query);
            }

            return res;
        }

        public override Response ExecProc(string proc, string valores = null)
        {
             
            var res = new Response();
			  
            try
            {

                executeProc(proc, valores);
                buildResponse(res);
            }
            catch (Exception ex)
            {
                logError("ExecProc", ex.Message, $"EXECUTE->> {proc} {valores}");
            }

            return res;
        } 

        public override Response ExecQueryWithParams(string proc, string valores = null)
        { 

            var res = new Response();
            var tmp = valores;
			var valorArray = valores.Split(',')
                                            .Select(v => v.Trim()) // Remove espaços em branco
                                            .Select(v => $"'{v}'") // Adiciona aspas simples
                                            .ToArray();

                    // Junta os valores formatados de volta em uma string
                    valores = string.Join(", ", valorArray);
					
            try
            {
                 
                executeProc(proc, valores);
                buildResponse(res);
            }
            catch (Exception ex)
            {
                logError("ExecProcWithParams", ex.Message, proc);
            }

            return res;
        }

        public override Response ExecProcWithParams(string proc, object[] _params = null)
        {
            string valores = "";
           
            if (_params != null)
            {
                foreach (var item in _params)
                {
                    valores += item.ToString() + ",";
                }
                 
            }
             

            var res = new Response();
            try
            {
                executeProc(proc, valores);
                buildResponse(res);
            }
            catch (Exception ex)
            {
                logError("ExecProcWithParams", ex.Message, proc);
            }

            return res;
        }

        public override Response ExecProcWithParams(
            string proc, 
            string valor = null)
        {
            var res = new Response();

            try
            {
                executeProc(proc, valor);
                buildResponse(res);
            }
            catch (Exception ex)
            {
                logError("ExecProcWithParams", ex.Message, proc);
            }

            return res;
        }
         
      
        private void executeProc(string proc, string valores)
        { 
            executeReader($"EXEC {proc} {valores}");
        }


        private void executeReader(string proc)
        {
            ensureConnected();
            var stmt = new OdbcStmt();
            m_ctx_db._mssql.hStmt = stmt;

            using (var cmd = new OdbcCommand(proc, m_ctx_db._mssql.hDbc))
            {
                cmd.CommandTimeout = 300;

                using (var reader = cmd.ExecuteReader())
                {
                    // Variáveis temporárias para armazenar o último resultado válido encontrado
                    List<string> lastColumns = new List<string>();
                    List<object[]> lastRows = new List<object[]>();
                    bool foundAnyData = false;

                    do
                    {
                        // Se este conjunto de resultados tiver colunas, tratamos como um candidato a dado real
                        if (reader.FieldCount > 0)
                        {
                            foundAnyData = true;
                            lastColumns.Clear();
                            lastRows.Clear();

                            // 1. Guarda os nomes das colunas deste resultado atual
                            for (int i = 0; i < reader.FieldCount; i++)
                                lastColumns.Add(reader.GetName(i));

                            // 2. Lê todas as linhas deste resultado atual
                            OdbcDataReaderEx readerEx = new OdbcDataReaderEx(reader);
                            while (reader.Read())
                            {
                                var row = new object[reader.FieldCount];
                                for (int i = 0; i < reader.FieldCount; i++)
                                    row[i] = readerEx.GetSafeValue(i);

                                lastRows.Add(row);
                            }
                        }
                    } while (reader.NextResult()); // Pula para o próximo (o último SELECT vencerá)

                    // Se não encontrou absolutamente nada em nenhum dos resultados, sai
                    if (!foundAnyData) return;

                    // 3. Agora que o loop acabou, o 'lastColumns' e 'lastRows' contém o ÚLTIMO SELECT da procedure
                    foreach (var colName in lastColumns)
                        stmt.Columns.Add(colName);

                    foreach (var rowData in lastRows)
                        stmt.Rows.Add(rowData);
                }
            }
        }

        private void buildResponse(Response res)
        {
            var stmt = (OdbcStmt)m_ctx_db._mssql.hStmt;
            uint numResults = 0;
            int numRows = 0;

            if (stmt != null && stmt.Rows.Count == 1)
            {
                numResults = 1;
            }
            if (stmt.Rows.Count > 1)
            {
                numResults = (uint)stmt.Rows.Count - 1;
            }
            numRows = (int)stmt.Columns.Count;
            res.setRowsAffected(numRows);
            if (numResults > 0)
            {
                foreach (var item in stmt.Rows)
                {
                    res.addResultSet(new Result_Set(
                  (uint)Result_Set.STATE_TYPE.HAVE_DATA,
                  numResults,
                  (uint)numRows,
                  item));
                }
            }
        }

        private void ensureConnected()
        {
            if (!is_connected())
                connect();
        }

        public override string makeEscapeKeyword(string value)
        {
            return $"[{value}]";
        }

        private void handleSchemaError(string sql, OdbcException ex)
        {
            foreach (OdbcError err in ex.Errors)
            {
                switch (err.SQLState)
                {
                    case "42S02": // tabela não existe
                        logSchema("executeReader", err, sql);
                        return;

                    case "42S22": // coluna não existe
                        logSchema("executeReader", err, sql);
                        return;

                    case "3F000": // schema inválido
                        logSchema("executeReader", err, sql);
                        return;

                    case "42000": // permissão / proc inválida
                        logSchema("executeReader", err, sql);
                        return;
                    case "IM002":
                        logSchema("Connection", err, sql);
                        break;
                }
            }

            // erro genérico
            logSchema("ODBC_ERROR", ex.Errors[0], sql);
        }

        private void logSchema(string type, OdbcError err, string sql)
        {
            _smp.message_pool.getInstance().push(
                new message(
                    $"[mssql::{type}][ErrorCode: {err.SQLState}/{err.NativeError}, {err.Message}]",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        private void logError(string where, string msg, string sql)
        {
            _smp.message_pool.getInstance().push(
                new message(
                    $"[mssql::{where}][Error] {msg} | SQL: {sql}",
                    type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }
}