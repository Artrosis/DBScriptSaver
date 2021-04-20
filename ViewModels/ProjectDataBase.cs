using DBScriptSaver.Helpers;
using DBScriptSaver.Parse;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
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

        [JsonIgnore]
        public Project Project { get; set; }
        [JsonIgnore]
        public ObservableCollection<Procedure> Procedures = new ObservableCollection<Procedure>();
        [JsonIgnore]
        public ListCollectionView EditProcedures
        {
            get
            {
                return new ListCollectionView(Procedures);
            }
        }

        internal void SaveDependencies()
        {
            File.WriteAllText(DependenciesFile, JsonConvert.SerializeObject(Dependencies, Formatting.Indented));
        }

        [JsonIgnore]
        public ObservableCollection<Function> Functions = new ObservableCollection<Function>();
        [JsonIgnore]
        public ListCollectionView EditFunctions
        {
            get
            {
                return new ListCollectionView(Functions);
            }
        }

        [JsonIgnore]
        public ObservableCollection<Sch> Schemas = new ObservableCollection<Sch>();
        [JsonIgnore]
        public ListCollectionView EditSchemas
        {
            get
            {
                return new ListCollectionView(Schemas);
            }
        }

        [JsonIgnore]
        public ObservableCollection<Tbl> Tables = new ObservableCollection<Tbl>();
        [JsonIgnore]
        public ListCollectionView EditTables
        {
            get
            {
                return new ListCollectionView(Tables);
            }
        }

        public List<DependenceObject> GetDbObjects()
        {
            DirectoryInfo d = new DirectoryInfo(SourceFolder);

            d.GetFiles(@"*.sql", SearchOption.TopDirectoryOnly)
                .ToList().ForEach(f =>
                {
                    if (f.Name.Contains(@".UserDefinedFunction"))
                    {
                        File.Move(f.FullName, f.DirectoryName + System.IO.Path.DirectorySeparatorChar + f.Name.Replace(@".UserDefinedFunction", ""));
                    }
                    if (f.Name.Contains(@".StoredProcedure"))
                    {
                        File.Move(f.FullName, f.DirectoryName + System.IO.Path.DirectorySeparatorChar + f.Name.Replace(@".StoredProcedure", ""));
                    }
                });

            return d.GetFiles(@"*.sql", SearchOption.TopDirectoryOnly)
                    .Select(f => new DependenceObject() { ObjectType = "source", ObjectName = f.Name })
                    .ToList();
        }

        internal void RevertObject(ScriptWrapper obj)
        {
            string objectFileName = obj.FileName;
            bool delete = !File.Exists(SourceFolder + objectFileName);

            string Script = delete ? obj.ScriptText 
                                    : File.ReadAllText(SourceFolder + objectFileName);

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                var DelCmd = conn.CreateCommand();

                if (Script.ToUpper().Contains("CREATE PROCEDURE".ToUpper()))
                {
                    DelCmd.CommandText = $@"DROP PROCEDURE [{objectFileName.GetSchema()}].[{objectFileName.GetName()}]";
                }

                if (Script.ToUpper().Contains("CREATE FUNCTION".ToUpper()))
                {
                    DelCmd.CommandText = $@"DROP FUNCTION [{objectFileName.GetSchema()}].[{objectFileName.GetName()}]";
                }

                if (Script.ToUpper().Contains("CREATE TRIGGER".ToUpper()))
                {
                    DelCmd.CommandText = $@"DROP TRIGGER [{objectFileName.GetSchema()}].[{objectFileName.GetName()}]";
                }

                DelCmd.ExecuteNonQuery();

                if (!delete)
                {
                    Server server = new Server(new ServerConnection(conn));
                    server.ConnectionContext.ExecuteNonQuery(Script);
                }
            }
        }

        public List<DependenceObject> GetChanges()
        {
            DirectoryInfo d = new DirectoryInfo(ChangesFolder);

            return d.GetFiles(@"*.sql", SearchOption.TopDirectoryOnly)
                    .Select(f => new DependenceObject() { ObjectType = "change", ObjectName = f.Name })
                    .ToList();
        }

        public List<DependenceObject> GetScripts()
        {
            List<DependenceObject> result = new List<DependenceObject>();
            result.AddRange(GetDbObjects());
            result.AddRange(GetChanges());
            return result
                    .OrderBy(o => o.ObjectName)
                    .ToList();
        }

        [JsonIgnore]
        public ListCollectionView ViewScripts
        {
            get
            {
                return new ListCollectionView(GetScripts());
            }
        }

        [JsonIgnore]
        public ObservableCollection<Dependence> Dependencies = new ObservableCollection<Dependence>();
        [JsonIgnore]
        public ListCollectionView EditDependencies
        {
            get
            {
                return new ListCollectionView(Dependencies);
            }
        }

        [JsonIgnore]
        public string BaseFolder => Project.Path + System.IO.Path.DirectorySeparatorChar + Path + System.IO.Path.DirectorySeparatorChar;
        [JsonIgnore]
        public string SourceFolder => BaseFolder + @"source" + System.IO.Path.DirectorySeparatorChar;
        [JsonIgnore]
        public string ChangesFolder => BaseFolder + @"changes" + System.IO.Path.DirectorySeparatorChar;
        [JsonIgnore]
        public string TableFolder => BaseFolder + @"tables" + System.IO.Path.DirectorySeparatorChar;
        [JsonIgnore]
        public string ChangesXML => ChangesFolder + @"changes.xml";
        [JsonIgnore]
        public string FilterFile => BaseFolder + @"ObjectsFilter.cfg";
        [JsonIgnore]
        public string DependenciesFile => BaseFolder + @"Dependencies.cfg";

        internal void UpdateDBObjects()
        {
            if (File.Exists(DependenciesFile))
            {
                try
                {
                    Dependencies.Clear();
                    JsonConvert
                        .DeserializeObject<List<Dependence>>(File.ReadAllText(DependenciesFile))
                        .ForEach(d => Dependencies.Add(d));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($@"Не удалось определить зависимости. {ex.Message}");
                    MessageBox.Show($@"Не удалось определить зависимости. {ex.Message}");
                }
            }
        }

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

        public List<Script> GetUpdateScripts()
        {
            UpdateFilterDataFromConfig();

            if (!Directory.Exists(SourceFolder))
            {
                Directory.CreateDirectory(SourceFolder);
            }

            DirectoryInfo d = new DirectoryInfo(SourceFolder);

            d.GetFiles(@"*.sql", SearchOption.TopDirectoryOnly)
                .ToList().ForEach(f =>
                {
                    if (f.Name.Contains(@".UserDefinedFunction"))
                    {
                        File.Move(f.FullName, f.DirectoryName + System.IO.Path.DirectorySeparatorChar + f.Name.Replace(@".UserDefinedFunction", ""));
                    }
                    if (f.Name.Contains(@".StoredProcedure"))
                    {
                        File.Move(f.FullName, f.DirectoryName + System.IO.Path.DirectorySeparatorChar + f.Name.Replace(@".StoredProcedure", ""));
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

                var (ОтслеживаемыеСхемы, ИгнорируемыеСхемы, ОтслеживаемыеОбъекты, ИгнорируемыеОбъекты) = GetFilters();

                List<string> @objects = SourcesData.Keys.ToList();

                if (ОтслеживаемыеОбъекты != null && ОтслеживаемыеОбъекты.Count > 0)
                {
                    @objects.AddRange(ОтслеживаемыеОбъекты.Select(o => o + ".sql").ToList());
                    @objects = @objects.Distinct().ToList();
                }

                cmd.CommandText = @"SELECT s.[name] + N'.' + o.[name] AS ObjectName, sm.[definition], o.[TYPE]," + Environment.NewLine
                                + @"ISNULL(sm.uses_ansi_nulls, 0) AS uses_ansi_nulls," + Environment.NewLine
                                + @"ISNULL(sm.uses_quoted_identifier, 0) AS uses_quoted_identifier" + Environment.NewLine
                                + @"FROM   sys.sql_modules   AS sm" + Environment.NewLine
                                + @"       JOIN sys.objects  AS o" + Environment.NewLine
                                + @"            ON  o.[object_id] = sm.[object_id]" + Environment.NewLine
                                + @"       JOIN sys.schemas  AS s" + Environment.NewLine
                                + @"            ON  o.[schema_id] = s.[schema_id]" + Environment.NewLine
                                + $"WHERE" + Environment.NewLine;

                string condition = "";

                if (@objects.Count > 0)
                {
                    condition = $"sm.object_id IN ({@objects.GetObjectIdString()})";
                }

                if (ОтслеживаемыеСхемы != null && ОтслеживаемыеСхемы.Count > 0)
                {
                    if (condition != "")
                    {
                        condition += Environment.NewLine;
                        condition += " OR ";
                    }
                    condition += $"s.[name] IN ({ОтслеживаемыеСхемы.GetObjectsList()})";
                }

                if (ИгнорируемыеОбъекты != null && ИгнорируемыеОбъекты.Count > 0)
                {
                    if (condition != "")
                    {
                        condition = $@"({condition})" + Environment.NewLine;
                        condition += " AND ";
                    }
                    condition += $"sm.object_id NOT IN ({ИгнорируемыеОбъекты.Select(s => s + ".sql").ToList().GetObjectIdString()})";
                }

                cmd.CommandText += condition;

                List<Script> UpdateScripts = new List<Script>();

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string FileName = ((string)r["ObjectName"]) + ".sql";

                        string objectType = objectTypeDescription((string)r["TYPE"]);

                        string TextFromDB = string.Empty;

                        if ((bool)r["uses_ansi_nulls"])
                        {
                            TextFromDB += @"SET ANSI_NULLS ON" + Environment.NewLine;
                            TextFromDB += @"GO" + Environment.NewLine;
                        }

                        if ((bool)r["uses_quoted_identifier"])
                        {
                            TextFromDB += @"SET QUOTED_IDENTIFIER ON" + Environment.NewLine;
                            TextFromDB += @"GO" + Environment.NewLine;
                        }

                        TextFromDB += (string)r["definition"];

                        string SourcesKey = SourcesData
                                            .Keys
                                            .Select(k => new { Value = k, Upper = k.ToUpper() })
                                            .SingleOrDefault(k => k.Upper == FileName.ToUpper())
                                            ?.Value;
                        string TextFromFile = (SourcesKey != null) ? SourcesData[SourcesKey].Item1 : null;

                        if ((TextFromFile == null) || (TextFromFile != TextFromDB))
                        {
                            ChangeType ChangeType = (TextFromFile == null) ? ChangeType.Новый : ChangeType.Изменённый;
                            
                            UpdateScripts.Add( 
                                new Script()
                                {
                                    FileName = FileName,
                                    FullPath = SourceFolder + FileName,
                                    ScriptText = TextFromDB,
                                    ObjectType = objectType,
                                    ChangeState = ChangeType
                                });
                        }
                    }
                }

                Server server = new Server(new ServerConnection(conn));
                var dataBase = server.Databases.Cast<Database>().Single(db => db.Name.ToUpper() == Name.ToUpper());

                if (ОтслеживаемыеСхемы != null)
                {
                    var tbls = dataBase.Tables.Cast<object>().Cast<Table>().Where(t => ОтслеживаемыеСхемы.Contains(t.Schema)).ToList();

                    foreach (var tbl in tbls)
                    {
                        string fileName = $@"{tbl.Schema}.{tbl.Name}.sql";
                        string script = "";
                        tbl.Script().Cast<string>().ToList().ForEach(l =>
                        {
                            if (l != "")
                            {
                                script += "GO" + Environment.NewLine;
                            }
                            script += l + Environment.NewLine;
                        });

                        string tableFileName = TableFolder + fileName;

                        if (File.Exists(tableFileName) && script == File.ReadAllText(tableFileName))
                        {
                            continue;
                        }

                        ChangeType ChangeType = !File.Exists(tableFileName) ? ChangeType.Новый : ChangeType.Изменённый;

                        Script tblScript = new Script()
                        {
                            FileName = fileName,
                            FullPath = TableFolder + fileName,
                            ScriptText = script,
                            ObjectType = @"Таблица",
                            ChangeState = ChangeType
                        };

                        switch (ChangeType)
                        {
                            case ViewModels.ChangeType.Новый:
                                tblScript.Migrations = MakeCreateTableMigration(server, tbl);
                                break;
                            case ViewModels.ChangeType.Изменённый:
                                tblScript.Migrations = MakeAlterTableMigration(server, tbl, File.ReadAllText(tableFileName));
                                break;
                        }

                        UpdateScripts.Add(tblScript);
                    }
                }

                return UpdateScripts;
            }
        }
        private List<Migration> MakeAlterTableMigration(Server server, Table tbl, string sourceScript)
        {
            return TableComparer.GetChanges(server.GetScript(tbl), sourceScript);
        }
        public List<Migration> MakeCreateTableMigration(Server myServer, Table tbl)
        {
            return new List<Migration>()
            { 
                new Migration()
                {
                    Name = FileHelper.CreateMigrationName(tbl.Name),
                    Script = myServer.GetScript(tbl) 
                }
            };
        }        

        private static readonly Dictionary<string, string> TypeDescriptions = new Dictionary<string, string>()
        {
            { "C ", @"Ограничение: проверка"},
            { "D ", @"Ограничение: значение по умолчанию"},
            { "F ", @"Внешний ключ"},
            { "FN", @"Функция"},
            { "IF", @"Встраиваемая табличная функция"},
            { "IT", @"Внутренняя таблица"},
            { "P ", @"Хранимая процедура"},
            { "PK", @"Основной ключ"},
            { "S ", @"Системная таблица"},
            { "SN", @"Синоним"},
            { "SQ", @"Служба очередей"},
            { "TF", @"Табличная функция"},
            { "TR", @"Триггер"},
            { "TT", @"Табличный тип"},
            { "U ", @"Таблица"},
            { "UQ", @"Ограничение на уникальность"},
            { "V ", @"Представление"}
        };

        private static string objectTypeDescription(string type)
        {
            return TypeDescriptions[type] ?? type;
        }

        private (List<string> ОтслеживаемыеСхемы, List<string> ИгнорируемыеСхемы, List<string> ОтслеживаемыеОбъекты, List<string> ИгнорируемыеОбъекты) GetFilters()
        {
            if (!File.Exists(FilterFile))
            {
                return (null, null, null, null);
            }

            XDocument xFilter = XDocument.Load(FilterFile);

            List<string> schemas = xFilter
                                    .Element("DBObjects")
                                    .Element("Schemas")
                                    .Elements("Schema")
                                    .Where(e => e.Attribute("State").Value == ObjectState.Отслеживаемый.ToString())
                                    .Select(e => e.Value)
                                    .ToList();

            List<string> ignoreSchemas = xFilter
                                        .Element("DBObjects")
                                        .Element("Schemas")
                                        .Elements("Schema")
                                        .Where(e => e.Attribute("State").Value == ObjectState.Игнорируемый.ToString())
                                        .Select(e => e.Value)
                                        .ToList();

            List<string> @objects = xFilter
                                    .Element("DBObjects")
                                    .Element("Procedures")
                                    .Elements("Procedure")
                                    .Where(e => e.Attribute("State").Value == ObjectState.Отслеживаемый.ToString())
                                    .Select(e => e.Value)
                                    .ToList();

            List<string> functions = xFilter
                                    .Element("DBObjects")
                                    .Element("Functions")
                                    .Elements("Function")
                                    .Where(e => e.Attribute("State").Value == ObjectState.Отслеживаемый.ToString())
                                    .Select(e => e.Value)
                                    .ToList();

            @objects.AddRange(functions);

            List<string> ignoreObjects = xFilter
                                        .Element("DBObjects")
                                        .Element("Procedures")
                                        .Elements("Procedure")
                                        .Where(e => e.Attribute("State").Value == ObjectState.Игнорируемый.ToString())
                                        .Select(e => e.Value)
                                        .ToList();

            

            List<string> ignoreFunctions = xFilter
                                            .Element("DBObjects")
                                            .Element("Functions")
                                            .Elements("Function")
                                            .Where(e => e.Attribute("State").Value == ObjectState.Игнорируемый.ToString())
                                            .Select(e => e.Value)
                                            .ToList();

            ignoreObjects.AddRange(ignoreFunctions);

            return (schemas, ignoreSchemas, @objects, ignoreObjects);
        }

        public void UpdateScripts(bool UseMigrations)
        {
            UpdateScripts(GetUpdateScripts(), UseMigrations);
        }

        public void UpdateScripts(List<Script> scripts, bool UseMigrations)
        {
            foreach (var script in scripts)
            {
                string dir = System.IO.Path.GetDirectoryName(script.FullPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(script.FullPath, script.ScriptText, new UTF8Encoding(true));

                if (!UseMigrations)
                {
                    continue;
                }

                if (script.Migrations != null)
                {
                    CreateChangesXML();
                    script
                        .Migrations
                        .Where(m => m.Script != null)
                        .ToList()
                        .ForEach(m => AddMigration(m));
                }
            }
        }

        public void AddMigration(Migration migration)
        {
            string NewFileName = migration.Name + ".sql";

            if (!Directory.Exists(ChangesFolder))
            {
                Directory.CreateDirectory(ChangesFolder);
            }

            File.WriteAllText(ChangesFolder + NewFileName, migration.Script, new UTF8Encoding(true));

            CreateChangesXML();

            XDocument xdoc = XDocument.Load(ChangesXML);

            var LastVer = xdoc.Element("project").Elements("ver").Last();

            var NewElement = new XElement("file", NewFileName);
            NewElement.Add(new XAttribute("autor", Environment.MachineName));
            NewElement.Add(new XAttribute("date", DateTime.Now.ToShortDateString()));

            LastVer.Add(NewElement);

            xdoc.Save(ChangesXML);
        }

        private bool HasChangesXML = false;

        public void CreateChangesXML()
        {
            if (HasChangesXML)
            {
                return;
            }

            if (File.Exists(ChangesXML))
            {
                XDocument xdoc = XDocument.Load(ChangesXML);

                if (xdoc.Elements("project").Count() > 0)
                {
                    if (xdoc.Element("project").Elements("ver").Count() > 0)
                    {
                        HasChangesXML = true;
                        return;
                    }
                }

                File.Delete(ChangesXML);
            }

            XDocument EmptyChanges = new XDocument(
                new XElement("project",
                    new XAttribute("name", Project.Name),
                    new XElement("ver",
                        new XAttribute("id", "1.0.0.0"),
                        new XAttribute("date", DateTime.Now.ToString("d"))
                        )
                    )
                );

            EmptyChanges.Save(ChangesXML);

            HasChangesXML = true;
        }

        public string GetConnectionString()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder()
            {
                DataSource = Project.Server.Path,
                InitialCatalog = Name ?? @"master",
                UserID = Project.Server.DBLogin,
                Password = Cryptography.Decrypt(Project.Server.DBPassword, fmProjectsEditor.GetSalt()),
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