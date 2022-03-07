using DBScriptSaver.ViewModels;
using System;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;

namespace DBScriptSaver.Core
{
    internal class PGScriptWriter : BaseScriptWriter
    {
        public PGScriptWriter(ProjectDataBase projectDataBase) : base(projectDataBase) 
        {
            FillNameSpaces();
            FillTypes();
            FillLanguages();
        }

        private static readonly Dictionary<uint, string> pgLanguages = new Dictionary<uint, string>();

        private void FillLanguages()
        {
            if (pgLanguages.Count > 0) return;

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT l.oid,
                                       l.lanname
                                FROM   pg_language AS l";

            using (DbDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    pgLanguages.Add((uint)r["oid"], (string)r["lanname"]);
                }
            }
        }

        private static readonly Dictionary<uint, string> nameSpaces = new Dictionary<uint, string>();

        private void FillNameSpaces()
        {
            if (nameSpaces.Count > 0) return;

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT n.oid,
	                                   n.nspname
                                FROM   pg_namespace AS n";

            using (DbDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    nameSpaces.Add((uint)r["oid"], (string)r["nspname"]);
                }
            }
        }

        private void FillTypes()
        {
            if (typesDescriptions.Count > 0) return;

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT t.oid,
                                       t.typname,
                                       t.typnamespace
                               FROM   pg_type AS t";

            using (DbDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    typesDescriptions.Add((uint)r["oid"], ((string)r["typname"], (uint)r["typnamespace"]));
                }
            }
        }

        public override string FileNameForObject(DbDataReader reader)
        {
            return (string)reader["fileName"];
        }

        private static readonly Dictionary<char, string> volatiles = new Dictionary<char, string>
        {
            { 'v', "VOLATILE" }
        };

        public override string GetScriptFromReader(DbDataReader reader)
        {
            string result = "CREATE OR REPLACE FUNCTION ";

            int argsCount = (short)reader["pronargs"];
            string[] args = ((string[])reader["proargnames"]).Take(argsCount).ToArray();

            result += $@"""{reader["nspname"]}"".""{reader["proname"]}""({MakeArgs(args, (uint[])reader["proargtypes"])}){Environment.NewLine}";
            result += $@"RETURNS ";

            bool proretset = (bool)reader["proretset"];

            if (proretset)
            {
                result += $@"SETOF ";
            }

            uint typeId = (uint)reader["prorettype"];
            uint nsId = typesDescriptions[typeId].nsId;

            result += $@"""{nameSpaces[nsId]}"".""{typesDescriptions[typeId].name}"" {Environment.NewLine}";


            result += $@"AS{Environment.NewLine}";
            result += $@"$BODY${Environment.NewLine}";

            result += (string)reader["prosrc"];

            result += $@"$BODY${Environment.NewLine}";
            result += $@"LANGUAGE {pgLanguages[(uint)reader["prolang"]]} {volatiles[(char)reader["provolatile"]]} {Environment.NewLine}";
            result += $@"COST {(float)reader["procost"]}{Environment.NewLine}";
            if (proretset)
            {
                result += $@"ROWS {(uint)reader["prolang"]}";
            }

            result += $@";";

            return result;
        }

        private static readonly Dictionary<uint, (string name, uint nsId)> typesDescriptions = new Dictionary<uint, (string name, uint nsId)>();

        private string MakeArgs(string[] names, uint[] types)
        {
            string result = "";

            for (int paramIndex = 0; paramIndex < names.Length; paramIndex++)
            {
                if (result != "")
                {
                    result += ", ";
                }
                result += $@"{names[paramIndex]} {typesDescriptions[types[paramIndex]].name}";
            }

            return result;
        }

        public override string GetSourceDefinitionQuery()
        {
            string result = @"SELECT n.nspname || '.' || p.proname as ""fileName"", n.nspname, p.proname, p.prosrc, p.proargnames, p.proargtypes, p.proretset, p.prorettype, p.procost, p.prolang, p.provolatile, p.pronargs
                                FROM   pg_catalog.pg_proc p
                                       JOIN pg_catalog.pg_namespace AS n
                                            ON  n.oid = p.pronamespace
                                WHERE  n.nspname NOT LIKE 'pg_%'
                                       AND n.nspname != 'information_schema'";
            string objCondition = "";

            if (@objects.Count > 0)
            {
                foreach (var obj in @objects)
                {
                    if (objCondition != "")
                    {
                        objCondition += Environment.NewLine;
                        objCondition += " OR ";
                    }
                    objCondition += $@"(n.nspname = '{obj.GetSchema()}' AND p.proname = '{obj.GetName()}')";
                }

                objCondition = $@"({objCondition})";
            }

            string schemaCondition = "";

            if (ОтслеживаемыеСхемы != null && ОтслеживаемыеСхемы.Count > 0)
            {
                schemaCondition += $"(n.nspname IN ({GetObjectsList(ОтслеживаемыеСхемы)}))";
            }

            string hideCondition = "";

            if (ИгнорируемыеОбъекты.Count > 0)
            {
                foreach (var obj in ИгнорируемыеОбъекты)
                {
                    if (hideCondition != "")
                    {
                        hideCondition += Environment.NewLine;
                        hideCondition += " AND ";
                    }
                    hideCondition += $@" NOT (n.nspname = '{obj.GetSchema()}' AND p.proname = '{obj.GetName()}')";
                }

                hideCondition = $@"({hideCondition})";
            }

            string condition = "";

            if (schemaCondition.Length > 0)
            {
                condition = schemaCondition;
            }

            if (objCondition.Length > 0)
            {
                if (condition.Length > 0)
                {
                    condition = $@"({condition} OR {objCondition})";
                }
                else
                {
                    condition = objCondition;
                }
            }

            if (hideCondition.Length > 0)
            {
                if (condition.Length > 0)
                {
                    condition = $@"({condition} AND {hideCondition})";
                }
                else
                {
                    condition = hideCondition;
                }
            }

            if (condition.Length > 0)
            {
                result += Environment.NewLine + $@"AND ({condition})";
            }

            return result;
        }

        private static string GetObjectsList(List<string> lst)
        {
            string result = "";
            foreach (string ObjectName in lst)
            {
                if (result != "")
                {
                    result += ", ";
                }
                result += $@"'{ObjectName}'";
            }
            return result;
        }

        public override string objectTypeDescription(DbDataReader reader)
        {
            return @"Функция";
        }
        string GetChangesObjectScript()
        {
            string result = @"
CREATE TEMP TABLE tbls ON COMMIT DROP
AS
SELECT c.oid                AS id
FROM   pg_catalog.pg_class  AS c
       JOIN pg_catalog.pg_namespace n
            ON  n.oid = c.relnamespace
       JOIN pg_catalog.pg_tables AS pt
       		ON pt.schemaname = n.nspname
       		AND pt.tablename = c.relname" + Environment.NewLine;

            string schemaCondition = "";

            if (ОтслеживаемыеСхемы.Count > 0)
            {
                schemaCondition += $"(n.nspname IN ({GetObjectsList(ОтслеживаемыеСхемы)}))";
            }

            string tblCondition = "";

            if (ОтслеживаемыеТаблицы.Count > 0)
            {
                foreach (var tbl in ОтслеживаемыеТаблицы)
                {
                    if (tblCondition != "")
                    {
                        tblCondition += Environment.NewLine;
                        tblCondition += " OR ";
                    }
                    tblCondition += $@"(n.nspname = '{tbl.GetSchema()}' AND c.relname = '{tbl.GetName()}')";
                }

                tblCondition = $@"({tblCondition})";
            }

            string hideTblsCondition = "";

            if (ИгнорируемыеТаблицы.Count > 0)
            {
                foreach (var obj in ИгнорируемыеОбъекты)
                {
                    if (hideTblsCondition != "")
                    {
                        hideTblsCondition += Environment.NewLine;
                        hideTblsCondition += " AND ";
                    }
                    hideTblsCondition += $@" NOT (n.nspname = '{obj.GetSchema()}' AND c.relname = '{obj.GetName()}')";
                }

                hideTblsCondition = $@"({hideTblsCondition})";
            }

            string condition = "";

            if (schemaCondition.Length > 0)
            {
                condition = schemaCondition;
            }

            if (tblCondition.Length > 0)
            {
                if (condition.Length > 0)
                {
                    condition += $@"({condition} OR {tblCondition})";
                }
                else
                {
                    condition = tblCondition;
                }                
            }

            if (hideTblsCondition.Length > 0)
            {
                if (condition.Length > 0)
                {
                    condition += $@"({condition} AND {hideTblsCondition})";
                }
                else
                {
                    condition = tblCondition;
                }
            }

            if (condition.Length > 0)
            {
                result += $@"WHERE {condition};" + Environment.NewLine;
            }

            result += @"
SELECT t.id,
       c.relname AS ""TableName"",
       n.nspname AS ""SchemaName""
FROM   tbls                      AS t
       JOIN pg_catalog.pg_class  AS c
            ON  t.id = c.oid
       JOIN pg_catalog.pg_namespace n
            ON  n.oid = c.relnamespace;

SELECT '""' || a.attname || '"" ' || pg_catalog.format_type (a.atttypid, a.atttypmod)
        || CASE WHEN n.nspname IS NOT NULL THEN ' COLLATE ' || '""' || n.nspname || '"".""' || c.collname || '""'  ELSE '' END
        || CASE WHEN a.attnotnull = TRUE THEN ' NOT NULL' ELSE '' END
        || CASE WHEN a.atthasdef = TRUE THEN ' DEFAULT ' || pg_get_expr(d.adbin, d.adrelid) ELSE '' END AS coldef, 
        a.attname AS colname,
		t.id
FROM   tbls AS t
JOIN pg_catalog.pg_attribute AS a
ON  t.id = a.attrelid
       JOIN pg_catalog.pg_type AS pt
 ON  pt.oid = a.atttypid
       LEFT JOIN pg_catalog.pg_collation AS c
           ON  a.attcollation = c.oid
       LEFT JOIN pg_catalog.pg_namespace AS n
           ON  n.oid = c.collnamespace
       LEFT JOIN pg_catalog.pg_attrdef AS d
         ON  d.adrelid = t.id
            AND d.adnum = a.attnum
WHERE a.attnum > 0
       AND NOT a.attisdropped
ORDER BY
       a.attnum;

SELECT 'CONSTRAINT ' || '""' || conname || '"" ' || pg_get_constraintdef (c.oid) AS condef,
		t.id
FROM tbls AS t
       JOIN pg_catalog.pg_constraint c
            ON t.id = c.conrelid
ORDER BY
       c.oid;

SELECT i.indexdef || ';'             AS ""IndexDef"",
       i.indexname AS ""IndexName"",
       t.id,
       ind.oid AS index_id
FROM tbls                          AS t
       JOIN pg_catalog.pg_index AS ix
       ON  ix.indrelid = t.id
       JOIN pg_catalog.pg_class AS tbl
       ON  t.id = tbl.oid
       JOIN pg_catalog.pg_class AS ind
       ON  ind.oid = ix.indexrelid
       JOIN pg_catalog.pg_namespace AS n
           ON  n.oid = ind.relnamespace
       JOIN pg_catalog.pg_indexes AS i
         ON  i.tablename = tbl.relname
            AND i.schemaname = n.nspname
            AND i.indexname = ind.relname
WHERE ix.indisprimary = FALSE
ORDER BY
       ind.oid;

SELECT 'COMMENT ON COLUMN ""' || n.nspname || '"".""' || tbl.relname || '"".""' || a.attname || '"" IS ''' ||
       d.description || ''';' AS ""Comment"", d.objsubid, t.id
FROM   tbls AS t
       JOIN pg_catalog.pg_class AS tbl
            ON  t.id = tbl.oid

       JOIN pg_catalog.pg_namespace AS n
            ON  n.oid = tbl.relnamespace

       JOIN pg_catalog.pg_attribute AS a
            ON  t.id = a.attrelid

       JOIN pg_catalog.pg_description AS d
            ON  t.id = d.objoid

            AND d.objsubid = a.attnum
UNION
SELECT 'COMMENT ON TABLE ""' || n.nspname || '"".""' || tbl.relname || '"" IS ''' ||
       d.description || ''';', d.objsubid, t.id
FROM   tbls AS t
       JOIN pg_catalog.pg_class AS tbl
            ON  t.id = tbl.oid

       JOIN pg_catalog.pg_namespace AS n
            ON  n.oid = tbl.relnamespace

       JOIN pg_catalog.pg_description AS d
            ON  t.id = d.objoid AND d.objsubid = 0
ORDER BY 2;

SELECT 'CREATE SEQUENCE ""' || n.nspname || '"".""' || c.relname || '"";' AS create,
       'ALTER SEQUENCE ""' || n.nspname || '"".""' || c.relname || '"" OWNED BY ""' || n.nspname || '"".""' || tbl.relname ||
       '"".""' || a.attname || '"";'    AS alter,
       tbl.oid AS ""TableId""
FROM tbls                          AS t
       JOIN pg_catalog.pg_class AS tbl
       ON  tbl.oid = t.id
       JOIN pg_catalog.pg_depend AS d
        ON  d.refobjid = tbl.oid
            AND d.refobjsubid > 0
       JOIN pg_catalog.pg_class AS c
       ON  d.objid = c.relfilenode
       JOIN pg_catalog.pg_sequence AS s
          ON  s.seqrelid = c.oid
       JOIN pg_catalog.pg_attribute AS a
            ON(a.attnum = d.refobjsubid AND a.attrelid = d.refobjid)
       JOIN pg_catalog.pg_namespace n
            ON n.oid = c.relnamespace;

COMMIT;";

            return result;
        }

        protected override void LoadChanges()
        {
            if (ОтслеживаемыеТаблицы.Count == 0 && ОтслеживаемыеСхемы.Count == 0) return;

            Begin();

            var cmd = connection.CreateCommand();

            cmd.CommandText = GetChangesObjectScript();

            using (DbDataReader r = cmd.ExecuteReader())
            {
                //Таблицы
                while (r.Read())
                {
                    int id = (int)(uint)r["id"];
                    tables.Add(id, new PGTableData()
                    {
                        id = id,
                        Name = (string)r["TableName"],
                        Schema = (string)r["SchemaName"],
                        TableFolder = TableFolder
                    });
                }

                r.NextResult();
                //Столбцы таблиц
                while (r.Read())
                {
                    ((PGTableData)tables[(int)(uint)r["id"]]).Columns.Add(new PGColumnData()
                    {
                        Script = (string)r["coldef"],
                        Name = (string)r["colname"]
                    });
                }

                r.NextResult();
                //Ограницения
                while (r.Read())
                {
                    ((PGTableData)tables[(int)(uint)r["id"]]).Constrains.Add(new PGConstrainsData()
                    {
                        Script = (string)r["condef"]
                    });
                }

                r.NextResult();
                //Индексы
                while (r.Read())
                {
                    int tableId = (int)(uint)r["id"];
                    int indexId = (int)(uint)r["index_id"];

                    indexes.Add((tableId, indexId), new PGIndexData()
                    {
                        Script = (string)r["IndexDef"],
                        IndexFolder = IndexFolder,
                        Name = (string)r["IndexName"],
                        table = (PGTableData)tables[tableId]
                    });
                }

                r.NextResult();
                //Комментарии
                while (r.Read())
                {
                    ((PGTableData)tables[(int)(uint)r["id"]]).Comments.Add(new PGCommentData()
                    {
                        Script = (string)r["Comment"]
                    });
                }

                r.NextResult();
                //Последовательности
                while (r.Read())
                {
                    int tableId = (int)(uint)r["TableId"];

                    ((PGTableData)tables[(int)(uint)r["tableId"]]).последовательности.Add(((string)r["create"], (string)r["alter"]));

                }
            }
        }

        private void Begin()
        {
            var cmd = connection.CreateCommand();

            cmd.CommandText = "BEGIN;";

            cmd.ExecuteNonQuery();
        }

        protected override BaseScript CreateSourceScript()
        {
            return new PGScript();
        }
    }
}