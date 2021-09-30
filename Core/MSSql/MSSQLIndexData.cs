using DBScriptSaver.ViewModels;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DBScriptSaver.Core
{
    internal class MSSQLIndexData: IIndexData
    {
        public string Name;
        public bool isPrimaryKey;
        public bool isUniqueConstraint;
        public bool isUnique;
        public string typeDesc;
        public bool isPadded;
        public bool noRecompute;
        public bool ignoreDupKey;
        public bool allowRowLocks;
        public bool allowPageLocks;
        public string IndexFolder;
        public Database dataBase;
        public MSSQLTableData table;

        public List<MSSQLIndexColumnData> Columns = new List<MSSQLIndexColumnData>();

        public IScript GetScript()
        {
            string fileName = $@"{table.Schema}.{table.Name}.{Name}.sql";
            string indexFileName = IndexFolder + fileName;
            string script = MakeScript(table.FullName);

            if (File.Exists(indexFileName) && script == File.ReadAllText(indexFileName))
            {
                return null;
            }

            ChangeType ChangeType = !File.Exists(indexFileName) ? ChangeType.Новый : ChangeType.Изменённый;

            var dbInd = dataBase
                        .Tables.Cast<Table>().Single(t => t.Schema == table.Schema && t.Name == table.Name)
                        .Indexes.Cast<Index>().Single(i => i.Name == Name);

            return new MSSQLScript()
            {
                FileName = fileName,
                FullPath = IndexFolder + fileName,
                ScriptText = script,
                ObjectType = @"Индекс",
                ChangeState = ChangeType,
                urn = dbInd.Urn,
                ObjName = Name
            };
        }

        public override string ToString()
        {
            return Name;
        }

        internal string MakeScript(string tableName)
        {
            string script = string.Empty;

            if (isUniqueConstraint)
            {
                script += $@"ALTER TABLE {tableName} ADD  CONSTRAINT [{Name}] {(isUnique ? "UNIQUE " : "")} {typeDesc}";
            }
            else
            {
                script += $@"CREATE {typeDesc} INDEX [{Name}] ON {tableName}";
            }

            script += Environment.NewLine;
            script += "(" + Environment.NewLine;

            string columnsDef = "";
            foreach (var column in Columns.Where(c => !c.IsIncluded).OrderBy(c => c.Order))
            {
                if (columnsDef.Length > 0)
                {
                    columnsDef += "," + Environment.NewLine;
                }
                columnsDef += column.Definition;
            }

            script += columnsDef + Environment.NewLine;
            script += ")" + Environment.NewLine;

            string include = "";
            foreach (var column in Columns.Where(c => c.IsIncluded).OrderBy(c => c.Name))
            {
                if (include != "")
                {
                    include += ", ";
                }
                include += $@"[{column.Name}]";
            }

            if (include.Length > 0)
            {
                script += $@"INCLUDE({include})" + Environment.NewLine;
            }

            script += $@"WITH (PAD_INDEX = {(isPadded ? "ON" : "OFF")}, STATISTICS_NORECOMPUTE = {(noRecompute ? "ON" : "OFF")}, SORT_IN_TEMPDB = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = {(allowRowLocks ? "ON" : "OFF")}, ALLOW_PAGE_LOCKS = {(allowPageLocks ? "ON" : "OFF")}) ON [PRIMARY]";

            return script;
        }
    }
}