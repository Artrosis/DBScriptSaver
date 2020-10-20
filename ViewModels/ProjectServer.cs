using Newtonsoft.Json;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Data.SqlClient;
using System.Windows.Data;
using System.Collections.Generic;
using System.Data;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ProjectServer
    {
        [JsonIgnore]
        public DBScriptViewModel vm;
        public string Name { get; set; }
        public string Path { get; set; }
        public string DBLogin { get; set; }
        public string DBPassword { get; set; }
        public ProjectServer(DBScriptViewModel vm) : base()
        {
            this.vm = vm;
        }  

        public override string ToString()
        {
            return Name;
        }

        public string GetConnectionString()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder()
            {
                DataSource = Path,
                InitialCatalog = @"master",
                UserID = DBLogin,
                Password = Cryptography.Decrypt(DBPassword, fmProjectsEditor.GetSalt()),
                ConnectTimeout = 3
            };
            return builder.ConnectionString;
        }
        public List<string> GetNamesOfDB()
        {
            List<string> list = new List<string>();

            using (SqlConnection con = new SqlConnection(GetConnectionString()))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT name FROM master.sys.databases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb');", con))
                using (IDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(dr[0].ToString());
                    }
                }
            }
            return list;
        }
    }
}