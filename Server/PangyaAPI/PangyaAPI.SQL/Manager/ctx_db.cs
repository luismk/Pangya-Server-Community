using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Runtime.InteropServices;
namespace PangyaAPI.SQL
{
    public class ctx_db
    {
        public string engine;
        public string ip;
        public string db_name;
        public string user;
        public string pass;
        public uint port;
        public bool cmd_log;
        public _MSSQL _mssql;
        public _MYSQL _mysql;
        public _POSTGRESQL _postgresql;

        public class _MSSQL
        {
            public OdbcCommand hEnv = new OdbcCommand();
            public OdbcConnection hDbc = new OdbcConnection();
            public OdbcStmt hStmt = new OdbcStmt();

            public void clear()
            {
                hDbc.Dispose();
                hStmt.Dispose();
                hEnv.Dispose();
            }
        }

        public class _MYSQL
        {
            public OdbcCommand hEnv = new OdbcCommand();
            public OdbcConnection hDbc = new OdbcConnection();
            public OdbcStmt hStmt = new OdbcStmt();

            public void clear()
            {
                hDbc.Dispose();
                hStmt.Dispose();
                hEnv.Dispose();
            }
        }

        public class _POSTGRESQL
        {
            public OdbcCommand hEnv = new OdbcCommand();
            public OdbcConnection hDbc = new OdbcConnection();
            public OdbcStmt hStmt = new OdbcStmt();

            public void clear()
            {
                hDbc.Dispose();
                hStmt.Dispose();
                hEnv.Dispose();
            }
        }

        // Método para criar a string de conexão com base no tipo de banco de dados
        public string CreateStrConnection()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            switch (engine.ToUpper())
            {
                case "MSSQL":
                    {
                        if (isWindows)
                        {
                            return $"DSN={ip};DATABASE={db_name};UID={user};PWD={pass};";
                        }

                        else
                        {
                            return
                                "Driver={ODBC Driver 18 for SQL Server};" +
                                $"Server={ip},{port};Database={db_name};Uid={user};Pwd={pass};" +
                                "Encrypt=No;"
                                + "TrustServerCertificate=Yes;";
                        }
                    }
                case "MYSQL":
                    {
                        if (isWindows)
                        {
                            return $"DSN={ip};DATABASE={db_name};UID={user};PWD={pass};";
                        }

                        else
                        {
                            return
                                "Driver={MySQL ODBC 8.0 Unicode Driver};" +
                                $"Server={ip},{port};Database={db_name};User={user};Password={pass};" +
                                "OPTION=3;";
                        }
                    }
                case "POSTGRESQL":
                    return $"Driver={{PostgreSQL Unicode}};Server={ip};Port={port};" +
                           $"Database={db_name};Uid={user};Pwd={pass};" +
                           $"MaxVarcharSize=255;Pooling=true;";
                default:
                    throw new Exception($"Engine '{engine}' não é suportado via ODBC.");
            }
        }
    }

    public sealed class OdbcStmt : IDisposable
    {
        private bool disposedValue;

        public List<string> Columns { get; } = new List<string>();
        public List<object[]> Rows { get; } = new List<object[]>();

        public void Clear()
        {
            Columns.Clear();
            Rows.Clear();
        }
        public void Dispose()
        { Dispose(true); }
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Clear();
                }

                // TODO: liberar recursos não gerenciados (objetos não gerenciados) e substituir o finalizador
                // TODO: definir campos grandes como nulos
                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            // Não altere este código. Coloque o código de limpeza no método 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public sealed class OdbcDataReaderEx : IDisposable
    {
        private readonly OdbcDataReader _reader;

        public OdbcDataReaderEx(OdbcDataReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public int FieldCount => _reader.FieldCount;

        public bool Read() => _reader.Read();

        public string GetName(int i) => _reader.GetName(i);

        public object GetSafeValue(int i)
        {
            if (_reader.GetDataTypeName(i) == null)
            {
                if (_reader.GetValue(i) != null)
                    return _reader.GetValue(i);
                else if (_reader.GetValue(i) == null)
                    return DBNull.Value;
            }

            string sqlType;

            try
            {
                sqlType = _reader.GetDataTypeName(i);
            }
            catch
            {
                return DBNull.Value;
            }


            if (sqlType.Equals("timestamp", StringComparison.OrdinalIgnoreCase))
            {
                return _reader.GetString(i);
            }

            if (sqlType.Equals("time", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return TimeSpan.Parse(_reader.GetString(i));
                }
                catch
                {
                    return DBNull.Value;
                }
            }

            // datetimeoffset costuma dar dor também
            if (sqlType.Equals("datetimeoffset", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return DateTimeOffset.Parse(_reader.GetString(i));
                }
                catch
                {
                    return DBNull.Value;
                }
            }

            try
            {
                return _reader.GetValue(i);
            }
            catch
            {
                try
                {
                    return _reader.GetString(i);
                }
                catch
                {
                    return DBNull.Value;
                }
            }
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
}
