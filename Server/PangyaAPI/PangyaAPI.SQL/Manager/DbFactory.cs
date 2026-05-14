using System;
using System.Data.SqlClient;
using static PangyaAPI.SQL.ctx_db;

namespace PangyaAPI.SQL.Manager
{
    public static class DbFactory
    {
        public static mssql _mssql;
        public static database Create(ctx_db ctx)
        {
            if (ctx == null || string.IsNullOrEmpty(ctx.engine))
                throw new ArgumentNullException(nameof(ctx), "ctx_db ou engine não informado");

            switch (ctx.engine.ToUpper())
            {
                case "MSSQL":
                case "SQLSERVER":
                    {
                        return new mssql(ctx);
                    }
                case "MYSQL":
                    return new mysql(ctx);

                //case "POSTGRESQL":
                //case "PGSQL":
                //    return new postgresql(ctx);

                default:
                    throw new NotSupportedException($"Engine '{ctx.engine}' não suportada");
            }
        }
    }
}
