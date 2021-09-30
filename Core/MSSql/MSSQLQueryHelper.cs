using DBScriptSaver.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Data.Common;

namespace DBScriptSaver.Core
{
    internal class MSSQLQueryHelper : IDBQueryHelper
    {
        private readonly string path;
        private readonly string login;
        private readonly string pass;

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
                ApplicationName = "DBScriptSaver",
                MultipleActiveResultSets = true
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
        public string GetDropQuery(string script, string SchemaName, string ObjectName)
        {
            return $@"{DropCommand(script)} [{SchemaName}].[{ObjectName}]";
        }
        private string DropCommand(string script)
        {
            if (script.ToUpper().Contains("CREATE PROCEDURE".ToUpper()))
            {
                return $@"DROP PROCEDURE";
            }

            if (script.ToUpper().Contains("CREATE FUNCTION".ToUpper()))
            {
                return $@"DROP FUNCTION";
            }

            if (script.ToUpper().Contains("CREATE TRIGGER".ToUpper()))
            {
                return $@"DROP TRIGGER";
            }

            if (script.ToUpper().Contains("CREATE TABLE".ToUpper()))
            {
                return $@"DROP TABLE";
            }

            if (script.ToUpper().Contains("CREATE NONCLUSTERED INDEX".ToUpper()))
            {
                return $@"DROP INDEX";
            }

            throw new Exception(@"Неизвестный тип скрипта");
        }
        public void ExecuteNonQuery(DbConnection conn, string Script)
        {
            if (!(conn is SqlConnection))
            {
                throw new Exception($@"Не верный тип подключения: {conn.GetType().FullName}");
            }
            Server server = new Server(new ServerConnection((SqlConnection)conn));
            server.ConnectionContext.StatementTimeout = 30 * 60; //30 минут на выполнение скрипта
            server.ConnectionContext.ExecuteNonQuery(Script);
        }

        public IMigrationMaker GetMigrationMaker(DbConnection dbConnection, IScript script)
        {
            return new MSSQLMigrationMaker(dbConnection, (MSSQLScript)script);
        }

        public IScriptWriter GetScriptWriter(ProjectDataBase projectDataBase)
        {
            return new MSSQLScriptWriter(projectDataBase);
        }
    }
}