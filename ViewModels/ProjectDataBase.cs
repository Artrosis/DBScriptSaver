using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows.Controls;
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
        [JsonIgnoreAttribute]
        public ObservableCollection<Procedure> Procedures = new ObservableCollection<Procedure>();
        [JsonIgnoreAttribute]
        public ListCollectionView EditProcedures
        {
            get
            {
                return new ListCollectionView(Procedures);
            }
        }
        [JsonIgnoreAttribute]
        public ObservableCollection<Function> Functions = new ObservableCollection<Function>();
        [JsonIgnoreAttribute]
        public ListCollectionView EditFunctions
        {
            get
            {
                return new ListCollectionView(Functions);
            }
        }

        [JsonIgnoreAttribute]
        public ObservableCollection<Sch> Schemas = new ObservableCollection<Sch>();
        [JsonIgnoreAttribute]
        public ListCollectionView EditSchemas
        {
            get
            {
                return new ListCollectionView(Schemas);
            }
        }

        [JsonIgnoreAttribute]
        public ObservableCollection<Tbl> Tables = new ObservableCollection<Tbl>();
        [JsonIgnoreAttribute]
        public ListCollectionView EditTables
        {
            get
            {
                return new ListCollectionView(Tables);
            }
        }

        public string BaseFolder => Project.Path + System.IO.Path.DirectorySeparatorChar + Path + System.IO.Path.DirectorySeparatorChar;
        public string SourceFolder => BaseFolder + @"source" + System.IO.Path.DirectorySeparatorChar;
        public string ChangesFolder => BaseFolder + @"changes" + System.IO.Path.DirectorySeparatorChar;
        public string ChangesXML => ChangesFolder + @"changes.xml";
        public string FilterFile => BaseFolder + @"ObjectsFilter.cfg";

        internal void UpdateFilterDataFromConfig()
        {
            if (File.Exists(FilterFile))
            {
                XElement DBObjects = XElement.Parse(File.ReadAllText(FilterFile));

                if (DBObjects.Elements(XName.Get("Schemas")).Count() > 0)
                {
                    XElement XSchemas = DBObjects.Element(XName.Get("Schemas"));
                    foreach (var s in XSchemas.Elements())
                    {
                        var sch = new Sch(s);
                        if (!Schemas.Any(sh => sh.Name == sch.Name))
                        {
                            Schemas.Add(sch);
                        }
                    }
                }

                if (DBObjects.Elements(XName.Get("Procedures")).Count() > 0)
                {
                    XElement storedProcedures = DBObjects.Element(XName.Get("Procedures"));
                    foreach (var s in storedProcedures.Elements())
                    {
                        var proc = new Procedure(s);
                        if (!Procedures.Any(sp => sp.FullName == proc.FullName))
                        {
                            Procedures.Add(proc);
                        }
                    }
                }

                if (DBObjects.Elements(XName.Get("Functions")).Count() > 0)
                {
                    XElement storedFunctions = DBObjects.Element(XName.Get("Functions"));
                    foreach (var fn in storedFunctions.Elements())
                    {
                        var fun = new Function(fn);
                        if (!Functions.Any(f => f.FullName == fun.FullName))
                        {
                            Functions.Add(fun);
                        }
                    }
                }

                if (DBObjects.Elements(XName.Get("Tables")).Count() > 0)
                {
                    XElement storedTables = DBObjects.Element(XName.Get("Tables"));
                    foreach (var t in storedTables.Elements())
                    {
                        var tbl = new Tbl(t);
                        if (!Tables.Any(tb => tb.FullName == tbl.FullName))
                        {
                            Tables.Add(tbl);
                        }
                    }
                }
            }
        }

        public List<(string FileName, string FullPath, string ScriptText)> GetUpdateScripts()
        {
            UpdateFilterDataFromConfig();

            DirectoryInfo d = new DirectoryInfo(SourceFolder);

            d.GetFiles(@"*.sql", SearchOption.TopDirectoryOnly)
                .ToList().ForEach(f =>
                {
                    if (f.Name.Contains(@".UserDefinedFunction"))
                    {
                        File.Move(f.FullName, f.DirectoryName + f.Name.Replace(@".UserDefinedFunction", ""));
                    }
                    if (f.Name.Contains(@".StoredProcedure"))
                    {
                        File.Move(f.FullName, f.DirectoryName + f.Name.Replace(@".StoredProcedure", ""));
                    }
                });

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

            foreach (var function in Functions.Where(f => f.IsTrace))
            {
                if (!SourcesData.Keys.Contains(function.FullName + ".sql"))
                {
                    SourcesData.Add(function.FullName + ".sql", new Tuple<string, DateTime>("", DateTime.Now));
                }
            }

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
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
                    List<(string FileName, string FullPath, string ScriptText)> UpdateScripts = new List<(string FileName, string FullPath, string ScriptText)>();
                    while (r.Read())
                    {
                        string FileName = ((string)r["ObjectName"]) + ".sql";
                        string TextFromDB = ((string)r["definition"]);

                        string SourcesKey = SourcesData.Keys.Select(k => new { Value = k, Upper = k.ToUpper() }).SingleOrDefault(k => k.Upper == FileName.ToUpper())?.Value;
                        Tuple<string, DateTime> SavedText = SourcesData[SourcesKey];
                        string TextFromFile = SavedText?.Item1;

                        if ((TextFromFile == null) || (TextFromFile != TextFromDB))
                        {
                            UpdateScripts.Add((FileName, SourceFolder + FileName, TextFromDB));
                        }
                    }

                    return UpdateScripts;
                }
            }
        }

        public void UpdateScripts()
        {
            UpdateScripts(GetUpdateScripts());
        }

        public void UpdateScripts(List<(string FileName, string FullPath, string ScriptText)> scripts)
        {
            foreach (var script in scripts)
            {
                File.WriteAllText(script.FullPath, script.ScriptText, new UTF8Encoding(true));
            }
        }        

        public string GetConnectionString()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder()
            {
                DataSource = Project.Server,
                InitialCatalog = Name ?? @"master",
                UserID = Project.DBLogin,
                Password = Cryptography.Decrypt(Project.DBPassword, fmProjectsEditor.GetSalt()),
                ConnectTimeout = 3
            };
            return builder.ConnectionString;
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