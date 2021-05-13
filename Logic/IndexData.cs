using System;
using System.Collections.Generic;
using System.Linq;

namespace DBScriptSaver.Logic
{
    internal class IndexData
    {
        public int TableId;
        public int IndexId;
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

        public override string ToString()
        {
            return Name;
        }

        internal string MakeScript(string tableName, List<IndexColumnData> columns)
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
            foreach (var column in columns.Where(c => !c.IsIncluded).OrderBy(c => c.Order))
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
            foreach (var column in columns.Where(c => c.IsIncluded).OrderBy(c => c.Name))
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