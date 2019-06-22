using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Ude;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ProjectDataBase
    {
        public ProjectDataBase(Project Project)
        {
            this.Project = Project;
        }
        public string Name { get; set; }
        public string Path { get; set; }

        [JsonIgnoreAttribute]
        public Project Project { get; set; }

        public List<string> traceProcedures = new List<string>();

        public string BaseFolder => Project.Path + System.IO.Path.DirectorySeparatorChar + Path + System.IO.Path.DirectorySeparatorChar;
        public string SourceFolder => BaseFolder + "source" + System.IO.Path.DirectorySeparatorChar;
        public string FilterFile => BaseFolder + "ObjectsFilter.cfg";

        internal void UpdateTraceProcedures()
        {
            if (File.Exists(FilterFile))
            {
                XElement storedProcedures = XElement.Parse(File.ReadAllText(FilterFile));

                storedProcedures.Elements().ToList().ForEach(sp => traceProcedures.Add(sp.Value));
            }
        }

        internal void UpdateScripts()
        {
            DirectoryInfo d = new DirectoryInfo(SourceFolder);
            Dictionary<string, Tuple<string, DateTime>> SourcesData = d.GetFiles(@"*.sql", SearchOption.TopDirectoryOnly)
                                    .OrderBy(f => f.LastWriteTime)
                                    .ToDictionary(f => f.Name, f => new Tuple<string, DateTime>(File.ReadAllText(f.FullName, GetEncoding(f.FullName)), f.LastWriteTime));

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder()
            {
                DataSource = Project.Server,
                InitialCatalog = Name ?? @"master",
                UserID = "Kobra_main",
                Password = "Ggv123",
                ConnectTimeout = 3
            };

            using (SqlConnection conn = new SqlConnection(builder.ConnectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT s.[name] + N'.' + o.[name] AS ObjectName, sm.[definition], o.[TYPE]" + Environment.NewLine
                                + @"FROM   sys.sql_modules   AS sm" + Environment.NewLine
                                + @"       JOIN sys.objects  AS o" + Environment.NewLine
                                + @"            ON  o.[object_id] = sm.[object_id]" + Environment.NewLine
                                + @"       JOIN sys.schemas  AS s" + Environment.NewLine
                                + @"            ON  o.[schema_id] = s.[schema_id]" + Environment.NewLine
                                + $"WHERE  sm.object_id IN ({SourcesData.Keys.ToList().GetObjectIdString()})";

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string FileName = ((string)r["ObjectName"]) + ".sql";
                        string TextFromDB = ((string)r["definition"]);

                        string SourcesKey = SourcesData.Keys.Select(k => new { Value = k, Upper = k.ToUpper() }).SingleOrDefault(k => k.Upper == FileName.ToUpper())?.Value;
                        Tuple<string, DateTime> SavedText = SourcesData[SourcesKey];
                        string TextFromFile = SavedText?.Item1;

                        if ((TextFromFile == null) || (TextFromFile != TextFromDB))
                        {
                            File.WriteAllText(SourceFolder + FileName, TextFromDB, new UTF8Encoding(true));
                        }
                    }
                }
            }
        }
        private static Encoding GetEncoding(string FullFileName)
        {
            var detector = new CharsetDetector();
            byte[] bytes = File.ReadAllBytes(FullFileName);
            detector.Feed(bytes, 0, bytes.Length);
            detector.DataEnd();
            string encoding = detector.Charset;
            if (encoding == "windows-1255")
            {
                encoding = "windows-1251";
            }
            Encoding enc = Encoding.GetEncoding(encoding);
            return enc;
        }
    }
}