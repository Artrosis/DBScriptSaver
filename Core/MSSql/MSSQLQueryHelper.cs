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
    }
}