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
            volatiles.Add("v", "VOLATILE");

            FillNameSpaces();
            FillTypes();
            FillLanguages();
        }

        private Dictionary<int, string> pgLanguages = new Dictionary<int, string>();

        private void FillLanguages()
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT l.oid,
                                       l.lanname
                                FROM   pg_language AS l";

            using (DbDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    pgLanguages.Add((int)r["oid"], (string)r["lanname"]);
                }
            }
        }

        private Dictionary<int, string> nameSpaces = new Dictionary<int, string>();

        private void FillNameSpaces()
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT n.oid,
	                                   n.nspname
                                FROM   pg_namespace AS n";

            using (DbDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    nameSpaces.Add((int)r["oid"], (string)r["nspname"]);
                }
            }
        }

        private void FillTypes()
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT t.oid,
                                       t.typname,
                                       t.typnamespace
                               FROM   pg_type AS t";

            using (DbDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    typesDescriptions.Add((int)r["oid"], ((string)r["typname"], (int)r["typnamespace"]));
                }
            }
        }

        public override string FileNameForObject(DbDataReader reader)
        {
            return (string)reader["proname"];
        }

        private Dictionary<string, string> volatiles = new Dictionary<string, string>();

        public override string GetScriptFromReader(DbDataReader reader)
        {
            string result = "CREATE OR REPLACE FUNCTION ";

            result += $@"""{reader["nspname"]}"".""{reader["proname"]}""({MakeArgs((string)reader["proargnames"], (string)reader["proargtypes"])}){Environment.NewLine}";
            result += $@"RETURNS ";

            if ((bool)reader["proretset"])
            {
                result += $@"SETOF ";
            }

            int typeId = (int)reader["prorettype"];
            int nsId = typesDescriptions[typeId].nsId;

            result += $@"""{nameSpaces[nsId]}"".""{typesDescriptions[typeId].name}"" {Environment.NewLine}";


            result += $@"AS{Environment.NewLine}";
            result += $@"$BODY${Environment.NewLine}";

            result += (string)reader["prosrc"] + ";" + Environment.NewLine;

            result += $@"$BODY${Environment.NewLine}";
            result += $@"LANGUAGE {pgLanguages[(int)reader["prolang"]]} {volatiles[(string)reader["provolatile"]]} {Environment.NewLine}";
            result += $@"COST {(float)reader["procost"]}{Environment.NewLine}";
            result += $@"ROWS {(float)reader["prolang"]}";
            return result;
        }

        private Dictionary<int, (string name, int nsId)> typesDescriptions = new Dictionary<int, (string name, int nsId)>();

        private string MakeArgs(string namesStr, string typesStr)
        {
            namesStr = namesStr.Substring(1, namesStr.Length - 2);

            var names = namesStr.Split(',');

            typesStr = typesStr.Substring(1, typesStr.Length - 2);
            var types = typesStr.Split(' ').Select(t => int.Parse(t)).ToArray();

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
            string result = @"SELECT *
                                FROM   pg_catalog.pg_proc p
                                       JOIN pg_catalog.pg_namespace AS n
                                            ON  n.oid = p.pronamespace
                                WHERE  n.nspname NOT LIKE 'pg_%'
                                       AND n.nspname != 'information_schema'
                                WHERE ";

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
                condition += $"n.nspname IN ({ОтслеживаемыеСхемы.GetObjectsList()})";
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

            result += condition;

            return result;
        }

        public override string objectTypeDescription(DbDataReader reader)
        {
            throw new NotImplementedException();
        }
    }
}