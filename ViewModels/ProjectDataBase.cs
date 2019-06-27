using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
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

        public ObservableCollection<Procedure> Procedures = new ObservableCollection<Procedure>();
        public ListCollectionView EditProcedures
        {
            get
            {
                return new ListCollectionView(Procedures);
            }
        }

        public ObservableCollection<Function> Functions = new ObservableCollection<Function>();
        public ListCollectionView EditFunctions
        {
            get
            {
                return new ListCollectionView(Functions);
            }
        }

        public List<Sch> Schemas = new List<Sch>();
        

        public string BaseFolder => Project.Path + System.IO.Path.DirectorySeparatorChar + Path + System.IO.Path.DirectorySeparatorChar;
        public string SourceFolder => BaseFolder + "source" + System.IO.Path.DirectorySeparatorChar;
        public string FilterFile => BaseFolder + "ObjectsFilter.cfg";

        internal void UpdateFilterDataFromConfig()
        {
            if (File.Exists(FilterFile))
            {
                XElement DBObjects = XElement.Parse(File.ReadAllText(FilterFile));

                if (DBObjects.Elements(XName.Get("Schemas")).Count() > 0)
                {
                    XElement XSchemas = DBObjects.Element(XName.Get("Schemas"));
                    XSchemas.Elements().ToList().ForEach(s => Schemas.Add(new Sch(s)));
                }

                if (DBObjects.Elements(XName.Get("Procedures")).Count() > 0)
                {
                    XElement storedProcedures = DBObjects.Element(XName.Get("Procedures"));
                    storedProcedures.Elements().ToList().ForEach(sp => Procedures.Add(new Procedure(sp)));
                }

                if (DBObjects.Elements(XName.Get("Functions")).Count() > 0)
                {
                    XElement storedFunctions = DBObjects.Element(XName.Get("Functions"));
                    storedFunctions.Elements().ToList().ForEach(f => Functions.Add(new Function(f)));
                }
            }
        }

        internal void UpdateScripts()
        {
            UpdateFilterDataFromConfig();

            DirectoryInfo d = new DirectoryInfo(SourceFolder);
            Dictionary<string, Tuple<string, DateTime>> SourcesData = d.GetFiles(@"*.sql", SearchOption.TopDirectoryOnly)
                                    .OrderBy(f => f.LastWriteTime)
                                    .ToDictionary(f => f.Name, f => new Tuple<string, DateTime>(File.ReadAllText(f.FullName, GetEncoding(f.FullName)), f.LastWriteTime));

            foreach (var procedure in Procedures.Where(p => p.IsTrace))
            {
                if (!SourcesData.Keys.Contains(procedure.FullName + ".sql"))
                {
                    SourcesData.Add(procedure.FullName + ".sql", new Tuple<string, DateTime>("", DateTime.Now));
                }
            }

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