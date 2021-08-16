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
using System.Text;
using Ude;

namespace DBScriptSaver.Logic
{
    public class ScriptWriter : IDisposable
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
        public void ObserveScripts()
        {
            UpdateFilters();
            changeProgress.Invoke(@"Сравниваем хранимки/функции", 3);
            GetSourceScripts().ForEach(s => observer?.Invoke(s));
            changeProgress.Invoke(@"Сравниваем таблицы", 50);
            GetChangesScripts();
            changeProgress.Invoke(@"", 0);
        }
        private List<string> ОтслеживаемыеСхемы = new List<string>();
        private List<string> ОтслеживаемыеТаблицы = new List<string>();
        private List<string> ИгнорируемыеТаблицы = new List<string>();
        private List<string> ОтслеживаемыеОбъекты = new List<string>();
        private List<string> ИгнорируемыеОбъекты = new List<string>();


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

        public Action<string, int> changeProgress;
        public Action<Script> observer;

        private Dictionary<int, TableData> tables = new Dictionary<int, TableData>();
        private Dictionary<int, List<ColumnData>> columns = new Dictionary<int, List<ColumnData>>();
        private Dictionary<(int tableId, int indexId), IndexData> indexes = new Dictionary<(int tableId, int indexId), IndexData>();
        private Dictionary<(int tableId, int indexId), List<IndexColumnData>> indColumns = new Dictionary<(int, int), List<IndexColumnData>>();
        private Database dataBase;

        private void LoadChanges()
        {
            if (ОтслеживаемыеТаблицы.Count == 0 && ОтслеживаемыеСхемы.Count == 0) return;

            dataBase = server.Databases.Cast<Database>().Single(b => b.Name.ToUpper() == db.Name.ToUpper());

            var cmd = connection.CreateCommand();

            cmd.CommandText = GetChangesObjectScript();

            using (SqlDataReader r = cmd.ExecuteReader())
            {
                //Таблицы
                while (r.Read())
                {
                    int id = (int)r["id"];
                    tables.Add(id, new TableData()
                    {
                        id = id,
                        Schema = (string)r["schemaName"],
                        Name = (string)r["tableName"],
                        UsesAnsiNulls = (bool)r["uses_ansi_nulls"]
                    });

                    columns.Add(id, new List<ColumnData>());
                }

                r.NextResult();
                //Столбцы таблиц
                while (r.Read())
                {
                    bool is_identity = (bool)r["is_identity"];
                    long? seed_value = null;
                    long? increment_value = null;
                    if (is_identity)
                    {
                        seed_value = (long)r["seed_value"];
                        increment_value = (long)r["increment_value"];
                    }
                    string collation_name = null;
                    if (r["collation_name"] != DBNull.Value)
                    {
                        collation_name = (string)r["collation_name"];
                    }

                    columns[(int)r["id"]].Add(new ColumnData()
                    {
                        Order = (int)r["column_id"],
                        Name = (string)r["column"],
                        Type = (string)r["TYPE"],
                        MaxLength = (short)r["max_length"],
                        Precision = (byte)r["precision"],
                        Scale = (byte)r["scale"],
                        IsIdentity = is_identity,
                        SeedValue = seed_value,
                        IncrementalValue = increment_value,
                        Collation = collation_name,
                        Nullable = (bool)r["is_nullable"]
                    });
                }

                r.NextResult();
                //Индексы
                while (r.Read())
                {
                    int tableId = (int)r["id"];
                    int indexId = (int)r["index_id"];
                    indexes.Add((tableId, indexId), new IndexData()
                    {
                        TableId = tableId,
                        IndexId = indexId,
                        isPrimaryKey = (bool)r["is_primary_key"],
                        isUniqueConstraint = (bool)r["is_unique_constraint"],
                        isUnique = (bool)r["is_unique"],
                        typeDesc = (string)r["type_desc"],
                        Name = (string)r["IndexName"],
                        isPadded = (bool)r["is_padded"],
                        noRecompute = (bool)r["no_recompute"],
                        ignoreDupKey = (bool)r["ignore_dup_key"],
                        allowRowLocks = (bool)r["allow_row_locks"],
                        allowPageLocks = (bool)r["allow_page_locks"]
                    });

                    indColumns.Add((tableId, indexId), new List<IndexColumnData>());
                }

                r.NextResult();
                //Столбы индексов
                while (r.Read())
                {
                    int tableId = (int)r["id"];
                    int indexId = (int)r["index_id"];

                    indColumns[(tableId, indexId)].Add(new IndexColumnData
                    {
                        Name = (string)r["name"],
                        Order = (byte)r["key_ordinal"],
                        IsDesc = (bool)r["is_descending_key"],
                        IsIncluded = (bool)r["is_included_column"]
                    });
                }
            }
        }

