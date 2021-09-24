using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBScriptSaver.Core
{
    public interface IDBQueryHelper
    {
        DbConnection GetConnection(string dBName = null);
        string GetDataBaseQuery();
        string GetSchemasQuery();
        bool CheckConnection();
        string GetStoredProceduresQuery();
        string GetFunctionsQuery();
        string GetTablesQuery();
        string GetDropQuery(string script, string SchemaName, string ObjectName);
        void ExecuteNonQuery(DbConnection conn, string Script);
        IMigrationMaker GetMigrationMaker(DbConnection dbConnection, Script script);
        IScriptWriter GetScriptWriter(ProjectDataBase projectDataBase);
    }
}
