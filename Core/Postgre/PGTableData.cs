using DBScriptSaver.Helpers;
using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBScriptSaver.Core
{
    public class PGTableData : ITableData, IPGObject
    {
        public int id;
        public string Name;
        public string Schema;
        public string TableFolder;
        public override string ToString()
        {
            return $@"{Schema}.{Name}";
        }

        public List<PGColumnData> Columns = new List<PGColumnData>();
        public List<PGConstrainsData> Constrains = new List<PGConstrainsData>();
        public List<PGCommentData> Comments = new List<PGCommentData>();
        public List<(string createScript, string AlterScript)> последовательности = new List<(string createScript, string AlterScript)>();

        public IScript GetScript()
        {
            string script = MakeScript();

            string name = $@"{Schema}.{Name}";
            string fileName = $@"{name}.sql";

            string tableFileName = TableFolder + fileName;

            if (File.Exists(tableFileName))
            {
                string oldScript = File.ReadAllText(tableFileName);
                if (script == oldScript)
                {
                    return null;
                }
            }

            ChangeType changeType = !File.Exists(tableFileName) ? ChangeType.Новый : ChangeType.Изменённый;

            return new PGScript(this)
            {
                FileName = fileName,
                FullPath = TableFolder + fileName,
                ScriptText = script,
                ObjectType = @"Таблица",
                ChangeState = changeType,
                ObjName = name
            };
        }

        public string FullName => $@"""{Schema}"".""{Name}""";

        private string MakeScript()
        {
            string script = string.Empty;

            foreach (var seq in последовательности)
            {
                script += seq.createScript + Environment.NewLine;
            }

            script += $@"CREATE TABLE {FullName}(" + Environment.NewLine;

            string columnsDef = "";

            foreach (var column in Columns)
            {
                if (columnsDef.Length > 0)
                {
                    columnsDef += "," + Environment.NewLine;
                }
                columnsDef += column.Script;
            }

            foreach (var constr in Constrains)
            {
                if (columnsDef.Length > 0)
                {
                    columnsDef += "," + Environment.NewLine;
                }
                columnsDef += constr.Script;
            }

            script += columnsDef + Environment.NewLine;

            script += @");" + Environment.NewLine;

            foreach (var seq in последовательности)
            {
                script += seq.AlterScript + Environment.NewLine;
            }

            script += Environment.NewLine;

            foreach (var c in Comments)
            {
                script += c.Script + Environment.NewLine;
            }

            return script;
        }

        public string CreateMirgration()
        {
            return MakeScript().Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS");
        }

        public List<Migration> CreateAlterMirgrations(string oldScript)
        {
            List<Migration> result = new List<Migration>();

            PGTableData oldTable = PostgreParser.ParseTable(oldScript);

            foreach (var col in Columns)
            {
                if (!oldTable.Columns.Any(c => c.Script == col.Script))
                {
                    var fileName = $@"Add_{Schema}_{Name}_{col.Name}";
                    result.Add(new Migration()
                    {
                        Name = FileHelper.CreateMigrationName(col.Name),
                        Script = $@"ALTER TABLE ""{Schema}"".""{Name}"" ADD COLUMN IF NOT EXISTS {col.Script};"
                    });
                }
            }

            return result;
        }
    }
}