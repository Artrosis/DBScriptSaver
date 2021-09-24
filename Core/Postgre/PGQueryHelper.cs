using Npgsql;
using System.Data.Common;

namespace DBScriptSaver.Core
{
    internal class PGQueryHelper : IDBQueryHelper
    {
        private string path;
        private string login;
        private string pass;

        public PGQueryHelper(string path, string login, string pass)
        {
            this.path = path;
            this.login = login;
            this.pass = pass;
        }

        public DbConnection GetConnection(string dBName = null)
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder()
            {
                Host = path,
                Database = dBName ?? @"postgres",
                Username = login,
                Password = pass,
                Timeout = 3,
                ApplicationName = "DBScriptSaver"
            };

            return new NpgsqlConnection(builder.ConnectionString);
        }
        public string GetDataBaseQuery()
        {
            return @"SELECT datname AS name 
                    FROM pg_database
                    ORDER BY datname";
        }

        public bool CheckConnection()
        {
            using (DbConnection con = GetConnection())
            {
                con.Open();
                return true;
            }
        }

        public string GetSchemasQuery()
        {
            return @"SELECT ""schema_name"" 
                        FROM information_schema.schemata
                        WHERE  ""schema_name"" NOT LIKE 'pg_%'
                               AND ""schema_name"" <> 'information_schema'";
        }

        public string GetStoredProceduresQuery()
        {
            //Т.к. хранимок в PostGre нет, никаких строк не должно возвращаться
            return @"SELECT *
                FROM   pg_catalog.pg_namespace  AS s
                WHERE s.nspowner <> s.nspowner";
        }

        public string GetFunctionsQuery()
        {
            return @"SELECT ""routine_schema"",
                           ""routine_name""
                    FROM information_schema.routines
                    WHERE  ""routine_schema"" NOT LIKE 'pg_%'
                           AND ""routine_schema"" != 'information_schema'";
        }

        public string GetTablesQuery()
        {
            return @"SELECT ""table_schema"",
                           ""table_name""
                    FROM information_schema.tables
                    WHERE  ""table_schema"" NOT LIKE 'pg_%'
                           AND ""table_schema"" != 'information_schema'";
        }
    }
}