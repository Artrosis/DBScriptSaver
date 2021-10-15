using DBScriptSaver.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace DBScriptSaver.Core
{
    internal class MSSQLScriptWriter : BaseScriptWriter
    {
        readonly Server server;

        public MSSQLScriptWriter(ProjectDataBase projectDataBase) : base(projectDataBase)
        {
            server = new Server(new ServerConnection((SqlConnection)connection));
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

        public override string objectTypeDescription(DbDataReader reader)
        {
            string type = (string)reader["TYPE"];
            return TypeDescriptions[type] ?? type;
        }

        public override string FileNameForObject(DbDataReader reader)
        {
            return (string)reader["ObjectName"];
        }

        public override string GetSourceDefinitionQuery()
        {
            string result = @"SELECT s.[name] + N'.' + o.[name] AS ObjectName, sm.[definition], o.[TYPE]," + Environment.NewLine
                            + @"ISNULL(sm.uses_ansi_nulls, 0) AS uses_ansi_nulls," + Environment.NewLine
                            + @"ISNULL(sm.uses_quoted_identifier, 0) AS uses_quoted_identifier" + Environment.NewLine
                            + @"FROM   sys.sql_modules   AS sm" + Environment.NewLine
                            + @"       JOIN sys.objects  AS o" + Environment.NewLine
                            + @"            ON  o.[object_id] = sm.[object_id]" + Environment.NewLine
                            + @"       JOIN sys.schemas  AS s" + Environment.NewLine
                            + @"            ON  o.[schema_id] = s.[schema_id]" + Environment.NewLine;

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

            if (condition.Length > 0)
            {
                result += $@"WHERE {condition}";
            }

            return result;
        }

        public override string GetScriptFromReader(DbDataReader r)
        {
            string result = string.Empty;

            if ((bool)r["uses_ansi_nulls"])
            {
                result += @"SET ANSI_NULLS ON" + Environment.NewLine;
                result += @"GO" + Environment.NewLine;
            }

            if ((bool)r["uses_quoted_identifier"])
            {
                result += @"SET QUOTED_IDENTIFIER ON" + Environment.NewLine;
                result += @"GO" + Environment.NewLine;
            }

            result += (string)r["definition"];
            return result;
        }

        Database dataBase;
        protected override void LoadChanges()
        {
            if (ОтслеживаемыеТаблицы.Count == 0 && ОтслеживаемыеСхемы.Count == 0) return;

            dataBase = server.Databases.Cast<Database>().Single(b => b.Name.ToUpper() == db.Name.ToUpper());

            var cmd = connection.CreateCommand();

            cmd.CommandText = GetChangesObjectScript();

            using (DbDataReader r = cmd.ExecuteReader())
            {
                //Таблицы
                while (r.Read())
                {
                    int id = (int)r["id"];
                    tables.Add(id, new MSSQLTableData()
                    {
                        id = id,
                        Schema = (string)r["schemaName"],
                        Name = (string)r["tableName"],
                        UsesAnsiNulls = (bool)r["uses_ansi_nulls"],
                        dataBase = dataBase,
                        TableFolder = TableFolder
                    });
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

                    ((MSSQLTableData)tables[(int)r["id"]]).Columns.Add(new MSSQLColumnData()
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
                    indexes.Add((tableId, indexId), new MSSQLIndexData()
                    {
                        isPrimaryKey = (bool)r["is_primary_key"],
                        isUniqueConstraint = (bool)r["is_unique_constraint"],
                        isUnique = (bool)r["is_unique"],
                        typeDesc = (string)r["type_desc"],
                        Name = (string)r["IndexName"],
                        isPadded = (bool)r["is_padded"],
                        noRecompute = (bool)r["no_recompute"],
                        ignoreDupKey = (bool)r["ignore_dup_key"],
                        allowRowLocks = (bool)r["allow_row_locks"],
                        allowPageLocks = (bool)r["allow_page_locks"],
                        IndexFolder = IndexFolder,
                        dataBase = dataBase,
                        table = (MSSQLTableData)tables[tableId]
                    });
                }

                r.NextResult();
                //Столбы индексов
                while (r.Read())
                {
                    int tableId = (int)r["id"];
                    int indexId = (int)r["index_id"];

                    ((MSSQLIndexData)indexes[((int)r["id"], (int)r["index_id"])]).Columns.Add(new MSSQLIndexColumnData
                    {
                        Name = (string)r["name"],
                        Order = (byte)r["key_ordinal"],
                        IsDesc = (bool)r["is_descending_key"],
                        IsIncluded = (bool)r["is_included_column"]
                    });
                }
            }
        }

        string GetChangesObjectScript()
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

        protected override BaseScript CreateSourceScript()
        {
            return new MSSQLScript();
        }
    }
}