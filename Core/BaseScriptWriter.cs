using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Ude;

namespace DBScriptSaver.Core
{
    internal abstract class BaseScriptWriter : IScriptWriter
    {
        readonly protected ProjectDataBase db;
        readonly protected DbConnection connection;

        protected BaseScriptWriter(ProjectDataBase projectDataBase)
        {
            db = projectDataBase;
            connection = db.GetConnection();
            connection.Open();
        }

        public string SourceFolder => db.SourceFolder;
        public string TableFolder => db.TableFolder;
        public string IndexFolder => db.IndexFolder;
        public ObservableCollection<Procedure> Procedures => db.Procedures;
        public ObservableCollection<Function> Functions => db.Functions;
        public string FilterFile => db.FilterFile;

        private Action<string, int> _changeProgress;
        public Action<string, int> changeProgress
        {
            get => _changeProgress;
            set => _changeProgress = value;
        }

        private Action<IScript> _observer;
        public Action<IScript> observer
        {
            get => _observer;
            set => _observer = value;
        }
        public void ObserveScripts()
        {
            UpdateFilters();
            changeProgress.Invoke(@"Сравниваем хранимки/функции", 3);
            GetSourceScripts().ForEach(s => observer?.Invoke(s));
            changeProgress.Invoke(@"Сравниваем таблицы", 50);
            LoadChanges();
            GetChangesScripts();
            changeProgress.Invoke(@"", 0);
        }

        protected Dictionary<int, ITableData> tables = new Dictionary<int, ITableData>();
        protected Dictionary<(int tableId, int indexId), IIndexData> indexes = new Dictionary<(int tableId, int indexId), IIndexData>();

        protected abstract void LoadChanges();
        void GetChangesScripts()
        {
            foreach (var p in tables)
            {
                IScript tblScript = p.Value.GetScript();

                if (tblScript == null) continue;

                observer?.Invoke(tblScript);
            }

            changeProgress.Invoke(@"Сравниваем индексы", 80);

            foreach (var p in indexes)
            {
                IScript indexScript = p.Value.GetScript();

                if (indexScript == null) continue;

                observer?.Invoke(indexScript);
            }
        }

        protected List<string> @objects;
        private List<IScript> GetSourceScripts()
        {
            var result = new List<IScript>();
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

            var cmd = connection.CreateCommand();

            @objects = SourcesData.Keys.ToList();

            if (ОтслеживаемыеОбъекты != null && ОтслеживаемыеОбъекты.Count > 0)
            {
                @objects.AddRange(ОтслеживаемыеОбъекты.Select(o => o + ".sql").ToList());
                @objects = @objects.Distinct().ToList();
            }

            cmd.CommandText = GetSourceDefinitionQuery();

            using (DbDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    string FileName = FileNameForObject(r) + ".sql";

                    string objectType = objectTypeDescription(r);

                    string TextFromDB = GetScriptFromReader(r);

                    string SourcesKey = SourcesData
                                        .Keys
                                        .Select(k => new { Value = k, Upper = k.ToUpper() })
                                        .SingleOrDefault(k => k.Upper == FileName.ToUpper())
                                        ?.Value;
                    string TextFromFile = (SourcesKey != null) ? SourcesData[SourcesKey].Item1 : null;

                    if ((TextFromFile == null) || (TextFromFile != TextFromDB))
                    {
                        ChangeType ChangeType = (TextFromFile == null) ? ChangeType.Новый : ChangeType.Изменённый;

                        result.Add(
                            new BaseScript()
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
            return result;
        }

        public abstract string GetSourceDefinitionQuery();
        public abstract string GetScriptFromReader(DbDataReader reader);

        protected List<string> ОтслеживаемыеСхемы = new List<string>();
        protected List<string> ОтслеживаемыеТаблицы = new List<string>();
        protected List<string> ИгнорируемыеТаблицы = new List<string>();
        protected List<string> ОтслеживаемыеОбъекты = new List<string>();
        protected List<string> ИгнорируемыеОбъекты = new List<string>();

        private void UpdateFilters()
        {
            if (!File.Exists(FilterFile))
            {
                ОтслеживаемыеСхемы = new List<string>();
                ОтслеживаемыеТаблицы = new List<string>();
                ИгнорируемыеТаблицы = new List<string>();
                ОтслеживаемыеОбъекты = new List<string>();
                ИгнорируемыеОбъекты = new List<string>();
                return;
            }

            db.UpdateFilterDataFromConfig();

            XElement DBObjects = XDocument.Load(FilterFile)
                                            .Element("DBObjects");

            ОтслеживаемыеСхемы = DBObjects
                                    .Element("Schemas")
                                    .Elements("Schema")
                                    .Where(e => e.Attribute("State").Value == ObjectState.Отслеживаемый.ToString())
                                    .Select(e => e.Value)
                                    .ToList() ?? new List<string>();

            ОтслеживаемыеТаблицы = DBObjects
                                    .Element("Tables")
                                    ?.Elements("Table")
                                    .Where(e => e.Attribute("State").Value == ObjectState.Отслеживаемый.ToString())
                                    .Select(e => e.Value)
                                    .ToList() ?? new List<string>();

            ИгнорируемыеТаблицы = DBObjects
                                    .Element("Tables")
                                    ?.Elements("Table")
                                    .Where(e => e.Attribute("State").Value == ObjectState.Игнорируемый.ToString())
                                    .Select(e => e.Value)
                                    .ToList() ?? new List<string>();

            ОтслеживаемыеОбъекты = DBObjects
                                    .Element("Procedures")
                                    .Elements("Procedure")
                                    .Where(e => e.Attribute("State").Value == ObjectState.Отслеживаемый.ToString())
                                    .Select(e => e.Value)
                                    .ToList() ?? new List<string>();

            List<string> functions = DBObjects
                                    .Element("Functions")
                                    .Elements("Function")
                                    .Where(e => e.Attribute("State").Value == ObjectState.Отслеживаемый.ToString())
                                    .Select(e => e.Value)
                                    .ToList() ?? new List<string>();

            ОтслеживаемыеОбъекты.AddRange(functions);

            ИгнорируемыеОбъекты = DBObjects
                                        .Element("Procedures")
                                        .Elements("Procedure")
                                        .Where(e => e.Attribute("State").Value == ObjectState.Игнорируемый.ToString())
                                        .Select(e => e.Value)
                                        .ToList() ?? new List<string>();

            List<string> ignoreFunctions = DBObjects
                                            .Element("Functions")
                                            .Elements("Function")
                                            .Where(e => e.Attribute("State").Value == ObjectState.Игнорируемый.ToString())
                                            .Select(e => e.Value)
                                            .ToList() ?? new List<string>();

            ИгнорируемыеОбъекты.AddRange(ignoreFunctions);
        }

        public abstract string objectTypeDescription(DbDataReader reader);
        public abstract string FileNameForObject(DbDataReader reader);

        public static Encoding GetEncoding(string FullFileName)
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
        public void Dispose()
        {
            connection.Dispose();
        }
    }
}