using DBScriptSaver.ViewModels;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBScriptSaver.Core
{
    public class MSSQLTableData: ITableData
    {
        public int id;
        public string TableFolder;
        public string Schema;
        public string Name;
        public bool UsesAnsiNulls;
        public Database dataBase;

        public List<MSSQLColumnData> Columns = new List<MSSQLColumnData>();

        public string FullName => $@"[{Schema}].[{Name}]";

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

            var tbl = dataBase.Tables.Cast<Table>().Single(t => t.Schema == Schema && t.Name == Name);

            return new MSSQLScript()
            {
                FileName = fileName,
                FullPath = TableFolder + fileName,
                ScriptText = script,
                ObjectType = @"Таблица",
                ChangeState = changeType,
                urn = tbl.Urn,
                ObjName = tbl.Name
            };
        }

        public string MakeScript()
        {
            string script = string.Empty;

            script += @"GO" + Environment.NewLine;
            script += @"SET ANSI_NULLS ";

            if (UsesAnsiNulls)
            {
                script += "ON";
            }
            else
            {
                script += "OFF";
            }

            script += Environment.NewLine + @"GO" + Environment.NewLine;

            script += @"SET QUOTED_IDENTIFIER ON" + Environment.NewLine;
            script += @"GO" + Environment.NewLine;

            script += $@"CREATE TABLE {FullName}(" + Environment.NewLine;

            string columnsDef = "";
            bool NeedTextImageOn = false;

            foreach (var column in Columns)
            {
                if (columnsDef.Length > 0)
                {
                    columnsDef += "," + Environment.NewLine;
                }
                columnsDef += column.Definition;

                if (column.IsTextImage())
                {
                    NeedTextImageOn = true;
                }
            }

            script += columnsDef + Environment.NewLine;

            script += @") ON [PRIMARY]";

            if (NeedTextImageOn)
            {
                script += @" TEXTIMAGE_ON [PRIMARY]";
            }

            return script;
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}