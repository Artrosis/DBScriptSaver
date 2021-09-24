using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace DBScriptSaver.Core
{
    internal class MSSQLQueryHelper : IDBQueryHelper
    {
        private string path;
        private string login;
        private string pass;

        public MSSQLQueryHelper(string path, string login, string pass)
        {
            this.path = path;
            this.login = login;
            this.pass = pass;
        }

        public DbConnection GetConnection(string dBName = null)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder()
            {
                DataSource = path,
                InitialCatalog = dBName ?? @"master",
                UserID = login,
                Password = pass,
                ConnectTimeout = 3,
                ApplicationName = "DBScriptSaver"
            };
            return new SqlConnection(builder.ConnectionString);
        }

        public string GetDataBaseQuery()
        {
            return @"SELECT d.name
                    FROM   sys.databases AS d
                    WHERE  NAME NOT IN('master', 'model', 'msdb', 'pubs', 'tempdb', 'distribution')
                    ORDER BY d.name";
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
            return @"SELECT s.name
                    FROM   sys.schemas        AS s
                            JOIN sys.sysusers  AS u
                                ON  s.principal_id = u.uid
                    WHERE  u.hasdbaccess = 1";
        }

        public string GetStoredProceduresQuery()
        {
            return @"SELECT s.name            AS SchemaName,
                           p.name            AS ProcedureName
                    FROM   sys.procedures    AS p
                           JOIN sys.schemas  AS s
                                ON  p.[schema_id] = s.[schema_id]";
        }

        public string GetFunctionsQuery()
        {
            return @"SELECT s.name            AS SchemaName,
                           f.name            AS FunctionName
                    FROM   sys.objects       AS f
                           JOIN sys.schemas  AS s
                                ON  f.[schema_id] = s.[schema_id]
                    WHERE  f.[type] IN ('FN', 'IF', 'TF')";
        }

        public string GetTablesQuery()
        {
            return @"SELECT s.name            AS SchemaName,
                           o.name            AS TableName
                    FROM   sys.objects       AS o
                           JOIN sys.schemas  AS s
                                ON  o.[schema_id] = s.[schema_id]
                    WHERE  o.[type] = 'U'";
        }
    }
}