using DBScriptSaver.Logic;
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
        public string IndexFolder => BaseFolder + @"indexes" + System.IO.Path.DirectorySeparatorChar;
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
            if (!File.Exists(FilterFile))
            {
                return;
            }

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

        public void ObserveScripts(Action<Script> observer, Action<string, int> changeProgress)
        {
            using (var sw = new ScriptWriter(this))
            {
                sw.changeProgress += changeProgress;
                sw.observer += observer;
                sw.ObserveScripts();
            }
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
                
                CreateChangesXML();
                using (var con = new SqlConnection(GetConnectionString()))
                {
                    script
                        .MakeMigrations(new Server(new ServerConnection(con)))
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
                ConnectTimeout = 3,
                MultipleActiveResultSets = true
            };
            return builder.ConnectionString;
        }
    }
}