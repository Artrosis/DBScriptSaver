using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace DBScriptSaver.Core
{
    public class PGTableData : ITableData, IPGObject
    {
        public int id;
        public string Name;
        public string Schema;
        public string TableFolder;

        public List<PGColumnData> Columns = new List<PGColumnData>();
        public List<PGConstrainsData> Constrains = new List<PGConstrainsData>();
        public List<PGCommentData> Comments = new List<PGCommentData>();

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

            script += @");";

            return script;
        }

        public string CreateMirgration()
        {
            return MakeScript().Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS");
        }

        public List<Migration> CreateAlterMirgrations(string oldScript)
        {
            throw new NotImplementedException();
        }
    }
}