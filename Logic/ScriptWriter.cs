using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Xml.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using DBScriptSaver.Parse;
using DBScriptSaver.Helpers;

namespace DBScriptSaver.Logic
{
    public class ScriptWriter: IDisposable
    {
        readonly ProjectDataBase db;
        readonly SqlConnection connection;
        readonly Server server;

        public string SourceFolder => db.SourceFolder;
        public string TableFolder => db.TableFolder;
        public string IndexFolder => db.IndexFolder;
        public ObservableCollection<Procedure> Procedures => db.Procedures;
        public ObservableCollection<Function> Functions => db.Functions;

        public string FilterFile => db.FilterFile;

        public string ConnectionString => db.GetConnectionString();

        public ScriptWriter(ProjectDataBase projectDataBase)
        {
            db = projectDataBase;
            connection = new SqlConnection(ConnectionString);
            connection.Open();
            server = new Server(new ServerConnection(connection));
        }
        public List<Script> Scripts()
        {
            UpdateFilters();
            List<Script> scripts = new List<Script>();

            scripts.AddRange(GetSourceScripts());

            scripts.AddRange(GetChangesScripts());

            return scripts;
        }
        private List<string> ОтслеживаемыеСхемы;
        private List<string> ИгнорируемыеСхемы;
        private List<string> ОтслеживаемыеОбъекты;
        private List<string> ИгнорируемыеОбъекты;


        private void UpdateFilters()
        {
            if (!File.Exists(FilterFile))
            {
                ОтслеживаемыеСхемы = null;
                ИгнорируемыеСхемы = null;
                ОтслеживаемыеОбъекты = null;
                ИгнорируемыеОбъекты = null;
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
                                    .ToList();

            ИгнорируемыеСхемы = DBObjects
                                        .Element("Schemas")
                                        .Elements("Schema")
                                        .Where(e => e.Attribute("State").Value == ObjectState.Игнорируемый.ToString())
                                        .Select(e => e.Value)
                                        .ToList();

            ОтслеживаемыеОбъекты = DBObjects
                                    .Element("Procedures")
                                    .Elements("Procedure")
                                    .Where(e => e.Attribute("State").Value == ObjectState.Отслеживаемый.ToString())
                                    .Select(e => e.Value)
                                    .ToList();

            List<string> functions = DBObjects
                                    .Element("Functions")
                                    .Elements("Function")
                                    .Where(e => e.Attribute("State").Value == ObjectState.Отслеживаемый.ToString())
                                    .Select(e => e.Value)
                                    .ToList();

            ОтслеживаемыеОбъекты.AddRange(functions);

            ИгнорируемыеОбъекты = DBObjects
                                        .Element("Procedures")
                                        .Elements("Procedure")
                                        .Where(e => e.Attribute("State").Value == ObjectState.Игнорируемый.ToString())
                                        .Select(e => e.Value)
                                        .ToList();

            List<string> ignoreFunctions = DBObjects
                                            .Element("Functions")
                                            .Elements("Function")
                                            .Where(e => e.Attribute("State").Value == ObjectState.Игнорируемый.ToString())
                                            .Select(e => e.Value)
                                            .ToList();

            ИгнорируемыеОбъекты.AddRange(ignoreFunctions);
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
        private List<Script> GetSourceScripts()
        {
            var result = new List<Script>();

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
                                    .ToDictionary(f => f.Name, f => new Tuple<string, DateTime>(File.ReadAllText(f.FullName, ProjectDataBase.GetEncoding(f.FullName)), f.LastWriteTime));

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

                        result.Add(
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

            return result;
        }
        private List<Migration> MakeAlterTableMigration(Table tbl, string sourceScript)
        {
            return TableComparer.GetChanges(server.GetScript(tbl), sourceScript);
        }
        public List<Migration> MakeCreateTableMigration(Table tbl)
        {
            return new List<Migration>()
            {
                new Migration()
                {
                    Name = FileHelper.CreateMigrationName(tbl.Name),
                    Script = server.GetScript(tbl)
                }
            };
        }
        public List<Migration> MakeCreateIndexMigration(Index index)
        {
            return new List<Migration>()
            {
                new Migration()
                {
                    Name = FileHelper.CreateMigrationName(index.Name),
                    Script = server.GetScript(index)
                }
            };
        }
        private IEnumerable<Script> GetChangesScripts()
        {
            var result = new List<Script>();
            
            var dataBase = server.Databases.Cast<Database>().Single(b => b.Name.ToUpper() == db.Name.ToUpper());

            if (ОтслеживаемыеСхемы != null)
            {
                var tbls = dataBase.Tables.Cast<Table>().Where(t => ОтслеживаемыеСхемы.Contains(t.Schema)).ToList();

                foreach (var tbl in tbls)
                {
                    result.AddRange(GetIndexesScripts(tbl));

                    string fileName = $@"{tbl.Schema}.{tbl.Name}.sql";
                    string script = "";

                    var options = new ScriptingOptions();
                    options.DriPrimaryKey = true;

                    tbl.Script().Cast<string>().ToList().ForEach(l =>
                    {
                        if (script != "")
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
                            tblScript.Migrations = MakeCreateTableMigration(tbl);
                            break;
                        case ViewModels.ChangeType.Изменённый:
                            tblScript.Migrations = MakeAlterTableMigration(tbl, File.ReadAllText(tableFileName));
                            break;
                    }

                    result.Add(tblScript);
                }
            }

            return result;
        }
        private List<Script> GetIndexesScripts(Table tbl)
        {
            var result = new List<Script>();

            foreach (Index index in tbl.Indexes)
            {
                string fileName = $@"{tbl.Schema}.{index.Name}.sql";
                string indexFileName = IndexFolder + fileName;
                string script = "";

                index.Script().Cast<string>().ToList().ForEach(l =>
                {
                    if (script != "")
                    {
                        script += "GO" + Environment.NewLine;
                    }
                    script += l + Environment.NewLine;
                });

                if (File.Exists(indexFileName) && script == File.ReadAllText(indexFileName))
                {
                    continue;
                }

                ChangeType ChangeType = !File.Exists(indexFileName) ? ChangeType.Новый : ChangeType.Изменённый;

                Script indexScript = new Script()
                {
                    FileName = fileName,
                    FullPath = IndexFolder + fileName,
                    ScriptText = script,
                    ObjectType = @"Индекс",
                    ChangeState = ChangeType
                };

                switch (ChangeType)
                {
                    case ChangeType.Новый:
                        indexScript.Migrations = MakeCreateIndexMigration(index);
                        break;
                }

                result.Add(indexScript);
            }

            return result;
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}