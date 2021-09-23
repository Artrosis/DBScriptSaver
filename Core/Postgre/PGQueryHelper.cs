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
    }
}