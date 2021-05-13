using System;
using System.Collections.Generic;

namespace DBScriptSaver.Logic
{
    public class TableData
    {
        public int id;
        public string Schema;
        public string Name;
        public bool UsesAnsiNulls;

        public string FullName => $@"[{Schema}].[{Name}]";

        public string MakeScript(List<ColumnData> columnDatas)
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

            foreach (var column in columnDatas)
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