        private void GetChangesScripts()
        {
            LoadChanges();

            foreach (var p in tables)
            {
                var table = p.Value;
                string script = table.MakeScript(columns[p.Key]);
                    
                string name = $@"{table.Schema}.{table.Name}";
                string fileName = $@"{name}.sql";

                string tableFileName = TableFolder + fileName;
                string oldScript = "";

                if (File.Exists(tableFileName))
                {
                    oldScript = File.ReadAllText(tableFileName);
                    if (script == oldScript)
                    {
                        continue;
                    }
                }

                ChangeType changeType = !File.Exists(tableFileName) ? ChangeType.Новый : ChangeType.Изменённый;

                var tbl = dataBase.Tables.Cast<Table>().Single(t => t.Schema == table.Schema && t.Name == table.Name);

                Script tblScript = new Script()
                {
                    FileName = fileName,
                    FullPath = TableFolder + fileName,
                    ScriptText = script,
                    ObjectType = @"Таблица",
                    ChangeState = changeType,
                    urn = tbl.Urn,
                    objName = tbl.Name
                };

                observer?.Invoke(tblScript);
            }

            changeProgress.Invoke(@"Сравниваем индексы", 80);

            foreach (var p in indexes)
            {
                var index = p.Value;
                var table = tables[p.Key.tableId];
                string fileName = $@"{table.Schema}.{table.Name}.{index.Name}.sql";
                string indexFileName = IndexFolder + fileName;
                string script = index.MakeScript(table.FullName, indColumns[p.Key]);

                if (File.Exists(indexFileName) && script == File.ReadAllText(indexFileName))
                {
                    continue;
                }

                ChangeType ChangeType = !File.Exists(indexFileName) ? ChangeType.Новый : ChangeType.Изменённый;

                var dbInd = dataBase
                            .Tables.Cast<Table>().Single(t => t.Schema == table.Schema && t.Name == table.Name)
                            .Indexes.Cast<Index>().Single(i => i.Name == index.Name);

                Script indexScript = new Script()
                {
                    FileName = fileName,
                    FullPath = IndexFolder + fileName,
                    ScriptText = script,
                    ObjectType = @"Индекс",
                    ChangeState = ChangeType,
                    urn = dbInd.Urn,
                    objName = index.Name
                };

                observer?.Invoke(indexScript);
            }
        }

        private string GetChangesObjectScript()
        {
            string result = @"
SELECT o.[object_id] AS id
INTO   #ids
FROM   sys.tables AS o
       JOIN sys.schemas AS s
            ON  s.[schema_id] = o.[schema_id]
WHERE " + Environment.NewLine;

            string condition = "";

            if (ОтслеживаемыеТаблицы.Count > 0)
            {
                condition = $@"o.[object_id] IN ({ОтслеживаемыеТаблицы.GetObjectIdStringSql()})";
            }

            if (ОтслеживаемыеСхемы.Count > 0)
            {
                var condition2 = $"s.name IN ({ОтслеживаемыеСхемы.GetObjectsList()})" + Environment.NewLine;

                if (ИгнорируемыеТаблицы.Count > 0)
                {
                    condition2 += $" AND o.[object_id] NOT IN ({ИгнорируемыеТаблицы.GetObjectIdStringSql()})";
                }

                if (condition.Length == 0)
                {
                    condition = condition2;
                }
                else
                {
                    condition = $@"({condition}) OR ({condition2})";
                }
            }

            result += condition + Environment.NewLine;

            result += @"
SELECT o.uses_ansi_nulls,
       s.name            AS schemaName,
       o.name            AS tableName,
       i.id
FROM   #ids              AS i
       JOIN sys.tables   AS o
            ON  o.[object_id] = i.id
       JOIN sys.schemas  AS s
            ON  s.[schema_id] = o.[schema_id]
ORDER BY
       i.id

SELECT c.column_id,
       c.name                          AS [column],
       t.name                          AS TYPE,
       c.max_length,
       c.precision,
       c.scale,
       c.is_identity,
       CAST(i.seed_value AS BIGINT) AS seed_value,
       CAST(i.increment_value AS BIGINT) AS increment_value,
       c.collation_name,
       c.is_nullable,
       o.id
FROM   #ids                            AS o
       JOIN sys.[columns]              AS c
            ON  c.[object_id] = o.id
       JOIN sys.types                  AS t
            ON  t.user_type_id = c.user_type_id
       LEFT JOIN sys.identity_columns  AS i
            ON  i.[object_id] = o.id
            AND i.column_id = c.column_id
ORDER BY
       o.id,
       c.column_id

SELECT o.id,
       i.index_id,
       i.is_primary_key,
       i.is_unique_constraint,
       i.is_unique,
       i.type_desc,
       i.name                  AS IndexName,
       i.is_padded,
       s.no_recompute,
       i.[ignore_dup_key],
       i.[allow_row_locks],
       i.[allow_page_locks]
FROM   #ids                    AS o
       JOIN sys.indexes        AS i
            ON  i.[object_id] = o.id
       JOIN sys.[stats]        AS s
            ON  s.[object_id] = i.[object_id]
            AND s.stats_id = i.index_id
WHERE  i.is_primary_key = 0

SELECT o.id,
       i.index_id,
       ic.key_ordinal,
       c.name,
       ic.is_descending_key,
       ic.is_included_column
FROM   #ids                    AS o
       JOIN sys.indexes        AS i
            ON  i.[object_id] = o.id
       JOIN sys.index_columns  AS ic
            ON  ic.[object_id] = i.[object_id]
            AND ic.index_id = i.index_id
       JOIN sys.[columns]      AS c
            ON  c.[object_id] = ic.[object_id]
            AND c.column_id = ic.column_id
WHERE  i.is_primary_key = 0
ORDER BY
       o.id,
       i.index_id,
       ic.key_ordinal";

            return result;
        }
        public void Dispose()
        {
            connection.Dispose();
        }
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
    }
}