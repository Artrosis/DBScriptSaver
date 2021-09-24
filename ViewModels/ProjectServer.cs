using Newtonsoft.Json;
using PropertyChanged;
using System.Collections.Generic;
using System.Data;
using DBScriptSaver.Core;
using System.Data.Common;

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
        public DBType Type { get; set; }
        public ProjectServer(DBScriptViewModel vm) : base()
        {
            this.vm = vm;
        }

        public override string ToString()
        {
            return Name;
        }
        [JsonIgnore]
        public List<string> DBNames => GetDataBases();

        private IDBQueryHelper helper;
        public IDBQueryHelper GetDBQueryHelper()
        {
            if (helper == null)
            {
                helper = DBQueryHelperFactory.Create(Type, Path, DBLogin, GetPassword());
            }
            return helper;
        }
        private List<string> GetDataBases()
        {
            List<string> list = new List<string>();

            using (DbConnection con = GetDBQueryHelper().GetConnection())
            {
                con.Open();

                using (DbCommand cmd = con.CreateCommand())
                {
                    cmd.CommandText = GetDBQueryHelper().GetDataBaseQuery();

                    using (IDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(dr[0].ToString());
                        }
                    }
                }
            }
            return list;
        }

        private string GetPassword()
        {
            return Cryptography.Decrypt(DBPassword, fmProjectsEditor.GetSalt());
        }
    }
}